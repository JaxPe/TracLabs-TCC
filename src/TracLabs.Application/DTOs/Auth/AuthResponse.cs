namespace TracLabs.Application.DTOs.Auth;

/// <summary>
/// Retornado após login ou registro bem-sucedido.
/// O frontend armazena o Token e o envia em cada requisição subsequente.
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string UserId,
    string FullName,
    string Email,
    string Role
);