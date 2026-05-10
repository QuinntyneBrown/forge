namespace Forge.Application.Abstractions;

public interface IHealthKitIngest
{
    Task IngestAsync(
        Guid userId,
        string sampleType,
        decimal value,
        string unit,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken);
}
