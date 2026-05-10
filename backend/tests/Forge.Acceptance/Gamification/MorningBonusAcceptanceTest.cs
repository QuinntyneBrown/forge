// Acceptance Test
// Traces to: BT-023 (Morning bonus), L2-019
// Description: When a session's StartedAt (converted to the user's local time
// zone) falls within [MorningWindowStart, MorningWindowEnd], the scorer
// appends a second ledger row of +25 (MorningBonus). Sessions outside the
// window receive only the Base row.

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

public class MorningBonusAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public MorningBonusAcceptanceTest()
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
    public async Task Session_inside_window_writes_base_and_morning_bonus_rows()
    {
        var (client, auth) = await Register();

        // 05:12 in America/New_York is well within the default window 05:00-07:30.
        var localStart = new DateTime(2026, 5, 1, 5, 12, 0, DateTimeKind.Unspecified);
        var nyOffset = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
            .GetUtcOffset(localStart);
        var startedAt = new DateTimeOffset(localStart, nyOffset);

        await PostSession(client, auth.AccessToken, startedAt, 22);

        var ledger = await LedgerFor(auth.UserId);
        Assert.Equal(2, ledger.Count);
        Assert.Contains(ledger, l => l.Reason == PointsLedgerReason.Base && l.Points == 44);
        Assert.Contains(ledger, l => l.Reason == PointsLedgerReason.MorningBonus && l.Points == 25);
    }

    [Fact]
    public async Task Session_outside_window_writes_only_base_row()
    {
        var (client, auth) = await Register();

        // 09:00 in America/New_York is outside the default window 05:00-07:30.
        var localStart = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var nyOffset = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
            .GetUtcOffset(localStart);
        var startedAt = new DateTimeOffset(localStart, nyOffset);

        await PostSession(client, auth.AccessToken, startedAt, 22);

        var ledger = await LedgerFor(auth.UserId);
        Assert.Single(ledger);
        Assert.Equal(PointsLedgerReason.Base, ledger[0].Reason);
        Assert.Equal(44, ledger[0].Points);
    }

    private async Task<(HttpClient client, AuthResponse auth)> Register()
    {
        var client = _factory!.CreateClient();
        var email = $"morningbonus+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Morning", "Bonus", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return (client, auth!);
    }

    private static async Task PostSession(HttpClient client, string token, DateTimeOffset startedAt, int durationMinutes)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(new CreateSessionRequest(
                EquipmentType.Treadmill, startedAt, durationMinutes, 2.5m, 145, 240, "test"))
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<PointsLedger>> LedgerFor(Guid userId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        return await db.PointsLedger.Where(l => l.UserId == userId).ToListAsync();
    }
}
