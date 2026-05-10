using Forge.Application.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Units,
            request.TimeZoneId,
            request.DailyActiveCaloriesTarget,
            request.DailyWorkoutMinutesTarget);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("weight")]
    public async Task<IActionResult> RecordWeight(
        [FromBody] RecordWeightRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RecordCurrentWeightCommand(request.WeightLb), cancellationToken);
        return NoContent();
    }

    [HttpPut("weight-goal")]
    public async Task<IActionResult> SetWeightGoal(
        [FromBody] SetWeightGoalRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetMonthlyWeightGoalCommand(request.MonthlyWeightGoalLb), cancellationToken);
        return NoContent();
    }

    [HttpPut("morning-window")]
    public async Task<IActionResult> UpdateMorningWindow(
        [FromBody] UpdateMorningWindowRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMorningWindowCommand(request.Start, request.End, request.ReminderEnabled),
            cancellationToken);
        return NoContent();
    }
}
