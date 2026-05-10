namespace Forge.Application.Dashboard;

public record DashboardSummaryDto(
    int CaloriesToday,
    int TargetCalories,
    int MinutesToday,
    int TargetMinutes,
    int CurrentStreak,
    int CurrentBalance,
    int LifetimePoints,
    string Tier,
    NextRewardDto? NextRewardWithinReach,
    decimal MonthToDateWeightLossLb,
    int MonthlyWeightGoalLb
);
