using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

public class CalibrationRecordDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentTag { get; set; } = string.Empty;
    public string EquipmentDescription { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public decimal NominalCapacity { get; set; } = 100.0m;
    public string Unit { get; set; } = "mL";
    public string AccuracyClass { get; set; } = "Classe A";
    public decimal MaxPermissibleError { get; set; } = 0.100m;
    public VolumetricMaterial Material { get; set; } = VolumetricMaterial.SodaLimeGlass;
    public string StandardMethod { get; set; } = "ABNT NBR ISO 4787:2013";
    
    // Condições Ambientais de Referência / Médias do Ensaio
    public decimal WaterTemperatureCelsius { get; set; } = 20.03m;
    public decimal AmbientTemperatureCelsius { get; set; } = 20.20m;
    public decimal AtmosphericPressureHpa { get; set; } = 1018.0m;
    public decimal RelativeHumidityPercent { get; set; } = 68.0m;

    // Densidade Manual da Água (Picnometria ou Densimetria Digital)
    public decimal? WaterDensityGcm3 { get; set; }
    public string? PycnometerStandardId { get; set; }
    public bool IsManualWaterDensity => WaterDensityGcm3.HasValue && WaterDensityGcm3.Value > 0;

    // Padrões Rastreáveis Selecionados (ISO/IEC 17025 e DOC-CGCRE-027)
    public string BalanceStandardId { get; set; } = string.Empty;

    public string LiquidThermometerStandardId { get; set; } = string.Empty;

    /// <summary>
    /// Propriedade de compatibilidade retroativa para LiquidThermometerStandardId.
    /// </summary>
    public string ThermometerStandardId
    {
        get => LiquidThermometerStandardId;
        set => LiquidThermometerStandardId = value;
    }

    public string BarometerStandardId { get; set; } = string.Empty;
    public string? AmbientThermometerStandardId { get; set; }
    public string? HygrometerStandardId { get; set; }
    public string? ThermoHygrometerStandardId { get; set; }

    // Leituras Gravimétricas
    public List<VolumeMeasurementPointDto> Measurements { get; set; } = new();

    // Resultados Metrológicos Calculados
    public decimal CalculatedWaterDensity { get; set; }
    public decimal CalculatedAirDensity { get; set; }
    public decimal CalculatedZFactor { get; set; }
    public decimal MeanV20 { get; set; }
    public decimal SampleStdDev { get; set; }
    public decimal RepeatabilityUncertainty { get; set; }
    public decimal SystematicError { get; set; }
    public decimal RelativeSystematicErrorPercent { get; set; }
    public decimal CoefficientOfVariationPercent { get; set; }
    public decimal CombinedUncertainty { get; set; }
    public decimal CoverageFactor { get; set; } = 2.02m;
    public decimal ExpandedUncertainty { get; set; }
    public CalibrationResult Result { get; set; } = CalibrationResult.Pending;
    public string ConformityStatement { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TechnicianId { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;

    // Métodos e Helpers de Validação Metrológica
    public bool RequiresPycnometer => WaterDensityGcm3.HasValue && WaterDensityGcm3.Value > 0;
    public bool HasValidPycnometer => !RequiresPycnometer || !string.IsNullOrWhiteSpace(PycnometerStandardId);

    /// <summary>
    /// Valida se todos os padrões obrigatórios foram informados conforme as regras ISO 4787 / ISO 8655.
    /// </summary>
    public bool HasValidStandards()
    {
        if (string.IsNullOrWhiteSpace(BalanceStandardId)) return false;
        if (string.IsNullOrWhiteSpace(LiquidThermometerStandardId)) return false;
        if (string.IsNullOrWhiteSpace(BarometerStandardId)) return false;

        var hasThermoHygro = !string.IsNullOrWhiteSpace(ThermoHygrometerStandardId);
        var hasSeparateAmbient = !string.IsNullOrWhiteSpace(AmbientThermometerStandardId) &&
                                 !string.IsNullOrWhiteSpace(HygrometerStandardId);

        if (!hasThermoHygro && !hasSeparateAmbient) return false;

        if (RequiresPycnometer && !HasValidPycnometer) return false;

        return true;
    }
}
