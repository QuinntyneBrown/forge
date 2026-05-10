using Forge.Application.Rewards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rewards")]
public class RewardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RewardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RewardItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListRewardsQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id:guid}/redeem")]
    public async Task<ActionResult<RedeemRewardResult>> Redeem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RedeemRewardCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/tier")]
    public async Task<ActionResult<TierDto>> Tier(CancellationToken cancellationToken)
    {
        var tier = await _mediator.Send(new GetCurrentTierQuery(), cancellationToken);
        return Ok(tier);
    }
}
