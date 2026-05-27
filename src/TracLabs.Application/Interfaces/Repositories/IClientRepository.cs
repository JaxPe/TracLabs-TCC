namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Repository contract for Client entities.
/// </summary>
public interface IClientRepository : IRepository<Client>
{
    /// <summary>
    /// Finds a client by their tax identification number (CNPJ or CPF).
    /// Used to prevent duplicate client registration.
    /// </summary>
    Task<Client?> GetByTaxIdAsync(
        string taxId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches clients by name (partial match).
    /// Used on the client search/autocomplete when registering equipment.
    /// </summary>
    Task<IReadOnlyList<Client>> SearchByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a client together with all their equipment.
    /// Used on the client detail screen.
    /// </summary>
    Task<Client?> GetWithEquipmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}