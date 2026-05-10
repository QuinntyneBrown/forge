using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class ListLeaderboardQueryHandler : IRequestHandler<ListLeaderboardQuery, IReadOnlyList<LeaderboardEntryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListLeaderboardQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> Handle(
        ListLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var callerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;

        var rows = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && (u.LeaderboardOptIn || u.Id == callerId))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                Points = _db.PointsLedger
                    .Where(l => l.UserId == u.Id)
                    .Sum(l => (int?)l.Points) ?? 0
            })
            .ToListAsync(cancellationToken);

        var ranked = rows
            .OrderByDescending(r => r.Points)
            .ThenBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .Select((r, i) => new LeaderboardEntryDto(r.Id, r.FirstName, r.LastName, r.Points, i + 1))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ranked;
    }
}
