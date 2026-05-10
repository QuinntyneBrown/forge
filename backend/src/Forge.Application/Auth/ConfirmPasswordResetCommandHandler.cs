using System.Security.Cryptography;
using System.Text;
using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;

    public ConfirmPasswordResetCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IRefreshTokenStore refreshTokens,
        IClock clock,
        IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _audit = audit;
    }

    public async Task Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(request.Token);
        var now = _clock.UtcNow;

        var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.ConsumedAt.HasValue || token.ExpiresAt <= now)
        {
            await _audit.WriteAsync("password-reset.failure", token?.UserId, payload: null, cancellationToken);
            throw new InvalidPasswordResetTokenException();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null)
        {
            await _audit.WriteAsync("password-reset.failure", token.UserId, payload: null, cancellationToken);
            throw new InvalidPasswordResetTokenException();
        }

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        token.ConsumedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _audit.WriteAsync("password-reset.success", user.Id, payload: null, cancellationToken);
    }

    private static string ComputeHash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
