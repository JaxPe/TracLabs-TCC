using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Dashboard.DTOs;

public record DashboardMetricsDto(
    int ActiveCalibrationsCount,
    int IssuedCertificatesMonthCount,
    int ExpiringStandardsCount,
    int NonConformingCount);

public record CalibrationTimelineEventDto(
    string Id,
    string TimeString,
    string Title,
    string EquipmentTag,
    string ClientName,
    string OperatorName,
    CalibrationResult Result,
    string Details);
