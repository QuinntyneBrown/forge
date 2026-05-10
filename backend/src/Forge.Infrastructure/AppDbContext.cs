using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

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
        });

        modelBuilder.Entity<WorkoutSession>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Equipment).HasConversion<int>().IsRequired();
            b.Property(s => s.DistanceMiles).HasPrecision(6, 2);
            b.Property(s => s.Notes).HasMaxLength(2000);
            b.HasIndex(s => new { s.UserId, s.StartedAt });
        });
    }
}
