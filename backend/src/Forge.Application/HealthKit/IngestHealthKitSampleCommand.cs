using MediatR;

namespace Forge.Application.HealthKit;

public record IngestHealthKitSampleCommand(
    string SampleType,
    decimal Value,
    string Unit,
    DateTimeOffset RecordedAt
) : IRequest;
