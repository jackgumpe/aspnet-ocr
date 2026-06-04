using AspNetOcr.Domain.Documents;

namespace AspNetOcr.Application.Contracts;

public sealed record DocumentManifest(
    Guid DocumentId,
    string CorrelationId,
    string SourceFileName,
    string ContentHash,
    DocumentStatus Status,
    string? OriginalArtifactPath,
    string? RawOcrArtifactPath,
    string? ValidationArtifactPath,
    string? ManifestPath,
    string? ExportPath,
    int ProductRowCount,
    decimal MeanConfidence,
    bool IsReplay,
    IReadOnlyList<PipelineStageEvent> Events,
    IReadOnlyList<string> Errors);
