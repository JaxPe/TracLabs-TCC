using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Resultados completos do cálculo metrológico de calibração volumétrica gravimétrica
/// conforme os requisitos da ISO 4787, ISO 8655 e DOC-CGCRE-027.
/// </summary>
public class VolumeCalibrationResultDto
{
    // ── Resumo Geral ────────────────────────────────────────────────────────

    /// <summary>Volume nominal do instrumento em mL.</summary>
    public decimal NominalVolumeMl { get; set; }

    /// <summary>Quantidade de medições/pesagens válidas realizadas (n).</summary>
    public int PointsCount { get; set; }

    /// <summary>Volume médio corrigido a 20°C (V20 médio) em mL.</summary>
    public decimal MeanVolumeV20Ml { get; set; }

    /// <summary>Erro sistemático de indicação (Erro = V20 - Vnominal) em mL.</summary>
    public decimal SystematicErrorMl { get; set; }

    /// <summary>Correção a aplicar na indicação (Correção = -Erro = Vnominal - V20) em mL.</summary>
    public decimal CorrectionMl { get; set; }

    // ── Condições Calculadas ────────────────────────────────────────────────

    /// <summary>Massa específica média do ar calculada (ra) em kg/m³.</summary>
    public decimal AirDensityKgPerM3 { get; set; }

    /// <summary>Massa específica média do ar em g/cm³.</summary>
    public decimal AirDensityGramsPerCm3 { get; set; }

    /// <summary>Incerteza padrão combinada da massa específica do ar uc(ra) em kg/m³.</summary>
    public decimal AirDensityStandardUncertaintyKgPerM3 { get; set; }

    /// <summary>Incerteza expandida da massa específica do ar U(ra) em kg/m³.</summary>
    public decimal AirDensityExpandedUncertaintyKgPerM3 { get; set; }

    /// <summary>Fator de abrangência k para a massa específica do ar.</summary>
    public decimal AirDensityCoverageFactor { get; set; }

    /// <summary>Graus de liberdade efetivos da massa específica do ar (Welch-Satterthwaite).</summary>
    public decimal AirDensityEffectiveDegreesOfFreedom { get; set; }

    /// <summary>Massa específica da água adotada/corrigida em g/cm³.</summary>
    public decimal WaterDensityGramsPerCm3 { get; set; }

    // ── Estatísticas e Incerteza GUM do Volume (V20) ─────────────────────────

    /// <summary>Desvio padrão amostral das repetições s(V20) em mL.</summary>
    public decimal RepeatabilityStandardDeviationMl { get; set; }

    /// <summary>Incerteza padrão de repetibilidade u(repe) = s / sqrt(n) em mL.</summary>
    public decimal RepeatabilityUncertaintyMl { get; set; }

    /// <summary>Incerteza padrão combinada uc(V) em mL.</summary>
    public decimal CombinedStandardUncertaintyMl { get; set; }

    /// <summary>Graus de liberdade efetivos calculados pela fórmula de Welch-Satterthwaite (veff).</summary>
    public decimal EffectiveDegreesOfFreedom { get; set; }

    /// <summary>Fator de abrangência k correspondente a 95.45% de confiança (obtido via t-Student para veff).</summary>
    public decimal CoverageFactor { get; set; }

    /// <summary>Incerteza expandida de medição U = k * uc(V) em mL (expressa no certificado).</summary>
    public decimal ExpandedUncertaintyMl { get; set; }

    /// <summary>Incerteza expandida relativa U_rel = (U / V20) * 100 em %.</summary>
    public decimal RelativeExpandedUncertaintyPercent { get; set; }

    // ── Detalhamento da Metrologia ──────────────────────────────────────────

    /// <summary>Lista de pontos de medição individuais com seus volumes calculados V20 e densidade do ar.</summary>
    public List<VolumeGravimetricPoint> CalculatedPoints { get; set; } = new();

    /// <summary>Tabela completa do orçamento de incerteza da massa específica do ar (9 fontes).</summary>
    public List<UncertaintyBudgetComponentDto> AirDensityUncertaintyBudget { get; set; } = new();

    /// <summary>Tabela completa do orçamento de incerteza do volume final.</summary>
    public List<UncertaintyBudgetComponentDto> UncertaintyBudget { get; set; } = new();
}
