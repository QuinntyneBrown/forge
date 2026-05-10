using MediatR;

namespace Forge.Application.Auth;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
