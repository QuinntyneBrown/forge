using FluentValidation;

namespace Forge.Application.Sessions;

public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Equipment).IsInEnum();
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(480);
        RuleFor(x => x.ActiveCalories).GreaterThanOrEqualTo(0).LessThanOrEqualTo(5000);
        RuleFor(x => x.AvgHeartRateBpm).InclusiveBetween(30, 240).When(x => x.AvgHeartRateBpm.HasValue);
        RuleFor(x => x.DistanceMiles).GreaterThanOrEqualTo(0).When(x => x.DistanceMiles.HasValue);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
