namespace TracLabs.Domain.Entities;

/// <summary>
/// Represents the complete record of a single calibration procedure
/// performed on a piece of client equipment.
/// </summary>
public class CalibrationRecord : BaseEntity
{
    // ── Links to other entities ───────────────────────────────────────────────

    /// <summary>The equipment that was calibrated in this procedure.</summary>
    public Guid EquipmentId { get; private set; }
    public Equipment? Equipment { get; private set; }

    /// <summary>
    /// The lab standard used as the reference during this calibration.
    /// Stored by ID so we preserve a point-in-time link — even if the
    /// standard is later recalibrated and its values change, this record
    /// reflects what was used at the time.
    /// </summary>
    public Guid StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

    /// <summary>
    /// The technician (application user) who performed the calibration.
    /// Stored as a string (User ID from ASP.NET Identity) to avoid a
    /// hard project reference from Domain to Identity infrastructure.
    /// </summary>
    public string TechnicianId { get; private set; } = string.Empty;

    /// <summary>
    /// Technician's full name captured at calibration time.
    /// Denormalized intentionally — if the user account is renamed,
    /// historical records must still show who actually performed the work.
    /// </summary>
    public string TechnicianName { get; private set; } = string.Empty;

    // ── Procedure Metadata ────────────────────────────────────────────────────

    /// <summary>The date the calibration procedure was performed.</summary>
    public DateTime CalibrationDate { get; private set; }

    /// <summary>
    /// The date this calibration expires and a new one is required.
    /// Determined by the lab based on instrument type and usage frequency.
    /// </summary>
    public DateTime NextCalibrationDue { get; private set; }

    /// <summary>
    /// Environmental conditions recorded at time of calibration.
    /// ISO/IEC 17025 clause 6.4 requires monitoring environmental conditions.
    /// </summary>
    public decimal TemperatureCelsius { get; private set; }
    public decimal HumidityPercent { get; private set; }

    // ── Metrological Results ──────────────────────────────────────────────────
    // These are the numeric outputs of the calibration procedure.
    // Stored as a JSON-serializable value object to keep the table flat
    // while allowing a variable number of measurement points.

    /// <summary>
    /// The set of individual error measurements taken at each calibration point.
    /// E.g. for a caliper calibrated at 10mm, 50mm, 100mm — three error values.
    /// Serialized to JSON in the database (one column, no child table needed).
    /// </summary>
    public List<MeasurementPoint> MeasurementPoints { get; private set; } = new();

    /// <summary>Mean error calculated from all measurement points (erro médio).</summary>
    public decimal MeanError { get; private set; }

    /// <summary>Maximum absolute error across all measurement points.</summary>
    public decimal MaxAbsoluteError { get; private set; }

    /// <summary>
    /// Combined standard uncertainty calculated per GUM methodology.
    /// Accounts for standard uncertainty, resolution, and repeatability.
    /// </summary>
    public decimal CombinedStandardUncertainty { get; private set; }

    /// <summary>
    /// Expanded uncertainty at k=2 (~95% confidence), as reported on the certificate.
    /// U = k × uc
    /// </summary>
    public decimal ExpandedUncertainty { get; private set; }

    /// <summary>Coverage factor used for expanded uncertainty calculation (typically 2).</summary>
    public decimal CoverageFactor { get; private set; }

    // ── Conclusion ────────────────────────────────────────────────────────────

    /// <summary>
    /// The overall result of the calibration.
    /// Note: Many labs and ISO/IEC 17025 itself discourage pass/fail statements
    /// on calibration certificates (as opposed to inspection certificates),
    /// but we include it for labs that require conformity statements.
    /// </summary>
    public CalibrationResult Result { get; private set; }

    /// <summary>
    /// Free-text observations recorded by the technician.
    /// E.g. "Visible wear on measuring jaw", "Customer requested as-found data only".
    /// </summary>
    public string? Observations { get; private set; }

    // ── Certificate ───────────────────────────────────────────────────────────

    /// <summary>
    /// The certificate generated from this calibration record.
    /// Null until the certificate has been formally issued.
    /// </summary>
    public Certificate? Certificate { get; private set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    private CalibrationRecord() { }

    public CalibrationRecord(
        Guid equipmentId,
        Guid standardEquipmentId,
        string technicianId,
        string technicianName,
        DateTime calibrationDate,
        DateTime nextCalibrationDue,
        decimal temperatureCelsius,
        decimal humidityPercent)
    {
        if (string.IsNullOrWhiteSpace(technicianId))
            throw new ArgumentException("Technician ID cannot be empty.", nameof(technicianId));
        if (nextCalibrationDue <= calibrationDate)
            throw new ArgumentException("Next calibration due date must be after calibration date.");
        if (humidityPercent is < 0 or > 100)
            throw new ArgumentException("Humidity must be between 0 and 100.");

        EquipmentId = equipmentId;
        StandardEquipmentId = standardEquipmentId;
        TechnicianId = technicianId;
        TechnicianName = technicianName;
        CalibrationDate = calibrationDate;
        NextCalibrationDue = nextCalibrationDue;
        TemperatureCelsius = temperatureCelsius;
        HumidityPercent = humidityPercent;
        Result = CalibrationResult.Pending;
    }

    // ── Domain Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Records the metrological results calculated by the Application layer.
    /// Separated from the constructor because calculation happens after the
    /// record is initially created and the technician enters measurement data.
    /// </summary>
    public void RecordResults(
        List<MeasurementPoint> measurementPoints,
        decimal meanError,
        decimal maxAbsoluteError,
        decimal combinedStandardUncertainty,
        decimal expandedUncertainty,
        decimal coverageFactor,
        CalibrationResult result,
        string? observations)
    {
        if (measurementPoints == null || measurementPoints.Count == 0)
            throw new ArgumentException("At least one measurement point is required.");
        if (coverageFactor <= 0)
            throw new ArgumentException("Coverage factor must be positive.");

        MeasurementPoints = measurementPoints;
        MeanError = meanError;
        MaxAbsoluteError = maxAbsoluteError;
        CombinedStandardUncertainty = combinedStandardUncertainty;
        ExpandedUncertainty = expandedUncertainty;
        CoverageFactor = coverageFactor;
        Result = result;
        Observations = observations;
        MarkAsUpdated();
    }

    /// <summary>
    /// Links the issued certificate to this calibration record.
    /// Called by the certificate issuance use case.
    /// </summary>
    public void AttachCertificate(Certificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (Certificate != null)
            throw new InvalidOperationException(
                "A certificate has already been attached to this calibration record.");

        Certificate = certificate;
        MarkAsUpdated();
    }
}