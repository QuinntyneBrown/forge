// Acceptance Test
// Traces to: BT-004 (Password reset request — request leg), L2-004
// Description: A reset request for an unknown email returns 202 and writes no
// PasswordResetTokens row. A reset request for a known email returns 202 and
// writes one PasswordResetTokens row with a TTL <= 30 minutes from issuance.
// Same status in both cases — no account enumeration.

using System.Net;
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

namespace Forge.Acceptance.Auth;

public class PasswordResetRequestAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public PasswordResetRequestAcceptanceTest()
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
    private record RequestPasswordResetRequest(string Email);

    [Fact]
    public async Task Reset_request_for_unknown_email_returns_202_without_writing_a_token()
    {
        var client = _factory!.CreateClient();

        var unknownEmail = $"unknown+{Guid.NewGuid():N}@forgefit.app";
        var response = await client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new RequestPasswordResetRequest(unknownEmail));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        Assert.Equal(0, await db.PasswordResetTokens.CountAsync());
    }

    [Fact]
    public async Task Reset_request_for_known_email_returns_202_and_writes_one_token_with_a_short_ttl()
    {
        var client = _factory!.CreateClient();

        var email = $"reset+{Guid.NewGuid():N}@forgefit.app";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Reset", "Test", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new RequestPasswordResetRequest(email));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var tokens = await db.PasswordResetTokens.ToListAsync();
        var token = Assert.Single(tokens);
        Assert.False(string.IsNullOrWhiteSpace(token.TokenHash));
        Assert.Null(token.ConsumedAt);

        var ttl = token.ExpiresAt - token.IssuedAt;
        Assert.True(ttl > TimeSpan.Zero, $"TTL must be positive; got {ttl}.");
        Assert.True(ttl <= TimeSpan.FromMinutes(30), $"TTL must be <= 30 minutes; got {ttl}.");
    }
}
