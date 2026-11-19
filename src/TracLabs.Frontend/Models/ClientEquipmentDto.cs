using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

public class ClientEquipmentDto
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string LabTag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = string.Empty;
    public decimal? RangeMin { get; set; }
    public decimal? RangeMax { get; set; }
    public string CurrentLocation { get; set; } = "Bancada de Recepção";
    public decimal NominalCapacity { get; set; }
    public string Unit { get; set; } = "mL";
    public string AccuracyClass { get; set; } = "Classe A";
    public decimal MaxPermissibleError { get; set; }
    public int CalibrationIntervalMonths { get; set; } = 12;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Received;
    public DateTime? LastCalibrationDate { get; set; }
    public DateTime? NextCalibrationDue { get; set; }
}
