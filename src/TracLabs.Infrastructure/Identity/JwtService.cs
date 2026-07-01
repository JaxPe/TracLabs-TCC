using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TracLabs.Application.Interfaces;

namespace TracLabs.Infrastructure.Identity;

/// <summary>Gera e valida tokens JWT para autenticação da API.</summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gera um token JWT assinado com as claims do usuário.
    /// O token carrega: userId, email, nome completo e role —
    /// tudo que o frontend precisa sem fazer outra requisição.
    /// </summary>
    public string GenerateToken(
        string userId,
        string email,
        string fullName,
        string role,
        out DateTime expiresAt)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret não configurado.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationHours = int.Parse(jwtSettings["ExpirationInHours"] ?? "8");
        expiresAt = DateTime.UtcNow.AddHours(expirationHours);

        // Claims são os dados embutidos no token — acessíveis sem consultar o banco
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("fullName", fullName),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}