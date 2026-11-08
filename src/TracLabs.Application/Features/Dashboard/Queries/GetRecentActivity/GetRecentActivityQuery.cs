using MediatR;
using TracLabs.Application.Features.Dashboard.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Dashboard.Queries.GetRecentActivity;

public record GetRecentActivityQuery(int Count = 10) : IRequest<IReadOnlyList<CalibrationTimelineEventDto>>;

public class GetRecentActivityQueryHandler : IRequestHandler<GetRecentActivityQuery, IReadOnlyList<CalibrationTimelineEventDto>>
{
    private readonly ICalibrationRepository _calibrationRepository;

    public GetRecentActivityQueryHandler(ICalibrationRepository calibrationRepository)
    {
        _calibrationRepository = calibrationRepository;
    }

    public async Task<IReadOnlyList<CalibrationTimelineEventDto>> Handle(GetRecentActivityQuery request, CancellationToken cancellationToken)
    {
        var records = await _calibrationRepository.GetRecentCalibrationsAsync(request.Count, cancellationToken);

        return records.Select(c => new CalibrationTimelineEventDto(
            Id: c.Id.ToString(),
            TimeString: c.CalibrationDate.ToString("dd/MM/yyyy HH:mm"),
            Title: $"Calibração {(c.Certificate != null ? "#" + c.Certificate.CertificateNumber : "#" + c.Id.ToString()[..8])}",
            EquipmentTag: c.Equipment?.LabTag ?? "N/A",
            ClientName: c.Equipment?.Client?.Name ?? "Cliente N/A",
            OperatorName: string.IsNullOrWhiteSpace(c.TechnicianName) ? (string.IsNullOrWhiteSpace(c.TechnicianId) ? "Técnico" : c.TechnicianId) : c.TechnicianName,
            Result: c.Result,
            Details: $"Ensaio volumétrico com {c.MeasurementPoints.Count} ponto(s) registrado(s)"
        )).ToList();
    }
}
