using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IJwtTokenIssuer _accessTokens;
    private readonly IAuditLogger _audit;

    public RefreshTokenCommandHandler(
        IAppDbContext db,
        IRefreshTokenStore refreshTokens,
        IJwtTokenIssuer accessTokens,
        IAuditLogger audit)
    {
        _db = db;
        _refreshTokens = refreshTokens;
        _accessTokens = accessTokens;
        _audit = audit;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var consumed = await _refreshTokens.ConsumeAsync(request.RefreshToken, cancellationToken);
        if (consumed is null)
        {
            await _audit.WriteAsync("token.refresh.failure", userId: null, payload: null, cancellationToken);
            throw new InvalidCredentialsException();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == consumed.UserId, cancellationToken);
        if (user is null)
        {
            await _audit.WriteAsync("token.refresh.failure", consumed.UserId, payload: null, cancellationToken);
            throw new InvalidCredentialsException();
        }

        var rotated = await _refreshTokens.IssueAsync(user.Id, consumed.FamilyId, cancellationToken);
        var accessToken = _accessTokens.Issue(user);
        await _audit.WriteAsync("token.refresh.success", user.Id, payload: null, cancellationToken);
        return new AuthResult(accessToken, rotated.RawToken, user.Id, user.Email, user.Role);
    }
}
