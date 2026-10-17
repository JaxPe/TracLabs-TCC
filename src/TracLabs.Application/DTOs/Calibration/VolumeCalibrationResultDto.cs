namespace TracLabs.Application.DTOs.Calibration;

public class VolumeCalibrationResultDto
{
    public int PointsCount { get; set; }
    public decimal MeanError { get; set; }
    public decimal MaxAbsoluteError { get; set; }
    public decimal CombinedStandardUncertainty { get; set; }
    public decimal ExpandedUncertainty { get; set; }
    public decimal CoverageFactor { get; set; }
}
