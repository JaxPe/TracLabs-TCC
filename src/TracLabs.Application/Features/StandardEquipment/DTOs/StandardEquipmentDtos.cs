using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.DTOs;

public record StandardEquipmentDto(
    Guid Id,
    string LabTag,
    string Name,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    string? SoftwareVersion,
    string? FirmwareVersion,
    string CurrentLocation,
    string? AcceptanceCriteria,
    int CalibrationIntervalMonths,
    string? ReferenceMaterialDocumentation,
    string? MaintenancePlan,
    StandardEquipmentStatus Status,
    decimal Error,
    decimal Correction,
    decimal ExpandedUncertainty,
    decimal CoverageFactor,
    decimal Resolution,
    DateTime LastCalibrationDate,
    DateTime CalibrationDueDate,
    string CalibrationCertificateNumber,
    string CalibratingLaboratory,
    bool IsCalibrationValid,
    int DaysUntilExpiry,
    bool IsUsableForCalibration,
    DateTime CreatedAt);

public record MaintenanceRecordDto(
    Guid Id,
    DateTime MaintenanceDate,
    MaintenanceType Type,
    string Description,
    string PerformedBy,
    string? ResultSummary);

public record OccurrenceRecordDto(
    Guid Id,
    DateTime OccurrenceDate,
    OccurrenceType Type,
    string Description,
    string ActionTaken,
    string ReportedBy,
    DateTime? ResolvedDate);

public record StandardEquipmentDetailDto(
    StandardEquipmentDto Standard,
    IReadOnlyList<MaintenanceRecordDto> MaintenanceHistory,
    IReadOnlyList<OccurrenceRecordDto> OccurrenceHistory);
