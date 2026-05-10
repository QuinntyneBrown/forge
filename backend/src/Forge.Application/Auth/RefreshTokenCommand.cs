using MediatR;

namespace Forge.Application.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;
