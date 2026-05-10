namespace Forge.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly TodayInTimeZone(string ianaTimeZoneId);
}
