using FluentValidation;

namespace Forge.Application.Auth;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        // No payload — the current user is read from the JWT in the handler.
        // Validator exists so the FluentValidation pipeline behavior runs even
        // for an empty command, keeping the shape consistent with every other
        // command in the slice.
    }
}
