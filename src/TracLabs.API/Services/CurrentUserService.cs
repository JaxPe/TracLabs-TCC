using System.Security.Claims;
using TracLabs.Application.Interfaces;

namespace TracLabs.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? FullName => User?.FindFirst("fullName")?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
