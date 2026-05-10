using Forge.Domain;

namespace Forge.Application.Sessions;

public record SessionDto(
    Guid Id,
    EquipmentType Equipment,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    decimal? DistanceMiles,
    int? AvgHeartRateBpm,
    int ActiveCalories,
    string? Notes,
    DateTimeOffset CreatedAt
);
