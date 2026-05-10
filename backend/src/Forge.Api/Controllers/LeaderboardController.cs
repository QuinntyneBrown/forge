using Forge.Application.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaderboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntryDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var entries = await _mediator.Send(new ListLeaderboardQuery(page, pageSize), cancellationToken);
        return Ok(entries);
    }
}
