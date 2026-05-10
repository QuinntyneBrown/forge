using Forge.Application.Abstractions;
using Forge.Application.Gamification;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Rewards;

public class GetCurrentTierQueryHandler : IRequestHandler<GetCurrentTierQuery, TierDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentTierQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TierDto> Handle(GetCurrentTierQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // Lifetime = positive ledger entries. Refunds and Redemptions stay
        // negative and so are excluded from tier progression.
        var lifetimePoints = await _db.PointsLedger
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Points > 0)
            .SumAsync(l => (int?)l.Points, cancellationToken) ?? 0;

        var thresholds = ScoringConstants.TierThresholds;
        var currentIndex = 0;
        for (var i = 0; i < thresholds.Count; i++)
        {
            if (lifetimePoints >= thresholds[i].MinLifetimePoints)
            {
                currentIndex = i;
            }
            else
            {
                break;
            }
        }

        var current = thresholds[currentIndex];
        string? nextName = null;
        int? pointsToNext = null;
        if (currentIndex + 1 < thresholds.Count)
        {
            var next = thresholds[currentIndex + 1];
            nextName = next.Name;
            pointsToNext = next.MinLifetimePoints - lifetimePoints;
        }

        return new TierDto(current.Name, lifetimePoints, nextName, pointsToNext);
    }
}
