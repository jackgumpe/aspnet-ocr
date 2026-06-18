using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Application.Contracts;

public sealed record ProviderOcrProcessResult(
    Guid DocumentId,
    string CorrelationId,
    string SourceFileName,
    string OriginalArtifactPath,
    string NormalizedOcrArtifactPath,
    int PageCount,
    decimal MeanConfidence,
    IReadOnlyList<OcrFieldResult> Fields);
