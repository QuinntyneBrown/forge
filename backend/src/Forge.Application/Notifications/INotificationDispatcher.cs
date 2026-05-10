namespace Forge.Application.Notifications;

public interface INotificationDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken);
}
