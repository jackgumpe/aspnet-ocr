namespace AspNetOcr.Domain.Ocr;

public sealed record OcrFieldResult(
    string Name,
    string Value,
    ConfidenceScore Confidence,
    int PageNumber,
    string SourceRule,
    bool Required);
