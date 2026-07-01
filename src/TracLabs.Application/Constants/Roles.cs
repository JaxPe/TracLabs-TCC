namespace TracLabs.Application.Constants;

/// <summary>
/// Roles disponíveis no sistema tracLabs.
/// Usadas tanto no Identity quanto nos atributos [Authorize(Roles = "...")] dos controllers.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Técnico de calibração — executa procedimentos, registra medições.
    /// Não pode emitir certificados nem acessar relatórios gerenciais.
    /// </summary>
    public const string Tecnico = "Tecnico";

    /// <summary>
    /// Supervisor — aprova laudos, emite certificados, acessa todos os relatórios.
    /// Pode também fazer tudo que o Técnico faz.
    /// </summary>
    public const string Supervisor = "Supervisor";
}