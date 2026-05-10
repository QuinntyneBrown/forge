// Acceptance Test
// Traces to: BT-016 (Record current weight), L2-015
// Description: A signed-in user POSTs current weight readings. Each call
// appends a WeightEntry row with a server-stamped timestamp. Two posts on the
// same day persist both rows so the dashboard can compute month-to-date
// progress from the most recent.

using System.Net;
using System.Net.Http.Headers;
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

namespace Forge.Acceptance.Profile;

public class RecordCurrentWeightAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public RecordCurrentWeightAcceptanceTest()
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
    private record RecordWeightRequest(decimal WeightLb);

    [Fact]
    public async Task Posting_two_weights_appends_two_rows_with_timestamps()
    {
        var client = _factory!.CreateClient();
        var email = $"weight+{Guid.NewGuid():N}@forgefit.app";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Weight", "Test", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        await Post(client, registered!.AccessToken, 195.4m);
        await Post(client, registered.AccessToken, 194.8m);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var entries = await db.WeightEntries
            .Where(w => w.UserId == registered.UserId)
            .OrderBy(w => w.RecordedAt)
            .ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal(195.4m, entries[0].WeightLb);
        Assert.Equal(194.8m, entries[1].WeightLb);
        Assert.True(entries[0].RecordedAt <= entries[1].RecordedAt);
    }

    [Fact]
    public async Task Posting_a_zero_weight_returns_400_via_FluentValidation()
    {
        var client = _factory!.CreateClient();
        var email = $"weight-bad+{Guid.NewGuid():N}@forgefit.app";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Weight", "Bad", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        using var bad = new HttpRequestMessage(HttpMethod.Post, "/api/profile/weight")
        {
            Content = JsonContent.Create(new RecordWeightRequest(0m))
        };
        bad.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);
        var response = await client.SendAsync(bad);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task Post(HttpClient client, string token, decimal weightLb)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/profile/weight")
        {
            Content = JsonContent.Create(new RecordWeightRequest(weightLb))
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(message);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
