using MediatR;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.Queries.SearchEquipment;

public record SearchEquipmentQuery(
    string? LabTag = null,
    string? SerialNumber = null,
    string? Model = null,
    string? ClientName = null,
    EquipmentStatus? Status = null,
    Guid? ClientId = null) : IRequest<IReadOnlyList<EquipmentSummaryDto>>;

public class SearchEquipmentQueryHandler : IRequestHandler<SearchEquipmentQuery, IReadOnlyList<EquipmentSummaryDto>>
{
    private readonly IEquipmentRepository _equipmentRepository;

    public SearchEquipmentQueryHandler(IEquipmentRepository equipmentRepository)
    {
        _equipmentRepository = equipmentRepository;
    }

    public async Task<IReadOnlyList<EquipmentSummaryDto>> Handle(SearchEquipmentQuery request, CancellationToken cancellationToken)
    {
        var list = await _equipmentRepository.SearchAsync(
            request.LabTag,
            request.SerialNumber,
            request.Model,
            request.ClientName,
            request.Status,
            request.ClientId,
            cancellationToken);

        return list.Select(e => new EquipmentSummaryDto(
            e.Id,
            e.LabTag,
            e.Name,
            e.Model,
            e.Manufacturer,
            e.SerialNumber,
            e.InstrumentType,
            e.Status,
            e.CurrentLocation,
            e.ClientId,
            e.Client?.Name,
            e.NominalCapacity,
            e.Unit,
            e.RangeMin,
            e.RangeMax)).ToList();
    }
}
