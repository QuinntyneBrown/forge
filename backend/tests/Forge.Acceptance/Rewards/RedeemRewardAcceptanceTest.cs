// Acceptance Test
// Traces to: BT-027 (Redeem reward), L2-021
// Description: A signed-in user redeems a catalog reward. The handler
// recomputes the user's balance from PointsLedger; if sufficient, it inserts
// a RewardRedemption row and a -cost ledger row, returning 200. If
// insufficient, it returns 400 with title "INSUFFICIENT_POINTS" and no
// state change.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

public class RedeemRewardAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public RedeemRewardAcceptanceTest()
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
    private record RedeemResponse(Guid RedemptionId, int RemainingBalance);

    [Fact]
    public async Task Sufficient_balance_returns_200_decrements_balance_creates_redemption_row()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        // Seed a Base ledger row of 1000 points so we can buy the cheapest
        // seeded reward (Smoothie, 200 points).
        Guid rewardId;
        await using (var seed = new AppDbContext(DbOptions()))
        {
            seed.PointsLedger.Add(new PointsLedger
            {
                Id = Guid.NewGuid(),
                UserId = auth.UserId,
                Reason = PointsLedgerReason.Base,
                Points = 1000,
                Description = "Seed",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();

            var smoothie = await seed.RewardCatalogItems
                .OrderBy(r => r.SortOrder)
                .FirstAsync(r => r.IsActive);
            rewardId = smoothie.Id;
        }

        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/rewards/{rewardId}/redeem");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(post);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RedeemResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.RedemptionId);
        Assert.Equal(800, body.RemainingBalance);

        await using var verify = new AppDbContext(DbOptions());
        var redemption = await verify.RewardRedemptions
            .SingleAsync(r => r.UserId == auth.UserId && r.RewardCatalogItemId == rewardId);
        Assert.Equal(200, redemption.CostPoints);

        var ledger = await verify.PointsLedger
            .Where(l => l.UserId == auth.UserId)
            .ToListAsync();
        Assert.Contains(ledger, l => l.Reason == PointsLedgerReason.Redemption
                                     && l.Points == -200
                                     && l.RedemptionId == redemption.Id);
        Assert.Equal(800, ledger.Sum(l => l.Points));
    }

    [Fact]
    public async Task Insufficient_balance_returns_400_with_INSUFFICIENT_POINTS_and_no_state_change()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        Guid expensiveRewardId;
        await using (var seed = new AppDbContext(DbOptions()))
        {
            // Seed only 50 points — not enough for any seeded reward.
            seed.PointsLedger.Add(new PointsLedger
            {
                Id = Guid.NewGuid(),
                UserId = auth.UserId,
                Reason = PointsLedgerReason.Base,
                Points = 50,
                Description = "Seed",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();

            var expensive = await seed.RewardCatalogItems
                .OrderByDescending(r => r.CostPoints)
                .FirstAsync(r => r.IsActive);
            expensiveRewardId = expensive.Id;
        }

        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/rewards/{expensiveRewardId}/redeem");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(post);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INSUFFICIENT_POINTS", problem.GetProperty("title").GetString());

        await using var verify = new AppDbContext(DbOptions());
        var redemptions = await verify.RewardRedemptions
            .Where(r => r.UserId == auth.UserId)
            .CountAsync();
        Assert.Equal(0, redemptions);

        var balance = await verify.PointsLedger
            .Where(l => l.UserId == auth.UserId)
            .SumAsync(l => l.Points);
        Assert.Equal(50, balance);
    }

    private DbContextOptions<AppDbContext> DbOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"redeem+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Redeem", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }
}
