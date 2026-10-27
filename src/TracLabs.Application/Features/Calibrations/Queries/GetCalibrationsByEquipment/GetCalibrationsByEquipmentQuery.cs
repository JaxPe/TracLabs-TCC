using MediatR;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Calibrations.Queries.GetCalibrationsByEquipment;

public record GetCalibrationsByEquipmentQuery(Guid EquipmentId)
    : IRequest<IReadOnlyList<CalibrationRecordSummaryDto>>;

public class GetCalibrationsByEquipmentQueryHandler
    : IRequestHandler<GetCalibrationsByEquipmentQuery, IReadOnlyList<CalibrationRecordSummaryDto>>
{
    private readonly ICalibrationRepository _calibrationRepository;

    public GetCalibrationsByEquipmentQueryHandler(ICalibrationRepository calibrationRepository)
    {
        _calibrationRepository = calibrationRepository;
    }

    public async Task<IReadOnlyList<CalibrationRecordSummaryDto>> Handle(
        GetCalibrationsByEquipmentQuery request, CancellationToken cancellationToken)
    {
        var records = await _calibrationRepository.GetHistoryByEquipmentIdAsync(request.EquipmentId, cancellationToken);

        return records.Select(c => new CalibrationRecordSummaryDto(
            c.Id,
            c.CalibrationDate,
            c.NextCalibrationDue,
            c.TechnicianName,
            c.MeanError,
            c.ExpandedUncertainty,
            c.CoverageFactor,
            c.Result,
            c.Certificate?.CertificateNumber)).ToList();
    }
}
