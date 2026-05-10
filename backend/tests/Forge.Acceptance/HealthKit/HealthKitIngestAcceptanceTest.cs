// Acceptance Test
// Traces to: BT-031 (HealthKit ingest stub), L2-023
// Description: A signed-in user posts an Apple-Watch HealthKit sample. The
// route accepts the payload, returns 202 Accepted, and writes a structured
// log line "healthkit.ingest.deferred" via the deferred no-op
// IHealthKitIngest implementation. The full integration is out of MVP
// scope (BP1 §8).

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
using Microsoft.Extensions.Logging;
using Xunit;

namespace Forge.Acceptance.HealthKit;

public class HealthKitIngestAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly CapturingLogProvider _logs = new();
    private WebApplicationFactory<Program>? _factory;

    public HealthKitIngestAcceptanceTest()
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
            builder.ConfigureLogging(b => b.AddProvider(_logs));
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
    private record IngestRequest(string SampleType, decimal Value, string Unit, DateTimeOffset RecordedAt);

    [Fact]
    public async Task Posting_a_sample_returns_202_and_logs_a_deferred_marker()
    {
        var client = _factory!.CreateClient();
        var auth = await Register(client);

        var payload = new IngestRequest("ActiveEnergyBurned", 245.5m, "kcal", DateTimeOffset.UtcNow);
        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/healthkit/ingest")
        {
            Content = JsonContent.Create(payload)
        };
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(post);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        Assert.Contains(_logs.Lines, line => line.Contains("healthkit.ingest.deferred"));
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = $"hk+{Guid.NewGuid():N}@forgefit.app";
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "HK", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth!;
    }

    private class CapturingLogProvider : ILoggerProvider
    {
        public List<string> Lines { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Lines);
        public void Dispose() { }

        private class CapturingLogger : ILogger
        {
            private readonly List<string> _lines;
            public CapturingLogger(List<string> lines) => _lines = lines;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (_lines)
                {
                    _lines.Add(formatter(state, exception));
                }
            }

            private class NoopScope : IDisposable
            {
                public static readonly NoopScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
