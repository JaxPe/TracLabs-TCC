using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetUsableStandards;

/// <summary>
/// Retorna apenas os padrões com Status Calibrated e calibração válida.
/// Usado para popular dropdowns do técnico na tela de calibração.
/// </summary>
public record GetUsableStandardsQuery : IRequest<IReadOnlyList<StandardEquipmentDto>>;

public class GetUsableStandardsQueryHandler : IRequestHandler<GetUsableStandardsQuery, IReadOnlyList<StandardEquipmentDto>>
{
    private readonly IStandardEquipmentRepository _standardRepository;

    public GetUsableStandardsQueryHandler(IStandardEquipmentRepository standardRepository)
    {
        _standardRepository = standardRepository;
    }

    public async Task<IReadOnlyList<StandardEquipmentDto>> Handle(GetUsableStandardsQuery request, CancellationToken cancellationToken)
    {
        var standards = await _standardRepository.GetValidStandardsAsync(cancellationToken);
        return standards
            .Select(CreateStandardEquipmentCommandHandler.MapToDto)
            .ToList();
    }
}
