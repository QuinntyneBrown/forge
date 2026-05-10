using MediatR;

namespace Forge.Application.Rewards;

public record GetCurrentTierQuery() : IRequest<TierDto>;
