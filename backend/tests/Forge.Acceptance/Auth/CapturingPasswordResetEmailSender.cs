using Forge.Application.Abstractions;

namespace Forge.Acceptance.Auth;

public class CapturingPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly object _gate = new();
    private string? _lastToken;
    private string? _lastEmail;

    public string? LastToken
    {
        get { lock (_gate) { return _lastToken; } }
    }

    public string? LastEmail
    {
        get { lock (_gate) { return _lastEmail; } }
    }

    public Task SendAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _lastToken = rawToken;
            _lastEmail = email;
        }
        return Task.CompletedTask;
    }
}
