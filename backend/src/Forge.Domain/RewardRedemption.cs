namespace Forge.Domain;

public class RewardRedemption
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RewardCatalogItemId { get; set; }
    public int CostPoints { get; set; }
    public DateTimeOffset RedeemedAt { get; set; }
}
