using Forge.Application.Abstractions;

namespace Forge.Acceptance.Auth;

public class FakeClock : IClock
{
    private DateTimeOffset _now;

    public FakeClock(DateTimeOffset now)
    {
        _now = now;
    }

    public DateTimeOffset UtcNow => _now;

    public DateOnly TodayInTimeZone(string ianaTimeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_now, tz).DateTime);
    }

    public void Advance(TimeSpan delta)
    {
        _now = _now.Add(delta);
    }
}
