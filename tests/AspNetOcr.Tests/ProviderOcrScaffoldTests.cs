using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Application.Options;
using AspNetOcr.Application.Services;
using AspNetOcr.Application.Validation;
using AspNetOcr.Domain.Ocr;
using AspNetOcr.Infrastructure.Ocr;
using Xunit;

namespace AspNetOcr.Tests;

public sealed class ProviderOcrScaffoldTests
{
    [Fact]
    public async Task ProviderOcrProcessor_WritesNormalizedOcrJsonFromMockProvider()
    {
        var artifactStore = new TestArtifactStore();
        var processor = new ProviderOcrProcessor(
            new DocumentUploadValidator(),
            new MockOcrProvider(),
            new MockPdfPageImagePipeline(),
            artifactStore,
            new PipelineOptions(),
            TimeProvider.System);
        var bytes = Encoding.UTF8.GetBytes("synthetic legal pdf bytes");
        var upload = new DocumentUploadRequest(
            "synthetic-complaint.pdf",
            "application/pdf",
            new MemoryStream(bytes),
            "asp-ocr-002a-test",
            bytes.Length);

        var result = await processor.ProcessAsync(upload, CancellationToken.None);
        var json = await File.ReadAllTextAsync(result.NormalizedOcrArtifactPath, CancellationToken.None);
        var normalized = JsonSerializer.Deserialize<NormalizedOcrResult>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(normalized);
        Assert.Equal("mock-ocr", normalized.ProviderId);
        Assert.Equal(2, normalized.PageCount);
        Assert.True(normalized.MeanConfidence >= 0.90m);
        Assert.Equal(0m, normalized.CharacterErrorRate);
        Assert.Equal(0m, normalized.WordErrorRate);
        Assert.Contains(normalized.Fields, field => field is { Name: "caseNumber", Value: "2026-CV-1001" });
        Assert.Contains(normalized.Fields, field => field is { Name: "documentTitle", Required: true });
        Assert.Equal("CPU-bound", normalized.Telemetry.StageClassification);
        Assert.Equal(0, normalized.Telemetry.CostPerPageUsdEstimate);
        Assert.Null(normalized.Telemetry.GpuPercent);
        Assert.Contains(artifactStore.Writes, path => path.EndsWith("ocr/normalized.json", StringComparison.Ordinal));
    }

    [Fact]
    public void TesseractLocalProvider_IsWorkstationDeferredStub()
    {
        var provider = new TesseractLocalProvider();

        Assert.Equal("tesseract-local", provider.Descriptor.ProviderId);
        Assert.Equal("workstation_deferred", provider.Descriptor.Status);
        Assert.Equal("cpu_baseline_deferred", provider.Descriptor.AccelerationStatus);
    }

    [Fact]
    public void EndpointDiscoveryReport_DoesNotAssumeDeepSeekOcrEndpoint()
    {
        var root = FindRepositoryRoot();
        var reportPath = Path.Combine(root, "evidence", "asp-ocr-002a", "endpoint_discovery_report.json");
        var report = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(reportPath));

        Assert.Equal("blocked_unproven", report.GetProperty("deepseek_ocr_endpoint").GetProperty("status").GetString());
        Assert.False(report.GetProperty("deepseek_ocr_endpoint").GetProperty("cloud_provider_implemented").GetBoolean());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AspNetOcr.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find AspNetOcr repository root.");
    }

    private sealed class MockPdfPageImagePipeline : IPdfPageImagePipeline
    {
        public Task<IReadOnlyList<OcrPageImage>> RenderAsync(
            string fileName,
            string contentType,
            byte[] content,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<OcrPageImage> pages =
            [
                BuildPage(1, "synthetic-page-image-1"),
                BuildPage(2, "synthetic-page-image-2")
            ];
            return Task.FromResult(pages);
        }

        private static OcrPageImage BuildPage(int pageNumber, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new OcrPageImage(
                pageNumber,
                "image/png",
                bytes,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                "mock-pdf-page-image");
        }
    }

    private sealed class MockOcrProvider : IOcrProvider
    {
        public OcrProviderDescriptor Descriptor { get; } = new(
            "mock-ocr",
            "test",
            "available",
            "not_applicable",
            "Deterministic test provider for ASP-OCR-002A scaffold verification.");

        public Task<NormalizedOcrResult> RecognizeAsync(OcrProviderRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pages = request.Pages
                .Select(page => new NormalizedOcrPage(
                    page.PageNumber,
                    page.PageNumber == 1
                        ? "IN THE SUPERIOR COURT OF TEST COUNTY\nCOMPLAINT\nCase No: 2026-CV-1001\nAcme Legal Holdings, Plaintiff"
                        : "Example Systems LLC, Defendant\nAttorney: Jane Counsel",
                    page.PageNumber == 1 ? 0.94m : 0.91m,
                    page.ContentHash,
                    page.RenderMode))
                .ToArray();
            IReadOnlyList<OcrFieldResult> fields =
            [
                new("court", "IN THE SUPERIOR COURT OF TEST COUNTY", ConfidenceScore.FromRatio(0.94m), 1, "mock-rule", true),
                new("documentTitle", "COMPLAINT", ConfidenceScore.FromRatio(0.94m), 1, "mock-rule", true),
                new("caseNumber", "2026-CV-1001", ConfidenceScore.FromRatio(0.94m), 1, "mock-rule", true),
                new("plaintiff", "Acme Legal Holdings", ConfidenceScore.FromRatio(0.92m), 1, "mock-rule", true),
                new("defendant", "Example Systems LLC", ConfidenceScore.FromRatio(0.91m), 2, "mock-rule", true)
            ];
            var telemetry = new OcrProviderTelemetry(
                Descriptor.ProviderId,
                "CPU-bound",
                DurationMs: 12,
                CostPerPageUsdEstimate: 0m,
                WattHoursEstimate: null,
                CpuPercent: null,
                RamMegabytes: null,
                GpuPercent: null,
                Notes: "Mock provider has no real resource utilization.");

            return Task.FromResult(new NormalizedOcrResult(
                request.DocumentId,
                request.CorrelationId,
                request.SourceFileName,
                Descriptor.ProviderId,
                pages.Length,
                0.925m,
                0m,
                0m,
                pages,
                fields,
                telemetry,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class TestArtifactStore : IArtifactStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        private readonly string _root = Path.Combine(Path.GetTempPath(), "aspnetocr-provider-tests", Guid.NewGuid().ToString("n"));

        public List<string> Writes { get; } = [];

        public string GetPath(Guid documentId, string relativeName)
        {
            var path = Path.Combine(_root, documentId.ToString("n"), relativeName);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _root);
            return path;
        }

        public async Task<string> SaveBytesAsync(Guid documentId, string relativeName, byte[] content, CancellationToken cancellationToken)
        {
            var path = GetPath(documentId, relativeName);
            await File.WriteAllBytesAsync(path, content, cancellationToken);
            Writes.Add(path);
            return path;
        }

        public async Task<string> SaveTextAsync(Guid documentId, string relativeName, string content, CancellationToken cancellationToken)
        {
            var path = GetPath(documentId, relativeName);
            await File.WriteAllTextAsync(path, content, cancellationToken);
            Writes.Add(path);
            return path;
        }

        public async Task<string> SaveJsonAsync<T>(Guid documentId, string relativeName, T content, CancellationToken cancellationToken)
        {
            var path = GetPath(documentId, relativeName);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(content, SerializerOptions), cancellationToken);
            Writes.Add(path);
            return path;
        }
    }
}
