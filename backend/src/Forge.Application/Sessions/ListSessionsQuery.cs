using Forge.Domain;
using MediatR;

namespace Forge.Application.Sessions;

public record ListSessionsQuery(
    EquipmentType? Equipment,
    SessionRange Range,
    string? Search,
    int Page,
    int PageSize
) : IRequest<SessionPage>;
