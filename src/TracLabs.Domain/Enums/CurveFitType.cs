namespace TracLabs.Domain.Enums;

/// <summary>
/// Método matemático utilizado para ajustar a curva de calibração do equipamento padrão
/// a partir dos pontos discretos do certificado RBC.
/// </summary>
public enum CurveFitType
{
    /// <summary>
    /// Regressão Linear por Mínimos Quadrados Ordinários (OLS) na forma y = ax + b,
    /// equivalente exato às funções SLOPE/INTERCEPT do Excel.
    /// </summary>
    LinearRegression = 0,

    /// <summary>
    /// Interpolação linear segmentada (piecewise linear) entre os dois pontos vizinhos mais próximos.
    /// </summary>
    PiecewiseLinear = 1,

    /// <summary>
    /// Média fixa das correções dos pontos calibrados (para instrumentos com erro homogêneo).
    /// </summary>
    FixedAverage = 2
}
