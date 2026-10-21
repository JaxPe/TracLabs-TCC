using TracLabs.Domain.Enums;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Registros de danos, mau funcionamento, modificações ou reparos do equipamento,
/// em atendimento mandatário ao item 6.4.13 (h) e 7.4.3 da ABNT NBR ISO/IEC 17025.
/// </summary>
public class EquipmentOccurrenceRecord : BaseEntity
{
    /// <summary>ID do equipamento do cliente (quando a ocorrência for em instrumento de cliente).</summary>
    public Guid? EquipmentId { get; private set; }
    public Equipment? Equipment { get; private set; }

    /// <summary>ID do equipamento padrão (quando a ocorrência for em padrão do laboratório).</summary>
    public Guid? StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

    /// <summary>Data em que a ocorrência ou anomalia foi constatada.</summary>
    public DateTime OccurrenceDate { get; private set; }

    /// <summary>Classificação da ocorrência (Dano, Mau funcionamento, Modificação, Reparo ou Anomalia no Recebimento).</summary>
    public OccurrenceType Type { get; private set; }

    /// <summary>Descrição minuciosa do dano, mau funcionamento ou modificação.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Ação tomada imediatamente pelo laboratório (ex: "Instrumento segregado em quarentena", "Enviado à assistência").</summary>
    public string ActionTaken { get; private set; } = string.Empty;

    /// <summary>Nome do técnico ou colaborador que identificou e reportou a ocorrência.</summary>
    public string ReportedBy { get; private set; } = string.Empty;

    /// <summary>Data em que a ocorrência foi solucionada ou o reparo concluído, se aplicável.</summary>
    public DateTime? ResolvedDate { get; private set; }

    // Construtor privado para o EF Core
    private EquipmentOccurrenceRecord() { }

    public EquipmentOccurrenceRecord(
        DateTime occurrenceDate,
        OccurrenceType type,
        string description,
        string actionTaken,
        string reportedBy,
        DateTime? resolvedDate = null,
        Guid? equipmentId = null,
        Guid? standardEquipmentId = null)
    {
        if (equipmentId == null && standardEquipmentId == null)
            throw new ArgumentException("A ocorrência deve estar vinculada a um Equipment ou StandardEquipment.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição da ocorrência não pode ser vazia.", nameof(description));

        if (string.IsNullOrWhiteSpace(actionTaken))
            throw new ArgumentException("A ação tomada não pode ser vazia.", nameof(actionTaken));

        if (string.IsNullOrWhiteSpace(reportedBy))
            throw new ArgumentException("O responsável pelo relato não pode ser vazio.", nameof(reportedBy));

        EquipmentId = equipmentId;
        StandardEquipmentId = standardEquipmentId;
        OccurrenceDate = occurrenceDate;
        Type = type;
        Description = description;
        ActionTaken = actionTaken;
        ReportedBy = reportedBy;
        ResolvedDate = resolvedDate;
    }

    /// <summary>Marca a ocorrência como resolvida.</summary>
    public void MarkAsResolved(DateTime resolvedDate)
    {
        if (resolvedDate < OccurrenceDate)
            throw new ArgumentException("A data de resolução não pode ser anterior à data da ocorrência.");

        ResolvedDate = resolvedDate;
        MarkAsUpdated();
    }
}
