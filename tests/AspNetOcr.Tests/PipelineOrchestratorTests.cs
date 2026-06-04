using System.Text;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Application.Options;
using AspNetOcr.Application.Parsing;
using AspNetOcr.Application.Services;
using AspNetOcr.Application.Validation;
using AspNetOcr.Domain.Documents;
using AspNetOcr.Domain.Ocr;
using AspNetOcr.Domain.Products;
using AspNetOcr.Infrastructure.Excel;
using Xunit;

namespace AspNetOcr.Tests;

public sealed class PipelineOrchestratorTests
{
    [Fact]
    public async Task ProcessAsync_PreservesArtifactsAndExportsWorkbook()
    {
        var harness = TestHarness.Create(SampleOcrText());

        var result = await harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(DocumentStatus.Exported, result.Status);
        Assert.NotNull(result.ManifestPath);
        Assert.NotNull(result.ExportPath);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.ExportPath));
        Assert.Contains(harness.ArtifactStore.Writes, path => path.EndsWith("ocr/raw.txt", StringComparison.Ordinal));
        Assert.Contains(harness.ArtifactStore.Writes, path => path.EndsWith("validation/report.json", StringComparison.Ordinal));
        Assert.Equal(1, harness.ExcelService.ExportCalls);
        Assert.Equal(1, result.ProductRowCount);
    }

    [Fact]
    public async Task ProcessAsync_RawOcrArtifactPreservesExactEngineText()
    {
        var rawOcrText = SampleOcrText();
        var harness = TestHarness.Create(rawOcrText);

        var result = await harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None);
        var document = await harness.Repository.FindByIdAsync(result.DocumentId, CancellationToken.None);

        Assert.NotNull(document?.RawOcrArtifactPath);
        Assert.Equal(rawOcrText, await File.ReadAllTextAsync(document.RawOcrArtifactPath, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_EmptyOcrFailsValidationAndDeadLetters()
    {
        var harness = TestHarness.Create(rawOcrText: "");

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None));

        Assert.Equal(DocumentStatus.DeadLettered, exception.Result.Status);
        Assert.Contains(exception.Result.Errors, error => error.Contains("No product rows extracted.", StringComparison.Ordinal));
        Assert.Equal(0, harness.ExcelService.ExportCalls);
        Assert.Contains(harness.ArtifactStore.Writes, path => path.EndsWith("ocr/raw.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessAsync_DuplicateSkuFailsValidationBeforeExport()
    {
        var harness = TestHarness.Create(DuplicateSkuOcrText());

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None));

        Assert.Equal(DocumentStatus.DeadLettered, exception.Result.Status);
        Assert.Contains(exception.Result.Errors, error => error.Contains("Duplicate SKU rows detected", StringComparison.Ordinal));
        Assert.Equal(0, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ProcessAsync_LowConfidenceFailsValidationBeforeExport()
    {
        var harness = TestHarness.Create(SampleOcrText(), confidence: 0.42m);

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None));

        Assert.Equal(DocumentStatus.DeadLettered, exception.Result.Status);
        Assert.Contains(exception.Result.Errors, error => error.Contains("below minimum", StringComparison.Ordinal));
        Assert.Equal(0, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ProcessAsync_ReplaySameContentDoesNotExportDuplicateRows()
    {
        var harness = TestHarness.Create(SampleOcrText());
        var first = await harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None);
        var second = await harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(second.IsReplay);
        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Equal(1, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ProcessAsync_CorrelationIdPropagatesToDocumentTelemetryAndManifest()
    {
        var harness = TestHarness.Create(SampleOcrText());
        const string correlationId = "review-hardening-correlation";

        var result = await harness.Orchestrator.ProcessAsync(
            CreateUpload("sheet.png", "image/png", correlationId),
            CancellationToken.None);
        var document = await harness.Repository.FindByIdAsync(result.DocumentId, CancellationToken.None);
        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath!, CancellationToken.None);

        Assert.Equal(correlationId, result.CorrelationId);
        Assert.Equal(correlationId, document?.CorrelationId);
        Assert.All(harness.Telemetry.Events, stageEvent => Assert.Equal(correlationId, stageEvent.CorrelationId));
        Assert.Contains($"\"correlationId\":\"{correlationId}\"", manifestJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_SuccessStateMachineReachesExported()
    {
        var harness = TestHarness.Create(SampleOcrText());

        var result = await harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None);

        Assert.Equal(DocumentStatus.Exported, result.Status);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Ingested);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Preprocessed);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Recognizing);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Validating);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Validated);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Exporting);
        Assert.Contains(harness.Telemetry.Events, stageEvent => stageEvent.Status == DocumentStatus.Exported);
    }

    [Fact]
    public async Task ProcessAsync_FailureStateMachinePersistsDeadLettered()
    {
        var harness = TestHarness.Create(SampleOcrText(), failOcr: true);

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None));
        var document = await harness.Repository.FindByIdAsync(exception.Result.DocumentId, CancellationToken.None);

        Assert.Equal(DocumentStatus.DeadLettered, document?.Status);
        Assert.Null(document?.ExportArtifactPath);
        Assert.NotNull(document?.ManifestArtifactPath);
    }

    [Fact]
    public async Task ProcessAsync_InvalidFileTypeIsRejectedBeforeArtifacts()
    {
        var harness = TestHarness.Create(SampleOcrText());

        await Assert.ThrowsAsync<DocumentRejectedException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("malware.exe", "application/octet-stream"), CancellationToken.None));

        Assert.Empty(harness.ArtifactStore.Writes);
        Assert.Equal(0, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ProcessAsync_EmptyUploadIsRejectedBeforeArtifacts()
    {
        var harness = TestHarness.Create(SampleOcrText());
        var upload = new DocumentUploadRequest("empty.png", "image/png", new MemoryStream([]), "test-correlation", 0);

        await Assert.ThrowsAsync<DocumentRejectedException>(() =>
            harness.Orchestrator.ProcessAsync(upload, CancellationToken.None));

        Assert.Empty(harness.ArtifactStore.Writes);
        Assert.Equal(0, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ProcessAsync_OcrFailureDeadLettersWithManifest()
    {
        var harness = TestHarness.Create(SampleOcrText(), failOcr: true);

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            harness.Orchestrator.ProcessAsync(CreateUpload("sheet.png", "image/png"), CancellationToken.None));

        Assert.Equal(DocumentStatus.DeadLettered, exception.Result.Status);
        Assert.False(exception.Result.Succeeded);
        Assert.NotNull(exception.Result.ManifestPath);
        Assert.True(File.Exists(exception.Result.ManifestPath));
        Assert.Equal(0, harness.ExcelService.ExportCalls);
    }

    [Fact]
    public async Task ClosedXmlExport_ThrowsWhenManifestIsMissing()
    {
        var exportPath = Path.Combine(Path.GetTempPath(), "aspnetocr-tests", Guid.NewGuid().ToString("n"), "products.xlsx");
        var manifest = new DocumentManifest(
            Guid.NewGuid(),
            "test-correlation",
            "sheet.png",
            "hash",
            DocumentStatus.Exporting,
            OriginalArtifactPath: "original",
            RawOcrArtifactPath: "raw",
            ValidationArtifactPath: "validation",
            ManifestPath: null,
            ExportPath: exportPath,
            ProductRowCount: 1,
            MeanConfidence: 0.96m,
            IsReplay: false,
            Events: [],
            Errors: []);

        var service = new ClosedXmlExcelService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportAsync([new ProductSheet("A-100", "Widget Alpha", "Hardware", 12, 4.50m, ConfidenceScore.FromRatio(0.96m))], manifest, exportPath, CancellationToken.None));
        Assert.False(File.Exists(exportPath));
    }

    private static DocumentUploadRequest CreateUpload(string fileName, string contentType, string correlationId = "test-correlation")
    {
        var bytes = Encoding.UTF8.GetBytes("fake image bytes");
        return new DocumentUploadRequest(fileName, contentType, new MemoryStream(bytes), correlationId, bytes.Length);
    }

    private static string SampleOcrText()
    {
        return """
            SKU: A-100
            Name: Widget Alpha
            Category: Hardware
            Quantity: 12
            UnitPrice: 4.50
            Confidence: 0.96
            """;
    }

    private static string DuplicateSkuOcrText()
    {
        return """
            SKU: A-100
            Name: Widget Alpha
            Category: Hardware
            Quantity: 12
            UnitPrice: 4.50
            Confidence: 0.96
            ---
            SKU: A-100
            Name: Widget Alpha Duplicate
            Category: Hardware
            Quantity: 5
            UnitPrice: 4.50
            Confidence: 0.95
            """;
    }

    private sealed class TestHarness
    {
        private TestHarness(
            PipelineOrchestrator orchestrator,
            TestArtifactStore artifactStore,
            InMemoryDocumentRepository repository,
            RecordingExcelService excelService,
            CapturingTelemetrySink telemetry)
        {
            Orchestrator = orchestrator;
            ArtifactStore = artifactStore;
            Repository = repository;
            ExcelService = excelService;
            Telemetry = telemetry;
        }

        public PipelineOrchestrator Orchestrator { get; }

        public TestArtifactStore ArtifactStore { get; }

        public InMemoryDocumentRepository Repository { get; }

        public RecordingExcelService ExcelService { get; }

        public CapturingTelemetrySink Telemetry { get; }

        public static TestHarness Create(string rawOcrText, bool failOcr = false, decimal confidence = 0.96m)
        {
            var artifactStore = new TestArtifactStore();
            var repository = new InMemoryDocumentRepository();
            var excelService = new RecordingExcelService();
            var telemetry = new CapturingTelemetrySink();
            var orchestrator = new PipelineOrchestrator(
                new DocumentUploadValidator(),
                new FakeOcrService(rawOcrText, failOcr, confidence),
                excelService,
                artifactStore,
                repository,
                telemetry,
                new ProductSheetParser(),
                new PipelineOptions(),
                TimeProvider.System);

            return new TestHarness(orchestrator, artifactStore, repository, excelService, telemetry);
        }
    }

    private sealed class TestArtifactStore : IArtifactStore
    {
        private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

        private readonly string _root = Path.Combine(Path.GetTempPath(), "aspnetocr-tests", Guid.NewGuid().ToString("n"));

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
            await File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(content, SerializerOptions), cancellationToken);
            Writes.Add(path);
            return path;
        }
    }

    private sealed class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, DocumentRecord> _byId = [];
        private readonly Dictionary<string, DocumentRecord> _byHash = new(StringComparer.OrdinalIgnoreCase);

        public Task<DocumentRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _byId.TryGetValue(id, out var document);
            return Task.FromResult(document);
        }

        public Task<DocumentRecord?> FindByContentHashAsync(string contentHash, CancellationToken cancellationToken)
        {
            _byHash.TryGetValue(contentHash, out var document);
            return Task.FromResult(document);
        }

        public Task SaveAsync(DocumentRecord document, CancellationToken cancellationToken)
        {
            _byId[document.Id] = document;
            _byHash[document.ContentHash] = document;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOcrService : IOcrService
    {
        private readonly string _rawOcrText;
        private readonly bool _fail;
        private readonly decimal _confidence;

        public FakeOcrService(string rawOcrText, bool fail, decimal confidence)
        {
            _rawOcrText = rawOcrText;
            _fail = fail;
            _confidence = confidence;
        }

        public Task<OcrResult> RecognizeAsync(OcrInput input, CancellationToken cancellationToken)
        {
            if (_fail)
            {
                throw new InvalidDataException("OCR engine rejected corrupt input.");
            }

            return Task.FromResult(new OcrResult(_rawOcrText, ConfidenceScore.FromRatio(_confidence), "fake", input.FileName));
        }

        public Task<OcrHealth> CheckHealthAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new OcrHealth(true, "fake", "available"));
        }
    }

    private sealed class RecordingExcelService : IExcelService
    {
        public int ExportCalls { get; private set; }

        public async Task<string> ExportAsync(
            IReadOnlyList<ProductSheet> products,
            DocumentManifest manifest,
            string exportPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(manifest.ManifestPath))
            {
                throw new InvalidOperationException("Manifest path required before export.");
            }

            ExportCalls++;
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath) ?? ".");
            await File.WriteAllTextAsync(exportPath, $"rows={products.Count}", cancellationToken);
            return exportPath;
        }
    }

    private sealed class CapturingTelemetrySink : ITelemetrySink
    {
        public List<PipelineStageEvent> Events { get; } = [];

        public Task RecordAsync(PipelineStageEvent stageEvent, CancellationToken cancellationToken)
        {
            Events.Add(stageEvent);
            return Task.CompletedTask;
        }
    }
}
