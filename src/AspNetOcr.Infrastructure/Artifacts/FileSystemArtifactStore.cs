using System.Text.Json;
using AspNetOcr.Application.Interfaces;

namespace AspNetOcr.Infrastructure.Artifacts;

public sealed class FileSystemArtifactStore : IArtifactStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _root;

    public FileSystemArtifactStore(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public string GetPath(Guid documentId, string relativeName)
    {
        var safeRelativeName = NormalizeRelativeName(relativeName);
        var documentRoot = Path.Combine(_root, documentId.ToString("n"));
        var path = Path.GetFullPath(Path.Combine(documentRoot, safeRelativeName));

        var safeDocumentRoot = Path.GetFullPath(documentRoot) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(safeDocumentRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Artifact path escapes the document artifact root.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? documentRoot);
        return path;
    }

    public async Task<string> SaveBytesAsync(Guid documentId, string relativeName, byte[] content, CancellationToken cancellationToken)
    {
        var path = GetPath(documentId, relativeName);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
        return path;
    }

    public async Task<string> SaveTextAsync(Guid documentId, string relativeName, string content, CancellationToken cancellationToken)
    {
        var path = GetPath(documentId, relativeName);
        await File.WriteAllTextAsync(path, content, cancellationToken);
        return path;
    }

    public async Task<string> SaveJsonAsync<T>(Guid documentId, string relativeName, T content, CancellationToken cancellationToken)
    {
        var path = GetPath(documentId, relativeName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, content, SerializerOptions, cancellationToken);
        return path;
    }

    private static string NormalizeRelativeName(string relativeName)
    {
        if (string.IsNullOrWhiteSpace(relativeName))
        {
            throw new ArgumentException("Artifact relative name is required.", nameof(relativeName));
        }

        var normalized = relativeName.Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Artifact relative name cannot contain path traversal.");
        }

        return normalized.TrimStart('/');
    }
}
