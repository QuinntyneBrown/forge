namespace Forge.Domain;

public class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public EquipmentType Equipment { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMinutes { get; set; }
    public decimal? DistanceMiles { get; set; }
    public int? AvgHeartRateBpm { get; set; }
    public int ActiveCalories { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
