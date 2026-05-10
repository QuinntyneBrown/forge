using MediatR;

namespace Forge.Application.Sessions;

public record DuplicateSessionCommand(Guid Id) : IRequest<DuplicateSessionResult>;
