using Forge.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forge.Infrastructure.Deferred;

/// <summary>
/// Deferred-integration no-op stand-in for transactional email. Logs the
/// intended action so a developer can pick the token out of the dev console
/// while a real transactional-email integration is out of MVP scope (per
/// L2-004 and BP1 §8). The interface registration is swapped at deploy time;
/// no handler code changes when a real sender lands.
/// </summary>
public class LoggingPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly ILogger<LoggingPasswordResetEmailSender> _logger;

    public LoggingPasswordResetEmailSender(ILogger<LoggingPasswordResetEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "password-reset.email.deferred email={Email} token={Token}",
            email,
            rawToken);
        return Task.CompletedTask;
    }
}
