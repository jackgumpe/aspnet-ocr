using System.Threading.RateLimiting;
using System.Text.Json;
using AspNetOcr.Api.Phase2;
using AspNetOcr.Application;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Application.Services;
using AspNetOcr.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .WriteTo.File(new CompactJsonFormatter(), Path.Combine("logs", "aspnetocr-.jsonl"), rollingInterval: RollingInterval.Day);
});

builder.Services.AddAspNetOcrApplication();
builder.Services.AddAspNetOcrInfrastructure(builder.Configuration);
builder.Services.AddSingleton<Phase2OcrJobStore>();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("upload", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var existing)
        ? existing.ToString()
        : Guid.NewGuid().ToString("n");

    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (LogContext.PushProperty("correlation_id", correlationId))
    {
        await next();
    }
});

app.MapPost("/documents", async (
        HttpRequest request,
        PipelineOrchestrator orchestrator,
        CancellationToken cancellationToken) =>
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Expected multipart form data with file field 'file'." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        if (file is null)
        {
            return Results.BadRequest(new { error = "Missing file field 'file'." });
        }

        var correlationId = request.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
        await using var stream = file.OpenReadStream();
        var upload = new DocumentUploadRequest(file.FileName, file.ContentType, stream, correlationId, file.Length);

        try
        {
            var result = await orchestrator.ProcessAsync(upload, cancellationToken);
            return Results.Accepted($"/documents/{result.DocumentId}", result);
        }
        catch (DocumentRejectedException exception)
        {
            return Results.BadRequest(new { error = exception.Message, correlation_id = correlationId });
        }
        catch (DocumentProcessingException exception)
        {
            return Results.UnprocessableEntity(exception.Result);
        }
    })
    .RequireRateLimiting("upload");

app.MapPost("/api/ocr/jobs", async (
        HttpRequest request,
        Phase2OcrJobStore jobStore,
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Expected multipart form data with file field 'file'." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        if (file is null)
        {
            return Results.BadRequest(new { error = "Missing file field 'file'." });
        }

        if (!IsSupportedPhase2Upload(file.FileName, file.ContentType))
        {
            return Results.BadRequest(new
            {
                error = "Only PDF and image uploads are supported for ASP-OCR-003."
            });
        }

        await using var upload = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await upload.CopyToAsync(buffer, cancellationToken);

        var now = clock.GetUtcNow();
        var correlationId = request.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
        var job = jobStore.Create(file.FileName, now);
        var payload = buffer.ToArray();
        var logger = loggerFactory.CreateLogger("Phase2OcrJobs");

        _ = Task.Run(
            () => ProcessPhase2JobAsync(
                job.Id,
                file.FileName,
                file.ContentType,
                payload,
                correlationId,
                jobStore,
                scopeFactory,
                clock,
                logger,
                CancellationToken.None),
            CancellationToken.None);

        return Results.Accepted($"/api/ocr/jobs/{job.Id}", job);
    })
    .RequireRateLimiting("upload");

app.MapGet("/api/ocr/jobs", (Phase2OcrJobStore jobStore) =>
{
    return Results.Ok(jobStore.List());
});

app.MapGet("/api/ocr/jobs/{id:guid}", (Guid id, Phase2OcrJobStore jobStore) =>
{
    var job = jobStore.Find(id);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

app.MapGet("/documents/{id:guid}", async (
        Guid id,
        IDocumentRepository repository,
        CancellationToken cancellationToken) =>
    {
        var document = await repository.FindByIdAsync(id, cancellationToken);
        return document is null ? Results.NotFound() : Results.Ok(document);
    });

app.MapGet("/documents/{id:guid}/export", async (
        Guid id,
        IDocumentRepository repository,
        CancellationToken cancellationToken) =>
    {
        var document = await repository.FindByIdAsync(id, cancellationToken);
        if (document?.ExportArtifactPath is null || !File.Exists(document.ExportArtifactPath))
        {
            return Results.NotFound();
        }

        return Results.File(
            document.ExportArtifactPath,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Path.GetFileName(document.ExportArtifactPath));
    });

app.MapGet("/health/ocr", async (IOcrService ocrService, CancellationToken cancellationToken) =>
{
    var health = await ocrService.CheckHealthAsync(cancellationToken);
    return health.Available ? Results.Ok(health) : Results.Problem(health.Detail, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/health/queue", () => Results.Ok(new
{
    status = "not_implemented",
    phase = "phase_2_interface_hook_only"
}));

app.MapHealthChecks("/health");

app.Run();

static bool IsSupportedPhase2Upload(string fileName, string contentType)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    return extension is ".pdf" or ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" ||
           contentType is "application/pdf" or "image/png" or "image/jpeg" or "image/tiff";
}

static async Task ProcessPhase2JobAsync(
    Guid jobId,
    string fileName,
    string contentType,
    byte[] content,
    string correlationId,
    Phase2OcrJobStore jobStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    try
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        jobStore.MarkProcessing(jobId, pagesProcessed: 0, totalPages: 1, clock.GetUtcNow());

        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ProviderOcrProcessor>();
        await using var stream = new MemoryStream(content);
        var upload = new DocumentUploadRequest(fileName, contentType, stream, correlationId, content.LongLength);
        var result = await processor.ProcessAsync(upload, cancellationToken);

        jobStore.MarkProcessing(jobId, pagesProcessed: result.PageCount, totalPages: result.PageCount, clock.GetUtcNow());
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        var rawJson = await File.ReadAllTextAsync(result.NormalizedOcrArtifactPath, cancellationToken);
        var normalized = JsonSerializer.Deserialize<NormalizedOcrResult>(
            rawJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (normalized is null)
        {
            throw new InvalidOperationException("Normalized OCR artifact could not be read.");
        }

        jobStore.MarkComplete(jobId, normalized, rawJson, clock.GetUtcNow());
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "ASP-OCR-003 mock OCR job {JobId} failed", jobId);
        jobStore.MarkFailed(jobId, exception.Message, clock.GetUtcNow());
    }
}

public partial class Program;
