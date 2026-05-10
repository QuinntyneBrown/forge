// Acceptance Test
// Traces to: BT-030 (List leaderboard), L2-027
// Description: GET /api/leaderboard lists users who have opted in, ordered by
// current point balance descending. The caller themselves is always
// included regardless of their opt-in flag — the leaderboard is for
// comparing against the field, not for hiding your own row.

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

namespace Forge.Acceptance.Leaderboard;

public class ListLeaderboardAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public ListLeaderboardAcceptanceTest()
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
    private record SetLeaderboardOptInRequest(bool LeaderboardOptIn);
    private record LeaderboardEntryDto(Guid UserId, string FirstName, string LastName, int Points, int Rank);

    [Fact]
    public async Task User_A_visible_to_user_B_only_after_opting_in()
    {
        var clientA = _factory!.CreateClient();
        var clientB = _factory!.CreateClient();
        var a = await Register(clientA, "leadA");
        var b = await Register(clientB, "leadB");

        // Seed both users with some balance so they would both rank.
        await using (var seed = new AppDbContext(DbOptions()))
        {
            seed.PointsLedger.AddRange(
                new PointsLedger
                {
                    Id = Guid.NewGuid(),
                    UserId = a.UserId,
                    Reason = PointsLedgerReason.Base,
                    Points = 500,
                    Description = "seed",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new PointsLedger
                {
                    Id = Guid.NewGuid(),
                    UserId = b.UserId,
                    Reason = PointsLedgerReason.Base,
                    Points = 300,
                    Description = "seed",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            await seed.SaveChangesAsync();
        }

        // A is opted out by default. B asks for the leaderboard. B sees self
        // (always included) but not A.
        var initial = await GetLeaderboard(clientB, b.AccessToken);
        Assert.DoesNotContain(initial, e => e.UserId == a.UserId);
        Assert.Contains(initial, e => e.UserId == b.UserId);

        // A opts in, then B re-fetches.
        var optIn = await PutOptIn(clientA, a.AccessToken, true);
        optIn.EnsureSuccessStatusCode();

        var afterOptIn = await GetLeaderboard(clientB, b.AccessToken);
        Assert.Contains(afterOptIn, e => e.UserId == a.UserId);
        // A has more points than B, so A ranks above B.
        var aRank = afterOptIn.Single(e => e.UserId == a.UserId).Rank;
        var bRank = afterOptIn.Single(e => e.UserId == b.UserId).Rank;
        Assert.True(aRank < bRank);

        // A toggles opt-in off again. A still sees themselves; B no longer
        // sees A.
        var optOut = await PutOptIn(clientA, a.AccessToken, false);
        optOut.EnsureSuccessStatusCode();

        var fromB = await GetLeaderboard(clientB, b.AccessToken);
        Assert.DoesNotContain(fromB, e => e.UserId == a.UserId);

        var fromA = await GetLeaderboard(clientA, a.AccessToken);
        Assert.Contains(fromA, e => e.UserId == a.UserId);
    }

    private DbContextOptions<AppDbContext> DbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

    private async Task<AuthResponse> Register(HttpClient client, string firstName)
    {
        var email = $"lead+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, firstName, "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task<HttpResponseMessage> PutOptIn(HttpClient client, string token, bool optIn)
    {
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/profile/leaderboard-opt-in")
        {
            Content = JsonContent.Create(new SetLeaderboardOptInRequest(optIn))
        };
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(put);
    }

    private static async Task<LeaderboardEntryDto[]> GetLeaderboard(HttpClient client, string token)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/leaderboard");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var entries = await response.Content.ReadFromJsonAsync<LeaderboardEntryDto[]>();
        Assert.NotNull(entries);
        return entries!;
    }
}
