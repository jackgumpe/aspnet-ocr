namespace AspNetOcr.Application.Contracts;

public sealed record ValidationReport(
    bool IsValid,
    int ProductRowCount,
    decimal MeanConfidence,
    IReadOnlyList<string> Errors);
