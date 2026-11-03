namespace TracLabs.Domain.ValueObjects;

/// <summary>
/// Resultado da resolução metrológica de um padrão no ponto de operação do ensaio,
/// contendo a correção e incerteza calculadas via curva (regressão/interpolação) ou pior caso,
/// juntamente com sinalização de extrapolação de faixa.
/// </summary>
public record ResolvedStandardCorrection(
    decimal OperatingValue,
    decimal CalculatedCorrection,
    decimal CalculatedUncertainty,
    decimal CoverageFactor,
    bool IsExtrapolated,
    string? WarningMessage,
    string MethodUsed);
