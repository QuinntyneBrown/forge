using FluentValidation;

namespace Forge.Application.Sessions;

public class DuplicateSessionCommandValidator : AbstractValidator<DuplicateSessionCommand>
{
    public DuplicateSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
