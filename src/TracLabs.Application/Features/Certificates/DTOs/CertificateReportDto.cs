using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.DTOs;

/// <summary>
/// Modelo completo de dados para renderização do relatório/documento de certificado de calibração.
/// Agrega de forma imutável as informações metrológicas, cadastrais e de rastreabilidade (ISO/IEC 17025 item 7.8).
/// </summary>
public record CertificateReportDto(
    Guid CertificateId,
    string CertificateNumber,
    CertificateStatus Status,
    DateTime? IssuedAt,
    string? IssuedByUserId,
    string? SupervisorName,
    DateTime CalibrationDate,
    DateTime NextCalibrationDue,
    string TechnicianId,
    string TechnicianName,
    // Dados do Cliente
    Guid ClientId,
    string ClientName,
    string ClientTaxId,
    string? ClientStreet,
    string? ClientCity,
    string? ClientState,
    string? ClientPostalCode,
    string? ClientContactPerson,
    // Dados do Equipamento Calibrado
    Guid EquipmentId,
    string EquipmentLabTag,
    string EquipmentName,
    string EquipmentInstrumentType,
    string EquipmentManufacturer,
    string EquipmentModel,
    string EquipmentSerialNumber,
    decimal? EquipmentRangeMin,
    decimal? EquipmentRangeMax,
    decimal? EquipmentCalibrationRangeMin,
    decimal? EquipmentCalibrationRangeMax,
    string EquipmentUnit,
    decimal? EquipmentResolution,
    decimal? EquipmentNominalCapacity,
    // Condições Ambientais
    decimal AmbientTemperatureCelsius,
    decimal RelativeHumidityPercent,
    decimal? WaterTemperatureCelsius,
    decimal? AtmosphericPressureHpa,
    decimal? WaterDensityGcm3,
    decimal? AirDensityGcm3,
    // Resultados Metrológicos Globais (Compatibilidade mono-ponto)
    decimal NominalVolumeMl,
    decimal CalculatedVolumeV20Ml,
    decimal SystematicErrorMl,
    decimal MaxAbsoluteErrorMl,
    decimal CombinedUncertaintyMl,
    decimal ExpandedUncertaintyMl,
    decimal CoverageFactor,
    decimal? EffectiveDegreesOfFreedom,
    CalibrationResult Result,
    string? Observations,
    // Coleções
    IReadOnlyList<CertificateStandardUsedItemDto> StandardsUsed,
    IReadOnlyList<CertificateMeasurementPointDto> MeasurementPoints,
    IReadOnlyList<CertificateResultPointDto> ResultPoints
);

public record CertificateResultPointDto(
    decimal NominalVolumeMl,
    decimal MeasuredVolumeMl,
    decimal ErrorMl,
    decimal ExpandedUncertaintyMl,
    decimal CoverageFactor,
    decimal? EffectiveDegreesOfFreedom,
    decimal? StandardDeviationMl = null
);

public record CertificateStandardUsedItemDto(
    string Role,
    string LabTag,
    string Description,
    string CertificateNumber,
    string? CalibratingLaboratory,
    decimal Correction,
    decimal ExpandedUncertainty,
    decimal CoverageFactor,
    decimal Resolution,
    decimal OperatingValue,
    bool IsExtrapolated,
    string? WarningMessage,
    DateTime? CertificateDueDate = null
);

public record CertificateMeasurementPointDto(
    int RunNumber,
    decimal NominalVolumeMl,
    decimal IndicatedVolumeMl,
    decimal ErrorMl,
    string Unit
);

public record CertificateSummaryDto(
    Guid Id,
    string CertificateNumber,
    Guid CalibrationRecordId,
    Guid EquipmentId,
    string EquipmentTag,
    string EquipmentName,
    string ClientName,
    CertificateStatus Status,
    DateTime CalibrationDate,
    DateTime? IssuedAt,
    string? PdfFileName,
    bool HasPdf
);

public record CertificateDetailDto(
    Guid Id,
    string CertificateNumber,
    Guid CalibrationRecordId,
    Guid EquipmentId,
    string EquipmentTag,
    string EquipmentName,
    Guid ClientId,
    string ClientName,
    CertificateStatus Status,
    DateTime CalibrationDate,
    DateTime NextCalibrationDue,
    DateTime? IssuedAt,
    string? IssuedByUserId,
    string? PdfFileName,
    bool HasPdf,
    decimal NominalVolumeMl,
    decimal SystematicErrorMl,
    decimal ExpandedUncertaintyMl,
    decimal CoverageFactor,
    CalibrationResult Result,
    string? Observations
);
