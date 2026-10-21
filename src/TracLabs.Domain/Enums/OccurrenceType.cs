namespace TracLabs.Domain.Enums;

/// <summary>
/// Tipos de ocorrências, danos, mau funcionamento ou modificações para atendimento
/// ao item 6.4.13 (h) e item 7.4.3 da ABNT NBR ISO/IEC 17025.
/// </summary>
public enum OccurrenceType
{
    /// <summary>Dano físico ou avaria constatada.</summary>
    Damage = 0,

    /// <summary>Mau funcionamento ou comportamento anômalo durante o uso.</summary>
    Malfunction = 1,

    /// <summary>Modificação estrutural, mecânica ou atualização técnica realizada.</summary>
    Modification = 2,

    /// <summary>Reparo realizado após dano ou falha.</summary>
    Repair = 3,

    /// <summary>Anomalia ou avaria identificada no ato do recebimento do instrumento (ISO 17025 item 7.4.3).</summary>
    ReceivingNonConformity = 4
}
