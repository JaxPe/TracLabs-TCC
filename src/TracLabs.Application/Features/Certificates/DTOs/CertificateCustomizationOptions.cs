namespace TracLabs.Application.Features.Certificates.DTOs;

/// <summary>
/// Estilo visual do template de certificado a ser renderizado pelo motor de PDF.
/// Permite adequar o documento à identidade visual do laboratório e perfil do cliente.
/// </summary>
public enum CertificateTemplateStyle
{
    /// <summary>
    /// Modelo Clássico RBC: Formato formal e rigoroso alinhado ao padrão da Rede Brasileira de Calibração (Cgcre/INMETRO).
    /// </summary>
    ClassicRbc = 0,

    /// <summary>
    /// Modelo Moderno: Layout contemporâneo com cartões informativos, linhas sutis e cabeçalhos destacados.
    /// </summary>
    ModernClean = 1,

    /// <summary>
    /// Modelo Compacto: Diagramação otimizada para condensar o certificado no menor número possível de páginas.
    /// </summary>
    Compact = 2
}

/// <summary>
/// Opções configuráveis pelo front-end para customização do certificado de calibração em PDF.
/// Permite flexibilidade de apresentação mantendo a integridade dos dados metrológicos no back-end.
/// </summary>
public record CertificateCustomizationOptions(
    string? LaboratoryName = null,
    string? LaboratoryTaxId = null,
    string? LaboratoryAddress = null,
    string? LaboratoryContact = null,
    string? AccreditationNumber = null,
    string? LogoBase64 = null,
    string? PrimaryColorHex = null,
    CertificateTemplateStyle TemplateStyle = CertificateTemplateStyle.ClassicRbc,
    bool ShowUncertaintyBudget = true,
    bool ShowDetailedPoints = true,
    bool ShowSignatures = true,
    string? CustomNotes = null
);
