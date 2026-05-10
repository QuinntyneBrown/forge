using Forge.Domain;

namespace Forge.Api.Controllers;

public record CreateSessionRequest(
    EquipmentType Equipment,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    decimal? DistanceMiles,
    int? AvgHeartRateBpm,
    int ActiveCalories,
    string? Notes
);
