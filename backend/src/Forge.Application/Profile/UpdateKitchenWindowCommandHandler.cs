using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class UpdateKitchenWindowCommandHandler : IRequestHandler<UpdateKitchenWindowCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateKitchenWindowCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateKitchenWindowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        user.KitchenClosedStart = request.Start;
        user.KitchenClosedEnd = request.End;
        user.KitchenNudgeEnabled = request.NudgeEnabled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
