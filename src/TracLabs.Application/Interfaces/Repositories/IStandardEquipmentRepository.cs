namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Repository contract for StandardEquipment (lab's own reference instruments).
/// These queries focus on calibration validity — an expired standard
/// cannot legally be used to calibrate client equipment.
/// </summary>
public interface IStandardEquipmentRepository : IRepository<StandardEquipment>
{
    /// <summary>
    /// Returns all standards whose calibration expires within the given number of days.
    /// This is the data source for the expiry alert system described in your pre-project.
    /// </summary>
    Task<IReadOnlyList<StandardEquipment>> GetExpiringSoonAsync(
        int withinDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all standards whose calibration has already expired.
    /// These must be flagged prominently in the UI and blocked from use.
    /// </summary>
    Task<IReadOnlyList<StandardEquipment>> GetExpiredAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only the standards with a currently valid calibration.
    /// Used to populate the "select standard" dropdown on the calibration panel —
    /// expired standards must not appear as selectable options.
    /// </summary>
    Task<IReadOnlyList<StandardEquipment>> GetValidStandardsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a standard by its lab tag.
    /// </summary>
    Task<StandardEquipment?> GetByLabTagAsync(
        string labTag,
        CancellationToken cancellationToken = default);
}