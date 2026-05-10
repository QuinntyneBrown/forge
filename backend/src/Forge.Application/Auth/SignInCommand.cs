using MediatR;

namespace Forge.Application.Auth;

public record SignInCommand(string Email, string Password) : IRequest<AuthResult>;
