using System.Threading.RateLimiting;
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

public partial class Program;
