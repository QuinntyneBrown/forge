// Acceptance Test
// Traces to: BT-019 (Update kitchen window + nudge toggle), L2-017, L2-026
// Description: A signed-in user PUTs the kitchen-closed window and the nudge
// enabled toggle. The window typically spans midnight (e.g. 20:00 -> 06:00),
// so start > end is permitted; only start == end is rejected. Both fields
// persist and are reflected on GET /api/me.

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

public class UpdateKitchenWindowAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public UpdateKitchenWindowAcceptanceTest()
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
    private record UpdateKitchenWindowRequest(TimeOnly Start, TimeOnly End, bool NudgeEnabled);
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
    public async Task Updating_window_spanning_midnight_persists_and_toggles_nudge_off()
    {
        var (client, token) = await Register();

        var put = await Put(client, token, new UpdateKitchenWindowRequest(new TimeOnly(21, 0), new TimeOnly(5, 30), false));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var me = await GetMe(client, token);
        Assert.Equal(new TimeOnly(21, 0), me.KitchenClosedStart);
        Assert.Equal(new TimeOnly(5, 30), me.KitchenClosedEnd);
        Assert.False(me.KitchenNudgeEnabled);
    }

    [Fact]
    public async Task Equal_start_and_end_returns_400()
    {
        var (client, token) = await Register();
        var response = await Put(client, token, new UpdateKitchenWindowRequest(new TimeOnly(20, 0), new TimeOnly(20, 0), true));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpClient client, string token)> Register()
    {
        var client = _factory!.CreateClient();
        var email = $"kitchen+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Kitchen", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        return (client, auth!.AccessToken);
    }

    private static async Task<HttpResponseMessage> Put(HttpClient client, string token, UpdateKitchenWindowRequest body)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, "/api/profile/kitchen-window")
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
