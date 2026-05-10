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
    }
}
