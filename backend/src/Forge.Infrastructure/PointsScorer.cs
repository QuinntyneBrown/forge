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

        if (user is not null && IsWithinMorningWindow(session.StartedAt, user))
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

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsWithinMorningWindow(DateTimeOffset startedAt, User user)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        var local = TimeZoneInfo.ConvertTime(startedAt, tz);
        var localTime = TimeOnly.FromTimeSpan(local.TimeOfDay);
        return localTime >= user.MorningWindowStart && localTime <= user.MorningWindowEnd;
    }
}
