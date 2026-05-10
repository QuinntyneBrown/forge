using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SignInAttempt> SignInAttempts { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<WeightEntry> WeightEntries { get; }
    DbSet<PointsLedger> PointsLedger { get; }
    DbSet<RewardCatalogItem> RewardCatalogItems { get; }
    DbSet<RewardRedemption> RewardRedemptions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
