namespace TracLabs.Domain.Enums;

/// <summary>
/// Política adotada para resolver a incerteza expandida do padrão metrológico no ponto de operação.
/// </summary>
public enum UncertaintyPolicy
{
    /// <summary>
    /// Adota a pior (maior) incerteza expandida tabelada no certificado (abordagem conservadora aceita por auditores).
    /// </summary>
    WorstCase = 0,

    /// <summary>
    /// Calcula a incerteza expandida interpolada linearmente no ponto exato de operação.
    /// </summary>
    Interpolated = 1
}
