using AspNetOcr.Application.Benchmarking;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Infrastructure.Ocr;

public sealed class MockOcrProvider : IOcrProvider
{
    public OcrProviderDescriptor Descriptor { get; } = new(
        "mock-ocr",
        "deterministic_harness",
        "available",
        "harness_only",
        "ASP-OCR-003 UI and API integration provider; not a benchmark provider.");

    public Task<NormalizedOcrResult> RecognizeAsync(OcrProviderRequest request, CancellationToken cancellationToken)
    {
        var text = """
            SKU: A-100
            Name: Widget Alpha
            Category: Hardware
            Quantity: 12
            UnitPrice: 4.50
            Confidence: 0.96
            """;
        var accuracy = OcrAccuracyCalculator.Calculate(text, text);
        var fields = new List<OcrFieldResult>
        {
            new("sku", "A-100", ConfidenceScore.FromRatio(0.98m), 1, "mock-field-map", Required: true),
            new("name", "Widget Alpha", ConfidenceScore.FromRatio(0.97m), 1, "mock-field-map", Required: true),
            new("category", "Hardware", ConfidenceScore.FromRatio(0.96m), 1, "mock-field-map", Required: true),
            new("quantity", "12", ConfidenceScore.FromRatio(0.95m), 1, "mock-field-map", Required: true),
            new("unitPrice", "4.50", ConfidenceScore.FromRatio(0.95m), 1, "mock-field-map", Required: true)
        };

        var result = new NormalizedOcrResult(
            request.DocumentId,
            request.CorrelationId,
            request.SourceFileName,
            Descriptor.ProviderId,
            request.Pages.Count,
            MeanConfidence: 0.96m,
            accuracy.CharacterErrorRate,
            accuracy.WordErrorRate,
            request.Pages
                .Select(page => new NormalizedOcrPage(
                    page.PageNumber,
                    text,
                    Confidence: 0.96m,
                    page.ContentHash,
                    page.RenderMode))
                .ToArray(),
            fields,
            new OcrProviderTelemetry(
                Descriptor.ProviderId,
                "mock harness integration",
                DurationMs: 420,
                CostPerPageUsdEstimate: 0m,
                WattHoursEstimate: null,
                CpuPercent: 12,
                RamMegabytes: 128,
                GpuPercent: 0,
                "Harness validation only; real OCR benchmark remains workstation-deferred."),
            DateTimeOffset.UtcNow);

        return Task.FromResult(result);
    }
}
