using MediatR;

namespace Forge.Application.Profile;

public record UpdateMorningWindowCommand(
    TimeOnly Start,
    TimeOnly End,
    bool ReminderEnabled
) : IRequest;
