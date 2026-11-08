using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

public class CalibrationTimelineEventDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TimeString { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EquipmentTag { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public CalibrationResult Result { get; set; }
    public string Details { get; set; } = string.Empty;
}
