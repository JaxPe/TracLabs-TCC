using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace TracLabs.Frontend.Services.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class UserContextService : IUserContextService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime? _jsRuntime;
    private bool _initialized;

    private static readonly UserProfile DefaultSupervisor = new(
        Id: "usr-01",
        Name: "Dr. Alexandre Morales",
        Email: "supervisor@traclabs.com",
        Role: UserRole.Supervisor,
        AvatarInitials: "AM",
        RegistrationNumber: "CRQ IV 04218901",
        Qualification: "Supervisor Técnico Metrológico (ISO/IEC 17025)"
    );

    private static readonly UserProfile DefaultTecnico = new(
        Id: "usr-02",
        Name: "Carlos Eduardo Silva",
        Email: "tecnico@traclabs.com",
        Role: UserRole.Tecnico,
        AvatarInitials: "CS",
        RegistrationNumber: "CFT/CRT 198420-SP",
        Qualification: "Técnico Metrologista Executor"
    );

    public UserProfile CurrentUser { get; private set; } = DefaultSupervisor;
    public bool IsAuthenticated { get; private set; } = false;
    public string? Token { get; private set; }

    public event Action? OnUserChanged;

    public UserContextService(HttpClient? httpClient = null, IJSRuntime? jsRuntime = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_initialized || _jsRuntime == null) return;

        try
        {
            var storedToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "traclabs_token");
            var storedUser = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "traclabs_user");

            if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedUser))
            {
                var authUser = JsonSerializer.Deserialize<AuthResponse>(storedUser);
                if (authUser != null)
                {
                    Token = storedToken;
                    CurrentUser = MapFromAuth(authUser);
                    IsAuthenticated = true;
                    OnUserChanged?.Invoke();
                }
            }
            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // Prerender safe fallback
        }
        catch (Exception)
        {
            // Erro ao restaurar sessão
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", new
            {
                email,
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.Token))
            {
                return false;
            }

            Token = authResponse.Token;
            CurrentUser = MapFromAuth(authResponse);
            IsAuthenticated = true;

            if (_jsRuntime != null)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "traclabs_token", Token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "traclabs_user", JsonSerializer.Serialize(authResponse));
                }
                catch (InvalidOperationException) { }
            }

            OnUserChanged?.Invoke();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        Token = null;
        IsAuthenticated = false;
        CurrentUser = DefaultSupervisor;

        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "traclabs_token");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "traclabs_user");
            }
            catch (InvalidOperationException) { }
        }

        OnUserChanged?.Invoke();
    }

    public void SetRole(UserRole role)
    {
        CurrentUser = role == UserRole.Supervisor ? DefaultSupervisor : DefaultTecnico;
        OnUserChanged?.Invoke();
    }

    public void ToggleRole()
    {
        SetRole(CurrentUser.Role == UserRole.Supervisor ? UserRole.Tecnico : UserRole.Supervisor);
    }

    private static UserProfile MapFromAuth(AuthResponse auth)
    {
        var isSupervisor = auth.Role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase);
        return new UserProfile(
            Id: auth.UserId,
            Name: auth.FullName,
            Email: auth.Email,
            Role: isSupervisor ? UserRole.Supervisor : UserRole.Tecnico,
            AvatarInitials: GetInitials(auth.FullName),
            RegistrationNumber: isSupervisor ? "CRQ IV 04218901" : "CFT/CRT 198420-SP",
            Qualification: isSupervisor ? "Supervisor Técnico Metrológico (ISO/IEC 17025)" : "Técnico Metrologista Executor"
        );
    }

    private static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "TL";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
