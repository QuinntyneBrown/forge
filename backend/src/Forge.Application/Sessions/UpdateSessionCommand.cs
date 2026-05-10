using Forge.Domain;
using MediatR;

namespace Forge.Application.Sessions;

public record UpdateSessionCommand(
    Guid Id,
    EquipmentType Equipment,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    decimal? DistanceMiles,
    int? AvgHeartRateBpm,
    int ActiveCalories,
    string? Notes
) : IRequest;
