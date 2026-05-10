using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class SignInCommandHandler : IRequestHandler<SignInCommand, AuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenIssuer _tokens;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IAuditLogger _audit;

    public SignInCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenIssuer tokens,
        IRefreshTokenStore refreshTokens,
        IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _audit = audit;
    }

    public async Task<AuthResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            await _audit.WriteAsync(
                "sign-in.failure",
                user?.Id,
                new { Email = normalizedEmail },
                cancellationToken);
            throw new InvalidCredentialsException();
        }

        var accessToken = _tokens.Issue(user);
        var refresh = await _refreshTokens.IssueAsync(user.Id, familyId: null, cancellationToken);
        await _audit.WriteAsync("sign-in.success", user.Id, new { user.Email }, cancellationToken);
        return new AuthResult(accessToken, refresh.RawToken, user.Id, user.Email, user.Role);
    }
}
