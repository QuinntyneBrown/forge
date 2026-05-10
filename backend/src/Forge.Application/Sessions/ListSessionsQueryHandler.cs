using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Sessions;

public class ListSessionsQueryHandler : IRequestHandler<ListSessionsQuery, SessionPage>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ListSessionsQueryHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<SessionPage> Handle(ListSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var query = _db.WorkoutSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (request.Equipment is { } equipment)
        {
            query = query.Where(s => s.Equipment == equipment);
        }

        if (request.Range != SessionRange.All)
        {
            var now = _clock.UtcNow;
            var since = request.Range switch
            {
                SessionRange.Today => now.Date,
                SessionRange.Week => now.AddDays(-7),
                SessionRange.Month => now.AddDays(-30),
                _ => DateTimeOffset.MinValue
            };
            query = query.Where(s => s.StartedAt >= since);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(s => s.Notes != null && EF.Functions.Like(s.Notes, $"%{search}%"));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(cancellationToken);

        return new SessionPage(items, page, pageSize, total);
    }
}
