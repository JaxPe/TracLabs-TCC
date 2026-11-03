namespace TracLabs.Domain.ValueObjects;

/// <summary>
/// Representa uma linha/ponto discreto da tabela de calibração do certificado RBC de um equipamento padrão.
/// Ex: Ponto 20°C -> Erro: -0.2°C, Correção: +0.2°C, Incerteza U: 0.1°C, Fator k: 2.0.
/// </summary>
public record StandardCalibrationPoint(
    int Order,
    decimal IndicatedValue,
    decimal StandardError,
    decimal Correction,
    decimal ExpandedUncertainty,
    decimal CoverageFactor = 2.0m);
