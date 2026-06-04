namespace AspNetOcr.Application.Interfaces;

public interface IPhase2QueuePort
{
    Task EnqueueBulkDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}
