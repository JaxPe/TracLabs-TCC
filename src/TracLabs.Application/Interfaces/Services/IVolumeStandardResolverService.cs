using TracLabs.Application.DTOs.Calibration;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;
using StandardEquipment = TracLabs.Domain.Entities.StandardEquipment;

namespace TracLabs.Application.Interfaces.Services;

public record VolumeStandardsResolutionRequest(
    decimal NominalVolumeMl,
    VolumetricMaterial Material,
    List<VolumeMeasurementInputDto> Measurements,
    Guid BalanceStandardId,
    Guid LiquidThermometerStandardId,
    Guid BarometerStandardId,
    Guid? AmbientThermometerStandardId = null,
    Guid? HygrometerStandardId = null,
    Guid? ThermoHygrometerStandardId = null,
    decimal? ThermalExpansionCoefficient = null,
    decimal? MeniscusSemiIntervalMl = null,
    decimal? WaterDensityGcm3 = null,
    Guid? PycnometerStandardId = null,
    decimal? WaterDensityUncertaintyGcm3 = null
);

public record VolumeStandardsResolutionResult(
    VolumeCalibrationRequest CalculationRequest,
    StandardEquipment Balance,
    StandardEquipment LiquidThermometer,
    StandardEquipment Barometer,
    StandardEquipment AmbientThermometer,
    StandardEquipment Hygrometer,
    ResolvedStandardCorrection BalanceFullResolved,
    ResolvedStandardCorrection BalanceEmptyResolved,
    ResolvedStandardCorrection LiquidThermometerResolved,
    ResolvedStandardCorrection BarometerResolved,
    ResolvedStandardCorrection AmbientThermometerResolved,
    ResolvedStandardCorrection HygrometerResolved,
    List<string> Warnings,
    Func<Guid, List<CalibrationStandardUsed>> BuildStandardsUsed
);

/// <summary>
/// Serviço de orquestração e resolução metrológica dos padrões obrigatórios para calibração volumétrica.
/// Garante que todos os 4 a 5 padrões essenciais estejam presentes, aptos para uso, e tenham suas
/// correções e incertezas resolvidas dinamicamente via certificados RBC ativos.
/// </summary>
public interface IVolumeStandardResolverService
{
    Task<VolumeStandardsResolutionResult> ResolveAsync(
        VolumeStandardsResolutionRequest request,
        CancellationToken cancellationToken = default);
}
