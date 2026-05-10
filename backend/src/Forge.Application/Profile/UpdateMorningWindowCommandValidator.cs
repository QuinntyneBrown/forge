using FluentValidation;

namespace Forge.Application.Profile;

public class UpdateMorningWindowCommandValidator : AbstractValidator<UpdateMorningWindowCommand>
{
    public UpdateMorningWindowCommandValidator()
    {
        RuleFor(x => x.Start).LessThan(x => x.End);
    }
}
