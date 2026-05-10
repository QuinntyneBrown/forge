using MediatR;

namespace Forge.Application.Profile;

public record SetLeaderboardOptInCommand(bool LeaderboardOptIn) : IRequest;
