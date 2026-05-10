using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SignInAttempt> SignInAttempts => Set<SignInAttempt>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<PointsLedger> PointsLedger => Set<PointsLedger>();
    public DbSet<RewardCatalogItem> RewardCatalogItems => Set<RewardCatalogItem>();
    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).HasMaxLength(254).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.FirstName).HasMaxLength(64).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(64).IsRequired();
            b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            b.Property(u => u.Role).HasMaxLength(32).IsRequired();

            // BT-015 profile columns with defaults so the migration backfills
            // existing rows and new registrations match the L2 spec without
            // the handler having to set them explicitly.
            b.Property(u => u.Units).HasMaxLength(16).IsRequired().HasDefaultValue("Imperial");
            b.Property(u => u.TimeZoneId).HasMaxLength(64).IsRequired().HasDefaultValue("America/New_York");
            b.Property(u => u.DailyActiveCaloriesTarget).HasDefaultValue(1500);
            b.Property(u => u.DailyWorkoutMinutesTarget).HasDefaultValue(60);
            b.Property(u => u.MonthlyWeightGoalLb).HasDefaultValue(20);
            b.Property(u => u.MorningWindowStart).HasDefaultValue(new TimeOnly(5, 0));
            b.Property(u => u.MorningWindowEnd).HasDefaultValue(new TimeOnly(7, 30));
            b.Property(u => u.KitchenClosedStart).HasDefaultValue(new TimeOnly(20, 0));
            b.Property(u => u.KitchenClosedEnd).HasDefaultValue(new TimeOnly(6, 0));
            b.Property(u => u.KitchenNudgeEnabled).HasDefaultValue(true);
            b.Property(u => u.MorningReminderEnabled).HasDefaultValue(true);
            b.Property(u => u.LeaderboardOptIn).HasDefaultValue(false);
        });

        modelBuilder.Entity<WorkoutSession>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Equipment).HasConversion<int>().IsRequired();
            b.Property(s => s.DistanceMiles).HasPrecision(6, 2);
            b.Property(s => s.Notes).HasMaxLength(2000);
            b.HasIndex(s => new { s.UserId, s.StartedAt });
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.FamilyId);
            b.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<SignInAttempt>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Email).HasMaxLength(254).IsRequired();
            b.Property(a => a.IpAddress).HasMaxLength(64);
            b.Property(a => a.UserAgent).HasMaxLength(512);
            b.HasIndex(a => new { a.Email, a.OccurredAt });
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Event).HasMaxLength(64).IsRequired();
            b.Property(a => a.IpAddress).HasMaxLength(64);
            b.Property(a => a.UserAgent).HasMaxLength(512);
            b.HasIndex(a => new { a.UserId, a.OccurredAt });
            b.HasIndex(a => a.Event);
        });

        modelBuilder.Entity<PasswordResetToken>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<WeightEntry>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.WeightLb).HasPrecision(6, 2);
            b.HasIndex(w => new { w.UserId, w.RecordedAt });
        });

        modelBuilder.Entity<PointsLedger>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Reason).HasConversion<int>().IsRequired();
            b.Property(l => l.Description).HasMaxLength(128).IsRequired();
            b.HasIndex(l => new { l.UserId, l.CreatedAt });
            b.HasIndex(l => l.SessionId);
        });

        modelBuilder.Entity<RewardCatalogItem>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).HasMaxLength(80).IsRequired();
            b.Property(r => r.Description).HasMaxLength(400).IsRequired();
            b.HasIndex(r => r.SortOrder);
            b.HasData(SeedRewards());
        });

        modelBuilder.Entity<RewardRedemption>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => new { r.UserId, r.RedeemedAt });
        });
    }

    private static IEnumerable<RewardCatalogItem> SeedRewards()
    {
        return new[]
        {
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000001"),
                Name = "Post-workout Smoothie",
                Description = "Trade points for a guilt-free recovery smoothie.",
                CostPoints = 200,
                IsActive = true,
                SortOrder = 1
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000002"),
                Name = "Rest Day Pass",
                Description = "Skip a session without breaking your streak.",
                CostPoints = 500,
                IsActive = true,
                SortOrder = 2
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000003"),
                Name = "New Athletic Socks",
                Description = "Treat yourself to a fresh pair of training socks.",
                CostPoints = 750,
                IsActive = true,
                SortOrder = 3
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000004"),
                Name = "Pair of Wireless Earbuds",
                Description = "Replace your gym earbuds with a fresh set.",
                CostPoints = 4000,
                IsActive = true,
                SortOrder = 4
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000005"),
                Name = "New Running Shoes",
                Description = "Upgrade the kicks once you bank enough.",
                CostPoints = 12000,
                IsActive = true,
                SortOrder = 5
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000006"),
                Name = "Massage Session",
                Description = "Recover with a one-hour deep tissue massage.",
                CostPoints = 8000,
                IsActive = true,
                SortOrder = 6
            },
            new RewardCatalogItem
            {
                Id = new Guid("11111111-1111-1111-1111-000000000007"),
                Name = "Premium Whey Protein",
                Description = "5 lb tub of premium whey.",
                CostPoints = 6000,
                IsActive = true,
                SortOrder = 7
            }
        };
    }
}
