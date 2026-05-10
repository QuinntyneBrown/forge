using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Rewards;

public class RedeemRewardCommandHandler : IRequestHandler<RedeemRewardCommand, RedeemRewardResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RedeemRewardCommandHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<RedeemRewardResult> Handle(RedeemRewardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var reward = await _db.RewardCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RewardId && r.IsActive, cancellationToken)
            ?? throw new RewardNotFoundException(request.RewardId);

        var balance = await _db.PointsLedger
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .SumAsync(l => (int?)l.Points, cancellationToken) ?? 0;

        if (balance < reward.CostPoints)
        {
            throw new InsufficientPointsException(balance, reward.CostPoints);
        }

        var now = _clock.UtcNow;
        var redemption = new RewardRedemption
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RewardCatalogItemId = reward.Id,
            CostPoints = reward.CostPoints,
            RedeemedAt = now
        };
        _db.RewardRedemptions.Add(redemption);

        _db.PointsLedger.Add(new PointsLedger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RedemptionId = redemption.Id,
            Reason = PointsLedgerReason.Redemption,
            Points = -reward.CostPoints,
            Description = $"Redemption — {reward.Name}",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new RedeemRewardResult(redemption.Id, balance - reward.CostPoints);
    }
}
