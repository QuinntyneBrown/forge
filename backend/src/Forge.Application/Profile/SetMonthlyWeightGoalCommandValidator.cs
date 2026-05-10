using FluentValidation;

namespace Forge.Application.Profile;

public class SetMonthlyWeightGoalCommandValidator : AbstractValidator<SetMonthlyWeightGoalCommand>
{
    public SetMonthlyWeightGoalCommandValidator()
    {
        RuleFor(x => x.MonthlyWeightGoalLb).InclusiveBetween(1, 30);
    }
}
