using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class SetLeaderboardOptInCommandHandler : IRequestHandler<SetLeaderboardOptInCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SetLeaderboardOptInCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetLeaderboardOptInCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        user.LeaderboardOptIn = request.LeaderboardOptIn;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
