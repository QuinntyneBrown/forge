using MediatR;

namespace Forge.Application.Profile;

public record ListLeaderboardQuery(int Page, int PageSize) : IRequest<IReadOnlyList<LeaderboardEntryDto>>;
