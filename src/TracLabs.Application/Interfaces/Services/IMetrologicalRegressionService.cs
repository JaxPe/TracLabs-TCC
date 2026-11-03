using TracLabs.Domain.ValueObjects;

namespace TracLabs.Application.Interfaces.Services;

/// <summary>
/// Serviço para ajuste de curvas metrológicas e interpolação de pontos discretos de certificados RBC.
/// Implementa Mínimos Quadrados Ordinários (OLS) na forma y = ax + b (equivalente a SLOPE/INTERCEPT do Excel).
/// </summary>
public interface IMetrologicalRegressionService
{
    /// <summary>
    /// Calcula a reta de regressão linear (y = ax + b) e o coeficiente de determinação (R²)
    /// a partir de pares ordenados (X, Y).
    /// </summary>
    (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<(decimal X, decimal Y)> points);

    /// <summary>
    /// Calcula a reta de regressão linear (y = ax + b) a partir de pontos de calibração padrão.
    /// </summary>
    (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<StandardCalibrationPoint> points);

    /// <summary>
    /// Interpola linearmente entre os dois pontos mais próximos de uma série discreta (piecewise linear).
    /// </summary>
    decimal InterpolatePiecewise(IEnumerable<(decimal X, decimal Y)> points, decimal targetX);

    /// <summary>
    /// Interpola linearmente a partir de pontos de calibração padrão.
    /// </summary>
    decimal InterpolatePiecewise(IEnumerable<StandardCalibrationPoint> points, decimal targetX);
}
