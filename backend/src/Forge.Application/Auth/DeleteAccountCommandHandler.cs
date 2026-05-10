using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;

    public DeleteAccountCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IRefreshTokenStore refreshTokens,
        IClock clock,
        IAuditLogger audit)
    {
        _db = db;
        _currentUser = currentUser;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _audit = audit;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException();
        }

        user.IsDeleted = true;
        user.DeletedAt = _clock.UtcNow;
        user.Email = $"deleted+{user.Id:N}@forgefit.local";
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.PasswordHash = string.Empty;

        await _db.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _audit.WriteAsync("account.delete", user.Id, payload: null, cancellationToken);
    }
}
