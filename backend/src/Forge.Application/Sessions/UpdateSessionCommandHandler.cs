using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Sessions;

public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPointsScorer _scorer;

    public UpdateSessionCommandHandler(IAppDbContext db, ICurrentUser currentUser, IPointsScorer scorer)
    {
        _db = db;
        _currentUser = currentUser;
        _scorer = scorer;
    }

    public async Task Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var session = await _db.WorkoutSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId, cancellationToken)
            ?? throw new SessionNotFoundException(request.Id);

        var materialChanged = session.Equipment != request.Equipment
            || session.StartedAt != request.StartedAt
            || session.DurationMinutes != request.DurationMinutes;

        if (materialChanged)
        {
            await _scorer.Refund(session, cancellationToken);
        }

        session.Equipment = request.Equipment;
        session.StartedAt = request.StartedAt;
        session.DurationMinutes = request.DurationMinutes;
        session.DistanceMiles = request.DistanceMiles;
        session.AvgHeartRateBpm = request.AvgHeartRateBpm;
        session.ActiveCalories = request.ActiveCalories;
        session.Notes = request.Notes;
        await _db.SaveChangesAsync(cancellationToken);

        if (materialChanged)
        {
            await _scorer.Score(session, cancellationToken);
        }
    }
}
