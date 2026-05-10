// Acceptance Test
// Traces to: BT-003 (Sign-out), L2-003, L2-033
// Description: Register a user, sign out (revoking the issued refresh token's
// family), attempt to refresh -> 401.

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

public class SignOutAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public SignOutAcceptanceTest()
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
                services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_connectionString));
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
    private record RefreshRequest(string RefreshToken);
    private record SignOutRequest(string RefreshToken);

    [Fact]
    public async Task Sign_out_revokes_the_refresh_token_family_and_blocks_subsequent_refresh()
    {
        var client = _factory!.CreateClient();

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"signout+{Guid.NewGuid():N}@forgefit.app", "Sign", "Out", "ForgeFit!2026"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        // Sign out using the issued refresh token. Endpoint is [Authorize], so
        // the access token must be attached as a Bearer credential.
        using var signOutMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/sign-out")
        {
            Content = JsonContent.Create(new SignOutRequest(registered!.RefreshToken))
        };
        signOutMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var signOut = await client.SendAsync(signOutMessage);
        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);

        // Attempting to refresh with the original token now returns 401 because
        // the family was revoked by sign-out.
        var refresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
