namespace AspNetOcr.Application.Options;

public sealed class PipelineOptions
{
    public long MaxUploadBytes { get; init; } = 25 * 1024 * 1024;

    public decimal MinimumMeanConfidence { get; init; } = 0.70m;
}
