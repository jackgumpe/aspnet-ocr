namespace AspNetOcr.Application.Contracts;

public sealed record OcrProviderRequest(
    Guid DocumentId,
    string CorrelationId,
    string SourceFileName,
    string ContentType,
    IReadOnlyList<OcrPageImage> Pages,
    DateTimeOffset RequestedAtUtc);
