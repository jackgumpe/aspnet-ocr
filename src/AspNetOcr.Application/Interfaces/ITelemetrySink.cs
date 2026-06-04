using AspNetOcr.Application.Contracts;

namespace AspNetOcr.Application.Interfaces;

public interface ITelemetrySink
{
    Task RecordAsync(PipelineStageEvent stageEvent, CancellationToken cancellationToken);
}
