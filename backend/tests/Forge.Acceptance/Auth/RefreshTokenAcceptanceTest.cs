// Acceptance Test
// Traces to: BT-002 (Refresh token issuance + rotation), L2-002, L2-033
// Description: Register a user, exchange the issued refresh token once (succeeds),
// replay the consumed refresh token (401 + entire family revoked, so a later
// presentation of any sibling rotation also returns 401).

using System.Net;
using System.Net.Http.Json;
using Forge.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forge.Acceptance.Auth;

public class RefreshTokenAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public RefreshTokenAcceptanceTest()
    {
        _connectionString =
            $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
    }

    public async Task InitializeAsync()
    {
        // Apply migrations directly so the per-test database exists before the
        // factory boots. The factory's own MigrateAsync becomes a no-op against
        // the already-up-to-date schema.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
            });
            builder.ConfigureTestServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }
                services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_connectionString));
            });
        });

        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await using var conn = new SqlConnection(
            @"Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END";
        await cmd.ExecuteNonQueryAsync();
    }

    private record RegisterRequest(string Email, string FirstName, string LastName, string Password);
    private record AuthResponse(string AccessToken, string RefreshToken, Guid UserId, string Email, string Role);
    private record RefreshRequest(string RefreshToken);

    [Fact]
    public async Task Refresh_token_rotates_on_use_and_replay_revokes_the_family()
    {
        var client = _factory!.CreateClient();

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"refresh+{Guid.NewGuid():N}@forgefit.app", "Refresh", "Test", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);
        Assert.False(string.IsNullOrWhiteSpace(registered!.RefreshToken),
            "Register response must include a refresh token (BT-002).");

        // 1. Exchange the refresh token once — succeeds, returns a new pair.
        var firstRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        var rotated = await firstRefresh.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(registered.RefreshToken, rotated!.RefreshToken);

        // 2. Replay the consumed refresh token — the family is revoked.
        var replay = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // 3. The rotated (sibling) refresh token must also be revoked now.
        var siblingAfterReplay = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, siblingAfterReplay.StatusCode);

        // 4. Verify the family is fully revoked at the database level.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var familyTokens = await db.RefreshTokens
            .Where(t => t.UserId == registered.UserId)
            .ToListAsync();
        Assert.NotEmpty(familyTokens);
        Assert.All(familyTokens, t => Assert.True(
            t.RevokedAt.HasValue || t.ConsumedAt.HasValue,
            $"Token {t.Id} should be either consumed or revoked after replay."));
    }
}
