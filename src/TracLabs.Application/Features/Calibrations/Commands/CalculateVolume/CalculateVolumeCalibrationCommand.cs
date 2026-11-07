using FluentValidation;
using MediatR;
using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Calibrations.Commands.CalculateVolume;

public record CalculateVolumeCalibrationCommand(
    decimal NominalVolumeMl = 0m,
    VolumetricMaterial Material = VolumetricMaterial.BorosilicateGlass33,
    List<VolumeMeasurementInputDto>? Measurements = null,
    Guid BalanceStandardId = default,
    Guid LiquidThermometerStandardId = default,
    Guid BarometerStandardId = default,
    Guid? AmbientThermometerStandardId = null,
    Guid? HygrometerStandardId = null,
    Guid? ThermoHygrometerStandardId = null,
    decimal? ThermalExpansionCoefficient = null,
    decimal? MeniscusSemiIntervalMl = null,
    VolumeCalibrationRequest? DirectRequest = null
) : IRequest<VolumeCalibrationResultDto>;

public class CalculateVolumeCalibrationCommandValidator : AbstractValidator<CalculateVolumeCalibrationCommand>
{
    public CalculateVolumeCalibrationCommandValidator()
    {
        When(c => c.DirectRequest != null, () =>
        {
            RuleFor(c => c.DirectRequest!.Measurements)
                .NotEmpty().WithMessage("A lista de medições gravimétricas é obrigatória.")
                .Must(m => m != null && m.Count >= 7)
                .WithMessage("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística conforme a ABNT NBR ISO 4787.");

            RuleFor(c => c.DirectRequest!.BalanceResolutionGrams)
                .GreaterThan(0).WithMessage("A resolução da balança deve ser maior que zero.");

            RuleFor(c => c.DirectRequest!.WaterThermometerResolutionCelsius)
                .GreaterThan(0).WithMessage("A resolução do termômetro de imersão (líquido) deve ser maior que zero.");

            RuleFor(c => c.DirectRequest!.BarometerResolutionHpa)
                .GreaterThan(0).WithMessage("A resolução do barômetro deve ser maior que zero.");

            RuleFor(c => c.DirectRequest!.AmbientThermometerResolutionCelsius)
                .GreaterThan(0).WithMessage("A resolução do termômetro ambiente deve ser maior que zero.");

            RuleFor(c => c.DirectRequest!.HygrometerResolutionPercent)
                .GreaterThan(0).WithMessage("A resolução do higrômetro deve ser maior que zero.");
        });

        When(c => c.DirectRequest == null, () =>
        {
            RuleFor(c => c.BalanceStandardId)
                .NotEmpty().WithMessage("O padrão de balança analítica é obrigatório para calibração volumétrica.");

            RuleFor(c => c.LiquidThermometerStandardId)
                .NotEmpty().WithMessage("O padrão de termômetro de imersão (líquido/água) é obrigatório para calibração volumétrica.");

            RuleFor(c => c.BarometerStandardId)
                .NotEmpty().WithMessage("O padrão de barômetro é obrigatório para calibração volumétrica.");

            RuleFor(c => c)
                .Must(HaveValidAmbientStandards)
                .WithMessage("É obrigatório informar o Termômetro Ambiente e o Higrômetro, ou um Termo-higrômetro único.");

            RuleFor(c => c.Measurements)
                .NotEmpty().WithMessage("A lista de medições gravimétricas é obrigatória.")
                .Must(m => m != null && m.Count >= 7)
                .WithMessage("A calibração volumétrica exige no mínimo 7 pontos de medição (repetições) para assegurar a confiabilidade estatística conforme a ABNT NBR ISO 4787.");
        });
    }

    private static bool HaveValidAmbientStandards(CalculateVolumeCalibrationCommand c)
    {
        if (c.ThermoHygrometerStandardId.HasValue && c.ThermoHygrometerStandardId.Value != Guid.Empty)
            return true;

        var hasAmb = c.AmbientThermometerStandardId.HasValue && c.AmbientThermometerStandardId.Value != Guid.Empty;
        var hasHyg = c.HygrometerStandardId.HasValue && c.HygrometerStandardId.Value != Guid.Empty;

        return hasAmb && hasHyg;
    }
}

public class CalculateVolumeCalibrationCommandHandler
    : IRequestHandler<CalculateVolumeCalibrationCommand, VolumeCalibrationResultDto>
{
    private readonly IVolumeStandardResolverService _standardResolver;
    private readonly IVolumeCalibrationService _volumeCalibrationService;

    public CalculateVolumeCalibrationCommandHandler(
        IVolumeStandardResolverService standardResolver,
        IVolumeCalibrationService volumeCalibrationService)
    {
        _standardResolver = standardResolver;
        _volumeCalibrationService = volumeCalibrationService;
    }

    public async Task<VolumeCalibrationResultDto> Handle(
        CalculateVolumeCalibrationCommand command, CancellationToken cancellationToken)
    {
        if (command.DirectRequest != null)
        {
            return _volumeCalibrationService.Calculate(command.DirectRequest);
        }

        var resolutionRequest = new VolumeStandardsResolutionRequest(
            command.NominalVolumeMl,
            command.Material,
            command.Measurements!,
            command.BalanceStandardId,
            command.LiquidThermometerStandardId,
            command.BarometerStandardId,
            command.AmbientThermometerStandardId,
            command.HygrometerStandardId,
            command.ThermoHygrometerStandardId,
            command.ThermalExpansionCoefficient,
            command.MeniscusSemiIntervalMl);

        var resolution = await _standardResolver.ResolveAsync(resolutionRequest, cancellationToken);
        var result = _volumeCalibrationService.Calculate(resolution.CalculationRequest);

        return result;
    }
}
