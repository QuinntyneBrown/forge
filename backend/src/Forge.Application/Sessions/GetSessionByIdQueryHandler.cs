using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Sessions;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, SessionDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetSessionByIdQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionDto?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var userId = _currentUser.UserId.Value;

        return await _db.WorkoutSessions
            .Where(s => s.Id == request.Id && s.UserId == userId)
            .Select(s => new SessionDto(
                s.Id,
                s.Equipment,
                s.StartedAt,
                s.DurationMinutes,
                s.DistanceMiles,
                s.AvgHeartRateBpm,
                s.ActiveCalories,
                s.Notes,
                s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
