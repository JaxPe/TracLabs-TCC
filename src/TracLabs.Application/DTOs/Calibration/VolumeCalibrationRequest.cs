using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.DTOs.Calibration;

public class VolumeCalibrationRequest
{
    public List<MeasurementPoint> MeasurementPoints { get; set; } = new();
    public decimal StandardExpandedUncertainty { get; set; }
    public decimal StandardCoverageFactor { get; set; } = 2m;
    public decimal InstrumentResolution { get; set; }
    public decimal ResultCoverageFactor { get; set; } = 2m;
}
