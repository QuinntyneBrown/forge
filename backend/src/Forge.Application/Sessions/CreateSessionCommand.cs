using Forge.Domain;
using MediatR;

namespace Forge.Application.Sessions;

public record CreateSessionCommand(
    EquipmentType Equipment,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    decimal? DistanceMiles,
    int? AvgHeartRateBpm,
    int ActiveCalories,
    string? Notes
) : IRequest<CreateSessionResult>;
