using System.Security.Cryptography;
using System.Text;
using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class RefreshTokenStore : IRefreshTokenStore
{
    private const int RawTokenBytes = 32;
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(14);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public RefreshTokenStore(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IssuedRefreshToken> IssueAsync(
        Guid userId,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken();
        var hash = ComputeHash(rawToken);
        var now = _clock.UtcNow;

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            FamilyId = familyId ?? Guid.NewGuid(),
            IssuedAt = now,
            ExpiresAt = now.Add(DefaultLifetime)
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(rawToken, token);
    }

    public async Task<RefreshToken?> ConsumeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(rawToken);
        var now = _clock.UtcNow;

        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return null;
        }

        if (token.RevokedAt.HasValue || token.ExpiresAt <= now)
        {
            // Token is revoked or expired — refuse without rotating.
            return null;
        }

        if (token.ConsumedAt.HasValue)
        {
            // Reuse of a consumed token: revoke the entire family per L2-033 and refuse.
            await RevokeFamilyAsync(token.FamilyId, cancellationToken);
            return null;
        }

        token.ConsumedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var members = await _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.RevokedAt = now;
        }
        if (members.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeByPresentedTokenAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return;
        }
        await RevokeFamilyAsync(token.FamilyId, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
        if (tokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawTokenBytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ComputeHash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
