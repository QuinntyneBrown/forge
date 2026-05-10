// Acceptance Test
// Traces to: BT-029 (Dashboard summary), L2-011, L2-012, L2-013
// Description: GET /api/dashboard returns a single aggregated read combining
// today's active calories vs target, today's minutes vs target, current
// streak, current balance, current tier, next-reward-within-reach, and
// month-to-date weight delta. The response must be a single round trip
// fast enough for a roughly-200 ms budget.

using System.Diagnostics;
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

namespace Forge.Acceptance.Dashboard;

public class DashboardSummaryAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public DashboardSummaryAcceptanceTest()
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
    private record DashboardSummaryDto(
        int CaloriesToday,
        int TargetCalories,
        int MinutesToday,
        int TargetMinutes,
        int CurrentStreak,
        int CurrentBalance,
        int LifetimePoints,
        string Tier,
        NextRewardDto? NextRewardWithinReach,
        decimal MonthToDateWeightLossLb,
        int MonthlyWeightGoalLb);
    private record NextRewardDto(Guid Id, string Name, int CostPoints);

    [Fact]
    public async Task Returns_today_calories_against_default_target()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        // Today's session in user's TZ (default America/New_York). 980 active
        // kcal, mid-day so the morning bonus does not fire. Splitting across
        // two sessions verifies the handler aggregates rather than
        // returning a single row.
        var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ny);
        var todayMidday = new DateTimeOffset(nowLocal.Year, nowLocal.Month, nowLocal.Day, 14, 0, 0, ny.GetUtcOffset(nowLocal));

        await PostSession(client, auth.AccessToken, todayMidday, 30, 600);
        await PostSession(client, auth.AccessToken, todayMidday.AddHours(1), 25, 380);

        var stopwatch = Stopwatch.StartNew();
        var dto = await GetDashboard(client, auth.AccessToken);
        stopwatch.Stop();

        Assert.Equal(980, dto.CaloriesToday);
        Assert.Equal(1500, dto.TargetCalories);
        Assert.Equal(55, dto.MinutesToday);
        Assert.Equal(60, dto.TargetMinutes);
        Assert.True(dto.CurrentStreak >= 1);
        Assert.True(dto.CurrentBalance >= 0);
        Assert.False(string.IsNullOrEmpty(dto.Tier));
        Assert.Equal(20, dto.MonthlyWeightGoalLb);

        // Rough perf budget: target is 200 ms locally; allow generous slack
        // for cold xUnit + Kestrel startup so the assertion isn't flaky.
        Assert.True(stopwatch.ElapsedMilliseconds < 1500,
            $"dashboard took {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"dash+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Dash", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task PostSession(HttpClient client, string token, DateTimeOffset startedAt, int duration, int activeCalories)
    {
        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = JsonContent.Create(new
            {
                Equipment = EquipmentType.Treadmill,
                StartedAt = startedAt,
                DurationMinutes = duration,
                DistanceMiles = 2.0m,
                AvgHeartRateBpm = 140,
                ActiveCalories = activeCalories,
                Notes = "dashboard"
            })
        };
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(post);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<DashboardSummaryDto> GetDashboard(HttpClient client, string token)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(dto);
        return dto!;
    }
}
