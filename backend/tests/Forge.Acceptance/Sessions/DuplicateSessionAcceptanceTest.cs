// Acceptance Test
// Traces to: BT-011 (Duplicate session), L2-009
// Description: Duplicating a workout session inserts a copy with the same
// equipment/duration/etc. but stamped at "now" via IClock and scored fresh
// against the new state.

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

namespace Forge.Acceptance.Sessions;

public class DuplicateSessionAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public DuplicateSessionAcceptanceTest()
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
    private record DuplicateSessionResponse(Guid Id);

    [Fact]
    public async Task Duplicating_creates_a_new_session_today_and_writes_fresh_base_score()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        // Original session: 5 days ago, 22 min treadmill, mid-day NY (no morning bonus).
        var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var fiveDaysAgoLocal = new DateTime(2026, 4, 25, 14, 0, 0, DateTimeKind.Unspecified);
        var originalStartedAt = new DateTimeOffset(fiveDaysAgoLocal, ny.GetUtcOffset(fiveDaysAgoLocal));

        var originalId = await PostSession(client, auth.AccessToken, new CreateSessionRequest(
            EquipmentType.Treadmill, originalStartedAt, 22, 2.5m, 145, 240, "shakeout"));

        using var dup = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{originalId}/duplicate");
        dup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var dupResponse = await client.SendAsync(dup);
        dupResponse.EnsureSuccessStatusCode();
        var duplicate = await dupResponse.Content.ReadFromJsonAsync<DuplicateSessionResponse>();
        Assert.NotNull(duplicate);
        Assert.NotEqual(originalId, duplicate!.Id);

        await using var db = new AppDbContext(DbOptions());
        var copied = await db.WorkoutSessions.SingleAsync(s => s.Id == duplicate.Id);
        Assert.Equal(EquipmentType.Treadmill, copied.Equipment);
        Assert.Equal(22, copied.DurationMinutes);
        Assert.Equal(2.5m, copied.DistanceMiles);
        Assert.Equal(145, copied.AvgHeartRateBpm);
        Assert.Equal(240, copied.ActiveCalories);
        Assert.Equal("shakeout", copied.Notes);

        // The duplicate should be stamped at "today" — i.e. > 1 day after the original.
        Assert.True(copied.StartedAt > originalStartedAt.AddDays(1),
            $"expected duplicate StartedAt > original+1d, got {copied.StartedAt} vs original {originalStartedAt}");

        // Fresh +44 Base ledger row attached to the duplicate session id.
        var ledger = await db.PointsLedger
            .Where(l => l.SessionId == duplicate.Id)
            .ToListAsync();
        Assert.Contains(ledger, l => l.Reason == PointsLedgerReason.Base && l.Points == 44);
    }

    private DbContextOptions<AppDbContext> DbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"dupsess+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Dup", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task<Guid> PostSession(HttpClient client, string token, CreateSessionRequest body)
    {
        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(body)
        };
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(post);
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        Assert.NotNull(session);
        return session!.Id;
    }
}
