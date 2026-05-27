namespace TracLabs.Domain.Enums;

/// <summary>
/// Lifecycle states of client equipment within the laboratory workflow.
/// The flow is linear in the happy path:
/// Received → Approved → InCalibration → Calibrated → CertificateIssued → Returned
/// NonConforming is a terminal state reachable from Received or Calibrated.
/// </summary>
public enum EquipmentStatus
{
    /// <summary>Equipment arrived at the lab, awaiting initial inspection.</summary>
    Received = 0,

    /// <summary>Initial inspection passed, ready to be assigned to a technician.</summary>
    Approved = 1,

    /// <summary>Currently being calibrated by a technician.</summary>
    InCalibration = 2,

    /// <summary>Calibration procedure complete, certificate pending issuance.</summary>
    Calibrated = 3,

    /// <summary>Certificate issued, equipment ready for client pickup.</summary>
    CertificateIssued = 4,

    /// <summary>Equipment has been returned to the client.</summary>
    Returned = 5,

    /// <summary>
    /// Non-conformity detected — equipment failed inspection or calibration.
    /// Must be communicated to the client before any further action.
    /// </summary>
    NonConforming = 6
}