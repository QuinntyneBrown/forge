// Acceptance Test
// Traces to: BT-020 (Set leaderboard opt-in), L2-027
// Description: A signed-in user PUTs the leaderboard opt-in flag. Default is
// false; toggling true persists and is reflected on GET /api/me. Toggling
// back to false also persists. The leaderboard surface itself (BT-030) will
// validate the row-visibility behaviour end-to-end once that slice ships.

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

public class SetLeaderboardOptInAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public SetLeaderboardOptInAcceptanceTest()
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
    private record CurrentUserDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string Units,
        string TimeZoneId,
        int DailyActiveCaloriesTarget,
        int DailyWorkoutMinutesTarget,
        int MonthlyWeightGoalLb,
        TimeOnly MorningWindowStart,
        TimeOnly MorningWindowEnd,
        TimeOnly KitchenClosedStart,
        TimeOnly KitchenClosedEnd,
        bool KitchenNudgeEnabled,
        bool MorningReminderEnabled,
        bool LeaderboardOptIn);

    [Fact]
    public async Task Default_is_false_and_toggle_on_then_off_round_trips()
    {
        var (client, token) = await Register();

        var initial = await GetMe(client, token);
        Assert.False(initial.LeaderboardOptIn);

        var on = await Put(client, token, new SetLeaderboardOptInRequest(true));
        Assert.Equal(HttpStatusCode.NoContent, on.StatusCode);
        Assert.True((await GetMe(client, token)).LeaderboardOptIn);

        var off = await Put(client, token, new SetLeaderboardOptInRequest(false));
        Assert.Equal(HttpStatusCode.NoContent, off.StatusCode);
        Assert.False((await GetMe(client, token)).LeaderboardOptIn);
    }

    private async Task<(HttpClient client, string token)> Register()
    {
        var client = _factory!.CreateClient();
        var email = $"leaderboard+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Lead", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        return (client, auth!.AccessToken);
    }

    private static async Task<HttpResponseMessage> Put(HttpClient client, string token, SetLeaderboardOptInRequest body)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, "/api/profile/leaderboard-opt-in")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(message);
    }

    private static async Task<CurrentUserDto> GetMe(HttpClient client, string token)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(me);
        return me!;
    }
}
