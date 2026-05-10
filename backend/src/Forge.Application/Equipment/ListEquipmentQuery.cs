using MediatR;

namespace Forge.Application.Equipment;

public record ListEquipmentQuery() : IRequest<IReadOnlyList<EquipmentDto>>;
