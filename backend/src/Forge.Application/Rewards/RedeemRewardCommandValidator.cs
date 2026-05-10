using FluentValidation;

namespace Forge.Application.Rewards;

public class RedeemRewardCommandValidator : AbstractValidator<RedeemRewardCommand>
{
    public RedeemRewardCommandValidator()
    {
        RuleFor(x => x.RewardId).NotEmpty();
    }
}
