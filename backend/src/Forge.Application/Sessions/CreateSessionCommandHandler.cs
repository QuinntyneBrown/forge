using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;

namespace Forge.Application.Sessions;

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, CreateSessionResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IPointsScorer _scorer;

    public CreateSessionCommandHandler(
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

    public async Task<CreateSessionResult> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            Equipment = request.Equipment,
            StartedAt = request.StartedAt,
            DurationMinutes = request.DurationMinutes,
            DistanceMiles = request.DistanceMiles,
            AvgHeartRateBpm = request.AvgHeartRateBpm,
            ActiveCalories = request.ActiveCalories,
            Notes = request.Notes,
            CreatedAt = _clock.UtcNow
        };

        _db.WorkoutSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        await _scorer.Score(session, cancellationToken);

        return new CreateSessionResult(session.Id);
    }
}
