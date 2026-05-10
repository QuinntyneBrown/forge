namespace Forge.Api.Controllers;

public record UpdateProfileRequest(
    string Email,
    string FirstName,
    string LastName,
    string Units,
    string TimeZoneId,
    int DailyActiveCaloriesTarget,
    int DailyWorkoutMinutesTarget
);
