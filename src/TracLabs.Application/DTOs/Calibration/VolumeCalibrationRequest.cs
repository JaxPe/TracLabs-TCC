using TracLabs.Domain.Enums;

namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Parâmetros de entrada completos para calibração gravimétrica de instrumentos volumétricos
/// conforme as normas ABNT NBR ISO 4787:2013 e DOC-CGCRE-027.
/// </summary>
public class VolumeCalibrationRequest
{
    // ── Dados do Instrumento sob Calibração ─────────────────────────────────

    /// <summary>Capacidade/Volume nominal do instrumento sob calibração em mL (ex: 100 mL).</summary>
    public decimal NominalVolumeMl { get; set; } = 100m;

    /// <summary>Tipo de material do instrumento (Borossilicato, Soda-Cal, etc.).</summary>
    public VolumetricMaterial Material { get; set; } = VolumetricMaterial.SodaLimeGlass;

    /// <summary>
    /// Coeficiente de dilatação térmica volumétrica do recipiente em °C^-1 (gamma).
    /// Se nulo ou zero, é deduzido automaticamente a partir do material (ex: 4.2e-5 para vidro soda-cal ou 9.9e-6 para borossilicato 3.3).
    /// </summary>
    public decimal? ThermalExpansionCoefficient { get; set; }

    /// <summary>Incerteza padrão do coeficiente de dilatação térmica (típico: 1.21e-6 °C^-1).</summary>
    public decimal ThermalExpansionUncertainty { get; set; } = 1.21e-6m;

    // ── Condições Ambientais do Laboratório ─────────────────────────────────

    /// <summary>Pressão atmosférica média durante a calibração em hPa (mbar) - (ex: 1018 hPa).</summary>
    public decimal AtmosphericPressureHpa { get; set; } = 1013.25m;

    /// <summary>Temperatura ambiente média do laboratório em °C (ex: 20.2 °C).</summary>
    public decimal AmbientTemperatureCelsius { get; set; } = 20.0m;

    /// <summary>Umidade relativa do ar média no laboratório em % (ex: 68 %).</summary>
    public decimal RelativeHumidityPercent { get; set; } = 60.0m;

    // ── Dados Metrológicos dos Padrões (Certificados de Calibração) ──────────

    // Balança Analítica
    public decimal BalanceResolutionGrams { get; set; } = 0.001m;
    public decimal BalanceCertificateUncertaintyGrams { get; set; } = 0.001m;
    public decimal BalanceCoverageFactor { get; set; } = 2.0m;
    public decimal BalanceFullCorrectionGrams { get; set; } = 0.0m;
    public decimal BalanceEmptyCorrectionGrams { get; set; } = 0.0m;

    // Termômetro do Líquido (Água)
    public decimal WaterThermometerResolutionCelsius { get; set; } = 0.01m;
    public decimal WaterThermometerUncertaintyCelsius { get; set; } = 0.005m;
    public decimal WaterThermometerCoverageFactor { get; set; } = 2.0m;
    public decimal WaterThermometerCorrectionCelsius { get; set; } = 0.0m;

    // Barômetro
    public decimal BarometerResolutionHpa { get; set; } = 1.0m;
    public decimal BarometerUncertaintyHpa { get; set; } = 1.2m;
    public decimal BarometerCoverageFactor { get; set; } = 2.0m;
    public decimal BarometerCorrectionHpa { get; set; } = -1.7m;

    // Termômetro Ambiente
    public decimal AmbientThermometerResolutionCelsius { get; set; } = 0.1m;
    public decimal AmbientThermometerUncertaintyCelsius { get; set; } = 0.02m;
    public decimal AmbientThermometerCoverageFactor { get; set; } = 2.0m;
    public decimal AmbientThermometerCorrectionCelsius { get; set; } = 0.01m;

    // Higrômetro
    public decimal HygrometerResolutionPercent { get; set; } = 1.0m;
    public decimal HygrometerUncertaintyPercent { get; set; } = 1.9m;
    public decimal HygrometerCoverageFactor { get; set; } = 2.0m;
    public decimal HygrometerCorrectionPercent { get; set; } = 0.9m;

    // Padrão de Densidade da Água Pura (ou picnômetro)
    /// <summary>
    /// Massa específica de referência da água a 20°C em g/cm³.
    /// Se zero ou nulo, o sistema calcula via equação padrão de Tanaka/Kell para água pura desaerada.
    /// </summary>
    public decimal? WaterDensityReferenceGramsPerCm3 { get; set; }
    public decimal WaterDensityUncertaintyGramsPerCm3 { get; set; } = 5.067e-5m;
    public decimal WaterDensityCoverageFactor { get; set; } = 2.126m;

    // Densidade dos pesos de calibração da balança (padrão OIML: 8.0 g/cm³)
    public decimal WeightsDensityGramsPerCm3 { get; set; } = 8.0m;

    // ── Medições (Série de Pesagens) ─────────────────────────────────────────

    /// <summary>Série de medições gravimétricas repetidas (recomenda-se de 6 a 11 pesagens).</summary>
    public List<VolumeMeasurementInputDto> Measurements { get; set; } = new();

    /// <summary>
    /// Semi-intervalo de variação visual do menisco em mL (se nulo, calculado via repetibilidade experimental: (Vmax - Vmin)/2).
    /// </summary>
    public decimal? MeniscusSemiIntervalMl { get; set; }
}
