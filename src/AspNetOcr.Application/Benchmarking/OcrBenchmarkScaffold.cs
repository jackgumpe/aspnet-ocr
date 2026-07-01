using System.Diagnostics;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;

namespace AspNetOcr.Application.Benchmarking;

public sealed record OcrPipelineStagePlan(
    string StageName,
    string Classification,
    string WorkstationMeasurement);

public sealed record OcrStageMetric(
    string StageName,
    string Classification,
    long DurationMs,
    int? CpuPercent,
    int? MemoryMegabytes,
    bool IsMeasuredBenchmark,
    string Notes);

public sealed record OcrBenchmarkRun(
    string RunLabel,
    bool IsOcrBenchmark,
    IReadOnlyList<OcrStageMetric> Stages,
    NormalizedOcrResult Result,
    OcrAccuracyMetrics Accuracy,
    decimal PagesPerMinute,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);

public static class OcrPipelineStages
{
    public const string PdfLoad = "PDF load";
    public const string Rasterization = "rasterization";
    public const string Preprocessing = "preprocessing";
    public const string Ocr = "OCR";
    public const string Normalization = "normalization";
    public const string Serialization = "serialization";

    public static IReadOnlyList<OcrPipelineStagePlan> DefaultPlans { get; } =
    [
        new(PdfLoad, "CPU-bound", "measure file read and parser setup on workstation"),
        new(Rasterization, "CPU-bound", "measure PDF-to-image rendering throughput"),
        new(Preprocessing, "GPU-candidate", "compare CPU image transforms with CUDA-capable path only after hardware arrival"),
        new(Ocr, "CPU-bound", "Tesseract 5 baseline is CPU local OCR; GPU remains data-dependent"),
        new(Normalization, "not worth", "text shaping and field normalization should remain CPU-resident"),
        new(Serialization, "not worth", "JSON artifact writes are IO-bound and not a GPU target")
    ];
}

public sealed class OcrBenchmarkScaffold
{
    public const string HarnessOnlyLabel = "harness validation only — not an OCR benchmark";

    private readonly IOcrProvider _provider;

    public OcrBenchmarkScaffold(IOcrProvider provider)
    {
        _provider = provider;
    }

    public async Task<OcrBenchmarkRun> RunHarnessAsync(
        OcrProviderRequest request,
        string expectedText,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var totalStopwatch = Stopwatch.StartNew();
        var stageMetrics = new List<OcrStageMetric>(OcrPipelineStages.DefaultPlans.Count);
        NormalizedOcrResult? result = null;

        foreach (var stage in OcrPipelineStages.DefaultPlans)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stageStopwatch = Stopwatch.StartNew();

            if (stage.StageName == OcrPipelineStages.Ocr)
            {
                result = await _provider.RecognizeAsync(request, cancellationToken);
            }

            stageStopwatch.Stop();
            var resourceProfile = HarnessResourceProfile(stage.StageName);
            stageMetrics.Add(new OcrStageMetric(
                stage.StageName,
                stage.Classification,
                stageStopwatch.ElapsedMilliseconds,
                resourceProfile.CpuPercent,
                resourceProfile.MemoryMegabytes,
                IsMeasuredBenchmark: false,
                Notes: HarnessOnlyLabel));
        }

        totalStopwatch.Stop();

        if (result is null)
        {
            throw new InvalidOperationException("OCR stage did not produce a result.");
        }

        var observedText = string.Join(
            Environment.NewLine,
            result.Pages.OrderBy(page => page.PageNumber).Select(page => page.Text));
        var accuracy = OcrAccuracyCalculator.Calculate(expectedText, observedText);
        var elapsedMinutes = Math.Max(totalStopwatch.Elapsed.TotalMinutes, 0.001d);
        var pagesPerMinute = Math.Round(request.Pages.Count / (decimal)elapsedMinutes, 6, MidpointRounding.AwayFromZero);

        return new OcrBenchmarkRun(
            HarnessOnlyLabel,
            IsOcrBenchmark: false,
            stageMetrics,
            result,
            accuracy,
            pagesPerMinute,
            startedAtUtc,
            DateTimeOffset.UtcNow);
    }

    private static (int CpuPercent, int MemoryMegabytes) HarnessResourceProfile(string stageName)
    {
        return stageName switch
        {
            OcrPipelineStages.PdfLoad => (8, 96),
            OcrPipelineStages.Rasterization => (18, 192),
            OcrPipelineStages.Preprocessing => (14, 160),
            OcrPipelineStages.Ocr => (24, 224),
            OcrPipelineStages.Normalization => (6, 112),
            OcrPipelineStages.Serialization => (4, 104),
            _ => (0, 0)
        };
    }
}
