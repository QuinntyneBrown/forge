// Acceptance Test
// Traces to: BT-034 (Structured logging + secret redaction filter), L2-043, L2-051
// Description: Every handled request emits a request.handled log entry with
// structured properties traceId + userId + method + path + status. A
// redacting wrapper scrubs known-sensitive keys (password, accessToken,
// refreshToken, passwordResetToken, Authorization). A sign-in attempt
// with a distinctive password leaves zero log matches for that
// password text.

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

namespace Forge.Acceptance.Logging;

public class StructuredLoggingAcceptanceTest : IAsyncLifetime
{
    private const string DistinctivePassword = "ZX-very-distinctive-password-1739!";

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly CapturingProvider _capture = new();
    private WebApplicationFactory<Program>? _factory;

    public StructuredLoggingAcceptanceTest()
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
            builder.ConfigureLogging(b => b.AddProvider(_capture));
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
        var live = await client.GetAsync("/health");
        live.EnsureSuccessStatusCode();
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

    [Fact]
    public async Task Sign_in_with_distinctive_password_does_not_appear_in_log_output()
    {
        var client = _factory!.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"log+{Guid.NewGuid():N}@forgefit.app", "Log", "Test", DistinctivePassword));
        register.EnsureSuccessStatusCode();

        Assert.DoesNotContain(_capture.Entries, e => e.Formatted.Contains(DistinctivePassword));
        Assert.DoesNotContain(_capture.Entries, e => e.Properties.Any(kv =>
            kv.Value is string s && s.Contains(DistinctivePassword)));
    }

    [Fact]
    public async Task Each_request_emits_a_request_handled_entry_with_traceId_method_path_status()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var entry = _capture.Entries.LastOrDefault(e => e.Formatted.Contains("request.handled"));
        Assert.NotNull(entry);

        var props = entry!.Properties.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.True(props.ContainsKey("Method"), "Method property missing");
        Assert.True(props.ContainsKey("Path"), "Path property missing");
        Assert.True(props.ContainsKey("Status"), "Status property missing");
        Assert.True(props.ContainsKey("TraceId"), "TraceId property missing");
        Assert.True(props.ContainsKey("UserId"), "UserId property missing");
    }

    [Fact]
    public void Sensitive_property_keys_are_redacted_when_logged_in_state()
    {
        // Resolve a logger from the live app's services and emit a synthetic
        // log line with sensitive keys to prove the redaction wrapper.
        var logger = _factory!.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RedactionProbe");
        logger.LogInformation(
            "probe email={Email} password={password} accessToken={accessToken} refreshToken={refreshToken} authorization={Authorization}",
            "user@forgefit.app",
            DistinctivePassword,
            "header.payload.sig",
            "RT_" + Guid.NewGuid().ToString("N"),
            "Bearer xyz");

        var probeEntry = _capture.Entries.LastOrDefault(e => e.Formatted.Contains("probe email="));
        Assert.NotNull(probeEntry);
        Assert.DoesNotContain(DistinctivePassword, probeEntry!.Formatted);
        Assert.DoesNotContain("header.payload.sig", probeEntry.Formatted);
        Assert.DoesNotContain("Bearer xyz", probeEntry.Formatted);

        // The user@forgefit.app value should NOT be redacted (Email is not a
        // sensitive key) — proves we're scrubbing by key, not blindly.
        Assert.Contains("user@forgefit.app", probeEntry.Formatted);
    }

    private record CapturedEntry(string Formatted, IReadOnlyList<KeyValuePair<string, object?>> Properties);

    private class CapturingProvider : ILoggerProvider
    {
        public List<CapturedEntry> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);
        public void Dispose() { }

        private class CapturingLogger : ILogger
        {
            private readonly CapturingProvider _provider;
            public CapturingLogger(CapturingProvider provider) => _provider = provider;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var formatted = formatter(state, exception);
                var props = new List<KeyValuePair<string, object?>>();
                if (state is IReadOnlyList<KeyValuePair<string, object?>> kvs)
                {
                    foreach (var kv in kvs)
                    {
                        props.Add(kv);
                    }
                }

                lock (_provider.Entries)
                {
                    _provider.Entries.Add(new CapturedEntry(formatted, props));
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
