using FluentValidation;

namespace Forge.Application.Sessions;

public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
{
    public DeleteSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
