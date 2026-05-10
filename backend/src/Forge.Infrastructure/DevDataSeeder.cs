using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Forge.Infrastructure;

public class DevDataSeeder
{
    public const string DevEmail = "dev@forge.local";
    public const string DevPassword = "DevPassword123!";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(AppDbContext db, IPasswordHasher hasher, IClock clock, ILogger<DevDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == DevEmail, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = DevEmail,
            FirstName = "Dev",
            LastName = "User",
            PasswordHash = _hasher.Hash(DevPassword),
            Role = "User",
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded development user {Email}.", DevEmail);
    }
}
