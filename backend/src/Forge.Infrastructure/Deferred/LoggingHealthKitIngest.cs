using Forge.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forge.Infrastructure.Deferred;

/// <summary>
/// Deferred-integration no-op stand-in for Apple HealthKit ingestion. Logs
/// the intended action so the developer can verify the contract while a
/// real integration with Apple HealthKit / iOS sync is out of MVP scope
/// (per L2-023 and BP1 §8). The interface registration is swapped at
/// deploy time; no handler code changes when a real ingester lands.
/// </summary>
public class LoggingHealthKitIngest : IHealthKitIngest
{
    private readonly ILogger<LoggingHealthKitIngest> _logger;

    public LoggingHealthKitIngest(ILogger<LoggingHealthKitIngest> logger)
    {
        _logger = logger;
    }

    public Task IngestAsync(
        Guid userId,
        string sampleType,
        decimal value,
        string unit,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "healthkit.ingest.deferred userId={UserId} sampleType={SampleType} value={Value} unit={Unit} recordedAt={RecordedAt}",
            userId,
            sampleType,
            value,
            unit,
            recordedAt);
        return Task.CompletedTask;
    }
}
