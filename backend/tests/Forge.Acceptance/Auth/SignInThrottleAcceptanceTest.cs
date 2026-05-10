// Acceptance Test
// Traces to: BT-007 (Sign-in throttling), L2-034
// Description: 5 failed sign-ins for the same email within 15 minutes lock the
// account. The 6th attempt returns 429 even with the correct password. After
// the FakeClock advances past the 15-minute window, the correct password
// works again.

using System.Net;
using System.Net.Http.Json;
using Forge.Application.Abstractions;
using Forge.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forge.Acceptance.Auth;

public class SignInThrottleAcceptanceTest : IAsyncLifetime
{
    private static readonly DateTimeOffset PinnedNow = new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
    private const string Password = "ForgeFit!2026";

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly FakeClock _clock = new(PinnedNow);
    private WebApplicationFactory<Program>? _factory;

    public SignInThrottleAcceptanceTest()
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
    private record SignInRequest(string Email, string Password);

    [Fact]
    public async Task Five_failures_lock_the_account_and_the_lock_clears_after_the_window()
    {
        var client = _factory!.CreateClient();
        var email = $"throttle+{Guid.NewGuid():N}@forgefit.app";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Throttle", "Test", Password));
        register.EnsureSuccessStatusCode();

        // 5 failed sign-ins within the same window.
        for (var i = 0; i < 5; i++)
        {
            var bad = await client.PostAsJsonAsync(
                "/api/auth/sign-in",
                new SignInRequest(email, "wrong-password!2026"));
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
            _clock.Advance(TimeSpan.FromSeconds(30));
        }

        // 6th attempt — even with the CORRECT password — returns 429.
        var locked = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, Password));
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);

        // Advance the clock past the 15-minute rolling window. The recorded
        // failures fall out of the window, so the correct password works again.
        _clock.Advance(TimeSpan.FromMinutes(16));
        var unlocked = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, Password));
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
    }
}
