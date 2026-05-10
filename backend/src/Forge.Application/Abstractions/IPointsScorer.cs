using Forge.Domain;

namespace Forge.Application.Abstractions;

public interface IPointsScorer
{
    Task Score(WorkoutSession session, CancellationToken cancellationToken);
}
