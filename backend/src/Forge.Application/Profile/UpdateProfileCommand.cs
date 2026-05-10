using MediatR;

namespace Forge.Application.Profile;

public record UpdateProfileCommand(
    string Email,
    string FirstName,
    string LastName,
    string Units,
    string TimeZoneId,
    int DailyActiveCaloriesTarget,
    int DailyWorkoutMinutesTarget
) : IRequest;
