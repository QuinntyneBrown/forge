using System.Security.Cryptography;
using System.Text;
using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private const int RawTokenBytes = 32;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IPasswordResetEmailSender _sender;

    public RequestPasswordResetCommandHandler(
        IAppDbContext db,
        IClock clock,
        IPasswordResetEmailSender sender)
    {
        _db = db;
        _clock = clock;
        _sender = sender;
    }

    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            // No user — still return 202 to avoid account enumeration (L2-004 ac 1).
            return;
        }

        var rawToken = GenerateRawToken();
        var hash = ComputeHash(rawToken);
        var now = _clock.UtcNow;

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            IssuedAt = now,
            ExpiresAt = now.Add(TokenLifetime)
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _sender.SendAsync(normalizedEmail, rawToken, cancellationToken);
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
