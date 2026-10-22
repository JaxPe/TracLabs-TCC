using MediatR;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces;

namespace TracLabs.Application.Features.Calibrations.Commands.CalculateVolume;

public record CalculateVolumeCalibrationCommand(VolumeCalibrationRequest Request)
    : IRequest<VolumeCalibrationResultDto>;

public class CalculateVolumeCalibrationCommandHandler
    : IRequestHandler<CalculateVolumeCalibrationCommand, VolumeCalibrationResultDto>
{
    private readonly IVolumeCalibrationService _volumeCalibrationService;

    public CalculateVolumeCalibrationCommandHandler(IVolumeCalibrationService volumeCalibrationService)
    {
        _volumeCalibrationService = volumeCalibrationService;
    }

    public Task<VolumeCalibrationResultDto> Handle(
        CalculateVolumeCalibrationCommand command, CancellationToken cancellationToken)
    {
        var result = _volumeCalibrationService.Calculate(command.Request);
        return Task.FromResult(result);
    }
}
