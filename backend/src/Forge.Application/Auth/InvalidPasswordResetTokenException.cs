namespace Forge.Application.Auth;

public class InvalidPasswordResetTokenException : Exception
{
    public InvalidPasswordResetTokenException()
        : base("Password reset token is invalid, expired, or already used.")
    {
    }
}
