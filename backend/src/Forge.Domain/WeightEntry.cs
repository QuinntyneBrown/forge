namespace Forge.Domain;

public class WeightEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal WeightLb { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
