namespace Forge.Domain;

public class SignInAttempt
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
