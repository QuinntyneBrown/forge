using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenIssuer _tokens;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;

    public RegisterCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenIssuer tokens,
        IRefreshTokenStore refreshTokens,
        IClock clock,
        IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _audit = audit;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (existing)
        {
            throw new EmailAlreadyRegisteredException(normalizedEmail);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = "User",
            CreatedAt = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _tokens.Issue(user);
        var refresh = await _refreshTokens.IssueAsync(user.Id, familyId: null, cancellationToken);
        await _audit.WriteAsync("register.success", user.Id, new { user.Email }, cancellationToken);
        return new AuthResult(accessToken, refresh.RawToken, user.Id, user.Email, user.Role);
    }
}
