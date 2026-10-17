using TracLabs.Application.DTOs.Calibration;

namespace TracLabs.Application.Interfaces;

public interface IVolumeCalibrationService
{
    VolumeCalibrationResultDto Calculate(VolumeCalibrationRequest request);
}
