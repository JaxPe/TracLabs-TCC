namespace TracLabs.Frontend.Models;

/// <summary>
/// Representa um certificado de calibração acreditado (RBC/Inmetro) emitido para um instrumento padrão.
/// Mantém os metadados de acreditação, vigência, status de ciclo de vida e resumo metrológico.
/// Conforme ABNT NBR ISO/IEC 17025:2017 item 6.4.13.
/// </summary>
public class StandardCertificateDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identificador do equipamento padrão proprietário deste certificado.</summary>
    public Guid StandardEquipmentId { get; set; }

    /// <summary>Número identificador oficial do certificado emitido pelo laboratório RBC (ex: "CAL-RBC-2025-8841").</summary>
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>Nome ou razão social do laboratório acreditado que realizou o ensaio.</summary>
    public string CalibratingLaboratory { get; set; } = string.Empty;

    /// <summary>Data em que a calibração foi executada.</summary>
    public DateTime CalibrationDate { get; set; } = DateTime.UtcNow;

    /// <summary>Data limite de validade metrológica do certificado.</summary>
    public DateTime CalibrationDueDate { get; set; } = DateTime.UtcNow.AddYears(1);

    /// <summary>Faixa mínima acreditada no certificado (ex: 0.001 g ou -10.0 °C).</summary>
    public decimal CalibrationRangeMin { get; set; }

    /// <summary>Faixa máxima acreditada no certificado (ex: 220.0 g ou 110.0 °C).</summary>
    public decimal CalibrationRangeMax { get; set; }

    /// <summary>Indica se este é o certificado atualmente vigente/ativo para uso operacional do padrão.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>URL para download ou visualização do laudo digital em PDF do certificado.</summary>
    public string? CertificateUrl { get; set; }

    /// <summary>Hash SHA-256 para auditoria de autenticidade e integridade do certificado.</summary>
    public string? CertificateHash { get; set; }

    /// <summary>Método matemático de ajuste dos pontos (ex: "LinearRegression", "PiecewiseLinear").</summary>
    public string FitType { get; set; } = "LinearRegression";

    public decimal? Slope { get; set; }
    public decimal? Intercept { get; set; }
    public decimal? R2 { get; set; }

    // ── Resumo Metrológico no Ponto Médio / Pior Caso ──
    
    /// <summary>Erro sistemático representativo do certificado.</summary>
    public decimal Error { get; set; }

    /// <summary>Correção metrológica a ser somada às leituras (Correção = -Erro).</summary>
    public decimal Correction { get; set; }

    /// <summary>Incerteza expandida de medição U no fator de abrangência k informado.</summary>
    public decimal ExpandedUncertainty { get; set; }

    /// <summary>Fator de abrangência (tipicamente k=2.00 para 95.45% de confiança).</summary>
    public decimal CoverageFactor { get; set; } = 2.00m;

    // ── Propriedades Computadas para Renderização na Interface Blazor ──

    /// <summary>Indica se a data atual está dentro do prazo de validade metrológica.</summary>
    public bool IsValid => DateTime.UtcNow.Date <= CalibrationDueDate.Date;

    /// <summary>Dias restantes até a expiração da validade.</summary>
    public int DaysUntilExpiry => (CalibrationDueDate.Date - DateTime.UtcNow.Date).Days;

    /// <summary>Rótulo textual de status para badges e tabelas na UI.</summary>
    public string Status
    {
        get
        {
            if (!IsActive)
                return "Substituído / Histórico";
            if (!IsValid)
                return "Expirado / Vencido";
            if (DaysUntilExpiry <= 7)
                return "Vencimento Crítico";
            if (DaysUntilExpiry <= 30)
                return "Alerta de Vencimento";
            return "Vigente";
        }
    }
}
