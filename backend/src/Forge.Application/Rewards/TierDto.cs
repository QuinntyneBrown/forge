namespace Forge.Application.Rewards;

public record TierDto(string Name, int LifetimePoints, string? NextTierName, int? PointsToNextTier);
