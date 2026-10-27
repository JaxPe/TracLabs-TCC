using MediatR;
using TracLabs.Application.Features.Calibrations.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Calibrations.Queries.GetCalibrationById;

public record GetCalibrationByIdQuery(Guid Id) : IRequest<CalibrationRecordDto>;

public class GetCalibrationByIdQueryHandler : IRequestHandler<GetCalibrationByIdQuery, CalibrationRecordDto>
{
    private readonly ICalibrationRepository _calibrationRepository;

    public GetCalibrationByIdQueryHandler(ICalibrationRepository calibrationRepository)
    {
        _calibrationRepository = calibrationRepository;
    }

    public async Task<CalibrationRecordDto> Handle(GetCalibrationByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _calibrationRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        if (record == null)
            throw new KeyNotFoundException($"Registro de calibração com ID '{request.Id}' não encontrado.");

        return new CalibrationRecordDto(
            record.Id,
            record.EquipmentId,
            record.Equipment?.LabTag ?? string.Empty,
            record.Equipment?.Name ?? string.Empty,
            record.StandardEquipmentId,
            record.StandardEquipment?.LabTag ?? string.Empty,
            record.TechnicianId,
            record.TechnicianName,
            record.CalibrationDate,
            record.NextCalibrationDue,
            record.TemperatureCelsius,
            record.HumidityPercent,
            record.MeanError,
            record.MaxAbsoluteError,
            record.CombinedStandardUncertainty,
            record.ExpandedUncertainty,
            record.CoverageFactor,
            record.Result,
            record.Observations,
            record.MeasurementPoints,
            null,
            record.Certificate?.CertificateNumber);
    }
}
