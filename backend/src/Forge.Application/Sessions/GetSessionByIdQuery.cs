using MediatR;

namespace Forge.Application.Sessions;

public record GetSessionByIdQuery(Guid Id) : IRequest<SessionDto?>;
