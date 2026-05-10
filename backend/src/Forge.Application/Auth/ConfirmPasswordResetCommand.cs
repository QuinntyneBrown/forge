using MediatR;

namespace Forge.Application.Auth;

public record ConfirmPasswordResetCommand(string Token, string NewPassword) : IRequest;
