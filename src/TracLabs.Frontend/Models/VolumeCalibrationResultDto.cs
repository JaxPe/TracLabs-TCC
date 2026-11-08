using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

/// <summary>
/// Modelo com o resultado completo do cálculo metrológico retornado pela API
/// (POST /api/v1/calibrations/volume/calculate) conforme ISO 4787 e DOC-CGCRE-027.
/// </summary>
public class VolumeCalibrationResultDto
{
    public decimal NominalVolumeMl { get; set; }
    public int PointsCount { get; set; }
    public decimal MeanVolumeV20Ml { get; set; }
    public decimal SystematicErrorMl { get; set; }
    public decimal CorrectionMl { get; set; }
    public decimal AirDensityKgPerM3 { get; set; }
    public decimal AirDensityGramsPerCm3 { get; set; }
    public decimal AirDensityStandardUncertaintyKgPerM3 { get; set; }
    public decimal AirDensityExpandedUncertaintyKgPerM3 { get; set; }
    public decimal AirDensityCoverageFactor { get; set; }
    public decimal AirDensityEffectiveDegreesOfFreedom { get; set; }
    public decimal WaterDensityGramsPerCm3 { get; set; }
    public decimal RepeatabilityStandardDeviationMl { get; set; }
    public decimal RepeatabilityUncertaintyMl { get; set; }
    public decimal CombinedStandardUncertaintyMl { get; set; }
    public decimal EffectiveDegreesOfFreedom { get; set; }
    public decimal CoverageFactor { get; set; }
    public decimal ExpandedUncertaintyMl { get; set; }
    public decimal RelativeExpandedUncertaintyPercent { get; set; }
    public List<CalculatedPointDto> CalculatedPoints { get; set; } = new();
    public List<UncertaintyBudgetComponentDto> AirDensityUncertaintyBudget { get; set; } = new();
    public List<UncertaintyBudgetComponentDto> UncertaintyBudget { get; set; } = new();
}

public class CalculatedPointDto
{
    public int RunNumber { get; set; }
    public decimal FullMassGrams { get; set; }
    public decimal EmptyMassGrams { get; set; }
    public decimal ApparentWaterMassGrams => FullMassGrams - EmptyMassGrams;
    public decimal WaterTemperatureCelsius { get; set; }
    public decimal AmbientTemperatureCelsius { get; set; }
    public decimal AtmosphericPressureHpa { get; set; }
    public decimal RelativeHumidityPercent { get; set; }
    public decimal CalculatedAirDensityKgPerM3 { get; set; }
    public decimal CalculatedVolumeMl { get; set; }
}

public class UncertaintyBudgetComponentDto
{
    public string SourceName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal StandardUncertainty { get; set; }
    public string Distribution { get; set; } = "Normal";
    public decimal Divisor { get; set; }
    public decimal SensitivityCoefficient { get; set; }
    public decimal UncertaintyContributionMl { get; set; }
    public decimal DegreesOfFreedom { get; set; }
}

public class VolumeCalibrationCalculationRequest
{
    public decimal NominalVolumeMl { get; set; }
    public VolumetricMaterial Material { get; set; }
    public List<VolumeMeasurementInputDto> Measurements { get; set; } = new();
    public Guid BalanceStandardId { get; set; }
    public Guid LiquidThermometerStandardId { get; set; }
    public Guid BarometerStandardId { get; set; }
    public Guid? AmbientThermometerStandardId { get; set; }
    public Guid? HygrometerStandardId { get; set; }
    public Guid? ThermoHygrometerStandardId { get; set; }
    public decimal? ThermalExpansionCoefficient { get; set; }
    public decimal? MeniscusSemiIntervalMl { get; set; }
    public decimal? WaterDensityGcm3 { get; set; }
    public Guid? PycnometerStandardId { get; set; }
    public decimal? WaterDensityUncertaintyGcm3 { get; set; }
}

public class VolumeMeasurementInputDto
{
    public int RunNumber { get; set; }
    public decimal FullMassGrams { get; set; }
    public decimal EmptyMassGrams { get; set; }
    public decimal WaterTemperatureCelsius { get; set; }
    public decimal AmbientTemperatureCelsius { get; set; } = 20.0m;
    public decimal AtmosphericPressureHpa { get; set; } = 1013.25m;
    public decimal RelativeHumidityPercent { get; set; } = 50.0m;
}
