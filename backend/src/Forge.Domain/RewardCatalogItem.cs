namespace Forge.Domain;

public class RewardCatalogItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CostPoints { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
