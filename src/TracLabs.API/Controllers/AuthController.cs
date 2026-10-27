using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TracLabs.Application.Constants;
using TracLabs.Application.DTOs.Auth;
using TracLabs.Application.Interfaces;
using TracLabs.Infrastructure.Identity;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Registra um novo usuário (técnico ou supervisor).
    /// Em produção, restringir este endpoint para [Authorize(Roles = Roles.Supervisor)].
    /// Durante o desenvolvimento, deixamos aberto para facilitar os testes.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Valida se a role informada é válida
        if (request.Role != Roles.Tecnico && request.Role != Roles.Supervisor)
            return BadRequest(new { message = "Role inválida. Use 'Tecnico' ou 'Supervisor'." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            ProfessionalRegistration = request.ProfessionalRegistration
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Falha ao criar usuário.", errors });
        }

        // Atribui a role ao usuário recém-criado
        await _userManager.AddToRoleAsync(user, request.Role);

        var token = _jwtService.GenerateToken(
            user.Id, user.Email!, user.FullName, request.Role, out var expiresAt);

        return Ok(new AuthResponse(token, expiresAt, user.Id, user.FullName, user.Email!, request.Role));
    }

    /// <summary>Autentica um usuário existente e retorna o token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Credenciais inválidas." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Conta bloqueada. Tente novamente mais tarde." });

            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        // Busca a role do usuário para incluir no token
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Tecnico;

        var token = _jwtService.GenerateToken(
            user.Id, user.Email!, user.FullName, role, out var expiresAt);

        return Ok(new AuthResponse(token, expiresAt, user.Id, user.FullName, user.Email!, role));
    }
}
