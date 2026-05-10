using Forge.Application.Abstractions;
using Forge.Application.Gamification;
using Forge.Domain;

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
        var basePoints = ScoringConstants.BasePointsPerMinute * session.DurationMinutes;
        _db.PointsLedger.Add(new PointsLedger
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            Reason = PointsLedgerReason.Base,
            Points = basePoints,
            Description = $"Base — {session.DurationMinutes} min logged",
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
