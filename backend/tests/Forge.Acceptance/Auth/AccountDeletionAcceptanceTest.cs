// Acceptance Test
// Traces to: BT-006 (Account deletion), L2-006, L2-050, L2-033
// Description: A signed-in user invokes DELETE /api/me. The handler soft-
// deletes the user (IsDeleted=true), anonymizes FirstName/LastName/Email to
// a sentinel pattern, and revokes every refresh token for the account. A
// subsequent sign-in attempt with the original email returns 401 as if the
// account never existed (L2-006 ac 2). Pre-deletion refresh tokens are
// rejected too (L2-033 family revocation).

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

namespace Forge.Acceptance.Auth;

public class AccountDeletionAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public AccountDeletionAcceptanceTest()
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
    private record AuthResponse(string AccessToken, string RefreshToken, Guid UserId, string Email, string Role);
    private record SignInRequest(string Email, string Password);
    private record RefreshRequest(string RefreshToken);

    [Fact]
    public async Task Delete_account_soft_deletes_anonymizes_revokes_and_blocks_subsequent_sign_in()
    {
        var client = _factory!.CreateClient();
        var email = $"delete+{Guid.NewGuid():N}@forgefit.app";
        const string Password = "ForgeFit!2026";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Delete", "Test", Password));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        using var deleteMessage = new HttpRequestMessage(HttpMethod.Delete, "/api/me");
        deleteMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);
        var delete = await client.SendAsync(deleteMessage);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Sign-in with the original email returns 401 as if the account never existed.
        var signIn = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, Password));
        Assert.Equal(HttpStatusCode.Unauthorized, signIn.StatusCode);

        // Pre-deletion refresh token is rejected (family revoked).
        var refresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // Inspect the row directly: IsDeleted, anonymized fields, no PII.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new AppDbContext(options);
        var user = await db.Users.SingleAsync(u => u.Id == registered.UserId);
        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAt);
        Assert.NotEqual(email, user.Email);
        Assert.Contains("@forgefit.local", user.Email);
        Assert.NotEqual("Delete", user.FirstName);
        Assert.NotEqual("Test", user.LastName);
    }
}
