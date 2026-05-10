namespace Forge.Application.Auth;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Units,
    string TimeZoneId,
    int DailyActiveCaloriesTarget,
    int DailyWorkoutMinutesTarget,
    int MonthlyWeightGoalLb,
    TimeOnly MorningWindowStart,
    TimeOnly MorningWindowEnd,
    TimeOnly KitchenClosedStart,
    TimeOnly KitchenClosedEnd,
    bool KitchenNudgeEnabled,
    bool MorningReminderEnabled,
    bool LeaderboardOptIn
);
