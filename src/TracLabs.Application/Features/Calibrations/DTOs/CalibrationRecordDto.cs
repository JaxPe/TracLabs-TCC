using TracLabs.Application.DTOs.Calibration;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Features.Calibrations.DTOs;

public record CalibrationRecordDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentLabTag,
    string EquipmentName,
    Guid StandardEquipmentId,
    string StandardLabTag,
    string TechnicianId,
    string TechnicianName,
    DateTime CalibrationDate,
    DateTime NextCalibrationDue,
    decimal TemperatureCelsius,
    decimal HumidityPercent,
    decimal MeanError,
    decimal MaxAbsoluteError,
    decimal CombinedStandardUncertainty,
    decimal ExpandedUncertainty,
    decimal CoverageFactor,
    CalibrationResult Result,
    string? Observations,
    IReadOnlyList<MeasurementPoint> MeasurementPoints,
    VolumeCalibrationResultDto? VolumeCalculationDetails,
    string? CertificateNumber);
