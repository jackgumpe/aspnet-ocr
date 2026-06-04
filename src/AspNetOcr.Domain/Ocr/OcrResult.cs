namespace AspNetOcr.Domain.Ocr;

public sealed record OcrResult(
    string RawText,
    ConfidenceScore MeanConfidence,
    string Engine,
    string SourceFileName);
