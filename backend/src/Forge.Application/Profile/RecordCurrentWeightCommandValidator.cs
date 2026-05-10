using FluentValidation;

namespace Forge.Application.Profile;

public class RecordCurrentWeightCommandValidator : AbstractValidator<RecordCurrentWeightCommand>
{
    public RecordCurrentWeightCommandValidator()
    {
        RuleFor(x => x.WeightLb).GreaterThan(0m).LessThanOrEqualTo(1500m);
    }
}
