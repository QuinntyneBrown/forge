// Acceptance Test
// Traces to: BT-009 (List sessions with filters), L2-008
// Description: A signed-in user lists workout sessions through GET /api/sessions
// with optional filters: equipment, range (today | week | month | all), search,
// page, pageSize. The handler scopes to the current user, projects to SessionDto
// with .AsNoTracking(), and applies filters server-side.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Forge.Application.Sessions;
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

public class ListSessionsAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public ListSessionsAcceptanceTest()
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

    [Fact]
    public async Task Equipment_filter_returns_only_treadmill_sessions()
    {
        var (client, token) = await Register();
        var now = DateTimeOffset.UtcNow;
        await Seed(client, token, now);

        var response = await Get(client, token, "/api/sessions?equipment=Treadmill");
        var page = await response.Content.ReadFromJsonAsync<SessionPage>();
        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, s => Assert.Equal(EquipmentType.Treadmill, s.Equipment));
    }

    [Fact]
    public async Task Range_week_returns_only_sessions_within_last_seven_days()
    {
        var (client, token) = await Register();
        var now = DateTimeOffset.UtcNow;
        await Seed(client, token, now);

        var response = await Get(client, token, "/api/sessions?range=week");
        var page = await response.Content.ReadFromJsonAsync<SessionPage>();
        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, s => Assert.True(s.StartedAt >= now.AddDays(-7)));
        Assert.DoesNotContain(page.Items, s => s.StartedAt < now.AddDays(-7));
    }

    [Fact]
    public async Task Search_returns_only_sessions_whose_notes_match()
    {
        var (client, token) = await Register();
        var now = DateTimeOffset.UtcNow;
        await Seed(client, token, now);

        var response = await Get(client, token, "/api/sessions?search=zone");
        var page = await response.Content.ReadFromJsonAsync<SessionPage>();
        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, s =>
        {
            Assert.NotNull(s.Notes);
            Assert.Contains("zone", s.Notes!, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Other_users_sessions_are_not_returned()
    {
        var (client, tokenA) = await Register();
        var now = DateTimeOffset.UtcNow;
        await Seed(client, tokenA, now);

        var (_, tokenB) = await Register();
        var response = await Get(client, tokenB, "/api/sessions?range=all");
        var page = await response.Content.ReadFromJsonAsync<SessionPage>();
        Assert.NotNull(page);
        Assert.Empty(page!.Items);
    }

    private async Task<(HttpClient client, string token)> Register()
    {
        var client = _factory!.CreateClient();
        var email = $"sessions+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Sessions", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        return (client, auth!.AccessToken);
    }

    private static async Task Seed(HttpClient client, string token, DateTimeOffset now)
    {
        // 10 sessions: mix of equipment, mix of ages, mix of notes.
        var seeds = new[]
        {
            new CreateSessionRequest(EquipmentType.Treadmill,   now.AddDays(-1), 30, 3.0m, 145, 320, "easy zone 2"),
            new CreateSessionRequest(EquipmentType.Treadmill,   now.AddDays(-3), 45, 4.5m, 150, 480, "tempo"),
            new CreateSessionRequest(EquipmentType.Treadmill,   now.AddDays(-9), 25, 2.5m, 140, 270, "shakeout"),
            new CreateSessionRequest(EquipmentType.IndoorBike,  now.AddDays(-2), 40, null, 138, 360, "zone 3 intervals"),
            new CreateSessionRequest(EquipmentType.IndoorBike,  now.AddDays(-12),60, null, 130, 520, "endurance"),
            new CreateSessionRequest(EquipmentType.BenchPress,  now.AddDays(-1), 20, null, 110, 140, "5x5"),
            new CreateSessionRequest(EquipmentType.BenchPress,  now.AddDays(-5), 25, null, 115, 170, "back-off sets"),
            new CreateSessionRequest(EquipmentType.Elliptical,  now.AddDays(-2), 35, 3.0m, 135, 300, "warm-up zone 1"),
            new CreateSessionRequest(EquipmentType.Elliptical,  now.AddDays(-15),50, 4.0m, 140, 420, "long aerobic"),
            new CreateSessionRequest(EquipmentType.Elliptical,  now.AddDays(-31),30, 2.5m, 132, 260, "older session")
        };

        foreach (var seed in seeds)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
            {
                Content = JsonContent.Create(seed)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<HttpResponseMessage> Get(HttpClient client, string token, string url)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private record SessionPage(IReadOnlyList<SessionDto> Items, int Page, int PageSize, int Total);
}
