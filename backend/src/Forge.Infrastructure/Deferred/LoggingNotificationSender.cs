using Forge.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forge.Infrastructure.Deferred;

/// <summary>
/// Deferred-integration no-op stand-in for push / email notification
/// transport. Logs the intended dispatch so a developer can verify the
/// schedule fires while the real transport (APNs / FCM / transactional
/// email) is out of MVP scope per L2-025/L2-026 and BP1 §8.
/// </summary>
public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(NotificationKind kind, Guid userId, CancellationToken cancellationToken)
    {
        var marker = kind switch
        {
            NotificationKind.MorningWindowStarting => "notification.morning",
            NotificationKind.KitchenClosing => "notification.kitchen",
            _ => "notification.unknown"
        };
        _logger.LogInformation("{Marker} userId={UserId}", marker, userId);
        return Task.CompletedTask;
    }
}
