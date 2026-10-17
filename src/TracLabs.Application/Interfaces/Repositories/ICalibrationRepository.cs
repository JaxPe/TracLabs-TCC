namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Repository contract for CalibrationRecord entities.
/// Calibration records are the core audit trail of the system —
/// all queries here are designed around traceability requirements.
/// </summary>
public interface ICalibrationRepository : IRepository<CalibrationRecord>
{
    /// <summary>
    /// Returns all calibration records for a specific piece of equipment,
    /// ordered from most recent to oldest.
    /// This is the data source for the calibration history screen.
    /// </summary>
    Task<IReadOnlyList<CalibrationRecord>> GetHistoryByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all calibration records performed by a specific technician.
    /// Useful for technician performance reports and audit trails.
    /// </summary>
    Task<IReadOnlyList<CalibrationRecord>> GetByTechnicianIdAsync(
        string technicianId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all calibration records performed within a date range.
    /// Drives the management reports described in section 2.2.
    /// </summary>
    Task<IReadOnlyList<CalibrationRecord>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a calibration record together with the standard equipment used,
    /// the technician who performed it, and the resulting certificate.
    /// Used on the calibration detail screen — all related data in one query.
    /// </summary>
    Task<CalibrationRecord?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}