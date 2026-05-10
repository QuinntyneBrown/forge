namespace Forge.Api.Controllers;

public record IngestHealthKitSampleRequest(string SampleType, decimal Value, string Unit, DateTimeOffset RecordedAt);
