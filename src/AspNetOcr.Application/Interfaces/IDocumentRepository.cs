using AspNetOcr.Domain.Documents;

namespace AspNetOcr.Application.Interfaces;

public interface IDocumentRepository
{
    Task<DocumentRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DocumentRecord?> FindByContentHashAsync(string contentHash, CancellationToken cancellationToken);

    Task SaveAsync(DocumentRecord document, CancellationToken cancellationToken);
}
