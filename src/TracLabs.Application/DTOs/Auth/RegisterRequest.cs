namespace TracLabs.Application.DTOs.Auth;

/// <summary>Dados necessários para registrar um novo usuário técnico.</summary>
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role,                        // "Tecnico" ou "Supervisor"
    string? ProfessionalRegistration    // CREA, CFQ etc. — opcional
);