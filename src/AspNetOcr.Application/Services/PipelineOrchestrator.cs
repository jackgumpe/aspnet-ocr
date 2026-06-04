using System.Security.Cryptography;
using System.Text;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Application.Options;
using AspNetOcr.Application.Parsing;
using AspNetOcr.Domain.Documents;
using AspNetOcr.Domain.Ocr;
using AspNetOcr.Domain.Products;
using FluentValidation;

namespace AspNetOcr.Application.Services;

public sealed class PipelineOrchestrator
{
    private readonly IValidator<DocumentUploadRequest> _uploadValidator;
    private readonly IOcrService _ocrService;
    private readonly IExcelService _excelService;
    private readonly IArtifactStore _artifactStore;
    private readonly IDocumentRepository _documentRepository;
    private readonly ITelemetrySink _telemetrySink;
    private readonly ProductSheetParser _productSheetParser;
    private readonly PipelineOptions _options;
    private readonly TimeProvider _clock;
    private readonly List<PipelineStageEvent> _events = [];

    public PipelineOrchestrator(
        IValidator<DocumentUploadRequest> uploadValidator,
        IOcrService ocrService,
        IExcelService excelService,
        IArtifactStore artifactStore,
        IDocumentRepository documentRepository,
        ITelemetrySink telemetrySink,
        ProductSheetParser productSheetParser,
        PipelineOptions options,
        TimeProvider clock)
    {
        _uploadValidator = uploadValidator;
        _ocrService = ocrService;
        _excelService = excelService;
        _artifactStore = artifactStore;
        _documentRepository = documentRepository;
        _telemetrySink = telemetrySink;
        _productSheetParser = productSheetParser;
        _options = options;
        _clock = clock;
    }

    public async Task<DocumentProcessResult> ProcessAsync(DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        _events.Clear();
        var validation = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DocumentRejectedException(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        var bytes = await ReadAllBytesAsync(request.Content, cancellationToken);
        if (bytes.Length == 0 || bytes.LongLength > _options.MaxUploadBytes)
        {
            throw new DocumentRejectedException("Uploaded file is empty or exceeds the Phase 1 size limit.");
        }

        var contentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var existing = await _documentRepository.FindByContentHashAsync(contentHash, cancellationToken);
        if (existing is { Status: DocumentStatus.Exported, ManifestArtifactPath: not null, ExportArtifactPath: not null })
        {
            return new DocumentProcessResult(
                existing.Id,
                existing.CorrelationId,
                existing.Status,
                Succeeded: true,
                IsReplay: true,
                existing.ManifestArtifactPath,
                existing.ExportArtifactPath,
                ProductRowCount: 0,
                Errors: []);
        }

        var now = _clock.GetUtcNow();
        var document = existing ?? DocumentRecord.Create(request.CorrelationId, request.FileName, contentHash, now);

        try
        {
            var originalPath = await _artifactStore.SaveBytesAsync(document.Id, $"original/{request.FileName}", bytes, cancellationToken);
            document.SetArtifacts(originalArtifactPath: originalPath);
            await TransitionAsync(document, DocumentStatus.Ingested, "ingest", "Original artifact preserved.", cancellationToken);

            await _artifactStore.SaveJsonAsync(document.Id, "preprocess.json", new
            {
                request.FileName,
                request.ContentType,
                ByteCount = bytes.Length,
                contentHash
            }, cancellationToken);
            await TransitionAsync(document, DocumentStatus.Preprocessed, "preprocess", "Preprocess metadata preserved.", cancellationToken);

            await TransitionAsync(document, DocumentStatus.Recognizing, "recognize", "OCR started.", cancellationToken);
            var ocrResult = await _ocrService.RecognizeAsync(
                new OcrInput(request.FileName, request.ContentType, bytes, request.CorrelationId),
                cancellationToken);
            var rawOcrPath = await _artifactStore.SaveTextAsync(document.Id, "ocr/raw.txt", ocrResult.RawText, cancellationToken);
            document.SetArtifacts(rawOcrArtifactPath: rawOcrPath);

            await TransitionAsync(document, DocumentStatus.Validating, "validate", "Validation started.", cancellationToken);
            var products = _productSheetParser.Parse(ocrResult.RawText);
            var report = ValidateProducts(products, ocrResult.MeanConfidence);
            var validationPath = await _artifactStore.SaveJsonAsync(document.Id, "validation/report.json", report, cancellationToken);
            document.SetArtifacts(validationArtifactPath: validationPath);
            if (!report.IsValid)
            {
                throw new DocumentValidationException(report.Errors);
            }

            await TransitionAsync(document, DocumentStatus.Validated, "validate", "Validation completed.", cancellationToken);

            var manifestPath = _artifactStore.GetPath(document.Id, "manifest.json");
            var manifest = BuildManifest(document, DocumentStatus.Validated, products, report, manifestPath, exportPath: null, isReplay: false, errors: []);
            await _artifactStore.SaveJsonAsync(document.Id, "manifest.json", manifest, cancellationToken);
            document.SetArtifacts(manifestArtifactPath: manifestPath);

            await TransitionAsync(document, DocumentStatus.Exporting, "export", "Excel export started.", cancellationToken);
            var exportPath = _artifactStore.GetPath(document.Id, "export/products.xlsx");
            manifest = manifest with { Status = DocumentStatus.Exporting, ExportPath = exportPath };
            await _excelService.ExportAsync(products, manifest, exportPath, cancellationToken);
            document.SetArtifacts(exportArtifactPath: exportPath);

            await TransitionAsync(document, DocumentStatus.Exported, "export", "Excel export completed.", cancellationToken);
            manifest = BuildManifest(document, DocumentStatus.Exported, products, report, manifestPath, exportPath, isReplay: false, errors: []);
            await _artifactStore.SaveJsonAsync(document.Id, "manifest.json", manifest, cancellationToken);
            await _documentRepository.SaveAsync(document, cancellationToken);

            return new DocumentProcessResult(
                document.Id,
                document.CorrelationId,
                document.Status,
                Succeeded: true,
                IsReplay: false,
                document.ManifestArtifactPath,
                document.ExportArtifactPath,
                products.Count,
                Errors: []);
        }
        catch (Exception exception) when (exception is not DocumentRejectedException)
        {
            var errors = exception is DocumentValidationException documentValidation
                ? documentValidation.Errors
                : [exception.Message];

            document.DeadLetter(string.Join("; ", errors), _clock.GetUtcNow());
            var deadLetterManifestPath = _artifactStore.GetPath(document.Id, "manifest.json");
            var manifest = BuildManifest(
                document,
                DocumentStatus.DeadLettered,
                products: [],
                report: new ValidationReport(false, 0, 0m, errors),
                manifestPath: deadLetterManifestPath,
                exportPath: null,
                isReplay: false,
                errors);

            await _artifactStore.SaveJsonAsync(document.Id, "manifest.json", manifest, cancellationToken);
            document.SetArtifacts(manifestArtifactPath: deadLetterManifestPath);
            await _documentRepository.SaveAsync(document, cancellationToken);

            var result = new DocumentProcessResult(
                document.Id,
                document.CorrelationId,
                document.Status,
                Succeeded: false,
                IsReplay: false,
                document.ManifestArtifactPath,
                ExportPath: null,
                ProductRowCount: 0,
                errors);

            throw new DocumentProcessingException("Document processing failed and was dead-lettered.", result, exception);
        }
    }

    private async Task TransitionAsync(
        DocumentRecord document,
        DocumentStatus status,
        string stage,
        string message,
        CancellationToken cancellationToken)
    {
        document.Mark(status, _clock.GetUtcNow());
        await _documentRepository.SaveAsync(document, cancellationToken);

        var stageEvent = new PipelineStageEvent(
            document.Id,
            document.CorrelationId,
            status,
            stage,
            message,
            _clock.GetUtcNow());
        _events.Add(stageEvent);
        await _telemetrySink.RecordAsync(stageEvent, cancellationToken);
    }

    private ValidationReport ValidateProducts(IReadOnlyList<ProductSheet> products, ConfidenceScore meanConfidence)
    {
        var errors = new List<string>();
        if (products.Count == 0)
        {
            errors.Add("No product rows extracted.");
        }

        var duplicateSkus = products
            .GroupBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateSkus.Length > 0)
        {
            errors.Add($"Duplicate SKU rows detected: {string.Join(", ", duplicateSkus)}.");
        }

        if (meanConfidence.Value < _options.MinimumMeanConfidence)
        {
            errors.Add($"Mean OCR confidence {meanConfidence.Value:0.0000} is below minimum {_options.MinimumMeanConfidence:0.0000}.");
        }

        return new ValidationReport(errors.Count == 0, products.Count, meanConfidence.Value, errors);
    }

    private DocumentManifest BuildManifest(
        DocumentRecord document,
        DocumentStatus status,
        IReadOnlyList<ProductSheet> products,
        ValidationReport report,
        string? manifestPath,
        string? exportPath,
        bool isReplay,
        IReadOnlyList<string> errors)
    {
        return new DocumentManifest(
            document.Id,
            document.CorrelationId,
            document.FileName,
            document.ContentHash,
            status,
            document.OriginalArtifactPath,
            document.RawOcrArtifactPath,
            document.ValidationArtifactPath,
            manifestPath,
            exportPath,
            products.Count,
            report.MeanConfidence,
            isReplay,
            _events.ToArray(),
            errors);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
