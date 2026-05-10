// Acceptance Test
// Traces to: BT-022 (Points scoring foundation), L2-018, L2-021, L2-022
// Description: Creating a workout session triggers IPointsScorer, which writes
// a Base ledger row of (BasePointsPerMinute × DurationMinutes). The migration
// also seeds the rewards catalog so the redemption flow can stand on real
// data.

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

namespace Forge.Acceptance.Gamification;

public class PointsScorerAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public PointsScorerAcceptanceTest()
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
    private record CreateSessionRequest(
        EquipmentType Equipment,
        DateTimeOffset StartedAt,
        int DurationMinutes,
        decimal? DistanceMiles,
        int? AvgHeartRateBpm,
        int ActiveCalories,
        string? Notes);
    private record CreateSessionResponse(Guid Id);

    [Fact]
    public async Task Migration_seeds_rewards_catalog()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var count = await db.RewardCatalogItems.CountAsync();
        Assert.True(count >= 5, $"expected at least 5 catalog rows, got {count}");

        var hasSmoothie = await db.RewardCatalogItems.AnyAsync(r => r.Name.Contains("Smoothie"));
        Assert.True(hasSmoothie);
    }

    [Fact]
    public async Task Creating_22_minute_session_writes_44_base_ledger_row()
    {
        var client = _factory!.CreateClient();
        var email = $"scorer+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Scorer", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(new CreateSessionRequest(
                EquipmentType.Treadmill,
                DateTimeOffset.UtcNow,
                22,
                2.5m,
                145,
                240,
                "test session"))
        };
        create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await client.SendAsync(create);
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        Assert.NotNull(session);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var baseRows = await db.PointsLedger
            .Where(l => l.UserId == auth.UserId && l.Reason == PointsLedgerReason.Base)
            .ToListAsync();

        Assert.Single(baseRows);
        Assert.Equal(44, baseRows[0].Points);
        Assert.Equal(session!.Id, baseRows[0].SessionId);
    }
}
