using Forge.Application.Sessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateSessionResult>> Create([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSessionCommand(
            request.Equipment,
            request.StartedAt,
            request.DurationMinutes,
            request.DistanceMiles,
            request.AvgHeartRateBpm,
            request.ActiveCalories,
            request.Notes);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetSessionByIdQuery(id), cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }
}
