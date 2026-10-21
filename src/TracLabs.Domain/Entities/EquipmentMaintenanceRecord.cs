using TracLabs.Domain.Enums;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Registros de plano de manutenção e manutenções realizadas até o momento,
/// em atendimento mandatário ao item 6.4.13 (g) da ABNT NBR ISO/IEC 17025.
/// </summary>
public class EquipmentMaintenanceRecord : BaseEntity
{
    /// <summary>ID do equipamento do cliente (quando a manutenção for em instrumento de cliente).</summary>
    public Guid? EquipmentId { get; private set; }
    public Equipment? Equipment { get; private set; }

    /// <summary>ID do equipamento padrão (quando a manutenção for em padrão do laboratório).</summary>
    public Guid? StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

    /// <summary>Data em que a manutenção foi realizada.</summary>
    public DateTime MaintenanceDate { get; private set; }

    /// <summary>Tipo de manutenção (preventiva, corretiva, ajuste ou limpeza técnica).</summary>
    public MaintenanceType Type { get; private set; }

    /// <summary>Descrição detalhada dos serviços executados.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Nome do técnico, fornecedor ou laboratório terceirizado que executou a manutenção.</summary>
    public string PerformedBy { get; private set; } = string.Empty;

    /// <summary>Resumo do resultado pós-manutenção (ex: "Instrumento ajustado e operando conforme especificações").</summary>
    public string? ResultSummary { get; private set; }

    // Construtor privado para o EF Core
    private EquipmentMaintenanceRecord() { }

    public EquipmentMaintenanceRecord(
        DateTime maintenanceDate,
        MaintenanceType type,
        string description,
        string performedBy,
        string? resultSummary = null,
        Guid? equipmentId = null,
        Guid? standardEquipmentId = null)
    {
        if (equipmentId == null && standardEquipmentId == null)
            throw new ArgumentException("O registro de manutenção deve estar vinculado a um Equipment ou StandardEquipment.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição da manutenção não pode ser vazia.", nameof(description));

        if (string.IsNullOrWhiteSpace(performedBy))
            throw new ArgumentException("O responsável pela manutenção deve ser informado.", nameof(performedBy));

        EquipmentId = equipmentId;
        StandardEquipmentId = standardEquipmentId;
        MaintenanceDate = maintenanceDate;
        Type = type;
        Description = description;
        PerformedBy = performedBy;
        ResultSummary = resultSummary;
    }
}
