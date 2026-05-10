// Acceptance Test
// Traces to: BT-026 (List rewards catalog), L2-021
// Description: GET /api/rewards returns active catalog items in stable
// SortOrder. Inactive items are hidden so the user only sees options that
// are currently redeemable.

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

public class ListRewardsAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public ListRewardsAcceptanceTest()
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
    private record RewardItemDto(Guid Id, string Name, string Description, int CostPoints, int SortOrder);

    [Fact]
    public async Task Lists_active_catalog_items_and_hides_inactive_ones()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        // Inactivate one seeded row directly so we can assert the query filters it out.
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        Guid hiddenId;
        await using (var db = new AppDbContext(dbOptions))
        {
            var item = await db.RewardCatalogItems.OrderBy(r => r.SortOrder).FirstAsync();
            item.IsActive = false;
            hiddenId = item.Id;
            await db.SaveChangesAsync();
        }

        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/rewards");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<RewardItemDto[]>();
        Assert.NotNull(items);
        Assert.True(items!.Length >= 6, $"expected at least 6 active rewards, got {items.Length}");
        Assert.DoesNotContain(items, r => r.Id == hiddenId);

        // Stable sort by SortOrder ascending.
        var sorted = items.OrderBy(r => r.SortOrder).ToArray();
        Assert.Equal(sorted, items);
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"rewards+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Rewards", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }
}
