namespace Forge.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Profile columns added by BT-015.
    public string Units { get; set; } = "Imperial";
    public string TimeZoneId { get; set; } = "America/New_York";
    public int DailyActiveCaloriesTarget { get; set; } = 1500;
    public int DailyWorkoutMinutesTarget { get; set; } = 60;
    public int MonthlyWeightGoalLb { get; set; } = 20;
    public TimeOnly MorningWindowStart { get; set; } = new(5, 0);
    public TimeOnly MorningWindowEnd { get; set; } = new(7, 30);
    public TimeOnly KitchenClosedStart { get; set; } = new(20, 0);
    public TimeOnly KitchenClosedEnd { get; set; } = new(6, 0);
    public bool KitchenNudgeEnabled { get; set; } = true;
    public bool MorningReminderEnabled { get; set; } = true;
    public bool LeaderboardOptIn { get; set; }
}
