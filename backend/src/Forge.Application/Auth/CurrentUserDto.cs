namespace Forge.Application.Auth;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role
);
