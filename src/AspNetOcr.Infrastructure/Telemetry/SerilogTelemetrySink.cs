using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AspNetOcr.Infrastructure.Telemetry;

public sealed class SerilogTelemetrySink : ITelemetrySink
{
    private readonly ILogger<SerilogTelemetrySink> _logger;

    public SerilogTelemetrySink(ILogger<SerilogTelemetrySink> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(PipelineStageEvent stageEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "Pipeline stage {Stage} reached {Status} for document {DocumentId} correlation_id {CorrelationId}",
            stageEvent.Stage,
            stageEvent.Status,
            stageEvent.DocumentId,
            stageEvent.CorrelationId);
        return Task.CompletedTask;
    }
}
