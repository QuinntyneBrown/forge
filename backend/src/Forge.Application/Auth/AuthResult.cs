namespace Forge.Application.Auth;

public record AuthResult(string AccessToken, Guid UserId, string Email, string Role);
