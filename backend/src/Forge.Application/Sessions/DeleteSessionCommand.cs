using MediatR;

namespace Forge.Application.Sessions;

public record DeleteSessionCommand(Guid Id) : IRequest;
