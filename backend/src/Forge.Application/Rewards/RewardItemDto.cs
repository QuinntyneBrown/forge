namespace Forge.Application.Rewards;

public record RewardItemDto(Guid Id, string Name, string Description, int CostPoints, int SortOrder);
