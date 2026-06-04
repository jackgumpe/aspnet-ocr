using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Domain.Products;

public sealed record ProductSheet(
    string Sku,
    string Name,
    string Category,
    int Quantity,
    decimal UnitPrice,
    ConfidenceScore Confidence);
