namespace TracLabs.Domain.Enums;

/// <summary>
/// Lifecycle states of a calibration certificate.
/// </summary>
public enum CertificateStatus
{
    /// <summary>Generated but not yet reviewed or formally issued.</summary>
    Draft = 0,

    /// <summary>Formally issued — this is the official, deliverable document.</summary>
    Issued = 1,

    /// <summary>
    /// Replaced by a corrected certificate. Superseded certificates must be
    /// retained in the system for traceability but clearly marked as invalid.
    /// </summary>
    Superseded = 2
}