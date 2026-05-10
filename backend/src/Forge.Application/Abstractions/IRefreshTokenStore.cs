using Forge.Domain;

namespace Forge.Application.Abstractions;

public interface IRefreshTokenStore
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, Guid? familyId, CancellationToken cancellationToken);
    Task<RefreshToken?> ConsumeAsync(string rawToken, CancellationToken cancellationToken);
    Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    Task RevokeByPresentedTokenAsync(string rawToken, CancellationToken cancellationToken);
}
