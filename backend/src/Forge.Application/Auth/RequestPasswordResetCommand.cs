using MediatR;

namespace Forge.Application.Auth;

public record RequestPasswordResetCommand(string Email) : IRequest;
