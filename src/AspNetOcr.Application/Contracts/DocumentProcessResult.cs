using AspNetOcr.Domain.Documents;

namespace AspNetOcr.Application.Contracts;

public sealed record DocumentProcessResult(
    Guid DocumentId,
    string CorrelationId,
    DocumentStatus Status,
    bool Succeeded,
    bool IsReplay,
    string? ManifestPath,
    string? ExportPath,
    int ProductRowCount,
    IReadOnlyList<string> Errors);
