using Forge.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.FirstName, request.LastName, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult<AuthResult>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var command = new SignInCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOut([FromBody] SignOutRequest request, CancellationToken cancellationToken)
    {
        var command = new SignOutCommand(request.RefreshToken);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestPasswordResetCommand(request.Email);
        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(
        [FromBody] ConfirmPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPasswordResetCommand(request.Token, request.NewPassword);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
