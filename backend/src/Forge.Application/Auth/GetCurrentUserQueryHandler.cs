using Forge.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new CurrentUserDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role,
                u.Units,
                u.TimeZoneId,
                u.DailyActiveCaloriesTarget,
                u.DailyWorkoutMinutesTarget,
                u.MonthlyWeightGoalLb,
                u.MorningWindowStart,
                u.MorningWindowEnd,
                u.KitchenClosedStart,
                u.KitchenClosedEnd,
                u.KitchenNudgeEnabled,
                u.MorningReminderEnabled,
                u.LeaderboardOptIn))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new UnauthorizedAccessException();
    }
}
