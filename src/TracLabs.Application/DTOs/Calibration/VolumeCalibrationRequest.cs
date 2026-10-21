using TracLabs.Domain.Enums;

namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Parâmetros de entrada para calibração gravimétrica de instrumentos volumétricos
/// conforme as normas ABNT NBR ISO 4787:2013 e DOC-CGCRE-027.
/// Estes dados são orquestrados e preenchidos automaticamente pelo Use Case a partir
/// dos certificados dos padrões cadastrados no sistema e das medições do técnico.
/// </summary>
public class VolumeCalibrationRequest
{
    // ── Dados do Instrumento sob Calibração ─────────────────────────────────

    /// <summary>Capacidade/Volume nominal do instrumento sob calibração em mL (ex: 100 mL).</summary>
    public decimal NominalVolumeMl { get; set; }

    /// <summary>Tipo de material do instrumento (Borossilicato 3.3, Borossilicato 5.0, Soda-Cal, Polipropileno, etc.).</summary>
    public VolumetricMaterial Material { get; set; }

    /// <summary>
    /// Coeficiente de dilatação térmica volumétrica do recipiente em °C^-1 (gamma).
    /// Se informado, substitui o valor padrão do material, permitindo materiais customizados pelo laboratório.
    /// </summary>
    public decimal? ThermalExpansionCoefficient { get; set; }

    /// <summary>Incerteza padrão do coeficiente de dilatação térmica u(gamma).</summary>
    public decimal ThermalExpansionUncertainty { get; set; }

    // ── Condições Ambientais Lidas pelo Técnico durante o Ensaio ─────────────

    /// <summary>Pressão atmosférica média durante a calibração em hPa (mbar) lida no barômetro.</summary>
    public decimal AtmosphericPressureHpa { get; set; }

    /// <summary>Temperatura ambiente média do laboratório em °C lida no termômetro ambiente.</summary>
    public decimal AmbientTemperatureCelsius { get; set; }

    /// <summary>Umidade relativa do ar média no laboratório em % lida no higrômetro.</summary>
    public decimal RelativeHumidityPercent { get; set; }

    // ── Dados Metrológicos dos Padrões (Preenchidos pelo Sistema via Certificados) ──

    // Balança Analítica
    public decimal BalanceResolutionGrams { get; set; }
    public decimal BalanceCertificateUncertaintyGrams { get; set; }
    public decimal BalanceCoverageFactor { get; set; } = 2.0m;
    public decimal BalanceFullCorrectionGrams { get; set; }
    public decimal BalanceEmptyCorrectionGrams { get; set; }

    // Termômetro do Líquido (Água)
    public decimal WaterThermometerResolutionCelsius { get; set; }
    public decimal WaterThermometerUncertaintyCelsius { get; set; }
    public decimal WaterThermometerCoverageFactor { get; set; } = 2.0m;
    public decimal WaterThermometerCorrectionCelsius { get; set; }

    // Barômetro
    public decimal BarometerResolutionHpa { get; set; }
    public decimal BarometerUncertaintyHpa { get; set; }
    public decimal BarometerCoverageFactor { get; set; } = 2.0m;
    public decimal BarometerCorrectionHpa { get; set; }

    // Termômetro Ambiente
    public decimal AmbientThermometerResolutionCelsius { get; set; }
    public decimal AmbientThermometerUncertaintyCelsius { get; set; }
    public decimal AmbientThermometerCoverageFactor { get; set; } = 2.0m;
    public decimal AmbientThermometerCorrectionCelsius { get; set; }

    // Higrômetro
    public decimal HygrometerResolutionPercent { get; set; }
    public decimal HygrometerUncertaintyPercent { get; set; }
    public decimal HygrometerCoverageFactor { get; set; } = 2.0m;
    public decimal HygrometerCorrectionPercent { get; set; }

    // Padrão de Massa Específica da Água Pura
    /// <summary>
    /// Massa específica de referência da água a 20°C em g/cm³.
    /// Se zero ou nulo, é calculada automaticamente pela formulação de Tanaka et al. (ISO 4787).
    /// </summary>
    public decimal? WaterDensityReferenceGramsPerCm3 { get; set; }
    public decimal WaterDensityUncertaintyGramsPerCm3 { get; set; }
    public decimal WaterDensityCoverageFactor { get; set; } = 2.0m;

    // Massa específica dos pesos padrão de calibração da balança (padrão OIML: 8.0 g/cm³)
    public decimal WeightsDensityGramsPerCm3 { get; set; } = 8.0m;

    // ── Medições Gravimétricas (Série de Pesagens do Técnico) ───────────────

    /// <summary>Série de medições gravimétricas repetidas inseridas pelo técnico.</summary>
    public List<VolumeMeasurementInputDto> Measurements { get; set; } = new();

    /// <summary>
    /// Semi-intervalo de variação visual do menisco em mL (opcional, calculado automaticamente se nulo).
    /// </summary>
    public decimal? MeniscusSemiIntervalMl { get; set; }
}
