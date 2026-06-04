namespace AspNetOcr.Application.Interfaces;

public interface IArtifactStore
{
    string GetPath(Guid documentId, string relativeName);

    Task<string> SaveBytesAsync(Guid documentId, string relativeName, byte[] content, CancellationToken cancellationToken);

    Task<string> SaveTextAsync(Guid documentId, string relativeName, string content, CancellationToken cancellationToken);

    Task<string> SaveJsonAsync<T>(Guid documentId, string relativeName, T content, CancellationToken cancellationToken);
}
