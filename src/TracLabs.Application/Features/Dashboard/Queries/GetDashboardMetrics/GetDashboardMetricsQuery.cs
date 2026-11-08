using MediatR;
using TracLabs.Application.Features.Dashboard.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Dashboard.Queries.GetDashboardMetrics;

public record GetDashboardMetricsQuery : IRequest<DashboardMetricsDto>;

public class GetDashboardMetricsQueryHandler : IRequestHandler<GetDashboardMetricsQuery, DashboardMetricsDto>
{
    private readonly ICalibrationRepository _calibrationRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly IStandardEquipmentRepository _standardEquipmentRepository;

    public GetDashboardMetricsQueryHandler(
        ICalibrationRepository calibrationRepository,
        ICertificateRepository certificateRepository,
        IStandardEquipmentRepository standardEquipmentRepository)
    {
        _calibrationRepository = calibrationRepository;
        _certificateRepository = certificateRepository;
        _standardEquipmentRepository = standardEquipmentRepository;
    }

    public async Task<DashboardMetricsDto> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var activeCount = await _calibrationRepository.GetActiveCalibrationsCountAsync(cancellationToken);
        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var issuedMonthCount = await _certificateRepository.GetIssuedCountSinceAsync(firstDayOfMonth, cancellationToken);
        var expiringStandards = await _standardEquipmentRepository.GetExpiringSoonAsync(30, cancellationToken);
        var nonConformingCount = await _calibrationRepository.GetNonConformingCountAsync(cancellationToken);

        return new DashboardMetricsDto(
            ActiveCalibrationsCount: activeCount,
            IssuedCertificatesMonthCount: issuedMonthCount,
            ExpiringStandardsCount: expiringStandards.Count,
            NonConformingCount: nonConformingCount);
    }
}
