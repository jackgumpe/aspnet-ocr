namespace AspNetOcr.Application.Contracts;

public sealed record OcrPageImage(
    int PageNumber,
    string ContentType,
    byte[] Content,
    string ContentHash,
    string RenderMode);
