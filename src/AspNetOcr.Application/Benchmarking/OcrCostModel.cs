namespace AspNetOcr.Application.Benchmarking;

public sealed record OcrCostEstimate(
    int PageCount,
    decimal MarginalLocalUsd,
    decimal FullyLoadedLocalUsd,
    decimal CloudUsd,
    decimal LocalVsCloudDeltaUsd,
    string Status,
    string Notes);

public static class OcrCostModel
{
    public static OcrCostEstimate Estimate(
        int pageCount,
        decimal wattHours,
        decimal electricityUsdPerKwh,
        decimal hardwareAmortizationUsd,
        decimal maintenanceUsd,
        decimal cloudUsd,
        decimal transferAndRetryUsd)
    {
        if (pageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Page count must be positive.");
        }

        var energyUsd = wattHours / 1000m * electricityUsdPerKwh;
        var marginalLocal = RoundMoney(energyUsd);
        var fullyLoadedLocal = RoundMoney(energyUsd + hardwareAmortizationUsd + maintenanceUsd);
        var cloud = RoundMoney(cloudUsd + transferAndRetryUsd);

        return new OcrCostEstimate(
            pageCount,
            marginalLocal,
            fullyLoadedLocal,
            cloud,
            RoundMoney(fullyLoadedLocal - cloud),
            "workstation_inputs_required",
            "Populate with workstation power, throughput, amortization, and cloud comparison inputs after ASP-OCR-002B live runs.");
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
