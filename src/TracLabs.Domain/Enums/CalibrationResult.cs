namespace TracLabs.Domain.Enums;

/// <summary>
/// The outcome conclusion of a calibration procedure.
/// </summary>
public enum CalibrationResult
{
    /// <summary>Results not yet recorded — calibration is in progress.</summary>
    Pending = 0,

    /// <summary>
    /// Equipment errors are within the manufacturer's maximum permissible error (MPE).
    /// Conforms to specification.
    /// </summary>
    Conforming = 1,

    /// <summary>
    /// Equipment errors exceed the MPE.
    /// Certificate will include a non-conformity statement.
    /// </summary>
    NonConforming = 2,

    /// <summary>
    /// Certificate reports measured values only, without a conformity statement.
    /// This is the most common case for pure calibration (as opposed to inspection).
    /// </summary>
    InformationOnly = 3
}