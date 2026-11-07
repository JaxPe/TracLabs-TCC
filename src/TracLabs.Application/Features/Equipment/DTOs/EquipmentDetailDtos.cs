using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.DTOs;

public record CalibrationRecordSummaryDto(
    Guid Id,
    DateTime CalibrationDate,
    DateTime NextCalibrationDue,
    string TechnicianName,
    decimal MeanError,
    decimal ExpandedUncertainty,
    decimal CoverageFactor,
    CalibrationResult Result,
    string? CertificateNumber);

public record EquipmentDetailDto(
    Guid Id,
    string LabTag,
    string Name,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    decimal? RangeMin,
    decimal? RangeMax,
    string Unit,
    decimal? Resolution,
    decimal? NominalCapacity,
    Guid ClientId,
    string? ClientName,
    string? SoftwareVersion,
    string? FirmwareVersion,
    string CurrentLocation,
    string? AcceptanceCriteria,
    string? MaintenancePlan,
    string? InitialConditionNotes,
    EquipmentStatus Status,
    DateTime CreatedAt);

public record EquipmentHistoryDto(
    EquipmentDetailDto Equipment,
    IReadOnlyList<CalibrationRecordSummaryDto> CalibrationHistory,
    IReadOnlyList<MaintenanceRecordDto> MaintenanceHistory,
    IReadOnlyList<OccurrenceRecordDto> OccurrenceHistory);
