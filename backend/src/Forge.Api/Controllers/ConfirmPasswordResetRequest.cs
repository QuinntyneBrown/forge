namespace Forge.Api.Controllers;

public record ConfirmPasswordResetRequest(string Token, string NewPassword);
