// Acceptance Test
// Traces to: BT-015 (Profile migration + update), L2-005, L2-014, L2-016, L2-017,
//            L2-026, L2-027
// Description: A signed-in user PUTs profile updates. GET /api/me reflects
// every change. Changing email to an address that already exists returns 409.

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

public class UpdateProfileAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public UpdateProfileAcceptanceTest()
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
    private record CurrentUserResponse(
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
        string MorningWindowStart,
        string MorningWindowEnd,
        string KitchenClosedStart,
        string KitchenClosedEnd,
        bool KitchenNudgeEnabled,
        bool MorningReminderEnabled,
        bool LeaderboardOptIn
    );
    private record UpdateProfileRequest(
        string Email,
        string FirstName,
        string LastName,
        string Units,
        string TimeZoneId,
        int DailyActiveCaloriesTarget,
        int DailyWorkoutMinutesTarget
    );

    [Fact]
    public async Task PUT_profile_persists_every_field_and_GET_me_reflects_them()
    {
        var client = _factory!.CreateClient();
        var email = $"profile+{Guid.NewGuid():N}@forgefit.app";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Quinn", "Forge", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        // Defaults from migration: GET /api/me before any update.
        var beforeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        beforeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);
        var beforeResponse = await client.SendAsync(beforeRequest);
        beforeResponse.EnsureSuccessStatusCode();
        var before = await beforeResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(before);
        Assert.Equal("Imperial", before!.Units);
        Assert.Equal(1500, before.DailyActiveCaloriesTarget);
        Assert.Equal(60, before.DailyWorkoutMinutesTarget);
        Assert.Equal(20, before.MonthlyWeightGoalLb);
        Assert.Equal("05:00:00", before.MorningWindowStart);
        Assert.Equal("07:30:00", before.MorningWindowEnd);
        Assert.Equal("20:00:00", before.KitchenClosedStart);
        Assert.Equal("06:00:00", before.KitchenClosedEnd);
        Assert.True(before.KitchenNudgeEnabled);
        Assert.True(before.MorningReminderEnabled);
        Assert.False(before.LeaderboardOptIn);

        // Update mutable fields.
        var newEmail = $"profile-renamed+{Guid.NewGuid():N}@forgefit.app";
        var update = new UpdateProfileRequest(
            Email: newEmail,
            FirstName: "Quinntyne",
            LastName: "Brown",
            Units: "Metric",
            TimeZoneId: "America/Toronto",
            DailyActiveCaloriesTarget: 1800,
            DailyWorkoutMinutesTarget: 75);

        var putRequest = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(update)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var putResponse = await client.SendAsync(putRequest);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // GET /api/me reflects the updates.
        var afterRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        afterRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var afterResponse = await client.SendAsync(afterRequest);
        afterResponse.EnsureSuccessStatusCode();
        var after = await afterResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(after);
        Assert.Equal(newEmail, after!.Email);
        Assert.Equal("Quinntyne", after.FirstName);
        Assert.Equal("Brown", after.LastName);
        Assert.Equal("Metric", after.Units);
        Assert.Equal("America/Toronto", after.TimeZoneId);
        Assert.Equal(1800, after.DailyActiveCaloriesTarget);
        Assert.Equal(75, after.DailyWorkoutMinutesTarget);
    }

    [Fact]
    public async Task PUT_profile_with_an_already_used_email_returns_409()
    {
        var client = _factory!.CreateClient();

        var firstEmail = $"first+{Guid.NewGuid():N}@forgefit.app";
        var secondEmail = $"second+{Guid.NewGuid():N}@forgefit.app";

        var registerFirst = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(firstEmail, "First", "User", "ForgeFit!2026"));
        registerFirst.EnsureSuccessStatusCode();

        var registerSecond = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(secondEmail, "Second", "User", "ForgeFit!2026"));
        registerSecond.EnsureSuccessStatusCode();
        var second = await registerSecond.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(second);

        // Try to update second user's email to first user's email.
        var update = new UpdateProfileRequest(
            Email: firstEmail,
            FirstName: "Second",
            LastName: "User",
            Units: "Imperial",
            TimeZoneId: "America/New_York",
            DailyActiveCaloriesTarget: 1500,
            DailyWorkoutMinutesTarget: 60);

        var putRequest = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(update)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", second!.AccessToken);
        var putResponse = await client.SendAsync(putRequest);
        Assert.Equal(HttpStatusCode.Conflict, putResponse.StatusCode);
    }
}
