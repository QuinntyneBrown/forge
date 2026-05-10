using Forge.Application.Abstractions;
using Forge.Application.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Profile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (normalizedEmail != user.Email)
        {
            var emailTaken = await _db.Users
                .AnyAsync(u => u.Id != userId && u.Email == normalizedEmail, cancellationToken);
            if (emailTaken)
            {
                throw new EmailAlreadyRegisteredException(normalizedEmail);
            }
            user.Email = normalizedEmail;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Units = request.Units;
        user.TimeZoneId = request.TimeZoneId;
        user.DailyActiveCaloriesTarget = request.DailyActiveCaloriesTarget;
        user.DailyWorkoutMinutesTarget = request.DailyWorkoutMinutesTarget;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
