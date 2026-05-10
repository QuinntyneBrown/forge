using Forge.Application.Abstractions;
using Forge.Application.Notifications;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class NotificationDispatcher : INotificationDispatcher
{
    private static readonly TimeSpan LookAhead = TimeSpan.FromMinutes(2);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly INotificationSender _sender;

    public NotificationDispatcher(IAppDbContext db, IClock clock, INotificationSender sender)
    {
        _db = db;
        _clock = clock;
        _sender = sender;
    }

    public async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && (u.MorningReminderEnabled || u.KitchenNudgeEnabled))
            .Select(u => new
            {
                u.Id,
                u.TimeZoneId,
                u.MorningReminderEnabled,
                u.MorningWindowStart,
                u.KitchenNudgeEnabled,
                u.KitchenClosedStart
            })
            .ToListAsync(cancellationToken);

        var nowUtc = _clock.UtcNow;
        foreach (var user in users)
        {
            var tz = TryFindTimeZone(user.TimeZoneId);
            if (tz is null)
            {
                continue;
            }

            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);
            var localFuture = localNow + LookAhead;

            if (user.MorningReminderEnabled
                && IsBoundaryWithin(user.MorningWindowStart, localNow, localFuture))
            {
                await _sender.SendAsync(NotificationKind.MorningWindowStarting, user.Id, cancellationToken);
            }

            if (user.KitchenNudgeEnabled
                && IsBoundaryWithin(user.KitchenClosedStart, localNow, localFuture))
            {
                await _sender.SendAsync(NotificationKind.KitchenClosing, user.Id, cancellationToken);
            }
        }
    }

    private static bool IsBoundaryWithin(TimeOnly boundary, DateTimeOffset localNow, DateTimeOffset localFuture)
    {
        var nowOnly = TimeOnly.FromTimeSpan(localNow.TimeOfDay);
        var futureOnly = TimeOnly.FromTimeSpan(localFuture.TimeOfDay);

        // Wrap-around when the look-ahead crosses midnight.
        if (futureOnly < nowOnly)
        {
            return boundary >= nowOnly || boundary <= futureOnly;
        }
        return boundary >= nowOnly && boundary <= futureOnly;
    }

    private static TimeZoneInfo? TryFindTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
