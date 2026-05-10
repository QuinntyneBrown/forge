namespace Forge.Application.Gamification;

public static class ScoringConstants
{
    public const int BasePointsPerMinute = 2;
    public const int MorningBonusPoints = 25;

    public const decimal StreakStepPerDay = 0.01m;
    public const decimal StreakMultiplierCap = 1.50m;

    public static readonly IReadOnlyList<TierThreshold> TierThresholds = new[]
    {
        new TierThreshold("Iron",        0),
        new TierThreshold("Bronze",   1000),
        new TierThreshold("Silver",   2500),
        new TierThreshold("Forged Iron", 5000),
        new TierThreshold("Gold",    10000),
        new TierThreshold("Platinum", 25000)
    };
}

public record TierThreshold(string Name, int MinLifetimePoints);
