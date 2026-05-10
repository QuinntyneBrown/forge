namespace Forge.Application.Rewards;

public class RewardNotFoundException : Exception
{
    public RewardNotFoundException(Guid rewardId)
        : base($"Reward {rewardId} not found or inactive.") { }
}
