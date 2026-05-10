using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class SetMonthlyWeightGoalCommandHandler : IRequestHandler<SetMonthlyWeightGoalCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SetMonthlyWeightGoalCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetMonthlyWeightGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        user.MonthlyWeightGoalLb = request.MonthlyWeightGoalLb;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
