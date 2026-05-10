using FluentValidation;

namespace Forge.Application.Auth;

public class SignOutCommandValidator : AbstractValidator<SignOutCommand>
{
    public SignOutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
