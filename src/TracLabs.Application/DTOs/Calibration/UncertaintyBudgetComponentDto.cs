namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Linha individual do orçamento de incerteza metrológica conforme o Guia para a Expressão da Incerteza de Medição (GUM / JCGM 100)
/// e o documento orientador DOC-CGCRE-027 da Cgcre/Inmetro.
/// </summary>
public class UncertaintyBudgetComponentDto
{
    /// <summary>Descrição da fonte de incerteza (ex: "Balança - Resolução", "Ajuste do Menisco").</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Símbolo metrológico da grandeza de entrada (ex: "Mc", "Mv", "ra", "rw", "T", "Menisco").</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Valor estimado da grandeza de entrada na condição de ensaio.</summary>
    public decimal EstimatedValue { get; set; }

    /// <summary>Unidade da grandeza de entrada (ex: "g", "°C", "g/cm³", "hPa", "%").</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Incerteza padrão da grandeza de entrada u(xi).</summary>
    public decimal StandardUncertainty { get; set; }

    /// <summary>Tipo de distribuição de probabilidade adotada ("Normal", "Retangular").</summary>
    public string Distribution { get; set; } = "Normal";

    /// <summary>Divisor associado à distribuição (ex: 2 para normal k=2; sqrt(3) para retangular; sqrt(n) para repetibilidade).</summary>
    public decimal Divisor { get; set; }

    /// <summary>Coeficiente de sensibilidade ci = dV/dxi obtido pela derivada parcial da equação de mensurando.</summary>
    public decimal SensitivityCoefficient { get; set; }

    /// <summary>Contribuição de incerteza no mensurando ui(V) = |ci| * u(xi) em mL.</summary>
    public decimal UncertaintyContributionMl { get; set; }

    /// <summary>Graus de liberdade efetivos associados à fonte (vi).</summary>
    public decimal DegreesOfFreedom { get; set; }
}
