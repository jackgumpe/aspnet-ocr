namespace AspNetOcr.Application.Contracts;

public sealed record OcrHealth(bool Available, string Engine, string Detail);
