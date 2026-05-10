using MediatR;

namespace Forge.Application.Rewards;

public record ListRewardsQuery() : IRequest<IReadOnlyList<RewardItemDto>>;
