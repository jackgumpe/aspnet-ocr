namespace AspNetOcr.Application.Contracts;

public sealed record OcrInput(
    string FileName,
    string ContentType,
    byte[] Content,
    string CorrelationId);
