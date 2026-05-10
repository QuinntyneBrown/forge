using MediatR;

namespace Forge.Application.Profile;

public record UpdateKitchenWindowCommand(
    TimeOnly Start,
    TimeOnly End,
    bool NudgeEnabled
) : IRequest;
