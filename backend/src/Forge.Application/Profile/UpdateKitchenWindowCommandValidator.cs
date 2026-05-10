using FluentValidation;

namespace Forge.Application.Profile;

public class UpdateKitchenWindowCommandValidator : AbstractValidator<UpdateKitchenWindowCommand>
{
    public UpdateKitchenWindowCommandValidator()
    {
        // The kitchen-closed window may span midnight (e.g. 20:00 -> 06:00),
        // so start > end is permitted. Only a zero-length window is rejected.
        RuleFor(x => x).Must(c => c.Start != c.End)
            .WithMessage("Start and end must differ.");
    }
}
