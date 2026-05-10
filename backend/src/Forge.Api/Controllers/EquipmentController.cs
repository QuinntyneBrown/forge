using Forge.Application.Equipment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListEquipmentQuery(), cancellationToken);
        return Ok(items);
    }
}
