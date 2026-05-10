using Forge.Domain;
using MediatR;

namespace Forge.Application.Equipment;

public class ListEquipmentQueryHandler : IRequestHandler<ListEquipmentQuery, IReadOnlyList<EquipmentDto>>
{
    public Task<IReadOnlyList<EquipmentDto>> Handle(ListEquipmentQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<EquipmentDto> items = new[]
        {
            new EquipmentDto(EquipmentType.Treadmill.ToString(), "Treadmill"),
            new EquipmentDto(EquipmentType.IndoorBike.ToString(), "Indoor Bike"),
            new EquipmentDto(EquipmentType.BenchPress.ToString(), "Bench Press"),
            new EquipmentDto(EquipmentType.Elliptical.ToString(), "Elliptical")
        };
        return Task.FromResult(items);
    }
}
