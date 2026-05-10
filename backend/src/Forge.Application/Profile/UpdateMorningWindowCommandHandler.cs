using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class UpdateMorningWindowCommandHandler : IRequestHandler<UpdateMorningWindowCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateMorningWindowCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateMorningWindowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        user.MorningWindowStart = request.Start;
        user.MorningWindowEnd = request.End;
        user.MorningReminderEnabled = request.ReminderEnabled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
