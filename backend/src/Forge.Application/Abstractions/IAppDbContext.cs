using Forge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
