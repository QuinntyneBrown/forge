// Acceptance Test
// Traces to: BT-028 (Get current tier), L2-022
// Description: GET /api/tier returns the user's current tier name based on
// cumulative lifetime points (positive ledger entries only — redemptions
// don't demote you). Crossing a threshold via a new session promotes the
// user immediately.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Forge.Domain;
using Forge.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forge.Acceptance.Rewards;

public class GetCurrentTierAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public GetCurrentTierAcceptanceTest()
    {
        _connectionString =
            AcceptanceSqlServer.ForDatabase(_databaseName);
    }

    public async Task InitializeAsync()
    {
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
                services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(_connectionString));
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
            AcceptanceSqlServer.MasterConnectionString);
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
    private record TierDto(string Name, int LifetimePoints, string? NextTierName, int? PointsToNextTier);
    private record CreateSessionRequest(
        EquipmentType Equipment,
        DateTimeOffset StartedAt,
        int DurationMinutes,
        decimal? DistanceMiles,
        int? AvgHeartRateBpm,
        int ActiveCalories,
        string? Notes);

    [Fact]
    public async Task At_4999_lifetime_points_user_is_silver_then_5001_promotes_to_forged_iron()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        await using (var seed = new AppDbContext(DbOptions()))
        {
            seed.PointsLedger.Add(new PointsLedger
            {
                Id = Guid.NewGuid(),
                UserId = auth.UserId,
                Reason = PointsLedgerReason.Base,
                Points = 4999,
                Description = "Seed",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var first = await GetTier(client, auth.AccessToken);
        Assert.Equal("Silver", first.Name);
        Assert.Equal(4999, first.LifetimePoints);

        // Mid-day NY local so the morning bonus does not fire.
        var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var localStart = new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Unspecified);
        var startedAt = new DateTimeOffset(localStart, ny.GetUtcOffset(localStart));

        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(new CreateSessionRequest(
                EquipmentType.Treadmill, startedAt, 1, 0.10m, 130, 12, "tier push"))
        };
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(post);
        response.EnsureSuccessStatusCode();

        var second = await GetTier(client, auth.AccessToken);
        Assert.Equal("Forged Iron", second.Name);
        Assert.True(second.LifetimePoints >= 5001);
    }

    private DbContextOptions<AppDbContext> DbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"tier+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Tier", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task<TierDto> GetTier(HttpClient client, string token)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/tier");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TierDto>();
        Assert.NotNull(dto);
        return dto!;
    }
}
