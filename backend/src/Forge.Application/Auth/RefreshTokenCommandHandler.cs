using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IJwtTokenIssuer _accessTokens;

    public RefreshTokenCommandHandler(
        IAppDbContext db,
        IRefreshTokenStore refreshTokens,
        IJwtTokenIssuer accessTokens)
    {
        _db = db;
        _refreshTokens = refreshTokens;
        _accessTokens = accessTokens;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var consumed = await _refreshTokens.ConsumeAsync(request.RefreshToken, cancellationToken);
        if (consumed is null)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == consumed.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var rotated = await _refreshTokens.IssueAsync(user.Id, consumed.FamilyId, cancellationToken);
        var accessToken = _accessTokens.Issue(user);
        return new AuthResult(accessToken, rotated.RawToken, user.Id, user.Email, user.Role);
    }
}
