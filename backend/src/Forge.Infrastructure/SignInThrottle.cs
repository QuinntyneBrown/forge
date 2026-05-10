using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class SignInThrottle : ISignInThrottle
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public const int FailureThreshold = 5;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _http;

    public SignInThrottle(IAppDbContext db, IClock clock, IHttpContextAccessor http)
    {
        _db = db;
        _clock = clock;
        _http = http;
    }

    public async Task<ThrottleDecision> CheckAsync(string email, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var windowStart = now - Window;

        var failuresInWindow = await _db.SignInAttempts
            .Where(a => a.Email == email && !a.Success && a.OccurredAt >= windowStart)
            .CountAsync(cancellationToken);

        if (failuresInWindow < FailureThreshold)
        {
            return ThrottleDecision.Allow();
        }

        var oldestFailureInWindow = await _db.SignInAttempts
            .Where(a => a.Email == email && !a.Success && a.OccurredAt >= windowStart)
            .OrderBy(a => a.OccurredAt)
            .Select(a => (DateTimeOffset?)a.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        var unlockAt = (oldestFailureInWindow ?? now).Add(Window);
        var retryAfter = unlockAt - now;
        if (retryAfter < TimeSpan.Zero)
        {
            retryAfter = TimeSpan.Zero;
        }
        return ThrottleDecision.Lock(retryAfter);
    }

    public async Task RecordAttemptAsync(string email, bool success, CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress?.ToString();
        var userAgent = ctx?.Request.Headers.UserAgent.ToString();

        var attempt = new SignInAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            IpAddress = string.IsNullOrEmpty(ip) ? null : ip,
            UserAgent = string.IsNullOrEmpty(userAgent) ? null : userAgent,
            Success = success,
            OccurredAt = _clock.UtcNow
        };

        _db.SignInAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
