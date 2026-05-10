using Forge.Application.Abstractions;
using Forge.Application.Gamification;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class PointsScorer : IPointsScorer
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public PointsScorer(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Refund(WorkoutSession session, CancellationToken cancellationToken)
    {
        var awarded = await _db.PointsLedger
            .Where(l => l.SessionId == session.Id)
            .SumAsync(l => (int?)l.Points, cancellationToken) ?? 0;
        if (awarded == 0)
        {
            return;
        }

        _db.PointsLedger.Add(new PointsLedger
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            Reason = PointsLedgerReason.Refund,
            Points = -awarded,
            Description = "Refund — session updated",
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task Score(WorkoutSession session, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var basePoints = ScoringConstants.BasePointsPerMinute * session.DurationMinutes;
        _db.PointsLedger.Add(new PointsLedger
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            Reason = PointsLedgerReason.Base,
            Points = basePoints,
            Description = $"Base — {session.DurationMinutes} min logged",
            CreatedAt = now
        });

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

        if (user is not null)
        {
            if (IsWithinMorningWindow(session.StartedAt, user))
            {
                _db.PointsLedger.Add(new PointsLedger
                {
                    Id = Guid.NewGuid(),
                    UserId = session.UserId,
                    SessionId = session.Id,
                    Reason = PointsLedgerReason.MorningBonus,
                    Points = ScoringConstants.MorningBonusPoints,
                    Description = "Morning bonus",
                    CreatedAt = now
                });
            }

            var streakDays = await ConsecutiveStreakDays(session.UserId, user.TimeZoneId, cancellationToken);
            var multiplier = Math.Min(
                ScoringConstants.StreakMultiplierCap,
                1.00m + ScoringConstants.StreakStepPerDay * streakDays);
            if (multiplier > 1.00m)
            {
                var streakBonus = (int)Math.Floor(basePoints * (multiplier - 1.00m));
                if (streakBonus > 0)
                {
                    _db.PointsLedger.Add(new PointsLedger
                    {
                        Id = Guid.NewGuid(),
                        UserId = session.UserId,
                        SessionId = session.Id,
                        Reason = PointsLedgerReason.StreakMultiplier,
                        Points = streakBonus,
                        Description = $"Streak multiplier ×{multiplier:0.00}",
                        CreatedAt = now
                    });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsWithinMorningWindow(DateTimeOffset startedAt, User user)
    {
        var tz = TryFindTimeZone(user.TimeZoneId);
        if (tz is null)
        {
            return false;
        }

        var local = TimeZoneInfo.ConvertTime(startedAt, tz);
        var localTime = TimeOnly.FromTimeSpan(local.TimeOfDay);
        return localTime >= user.MorningWindowStart && localTime <= user.MorningWindowEnd;
    }

    private async Task<int> ConsecutiveStreakDays(Guid userId, string timeZoneId, CancellationToken cancellationToken)
    {
        var today = _clock.TodayInTimeZone(timeZoneId);
        var since = today.AddDays(-90);
        var sinceUtc = new DateTimeOffset(since.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var raw = await _db.WorkoutSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAt >= sinceUtc)
            .Select(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        var tz = TryFindTimeZone(timeZoneId);
        var distinctDays = raw
            .Select(t => DateOnly.FromDateTime(
                tz is null ? t.UtcDateTime : TimeZoneInfo.ConvertTime(t, tz).DateTime))
            .Distinct()
            .ToHashSet();

        var streak = 0;
        var cursor = today;
        while (distinctDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
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
}
