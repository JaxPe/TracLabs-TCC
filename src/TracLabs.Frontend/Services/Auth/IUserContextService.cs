namespace TracLabs.Frontend.Services.Auth;

public enum UserRole
{
    Supervisor,
    Tecnico
}

public record UserProfile(
    string Id,
    string Name,
    string Email,
    UserRole Role,
    string AvatarInitials,
    string RegistrationNumber,
    string Qualification
)
{
    public string RoleDescription => Role == UserRole.Supervisor ? "Supervisor Técnico Metrológico" : "Técnico Metrologista Executor";
}

public interface IUserContextService
{
    UserProfile CurrentUser { get; }
    bool IsAuthenticated { get; }
    string? Token { get; }
    event Action? OnUserChanged;

    Task InitializeAsync();
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();

    void SetRole(UserRole role);
    void ToggleRole();
}
