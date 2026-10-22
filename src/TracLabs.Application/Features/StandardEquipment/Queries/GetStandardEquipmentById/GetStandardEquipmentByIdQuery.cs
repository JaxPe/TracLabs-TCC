using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetStandardEquipmentById;

public record GetStandardEquipmentByIdQuery(Guid Id) : IRequest<StandardEquipmentDetailDto>;

public class GetStandardEquipmentByIdQueryHandler : IRequestHandler<GetStandardEquipmentByIdQuery, StandardEquipmentDetailDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;

    public GetStandardEquipmentByIdQueryHandler(IStandardEquipmentRepository standardRepository)
    {
        _standardRepository = standardRepository;
    }

    public async Task<StandardEquipmentDetailDto> Handle(GetStandardEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        var dto = CreateStandardEquipmentCommandHandler.MapToDto(standard);

        var maintenances = standard.MaintenanceHistory
            .OrderByDescending(m => m.MaintenanceDate)
            .Select(m => new MaintenanceRecordDto(
                m.Id,
                m.MaintenanceDate,
                m.Type,
                m.Description,
                m.PerformedBy,
                m.ResultSummary))
            .ToList();

        var occurrences = standard.OccurrenceHistory
            .OrderByDescending(o => o.OccurrenceDate)
            .Select(o => new OccurrenceRecordDto(
                o.Id,
                o.OccurrenceDate,
                o.Type,
                o.Description,
                o.ActionTaken,
                o.ReportedBy,
                o.ResolvedDate))
            .ToList();

        return new StandardEquipmentDetailDto(dto, maintenances, occurrences);
    }
}
