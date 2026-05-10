using Forge.Application.Notifications;

namespace Forge.Api.HostedServices;

public class NotificationDispatcherHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationDispatcherHostedService> _logger;

    public NotificationDispatcherHostedService(
        IServiceProvider services,
        ILogger<NotificationDispatcherHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        try
        {
            do
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                    await dispatcher.DispatchPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "notification.dispatcher.tick.failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }
}
