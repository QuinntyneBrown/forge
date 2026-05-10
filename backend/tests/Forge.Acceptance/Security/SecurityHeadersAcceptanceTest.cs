// Acceptance Test
// Traces to: BT-033 (Security headers middleware), L2-052, L2-049
// Description: Every response carries the documented security headers:
// Content-Security-Policy, X-Content-Type-Options, Referrer-Policy. The
// HSTS header is gated to non-development so local dev tooling isn't
// pinned to HTTPS.

using Forge.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forge.Acceptance.Security;

public class SecurityHeadersAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public SecurityHeadersAcceptanceTest()
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

    [Fact]
    public async Task GET_health_carries_security_headers()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var csp));
        var cspValue = string.Join(";", csp!);
        Assert.Contains("default-src 'self'", cspValue);
        Assert.Contains("script-src 'self'", cspValue);
        Assert.Contains("frame-ancestors 'none'", cspValue);
        Assert.Contains("object-src 'none'", cspValue);

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var nosniff));
        Assert.Equal("nosniff", string.Join(",", nosniff!));

        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrer));
        Assert.Equal("no-referrer", string.Join(",", referrer!));
    }
}
