// Acceptance Test
// Traces to: BT-032 (Notification dispatcher), L2-025, L2-026
// Description: A periodic dispatcher scans users whose morning window or
// kitchen-closed boundary is about to fire (in their TZ) and calls
// INotificationSender. The MVP sender is a no-op that emits a structured
// log line. The dispatch is exposed as a public scan-once method so the
// test drives one tick deterministically against a FakeClock.

using System.Net.Http.Json;
using Forge.Application.Abstractions;
using Forge.Acceptance.Auth;
using Forge.Application.Notifications;
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

namespace Forge.Acceptance.Notifications;

public class NotificationDispatcherAcceptanceTest : IAsyncLifetime
{
    // Pin the clock to 14:00 America/New_York on 2026-04-01 (EDT, UTC-4).
    private static readonly DateTimeOffset Pinned = new(2026, 4, 1, 18, 0, 0, TimeSpan.Zero);

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly FakeClock _clock = new(Pinned);
    private readonly CapturingLogProvider _logs = new();
    private WebApplicationFactory<Program>? _factory;

    public NotificationDispatcherAcceptanceTest()
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
            builder.ConfigureLogging(b => b.AddProvider(_logs));
            builder.ConfigureTestServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }
                services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(_connectionString));

                var clockDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IClock));
                if (clockDescriptor is not null)
                {
                    services.Remove(clockDescriptor);
                }
                services.AddSingleton<IClock>(_clock);
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

    [Fact]
    public async Task Morning_window_starting_in_one_minute_emits_a_notification_on_dispatch()
    {
        var client = _factory!.CreateClient();
        var registered = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"notif+{Guid.NewGuid():N}@forgefit.app", "Notif", "Test", "ForgeFit!2026"));
        registered.EnsureSuccessStatusCode();
        var auth = await registered.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Pin user's morning window start to (clock-local + 1 minute) so the
        // dispatcher catches it as "about to fire".
        await using (var db = new AppDbContext(dbOptions))
        {
            var user = await db.Users.SingleAsync(u => u.Id == auth!.UserId);
            // Pinned clock is 14:00 NY. Set morning window 14:01-14:30 so the
            // start is exactly one minute away.
            user.MorningWindowStart = new TimeOnly(14, 1);
            user.MorningWindowEnd = new TimeOnly(14, 30);
            user.MorningReminderEnabled = true;
            user.TimeZoneId = "America/New_York";
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        Assert.Contains(_logs.Lines, line => line.Contains("notification.morning"));
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
