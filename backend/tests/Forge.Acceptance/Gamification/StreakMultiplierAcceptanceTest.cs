// Acceptance Test
// Traces to: BT-024 (Streak multiplier), L2-020
// Description: PointsScorer counts the user's distinct session calendar dates
// (in their TZ) ending at IClock.TodayInTimeZone. Multiplier =
// min(1.50, 1.00 + 0.01 * consecutiveDays). When floor(basePoints * (mult-1))
// is positive, a StreakMultiplier ledger row is appended. Skipping a day
// resets the streak.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Forge.Application.Abstractions;
using Forge.Acceptance.Auth;
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

public class StreakMultiplierAcceptanceTest : IAsyncLifetime
{
    // Day-1 anchor: 2026-04-01 14:00 America/New_York (EDT, UTC-4) = 18:00 UTC.
    // Mid-day so the morning window 05:00-07:30 never triggers.
    private static readonly DateTimeOffset Day1Utc = new(2026, 4, 1, 18, 0, 0, TimeSpan.Zero);

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly FakeClock _clock = new(Day1Utc);
    private WebApplicationFactory<Program>? _factory;

    public StreakMultiplierAcceptanceTest()
    {
        _connectionString =
            $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
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

                var clockDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IClock));
                if (clockDescriptor is not null)
                {
                    services.Remove(clockDescriptor);
                }
                services.AddSingleton<IClock>(_clock);
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
    public async Task Seven_consecutive_days_then_skip_then_resume()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);
        var sessionIds = new List<Guid>();

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            sessionIds.Add(await PostSession(client, auth.AccessToken, _clock.UtcNow));
            _clock.Advance(TimeSpan.FromDays(1));
        }

        var ledgerAfterDay7 = await LedgerFor(auth.UserId);
        var streakRowsAfterDay7 = ledgerAfterDay7
            .Where(l => l.Reason == PointsLedgerReason.StreakMultiplier)
            .ToList();
        Assert.Single(streakRowsAfterDay7);
        Assert.Equal(sessionIds[6], streakRowsAfterDay7[0].SessionId);
        Assert.Equal(3, streakRowsAfterDay7[0].Points);

        // Skip a day, then post again. consecutiveDays for the new session is 1
        // because the immediately-prior calendar day has no session.
        _clock.Advance(TimeSpan.FromDays(1));
        var skipResetSessionId = await PostSession(client, auth.AccessToken, _clock.UtcNow);

        var ledgerAfterSkip = await LedgerFor(auth.UserId);
        var streakRowForSkipResetSession = ledgerAfterSkip
            .Where(l => l.SessionId == skipResetSessionId
                        && l.Reason == PointsLedgerReason.StreakMultiplier)
            .ToList();
        Assert.Empty(streakRowForSkipResetSession);
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"streak+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Streak", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task<Guid> PostSession(HttpClient client, string token, DateTimeOffset startedAt)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(new CreateSessionRequest(
                EquipmentType.Treadmill, startedAt, 22, 2.5m, 145, 240, "streak"))
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        Assert.NotNull(session);
        return session!.Id;
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
