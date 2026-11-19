using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

public class CertificateDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CertificateNumber { get; set; } = string.Empty;
    public string CalibrationRecordId { get; set; } = string.Empty;
    public string EquipmentTag { get; set; } = string.Empty;
    public string EquipmentDescription { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientTaxId { get; set; } = string.Empty;
    public string ClientAddress { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime CalibrationDate { get; set; } = DateTime.UtcNow;
    public DateTime NextCalibrationDueDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public CertificateStatus Status { get; set; } = CertificateStatus.Draft;
    public CalibrationResult Result { get; set; } = CalibrationResult.Conforming;

    public decimal NominalVolume { get; set; }
    public decimal MeanVolumeV20 { get; set; }
    public decimal SystematicError { get; set; }
    public decimal ExpandedUncertainty { get; set; }
    public decimal CoverageFactor { get; set; } = 2.02m;
    public string ConfidenceLevel { get; set; } = "95.45%";
    public decimal MaxPermissibleError { get; set; } = 0.100m;

    public List<StandardEquipmentDto> StandardsUsed { get; set; } = new();

    public string TechnicianName { get; set; } = "Carlos Eduardo Silva";
    public string TechnicianRegistration { get; set; } = "CFT/CRT 198420-SP";
    public string SupervisorName { get; set; } = "Dr. Alexandre Morales";
    public string SupervisorRegistration { get; set; } = "CRQ IV 04218901";

    public string QrCodeValidationUrl { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public DateTime? ApprovalDate { get; set; }
    public string? CancellationReason { get; set; }
}
