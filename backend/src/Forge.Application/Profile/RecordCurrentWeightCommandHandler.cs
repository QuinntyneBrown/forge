using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;

namespace Forge.Application.Profile;

public class RecordCurrentWeightCommandHandler : IRequestHandler<RecordCurrentWeightCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RecordCurrentWeightCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(RecordCurrentWeightCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        _db.WeightEntries.Add(new WeightEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WeightLb = request.WeightLb,
            RecordedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
