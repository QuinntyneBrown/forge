using Forge.Application.HealthKit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/healthkit")]
public class HealthKitController : ControllerBase
{
    private readonly IMediator _mediator;

    public HealthKitController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestHealthKitSampleRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new IngestHealthKitSampleCommand(
                request.SampleType,
                request.Value,
                request.Unit,
                request.RecordedAt),
            cancellationToken);
        return Accepted();
    }
}
