using MediatR;
using TracLabs.Application.Features.Equipment.Commands.CreateEquipment;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Equipment.Queries.GetEquipmentHistory;

public record GetEquipmentHistoryQuery(Guid Id) : IRequest<EquipmentHistoryDto>;

public class GetEquipmentHistoryQueryHandler : IRequestHandler<GetEquipmentHistoryQuery, EquipmentHistoryDto>
{
    private readonly IEquipmentRepository _equipmentRepository;

    public GetEquipmentHistoryQueryHandler(IEquipmentRepository equipmentRepository)
    {
        _equipmentRepository = equipmentRepository;
    }

    public async Task<EquipmentHistoryDto> Handle(GetEquipmentHistoryQuery request, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentRepository.GetWithCalibrationHistoryAsync(request.Id, cancellationToken);
        if (equipment == null)
            throw new KeyNotFoundException($"Equipamento com ID '{request.Id}' não encontrado.");

        var detailDto = CreateEquipmentCommandHandler.MapToDetailDto(equipment, equipment.Client?.Name);

        var calHistory = equipment.CalibrationHistory
            .OrderByDescending(c => c.CalibrationDate)
            .Select(c => new CalibrationRecordSummaryDto(
                c.Id,
                c.CalibrationDate,
                c.NextCalibrationDue,
                c.TechnicianName,
                c.MeanError,
                c.ExpandedUncertainty,
                c.CoverageFactor,
                c.Result,
                c.Certificate?.CertificateNumber))
            .ToList();

        var maintHistory = equipment.MaintenanceHistory
            .OrderByDescending(m => m.MaintenanceDate)
            .Select(m => new MaintenanceRecordDto(
                m.Id,
                m.MaintenanceDate,
                m.Type,
                m.Description,
                m.PerformedBy,
                m.ResultSummary))
            .ToList();

        var occHistory = equipment.OccurrenceHistory
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

        return new EquipmentHistoryDto(detailDto, calHistory, maintHistory, occHistory);
    }
}
