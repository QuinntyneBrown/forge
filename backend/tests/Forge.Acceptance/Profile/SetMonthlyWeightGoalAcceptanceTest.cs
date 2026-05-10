// Acceptance Test
// Traces to: BT-017 (Set monthly weight goal), L2-014
// Description: A signed-in user PUTs a monthly weight-loss goal in pounds.
// Valid range is 1..30 lb/month inclusive. The value persists on the user
// record and is reflected by GET /api/me. Out-of-range values return 400.

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

public class SetMonthlyWeightGoalAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public SetMonthlyWeightGoalAcceptanceTest()
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
    private record SetWeightGoalRequest(int MonthlyWeightGoalLb);
    private record CurrentUserDto(
        Guid Id,
        string Email,
        string Role,
        string FirstName,
        string LastName,
        string Units,
        string TimeZoneId,
        int DailyActiveCaloriesTarget,
        int DailyWorkoutMinutesTarget,
        int MonthlyWeightGoalLb);

    [Fact]
    public async Task Valid_goal_persists_and_reflects_on_me()
    {
        var (client, token) = await Register();

        var put = await Put(client, token, new SetWeightGoalRequest(15));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        using var getMe = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        getMe.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResponse = await client.SendAsync(getMe);
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(me);
        Assert.Equal(15, me!.MonthlyWeightGoalLb);
    }

    [Fact]
    public async Task Goal_below_one_returns_400()
    {
        var (client, token) = await Register();
        var response = await Put(client, token, new SetWeightGoalRequest(0));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Goal_above_thirty_returns_400()
    {
        var (client, token) = await Register();
        var response = await Put(client, token, new SetWeightGoalRequest(31));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpClient client, string token)> Register()
    {
        var client = _factory!.CreateClient();
        var email = $"goal+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Goal", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        return (client, auth!.AccessToken);
    }

    private static async Task<HttpResponseMessage> Put(HttpClient client, string token, SetWeightGoalRequest body)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, "/api/profile/weight-goal")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(message);
    }
}
