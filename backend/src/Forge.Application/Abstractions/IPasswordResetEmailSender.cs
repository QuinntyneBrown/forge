namespace Forge.Application.Abstractions;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string rawToken, CancellationToken cancellationToken);
}
