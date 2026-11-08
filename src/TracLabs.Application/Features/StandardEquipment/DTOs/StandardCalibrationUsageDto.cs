using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.DTOs;

public record StandardCalibrationUsageDto(
    Guid Id,
    Guid StandardId,
    Guid CalibrationId,
    DateTime CalibrationDate,
    string CustomerName,
    string EquipmentTag,
    string EquipmentDescription,
    decimal NominalVolume,
    string Unit,
    string TechnicianName,
    string CertificateNumberIssued,
    StandardRole RoleUsed,
    decimal OperatingValue,
    string CertificateStatus);
