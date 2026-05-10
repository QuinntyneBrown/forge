using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Sessions;

public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPointsScorer _scorer;

    public DeleteSessionCommandHandler(IAppDbContext db, ICurrentUser currentUser, IPointsScorer scorer)
    {
        _db = db;
        _currentUser = currentUser;
        _scorer = scorer;
    }

    public async Task Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var session = await _db.WorkoutSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId, cancellationToken)
            ?? throw new SessionNotFoundException(request.Id);

        // Compensating Refund row first so the audit trail captures the
        // pre-delete balance change before the session row goes away.
        await _scorer.Refund(session, cancellationToken);

        _db.WorkoutSessions.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
