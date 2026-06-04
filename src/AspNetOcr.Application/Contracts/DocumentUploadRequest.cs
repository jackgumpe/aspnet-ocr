namespace AspNetOcr.Application.Contracts;

public sealed record DocumentUploadRequest(
    string FileName,
    string ContentType,
    Stream Content,
    string CorrelationId,
    long? DeclaredLength);
