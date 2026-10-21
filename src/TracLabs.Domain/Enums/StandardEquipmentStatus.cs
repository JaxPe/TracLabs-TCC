namespace TracLabs.Domain.Enums;

/// <summary>
/// Ciclo de vida e estados de prontidão metrológica do equipamento padrão do laboratório.
/// Padrões só podem ser utilizados em calibrações quando estiverem no estado Calibrated.
/// </summary>
public enum StandardEquipmentStatus
{
    /// <summary>Padrão calibrado, dentro da validade metrológica e liberado para uso.</summary>
    Calibrated = 0,

    /// <summary>Prazo de calibração expirado ou pendente de checagem intermediária. Uso bloqueado.</summary>
    PendingCalibration = 1,

    /// <summary>Enviado para calibração em laboratório credenciado externo (RBC/INMETRO). Uso bloqueado.</summary>
    InCalibration = 2,

    /// <summary>Fora de serviço (danificado, reprovado em aceitação, quarentena ou descontinuado). Uso bloqueado.</summary>
    OutOfService = 3
}
