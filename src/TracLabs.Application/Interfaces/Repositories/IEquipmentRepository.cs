namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Repository contract for client Equipment entities.
/// Extends the generic base with queries specific to the equipment management
/// and calibration workflow screens.
/// </summary>
public interface IEquipmentRepository : IRepository<Equipment>
{
    /// <summary>
    /// Finds equipment by its lab-assigned tag (e.g. "EQ-2025-001").
    /// Used on the search screen and when scanning barcodes/QR codes.
    /// </summary>
    Task<Equipment?> GetByLabTagAsync(
        string labTag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all equipment belonging to a specific client.
    /// Used on the client detail screen to show their full equipment list.
    /// </summary>
    Task<IReadOnlyList<Equipment>> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all equipment currently in a given status.
    /// Drives the Kanban-style calibration workflow board on the dashboard.
    /// </summary>
    Task<IReadOnlyList<Equipment>> GetByStatusAsync(
        EquipmentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads equipment together with its full calibration history.
    /// Use this when navigating to the equipment detail/history screen.
    /// Separated from GetByIdAsync to avoid unnecessary JOINs on list views.
    /// </summary>
    Task<Equipment?> GetWithCalibrationHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advanced search combining multiple optional filters.
    /// All parameters are nullable — passing null means "ignore this filter".
    /// Drives the search screen described in section 2.2 of your pre-project.
    /// </summary>
    Task<IReadOnlyList<Equipment>> SearchAsync(
        string? labTag,
        string? serialNumber,
        string? model,
        string? clientName,
        EquipmentStatus? status,
        CancellationToken cancellationToken = default);
}