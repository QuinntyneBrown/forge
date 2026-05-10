using Forge.Application.Abstractions;
using Forge.Application.Gamification;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Dashboard;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetDashboardSummaryQueryHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        var tz = TryFindTimeZone(user.TimeZoneId);
        var today = _clock.TodayInTimeZone(user.TimeZoneId);
        var todayStartUtc = ToUtc(today, tz);
        var todayEndUtc = ToUtc(today.AddDays(1), tz);
        var monthStartUtc = ToUtc(new DateOnly(today.Year, today.Month, 1), tz);

        var sessionsToday = await _db.WorkoutSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAt >= todayStartUtc && s.StartedAt < todayEndUtc)
            .GroupBy(s => 1)
            .Select(g => new { Cals = g.Sum(s => s.ActiveCalories), Mins = g.Sum(s => s.DurationMinutes) })
            .FirstOrDefaultAsync(cancellationToken);

        var caloriesToday = sessionsToday?.Cals ?? 0;
        var minutesToday = sessionsToday?.Mins ?? 0;

        var allDates = await _db.WorkoutSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAt >= todayStartUtc.AddDays(-90))
            .Select(s => s.StartedAt)
            .ToListAsync(cancellationToken);
        var distinctDays = allDates
            .Select(t => DateOnly.FromDateTime(tz is null ? t.UtcDateTime : TimeZoneInfo.ConvertTime(t, tz).DateTime))
            .Distinct()
            .ToHashSet();
        var streak = 0;
        var cursor = today;
        while (distinctDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        var ledger = await _db.PointsLedger
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .GroupBy(l => 1)
            .Select(g => new
            {
                Balance = g.Sum(l => l.Points),
                Lifetime = g.Where(l => l.Points > 0).Sum(l => l.Points)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var balance = ledger?.Balance ?? 0;
        var lifetime = ledger?.Lifetime ?? 0;

        var thresholds = ScoringConstants.TierThresholds;
        var tierName = thresholds[0].Name;
        for (var i = 0; i < thresholds.Count; i++)
        {
            if (lifetime >= thresholds[i].MinLifetimePoints)
            {
                tierName = thresholds[i].Name;
            }
            else
            {
                break;
            }
        }

        var nextReward = await _db.RewardCatalogItems
            .AsNoTracking()
            .Where(r => r.IsActive && r.CostPoints >= balance)
            .OrderBy(r => r.CostPoints)
            .Select(r => new NextRewardDto(r.Id, r.Name, r.CostPoints))
            .FirstOrDefaultAsync(cancellationToken);

        var weightThisMonth = await _db.WeightEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.RecordedAt >= monthStartUtc)
            .OrderBy(w => w.RecordedAt)
            .Select(w => w.WeightLb)
            .ToListAsync(cancellationToken);
        var weightLoss = weightThisMonth.Count >= 2
            ? weightThisMonth.First() - weightThisMonth.Last()
            : 0m;

        return new DashboardSummaryDto(
            caloriesToday,
            user.DailyActiveCaloriesTarget,
            minutesToday,
            user.DailyWorkoutMinutesTarget,
            streak,
            balance,
            lifetime,
            tierName,
            nextReward,
            weightLoss,
            user.MonthlyWeightGoalLb);
    }

    private static TimeZoneInfo? TryFindTimeZone(string ianaTimeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
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

    private static DateTimeOffset ToUtc(DateOnly date, TimeZoneInfo? tz)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        if (tz is null)
        {
            return new DateTimeOffset(local, TimeSpan.Zero);
        }
        var offset = tz.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
