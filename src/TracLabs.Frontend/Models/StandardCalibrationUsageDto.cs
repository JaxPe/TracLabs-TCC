using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Models;

/// <summary>
/// Registra uma ocorrência histórica em que um equipamento padrão do laboratório
/// foi utilizado como referência metrológica na calibração do instrumento de um cliente.
/// Atende aos requisitos de rastreabilidade do item 6.4.13 da ISO/IEC 17025.
/// </summary>
public class StandardCalibrationUsageDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID do equipamento padrão utilizado.</summary>
    public Guid StandardId { get; set; }

    /// <summary>ID do ensaio / registro de calibração volumétrica.</summary>
    public Guid CalibrationId { get; set; }

    /// <summary>Data e hora em que a calibração foi executada.</summary>
    public DateTime CalibrationDate { get; set; } = DateTime.UtcNow;

    /// <summary>Razão social ou nome do cliente solicitante do ensaio.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Tag ou código de identificação do instrumento calibrado.</summary>
    public string EquipmentTag { get; set; } = string.Empty;

    /// <summary>Descrição do instrumento calibrado (ex: "Balão Volumétrico 100 mL Rolha Poli").</summary>
    public string EquipmentDescription { get; set; } = string.Empty;

    /// <summary>Volume nominal do instrumento calibrado.</summary>
    public decimal NominalVolume { get; set; }

    /// <summary>Unidade de medida volumétrica ("mL" ou "µL").</summary>
    public string Unit { get; set; } = "mL";

    /// <summary>Nome do operador ou técnico metrologista responsável pelo ensaio.</summary>
    public string TechnicianName { get; set; } = string.Empty;

    /// <summary>Número do certificado oficial emitido para o cliente (ex: "CERT-2026-00042").</summary>
    public string CertificateNumberIssued { get; set; } = string.Empty;

    /// <summary>Papel desempenhado pelo padrão no ensaio (Balança, Termômetro, Barômetro, etc.).</summary>
    public StandardRole RoleUsed { get; set; }

    /// <summary>Valor operacional registrado no padrão durante o ensaio (temperatura, pressão, massa ou umidade).</summary>
    public decimal OperatingValue { get; set; }

    /// <summary>Status do certificado emitido (ex: "Emitido", "Rascunho", "Aprovado").</summary>
    public string CertificateStatus { get; set; } = "Emitido";
}
