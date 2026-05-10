namespace Forge.Application.Abstractions;

public enum NotificationKind
{
    MorningWindowStarting = 1,
    KitchenClosing = 2
}

public interface INotificationSender
{
    Task SendAsync(NotificationKind kind, Guid userId, CancellationToken cancellationToken);
}
