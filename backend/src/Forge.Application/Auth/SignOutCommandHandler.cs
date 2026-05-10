using Forge.Application.Abstractions;
using MediatR;

namespace Forge.Application.Auth;

public class SignOutCommandHandler : IRequestHandler<SignOutCommand>
{
    private readonly IRefreshTokenStore _refreshTokens;

    public SignOutCommandHandler(IRefreshTokenStore refreshTokens)
    {
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokens.RevokeByPresentedTokenAsync(request.RefreshToken, cancellationToken);
    }
}
