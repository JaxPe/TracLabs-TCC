namespace TracLabs.Application.Interfaces;

/// <summary>
/// Contrato para obtenção dos dados do usuário atualmente autenticado a partir dos claims JWT.
/// Desacopla a camada Application do HttpContext.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
