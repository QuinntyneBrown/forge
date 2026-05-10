using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Rewards;

public class ListRewardsQueryHandler : IRequestHandler<ListRewardsQuery, IReadOnlyList<RewardItemDto>>
{
    private readonly IAppDbContext _db;

    public ListRewardsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RewardItemDto>> Handle(ListRewardsQuery request, CancellationToken cancellationToken)
    {
        return await _db.RewardCatalogItems
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .Select(r => new RewardItemDto(r.Id, r.Name, r.Description, r.CostPoints, r.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
