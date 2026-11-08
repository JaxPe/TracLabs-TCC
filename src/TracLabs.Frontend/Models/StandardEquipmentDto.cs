using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

public class StandardEquipmentDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string LabTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = string.Empty;
    public StandardRole Role { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public decimal Resolution { get; set; }
    public decimal RangeMin { get; set; }
    public decimal RangeMax { get; set; }
    public StandardEquipmentStatus Status { get; set; } = StandardEquipmentStatus.Calibrated;
    public string CurrentLocation { get; set; } = string.Empty;
    public string CalibrationCertificateNumber { get; set; } = string.Empty;
    public string CalibratingLaboratory { get; set; } = string.Empty;
    public DateTime LastCalibrationDate { get; set; }
    public DateTime CalibrationDueDate { get; set; }
    public decimal ExpandedUncertainty { get; set; }
    public decimal CoverageFactor { get; set; } = 2.0m;
    public decimal Error { get; set; }
    public decimal Correction { get; set; }
    
    public int DaysUntilExpiry => (CalibrationDueDate.Date - DateTime.UtcNow.Date).Days;
    public bool IsValid => Status == StandardEquipmentStatus.Calibrated && DaysUntilExpiry >= 0;
}
