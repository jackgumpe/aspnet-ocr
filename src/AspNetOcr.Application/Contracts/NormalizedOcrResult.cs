using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Application.Contracts;

public sealed record NormalizedOcrResult(
    Guid DocumentId,
    string CorrelationId,
    string SourceFileName,
    string ProviderId,
    int PageCount,
    decimal MeanConfidence,
    decimal? CharacterErrorRate,
    decimal? WordErrorRate,
    IReadOnlyList<NormalizedOcrPage> Pages,
    IReadOnlyList<OcrFieldResult> Fields,
    OcrProviderTelemetry Telemetry,
    DateTimeOffset GeneratedAtUtc);

public sealed record NormalizedOcrPage(
    int PageNumber,
    string Text,
    decimal Confidence,
    string ContentHash,
    string RenderMode);

public sealed record OcrProviderTelemetry(
    string ProviderId,
    string StageClassification,
    long DurationMs,
    decimal? CostPerPageUsdEstimate,
    decimal? WattHoursEstimate,
    int? CpuPercent,
    int? RamMegabytes,
    int? GpuPercent,
    string Notes);
