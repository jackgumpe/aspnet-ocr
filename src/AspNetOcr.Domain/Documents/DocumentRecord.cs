namespace AspNetOcr.Domain.Documents;

public sealed class DocumentRecord
{
    private readonly List<DocumentStatus> _statusHistory = [];

    private DocumentRecord(
        Guid id,
        string correlationId,
        string fileName,
        string contentHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        CorrelationId = correlationId;
        FileName = fileName;
        ContentHash = contentHash;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = DocumentStatus.Received;
        _statusHistory.Add(Status);
    }

    public Guid Id { get; }

    public string CorrelationId { get; }

    public string FileName { get; }

    public string ContentHash { get; }

    public DocumentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? OriginalArtifactPath { get; private set; }

    public string? RawOcrArtifactPath { get; private set; }

    public string? ValidationArtifactPath { get; private set; }

    public string? ManifestArtifactPath { get; private set; }

    public string? ExportArtifactPath { get; private set; }

    public string? LastError { get; private set; }

    public IReadOnlyList<DocumentStatus> StatusHistory => _statusHistory;

    public static DocumentRecord Create(string correlationId, string fileName, string contentHash, DateTimeOffset now)
    {
        return new DocumentRecord(Guid.NewGuid(), correlationId, fileName, contentHash, now);
    }

    public static DocumentRecord Rehydrate(
        Guid id,
        string correlationId,
        string fileName,
        string contentHash,
        DocumentStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? originalArtifactPath,
        string? rawOcrArtifactPath,
        string? validationArtifactPath,
        string? manifestArtifactPath,
        string? exportArtifactPath,
        string? lastError)
    {
        var document = new DocumentRecord(id, correlationId, fileName, contentHash, createdAt)
        {
            Status = status,
            UpdatedAt = updatedAt,
            OriginalArtifactPath = originalArtifactPath,
            RawOcrArtifactPath = rawOcrArtifactPath,
            ValidationArtifactPath = validationArtifactPath,
            ManifestArtifactPath = manifestArtifactPath,
            ExportArtifactPath = exportArtifactPath,
            LastError = lastError
        };

        document._statusHistory.Clear();
        document._statusHistory.Add(status);
        return document;
    }

    public void Mark(DocumentStatus status, DateTimeOffset now)
    {
        if (Status == DocumentStatus.DeadLettered)
        {
            throw new InvalidOperationException("Dead-lettered documents cannot transition to another state.");
        }

        Status = status;
        UpdatedAt = now;
        _statusHistory.Add(status);
    }

    public void SetArtifacts(
        string? originalArtifactPath = null,
        string? rawOcrArtifactPath = null,
        string? validationArtifactPath = null,
        string? manifestArtifactPath = null,
        string? exportArtifactPath = null)
    {
        OriginalArtifactPath = originalArtifactPath ?? OriginalArtifactPath;
        RawOcrArtifactPath = rawOcrArtifactPath ?? RawOcrArtifactPath;
        ValidationArtifactPath = validationArtifactPath ?? ValidationArtifactPath;
        ManifestArtifactPath = manifestArtifactPath ?? ManifestArtifactPath;
        ExportArtifactPath = exportArtifactPath ?? ExportArtifactPath;
    }

    public void DeadLetter(string error, DateTimeOffset now)
    {
        Status = DocumentStatus.DeadLettered;
        UpdatedAt = now;
        LastError = error;
        _statusHistory.Add(Status);
    }
}
