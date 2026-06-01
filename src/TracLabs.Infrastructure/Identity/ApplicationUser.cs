using Microsoft.AspNetCore.Identity;

namespace TracLabs.Infrastructure.Identity;

/// <summary>
/// Usuário da aplicação — representa técnicos e supervisores do laboratório.
/// Estende o IdentityUser do ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Nome completo do técnico, usado nos certificados emitidos.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Registro profissional opcional (ex: CREA, CFQ).
    /// Alguns laboratórios exigem que conste no certificado.
    /// </summary>
    public string? ProfessionalRegistration { get; set; }

    /// <summary>Data de cadastro no sistema.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Indica se o usuário está ativo. Usar ao invés de deletar a conta.</summary>
    public bool IsActive { get; set; } = true;
}