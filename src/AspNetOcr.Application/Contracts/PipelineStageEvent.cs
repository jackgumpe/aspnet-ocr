using AspNetOcr.Domain.Documents;

namespace AspNetOcr.Application.Contracts;

public sealed record PipelineStageEvent(
    Guid DocumentId,
    string CorrelationId,
    DocumentStatus Status,
    string Stage,
    string Message,
    DateTimeOffset OccurredAt);
