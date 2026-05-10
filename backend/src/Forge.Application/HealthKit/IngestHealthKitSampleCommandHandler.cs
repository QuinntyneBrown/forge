using Forge.Application.Abstractions;
using MediatR;

namespace Forge.Application.HealthKit;

public class IngestHealthKitSampleCommandHandler : IRequestHandler<IngestHealthKitSampleCommand>
{
    private readonly IHealthKitIngest _ingest;
    private readonly ICurrentUser _currentUser;

    public IngestHealthKitSampleCommandHandler(IHealthKitIngest ingest, ICurrentUser currentUser)
    {
        _ingest = ingest;
        _currentUser = currentUser;
    }

    public async Task Handle(IngestHealthKitSampleCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        await _ingest.IngestAsync(
            userId,
            request.SampleType,
            request.Value,
            request.Unit,
            request.RecordedAt,
            cancellationToken);
    }
}
