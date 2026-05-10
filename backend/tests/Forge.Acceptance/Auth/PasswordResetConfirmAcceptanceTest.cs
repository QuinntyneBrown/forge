// Acceptance Test
// Traces to: BT-005 (Password reset confirm), L2-004, L2-031, L2-033
// Description: Submitting a valid reset token + new password updates the
// stored password hash, revokes the user's existing refresh tokens, and
// allows sign-in with the new password. A reused or expired token returns
// 400 and does not change the password.

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

public class PasswordResetConfirmAcceptanceTest : IAsyncLifetime
{
    private static readonly DateTimeOffset PinnedNow = new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly FakeClock _clock = new(PinnedNow);
    private readonly CapturingPasswordResetEmailSender _emailSender = new();
    private WebApplicationFactory<Program>? _factory;

    public PasswordResetConfirmAcceptanceTest()
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
                Replace<DbContextOptions<AppDbContext>>(services);
                services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(_connectionString));

                Replace<IClock>(services);
                services.AddSingleton<IClock>(_clock);

                Replace<IPasswordResetEmailSender>(services);
                services.AddSingleton<IPasswordResetEmailSender>(_emailSender);
            });
        });

        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
    }

    private static void Replace<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
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
    private record RequestPasswordResetRequest(string Email);
    private record ConfirmPasswordResetRequest(string Token, string NewPassword);
    private record SignInRequest(string Email, string Password);
    private record RefreshRequest(string RefreshToken);

    [Fact]
    public async Task Valid_token_updates_password_revokes_refresh_tokens_and_lets_user_sign_in_with_the_new_password()
    {
        var client = _factory!.CreateClient();

        var email = $"reset+{Guid.NewGuid():N}@forgefit.app";
        const string OldPassword = "ForgeFit!2026";
        const string NewPassword = "ZebraQuokka!9z!2026";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Reset", "Confirm", OldPassword));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);

        var resetRequest = await client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new RequestPasswordResetRequest(email));
        Assert.Equal(HttpStatusCode.Accepted, resetRequest.StatusCode);
        var rawToken = _emailSender.LastToken;
        Assert.False(string.IsNullOrEmpty(rawToken));

        var confirm = await client.PostAsJsonAsync(
            "/api/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(rawToken!, NewPassword));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        // Old password no longer works.
        var oldSignIn = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, OldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldSignIn.StatusCode);

        // New password works.
        var newSignIn = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, NewPassword));
        Assert.Equal(HttpStatusCode.OK, newSignIn.StatusCode);

        // The pre-confirm refresh token is now revoked (refresh-token family
        // revocation per L2-033 + L2-004 ac 3).
        var refresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Reused_token_returns_400_and_password_does_not_change()
    {
        var client = _factory!.CreateClient();

        var email = $"reset-reuse+{Guid.NewGuid():N}@forgefit.app";
        const string OldPassword = "ForgeFit!2026";
        const string NewPassword = "ZebraQuokka!9z!2026";
        const string Other = "OtherPass!2026X";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Reset", "Reuse", OldPassword));
        register.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new RequestPasswordResetRequest(email));
        var rawToken = _emailSender.LastToken!;

        var first = await client.PostAsJsonAsync(
            "/api/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(rawToken, NewPassword));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/api/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(rawToken, Other));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        // Confirm the reuse did not silently change the password to Other.
        var asOther = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, Other));
        Assert.Equal(HttpStatusCode.Unauthorized, asOther.StatusCode);

        var asNew = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, NewPassword));
        Assert.Equal(HttpStatusCode.OK, asNew.StatusCode);
    }

    [Fact]
    public async Task Expired_token_returns_400()
    {
        var client = _factory!.CreateClient();

        var email = $"reset-expire+{Guid.NewGuid():N}@forgefit.app";
        const string OldPassword = "ForgeFit!2026";
        const string NewPassword = "ZebraQuokka!9z!2026";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Reset", "Expire", OldPassword));
        register.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new RequestPasswordResetRequest(email));
        var rawToken = _emailSender.LastToken!;

        // Advance past the 30-minute TTL.
        _clock.Advance(TimeSpan.FromMinutes(31));

        var confirm = await client.PostAsJsonAsync(
            "/api/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(rawToken, NewPassword));
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);

        // The original password still works.
        var oldSignIn = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequest(email, OldPassword));
        Assert.Equal(HttpStatusCode.OK, oldSignIn.StatusCode);
    }
}
