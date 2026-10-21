namespace TracLabs.Domain.Enums;

/// <summary>
/// Tipos de manutenção para atendimento ao item 6.4.13 (g) da ABNT NBR ISO/IEC 17025.
/// </summary>
public enum MaintenanceType
{
    /// <summary>Manutenção preventiva periódica programada.</summary>
    Preventive = 0,

    /// <summary>Manutenção corretiva após falha ou defeito.</summary>
    Corrective = 1,

    /// <summary>Ajuste metrológico / calibração interna do instrumento.</summary>
    CalibrationAdjustment = 2,

    /// <summary>Limpeza técnica ou descontaminação antes do uso.</summary>
    CleaningOrDecontamination = 3
}
