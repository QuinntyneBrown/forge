using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Sessions;

public class DuplicateSessionCommandHandler : IRequestHandler<DuplicateSessionCommand, DuplicateSessionResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IPointsScorer _scorer;

    public DuplicateSessionCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IPointsScorer scorer)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _scorer = scorer;
    }

    public async Task<DuplicateSessionResult> Handle(
        DuplicateSessionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var source = await _db.WorkoutSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId, cancellationToken)
            ?? throw new SessionNotFoundException(request.Id);

        var now = _clock.UtcNow;
        var copy = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Equipment = source.Equipment,
            StartedAt = now,
            DurationMinutes = source.DurationMinutes,
            DistanceMiles = source.DistanceMiles,
            AvgHeartRateBpm = source.AvgHeartRateBpm,
            ActiveCalories = source.ActiveCalories,
            Notes = source.Notes,
            CreatedAt = now
        };

        _db.WorkoutSessions.Add(copy);
        await _db.SaveChangesAsync(cancellationToken);
        await _scorer.Score(copy, cancellationToken);

        return new DuplicateSessionResult(copy.Id);
    }
}
