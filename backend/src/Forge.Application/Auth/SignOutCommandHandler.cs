using Forge.Application.Abstractions;
using MediatR;

namespace Forge.Application.Auth;

public class SignOutCommandHandler : IRequestHandler<SignOutCommand>
{
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;

    public SignOutCommandHandler(
        IRefreshTokenStore refreshTokens,
        ICurrentUser currentUser,
        IAuditLogger audit)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokens.RevokeByPresentedTokenAsync(request.RefreshToken, cancellationToken);
        await _audit.WriteAsync("sign-out", _currentUser.UserId, payload: null, cancellationToken);
    }
}
