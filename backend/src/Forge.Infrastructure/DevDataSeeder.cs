using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Forge.Infrastructure;

public class DevDataSeeder
{
    public const string DevEmail = "dev@forge.local";
    public const string DevPassword = "DevPassword123!";

    // Catalog item IDs seeded via EF migration HasData (see AppDbContext.SeedRewards).
    private static readonly Guid SmoothieRewardId = new("11111111-1111-1111-1111-000000000001");
    private static readonly Guid RestDayRewardId = new("11111111-1111-1111-1111-000000000002");
    private static readonly Guid SocksRewardId = new("11111111-1111-1111-1111-000000000003");

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(AppDbContext db, IPasswordHasher hasher, IClock clock, ILogger<DevDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var devUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == DevEmail, cancellationToken);
        if (devUser is null)
        {
            devUser = new User
            {
                Id = Guid.NewGuid(),
                Email = DevEmail,
                FirstName = "Dev",
                LastName = "User",
                PasswordHash = _hasher.Hash(DevPassword),
                Role = "User",
                CreatedAt = _clock.UtcNow
            };
            _db.Users.Add(devUser);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded development user {Email}.", DevEmail);
        }

        // Deterministic randomness so the seeded dataset is stable across runs.
        var rng = new Random(42);

        await SeedWorkoutsAndPointsAsync(devUser, rng, cancellationToken);
        await SeedWeightEntriesAsync(devUser.Id, rng, cancellationToken);
        await SeedRedemptionsAsync(devUser.Id, rng, cancellationToken);
    }

    private async Task SeedWorkoutsAndPointsAsync(User user, Random rng, CancellationToken cancellationToken)
    {
        var hasSessions = await _db.WorkoutSessions.AnyAsync(s => s.UserId == user.Id, cancellationToken);
        if (hasSessions)
        {
            return;
        }

        var now = _clock.UtcNow;
        var sessions = new List<WorkoutSession>();
        var ledger = new List<PointsLedger>();

        // Generate ~32 sessions across the last 60 days, including today and yesterday.
        var daysAgoOffsets = BuildSessionDayOffsets(rng);

        foreach (var dayOffset in daysAgoOffsets)
        {
            var equipment = PickEquipment(rng);
            var startedAt = BuildSessionStart(now, dayOffset, rng);
            var (duration, calories, distance, hr, notes) = BuildMetrics(equipment, rng);

            var session = new WorkoutSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Equipment = equipment,
                StartedAt = startedAt,
                DurationMinutes = duration,
                DistanceMiles = distance,
                AvgHeartRateBpm = hr,
                ActiveCalories = calories,
                Notes = notes,
                CreatedAt = startedAt
            };
            sessions.Add(session);

            // Base points: 1 point per active calorie.
            ledger.Add(new PointsLedger
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SessionId = session.Id,
                Reason = PointsLedgerReason.Base,
                Points = calories,
                Description = $"Base points for {equipment} workout.",
                CreatedAt = startedAt
            });

            // Morning bonus: workouts that started before 7:30 local-ish (we use UTC hour as a proxy here
            // since session times are deterministic).
            var localHour = startedAt.ToOffset(TimeSpan.FromHours(-5)).Hour;
            if (localHour >= 5 && localHour < 8)
            {
                ledger.Add(new PointsLedger
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    SessionId = session.Id,
                    Reason = PointsLedgerReason.MorningBonus,
                    Points = 100,
                    Description = "Morning workout bonus.",
                    CreatedAt = startedAt.AddMinutes(1)
                });
            }

            // Streak multiplier on roughly 1 in 4 sessions to net a healthy balance.
            if (rng.Next(0, 4) == 0)
            {
                var streakBonus = (int)Math.Round(calories * 0.25);
                ledger.Add(new PointsLedger
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    SessionId = session.Id,
                    Reason = PointsLedgerReason.StreakMultiplier,
                    Points = streakBonus,
                    Description = "Streak multiplier bonus.",
                    CreatedAt = startedAt.AddMinutes(2)
                });
            }
        }

        _db.WorkoutSessions.AddRange(sessions);
        _db.PointsLedger.AddRange(ledger);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Seeded {SessionCount} workout sessions and {LedgerCount} points entries for {Email}.",
            sessions.Count,
            ledger.Count,
            user.Email);
    }

    private async Task SeedWeightEntriesAsync(Guid userId, Random rng, CancellationToken cancellationToken)
    {
        var hasWeights = await _db.WeightEntries.AnyAsync(w => w.UserId == userId, cancellationToken);
        if (hasWeights)
        {
            return;
        }

        var now = _clock.UtcNow;
        const decimal startWeight = 215.0m;
        const decimal endWeight = 205.0m;
        const int entryCount = 20;

        var entries = new List<WeightEntry>();
        for (var i = 0; i < entryCount; i++)
        {
            // i = 0 is oldest (60 days ago), i = entryCount-1 is most recent (today).
            var dayOffset = 60 - (i * 3);
            var progress = (decimal)i / (entryCount - 1);
            var trendWeight = startWeight - ((startWeight - endWeight) * progress);
            // Add small random jitter (±0.6 lb) so the chart looks lifelike.
            var jitter = (decimal)((rng.NextDouble() * 1.2) - 0.6);
            var weight = Math.Round(trendWeight + jitter, 1);

            entries.Add(new WeightEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeightLb = weight,
                RecordedAt = now.AddDays(-dayOffset)
            });
        }

        _db.WeightEntries.AddRange(entries);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} weight entries for dev user.", entries.Count);
    }

    private async Task SeedRedemptionsAsync(Guid userId, Random rng, CancellationToken cancellationToken)
    {
        var hasRedemptions = await _db.RewardRedemptions.AnyAsync(r => r.UserId == userId, cancellationToken);
        if (hasRedemptions)
        {
            return;
        }

        var now = _clock.UtcNow;
        var redemptions = new (Guid CatalogId, int Cost, string Name, int DaysAgo)[]
        {
            (SmoothieRewardId, 200, "Post-workout Smoothie", 28),
            (SocksRewardId, 750, "New Athletic Socks", 14),
            (RestDayRewardId, 500, "Rest Day Pass", 5)
        };

        var redemptionEntities = new List<RewardRedemption>();
        var ledgerEntities = new List<PointsLedger>();

        foreach (var (catalogId, cost, name, daysAgo) in redemptions)
        {
            var redeemedAt = now.AddDays(-daysAgo).AddHours(-rng.Next(0, 6));
            var redemption = new RewardRedemption
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RewardCatalogItemId = catalogId,
                CostPoints = cost,
                RedeemedAt = redeemedAt
            };
            redemptionEntities.Add(redemption);

            ledgerEntities.Add(new PointsLedger
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RedemptionId = redemption.Id,
                Reason = PointsLedgerReason.Redemption,
                Points = -cost,
                Description = $"Redeemed: {name}.",
                CreatedAt = redeemedAt
            });
        }

        _db.RewardRedemptions.AddRange(redemptionEntities);
        _db.PointsLedger.AddRange(ledgerEntities);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Seeded {Count} reward redemptions for dev user.",
            redemptionEntities.Count);
    }

    private static List<int> BuildSessionDayOffsets(Random rng)
    {
        // Hand-picked anchor days so the dataset always covers today, yesterday,
        // and the last week densely, with broader coverage across 60 days.
        var anchors = new HashSet<int> { 0, 1, 2, 3, 5, 6, 8, 10, 12, 14, 17, 20, 23, 26, 30, 34, 38, 42, 47, 52, 58 };

        // Add a few extra random days to reach ~32 sessions total. Some days will
        // get two sessions which is realistic (e.g. cardio + lift).
        while (anchors.Count < 24)
        {
            anchors.Add(rng.Next(0, 60));
        }

        var offsets = anchors.ToList();
        // Inject a handful of double-up days for variety.
        offsets.Add(0);
        offsets.Add(1);
        offsets.Add(3);
        offsets.Add(7);
        offsets.Add(14);
        offsets.Add(21);
        offsets.Add(35);
        offsets.Add(45);

        offsets.Sort();
        return offsets;
    }

    private static EquipmentType PickEquipment(Random rng)
    {
        // Bias slightly toward Treadmill / IndoorBike since those are common cardio defaults.
        var roll = rng.Next(0, 10);
        return roll switch
        {
            < 4 => EquipmentType.Treadmill,
            < 7 => EquipmentType.IndoorBike,
            < 9 => EquipmentType.Elliptical,
            _ => EquipmentType.BenchPress
        };
    }

    private static DateTimeOffset BuildSessionStart(DateTimeOffset now, int dayOffset, Random rng)
    {
        // Mix of morning, midday, and evening starts. Morning slot triggers the bonus.
        var slot = rng.Next(0, 10);
        var hour = slot switch
        {
            < 4 => rng.Next(5, 8),     // morning (eligible for bonus)
            < 7 => rng.Next(11, 14),   // lunchtime
            _ => rng.Next(17, 20)      // evening
        };
        var minute = rng.Next(0, 60);

        // Build the start in the user's assumed local zone (Eastern, UTC-5 placeholder)
        // then convert back to UTC for storage.
        var localDate = now.ToOffset(TimeSpan.FromHours(-5)).AddDays(-dayOffset).Date;
        var local = new DateTimeOffset(
            localDate.Year, localDate.Month, localDate.Day,
            hour, minute, 0,
            TimeSpan.FromHours(-5));
        return local.ToUniversalTime();
    }

    private static (int Duration, int Calories, decimal? Distance, int? HeartRate, string? Notes)
        BuildMetrics(EquipmentType equipment, Random rng)
    {
        var duration = rng.Next(20, 76);
        var calories = rng.Next(200, 801);
        var hr = rng.Next(0, 4) == 0 ? (int?)null : rng.Next(110, 166);

        decimal? distance = equipment switch
        {
            EquipmentType.Treadmill => Math.Round((decimal)(duration * (rng.NextDouble() * 0.05 + 0.08)), 2),
            EquipmentType.IndoorBike => Math.Round((decimal)(duration * (rng.NextDouble() * 0.10 + 0.25)), 2),
            EquipmentType.Elliptical => Math.Round((decimal)(duration * (rng.NextDouble() * 0.04 + 0.07)), 2),
            _ => null
        };

        string? notes = null;
        if (rng.Next(0, 3) == 0)
        {
            notes = equipment switch
            {
                EquipmentType.Treadmill => "Steady-state Zone 2 run, felt smooth.",
                EquipmentType.IndoorBike => "Interval ride - 4x4 minute efforts.",
                EquipmentType.Elliptical => "Recovery day, kept HR low.",
                EquipmentType.BenchPress => "5x5 working sets, hit a small PR.",
                _ => null
            };
        }

        return (duration, calories, distance, hr, notes);
    }
}
