namespace Forge.Domain;

public class PointsLedger
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? RedemptionId { get; set; }
    public PointsLedgerReason Reason { get; set; }
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
