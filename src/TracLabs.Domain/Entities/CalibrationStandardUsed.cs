using TracLabs.Domain.Enums;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Registra a rastreabilidade imutável de um equipamento padrão utilizado em uma calibração,
/// congelando o snapshot de seus dados metrológicos (certificado, incerteza, correção, resolução)
/// exatamente como estavam no momento do ensaio, conforme o item 6.4.13 da ABNT NBR ISO/IEC 17025.
/// </summary>
public class CalibrationStandardUsed : BaseEntity
{
    public Guid CalibrationId { get; private set; }
    public CalibrationRecord? CalibrationRecord { get; private set; }

    public Guid StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

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

        // Snapshot congelado dos dados do padrão
        LabTag = standard.LabTag;
        Model = standard.Model;
        Manufacturer = standard.Manufacturer;
        SerialNumber = standard.SerialNumber;
        InstrumentType = standard.InstrumentType;
        CertificateNumber = standard.CalibrationCertificateNumber;
        CalibratingLaboratory = standard.CalibratingLaboratory;
        Resolution = standard.Resolution;
        Correction = standard.Error;
        ExpandedUncertainty = standard.ExpandedUncertainty;
        CoverageFactor = standard.CoverageFactor;
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
        decimal coverageFactor)
    {
        CalibrationId = calibrationId;
        StandardEquipmentId = standardEquipmentId;
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
    }
}
