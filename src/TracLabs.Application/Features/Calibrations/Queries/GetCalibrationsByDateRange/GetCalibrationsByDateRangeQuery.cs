using MediatR;
using TracLabs.Application.Features.Equipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Calibrations.Queries.GetCalibrationsByDateRange;

public record GetCalibrationsByDateRangeQuery(DateTime From, DateTime To)
    : IRequest<IReadOnlyList<CalibrationRecordSummaryDto>>;

public class GetCalibrationsByDateRangeQueryHandler
    : IRequestHandler<GetCalibrationsByDateRangeQuery, IReadOnlyList<CalibrationRecordSummaryDto>>
{
    private readonly ICalibrationRepository _calibrationRepository;

    public GetCalibrationsByDateRangeQueryHandler(ICalibrationRepository calibrationRepository)
    {
        _calibrationRepository = calibrationRepository;
    }

    public async Task<IReadOnlyList<CalibrationRecordSummaryDto>> Handle(
        GetCalibrationsByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var records = await _calibrationRepository.GetByDateRangeAsync(request.From, request.To, cancellationToken);

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
