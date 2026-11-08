namespace TracLabs.Frontend.Models.Enums;

public enum CertificateStatus
{
    Draft = 0,            // Rascunho / Aguardando Aprovação Técnica
    Issued = 1,           // Emitido Oficialmente
    Superseded = 2        // Cancelado / Substituído
}
