namespace AspNetOcr.Domain.Ocr;

public readonly record struct ConfidenceScore
{
    private ConfidenceScore(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static ConfidenceScore FromRatio(decimal ratio)
    {
        if (ratio < 0m || ratio > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(ratio), "Confidence ratio must be between 0 and 1.");
        }

        return new ConfidenceScore(decimal.Round(ratio, 4));
    }

    public static ConfidenceScore FromPercent(decimal percent)
    {
        if (percent < 0m || percent > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Confidence percent must be between 0 and 100.");
        }

        return FromRatio(percent / 100m);
    }
}
