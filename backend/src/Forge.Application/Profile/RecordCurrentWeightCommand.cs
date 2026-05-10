using MediatR;

namespace Forge.Application.Profile;

public record RecordCurrentWeightCommand(decimal WeightLb) : IRequest;
