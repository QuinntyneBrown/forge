// Acceptance Test
// Traces to: BT-010 (Update session), L2-009
// Description: A signed-in user PUTs an existing session. When equipment,
// start time, or duration changes, the scorer refunds the prior ledger
// rows for the session and re-scores against the new state. When only
// non-material fields (e.g. notes) change, the ledger is untouched.

using System.Net;
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

public class UpdateSessionAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public UpdateSessionAcceptanceTest()
    {
        _connectionString =
            $@"Server=.\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
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
            @"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
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
    private record UpdateSessionRequest(
        EquipmentType Equipment,
        DateTimeOffset StartedAt,
        int DurationMinutes,
        decimal? DistanceMiles,
        int? AvgHeartRateBpm,
        int ActiveCalories,
        string? Notes);

    [Fact]
    public async Task Updating_duration_refunds_old_score_and_writes_new_base()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var localStart = new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Unspecified);
        var startedAt = new DateTimeOffset(localStart, ny.GetUtcOffset(localStart));

        var sessionId = await PostSession(client, auth.AccessToken, new CreateSessionRequest(
            EquipmentType.Treadmill, startedAt, 22, 2.5m, 145, 240, "original"));

        // After create: +44 Base only (mid-day so no morning bonus, day 1 so no streak row).
        var balanceBefore = await BalanceFor(auth.UserId);
        Assert.Equal(44, balanceBefore);

        // Update duration from 22 -> 30. Material change -> refund + re-score.
        var put = await PutSession(client, auth.AccessToken, sessionId, new UpdateSessionRequest(
            EquipmentType.Treadmill, startedAt, 30, 3.0m, 145, 320, "updated"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        await using var db = new AppDbContext(DbOptions());
        var rows = await db.PointsLedger
            .Where(l => l.SessionId == sessionId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        // Original Base, Refund row, new Base — three entries summing to the new score.
        Assert.True(rows.Count >= 3, $"expected ≥3 ledger rows after refund + rescore, got {rows.Count}");
        Assert.Contains(rows, r => r.Reason == PointsLedgerReason.Refund && r.Points == -44);
        Assert.Equal(60, rows.Sum(r => r.Points));
    }

    [Fact]
    public async Task Updating_only_notes_leaves_ledger_unchanged()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var localStart = new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Unspecified);
        var startedAt = new DateTimeOffset(localStart, ny.GetUtcOffset(localStart));

        var sessionId = await PostSession(client, auth.AccessToken, new CreateSessionRequest(
            EquipmentType.Treadmill, startedAt, 22, 2.5m, 145, 240, "original"));

        var put = await PutSession(client, auth.AccessToken, sessionId, new UpdateSessionRequest(
            EquipmentType.Treadmill, startedAt, 22, 2.5m, 145, 240, "edited notes only"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        await using var db = new AppDbContext(DbOptions());
        var rows = await db.PointsLedger
            .Where(l => l.SessionId == sessionId)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(PointsLedgerReason.Base, rows[0].Reason);
        Assert.Equal(44, rows[0].Points);

        var session = await db.WorkoutSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal("edited notes only", session.Notes);
    }

    private DbContextOptions<AppDbContext> DbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

    private async Task<int> BalanceFor(Guid userId)
    {
        await using var db = new AppDbContext(DbOptions());
        return await db.PointsLedger
            .Where(l => l.UserId == userId)
            .SumAsync(l => (int?)l.Points) ?? 0;
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"updsess+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Update", "Session", "ForgeFit!2026"));
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

    private static async Task<HttpResponseMessage> PutSession(HttpClient client, string token, Guid id, UpdateSessionRequest body)
    {
        using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/sessions/{id}")
        {
            Content = JsonContent.Create(body)
        };
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(put);
    }
}
