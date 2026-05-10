using MediatR;

namespace Forge.Application.Auth;

public record SignOutCommand(string RefreshToken) : IRequest;
