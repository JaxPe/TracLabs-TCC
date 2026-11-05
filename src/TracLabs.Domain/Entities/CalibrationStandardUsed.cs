using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Registra a rastreabilidade imutável de um equipamento padrão utilizado em uma calibração,
/// congelando o snapshot de seus dados metrológicos (certificado, incerteza, correção, resolução,
/// ponto de operação e avisos de extrapolação) exatamente como estavam no momento do ensaio,
/// conforme o item 6.4.13 da ABNT NBR ISO/IEC 17025.
/// </summary>
public class CalibrationStandardUsed : BaseEntity
{
    public Guid CalibrationId { get; private set; }
    public CalibrationRecord? CalibrationRecord { get; private set; }

    public Guid StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

    public Guid? StandardCertificateId { get; private set; }
    public StandardCertificate? StandardCertificate { get; private set; }

    /// <summary>Papel do padrão na calibração (Balança, Termômetro Líquido, Termômetro Ambiente, Barômetro, Higrômetro).</summary>
    public StandardRole Role { get; private set; }

    // ── Snapshot Metrológico no Momento da Calibração (Imutável) ────────────

    public string LabTag { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string Manufacturer { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string InstrumentType { get; private set; } = string.Empty;
    public string CertificateNumber { get; private set; } = string.Empty;
    public string CalibratingLaboratory { get; private set; } = string.Empty;
    public decimal Resolution { get; private set; }
    public decimal Correction { get; private set; }
    public decimal ExpandedUncertainty { get; private set; }
    public decimal CoverageFactor { get; private set; }

    /// <summary>Valor operacional em que o padrão foi empregado (temperatura, pressão, umidade ou massa).</summary>
    public decimal OperatingValue { get; private set; }

    /// <summary>Indica se o ponto de ensaio extrapolou a margem de calibração do certificado.</summary>
    public bool IsExtrapolated { get; private set; }

    /// <summary>Mensagem de aviso metrológico em caso de extrapolação ou anomalia operacional.</summary>
    public string? WarningMessage { get; private set; }

    /// <summary>Prazo de validade do certificado do padrão no momento do ensaio (ISO 17025 6.4.13).</summary>
    public DateTime? CertificateDueDate { get; private set; }

    private CalibrationStandardUsed() { }

    public CalibrationStandardUsed(
        Guid calibrationId,
        StandardEquipment standard,
        StandardRole role)
    {
        ArgumentNullException.ThrowIfNull(standard);

        CalibrationId = calibrationId;
        StandardEquipmentId = standard.Id;
        Role = role;

        var activeCert = standard.GetActiveCertificate();
        StandardCertificateId = activeCert?.Id;

        // Snapshot congelado dos dados do padrão
        LabTag = standard.LabTag;
        Model = standard.Model;
        Manufacturer = standard.Manufacturer;
        SerialNumber = standard.SerialNumber;
        InstrumentType = standard.InstrumentType;
        CertificateNumber = activeCert?.CertificateNumber ?? standard.CalibrationCertificateNumber;
        CalibratingLaboratory = activeCert?.CalibratingLaboratory ?? standard.CalibratingLaboratory;
        Resolution = standard.Resolution;
        Correction = standard.Correction;
        ExpandedUncertainty = standard.ExpandedUncertainty;
        CoverageFactor = standard.CoverageFactor;
        OperatingValue = 0m;
        IsExtrapolated = false;
        WarningMessage = null;
        CertificateDueDate = activeCert?.CalibrationDueDate ?? standard.CalibrationDueDate;
    }

    public CalibrationStandardUsed(
        Guid calibrationId,
        StandardEquipment standard,
        StandardRole role,
        ResolvedStandardCorrection resolved,
        Guid? certificateId = null,
        string? certificateNumber = null,
        string? calibratingLab = null,
        DateTime? certificateDueDate = null)
    {
        ArgumentNullException.ThrowIfNull(standard);
        ArgumentNullException.ThrowIfNull(resolved);

        CalibrationId = calibrationId;
        StandardEquipmentId = standard.Id;
        StandardCertificateId = certificateId;
        Role = role;

        var activeCert = standard.GetActiveCertificate();

        LabTag = standard.LabTag;
        Model = standard.Model;
        Manufacturer = standard.Manufacturer;
        SerialNumber = standard.SerialNumber;
        InstrumentType = standard.InstrumentType;
        CertificateNumber = certificateNumber ?? activeCert?.CertificateNumber ?? standard.CalibrationCertificateNumber;
        CalibratingLaboratory = calibratingLab ?? activeCert?.CalibratingLaboratory ?? standard.CalibratingLaboratory;
        Resolution = standard.Resolution;

        OperatingValue = resolved.OperatingValue;
        Correction = resolved.CalculatedCorrection;
        ExpandedUncertainty = resolved.CalculatedUncertainty;
        CoverageFactor = resolved.CoverageFactor;
        IsExtrapolated = resolved.IsExtrapolated;
        WarningMessage = resolved.WarningMessage;
        CertificateDueDate = certificateDueDate ?? activeCert?.CalibrationDueDate ?? standard.CalibrationDueDate;
    }

    public CalibrationStandardUsed(
        Guid calibrationId,
        Guid standardEquipmentId,
        StandardRole role,
        string labTag,
        string model,
        string manufacturer,
        string serialNumber,
        string instrumentType,
        string certificateNumber,
        string calibratingLaboratory,
        decimal resolution,
        decimal correction,
        decimal expandedUncertainty,
        decimal coverageFactor,
        decimal operatingValue = 0m,
        bool isExtrapolated = false,
        string? warningMessage = null,
        Guid? standardCertificateId = null)
    {
        CalibrationId = calibrationId;
        StandardEquipmentId = standardEquipmentId;
        StandardCertificateId = standardCertificateId;
        Role = role;
        LabTag = labTag;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        CertificateNumber = certificateNumber;
        CalibratingLaboratory = calibratingLaboratory;
        Resolution = resolution;
        Correction = correction;
        ExpandedUncertainty = expandedUncertainty;
        CoverageFactor = coverageFactor;
        OperatingValue = operatingValue;
        IsExtrapolated = isExtrapolated;
        WarningMessage = warningMessage;
    }
}
