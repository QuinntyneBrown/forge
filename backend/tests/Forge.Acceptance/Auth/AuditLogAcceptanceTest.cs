// Acceptance Test
// Traces to: BT-008 (Security audit log), L2-035, L2-051
// Description: A failed sign-in writes one sign-in.failure audit row. A
// successful sign-in writes one sign-in.success row. The user's password
// never appears in any audit row's payload (L2-051 secret redaction).

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

public class AuditLogAcceptanceTest : IAsyncLifetime
{
    private const string DistinctivePassword = "ZebraQuokka!9z!2026";

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public AuditLogAcceptanceTest()
    {
        _connectionString =
            $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
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
            @"Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
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
    public async Task Sign_in_success_and_failure_both_write_audit_rows_without_leaking_the_password()
    {
        var client = _factory!.CreateClient();
        var email = $"audit+{Guid.NewGuid():N}@forgefit.app";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Audit", "Test", DistinctivePassword));
        register.EnsureSuccessStatusCode();

        var bad = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, "wrong-password!2026"));
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var good = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, DistinctivePassword));
        good.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var rows = await db.AuditLogs.OrderBy(a => a.OccurredAt).ToListAsync();

        Assert.Contains(rows, r => r.Event == "register.success");
        Assert.Contains(rows, r => r.Event == "sign-in.failure");
        Assert.Contains(rows, r => r.Event == "sign-in.success");

        // L2-051: password never appears in any audit row's payload.
        Assert.DoesNotContain(rows, r =>
            r.PayloadJson is not null && r.PayloadJson.Contains(DistinctivePassword));
    }
}
