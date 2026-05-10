using Forge.Application.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get(CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(summary);
    }
}
