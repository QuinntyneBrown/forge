using MediatR;

namespace Forge.Application.Rewards;

public record RedeemRewardCommand(Guid RewardId) : IRequest<RedeemRewardResult>;
