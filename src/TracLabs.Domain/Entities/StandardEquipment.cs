namespace TracLabs.Domain.Entities;

/// <summary>
/// Represents a laboratory standard instrument (equipamento padrão).
/// These are the reference instruments used TO calibrate client equipment.
/// They must themselves be calibrated and traceable to national/international standards (INMETRO/BIPM).
/// 
/// Stores the metrological data from the standard's own calibration certificate:
/// error, correction, and expanded uncertainty — critical for calculating
/// client equipment measurement uncertainty per GUM (Guide to the Expression of Uncertainty).
/// </summary>
public class StandardEquipment : BaseEntity
{
    public string LabTag { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string Manufacturer { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string InstrumentType { get; private set; } = string.Empty;
    
    // ── Metrological Data (from this standard's own calibration certificate) ───
    
    /// <summary>
    /// The systematic measurement error of this standard (erro sistemático).
    /// Error = Indicated Value - Reference Value.
    /// </summary>
    public decimal Error { get; private set; }
    
    /// <summary>
    /// The correction to apply to readings (correção = -Erro).
    /// Applied to compensate for the systematic error.
    /// </summary>
    public decimal Correction { get; private set; }
    
    /// <summary>
    /// Expanded measurement uncertainty at k=2 (approximately 95% confidence level).
    /// Required for uncertainty budget calculations per GUM.
    /// </summary>
    public decimal ExpandedUncertainty { get; private set; }
    
    /// <summary>Coverage factor used (typically k=2 for ~95% confidence).</summary>
    public decimal CoverageFactor { get; private set; } = 2;

    /// <summary>
    /// Instrument resolution / scale division (menor divisão da escala ou resolução digital).
    /// Fundamental para cálculo da componente de incerteza por distribuição retangular (d / (2*sqrt(3))).
    /// </summary>
    public decimal Resolution { get; private set; }
    
    // ── Calibration Validity ────────────────────────────────────────────────────
    
    /// <summary>Date this standard was last calibrated.</summary>
    public DateTime LastCalibrationDate { get; private set; }
    
    /// <summary>Date this standard's calibration expires. Must be monitored.</summary>
    public DateTime CalibrationDueDate { get; private set; }
    
    /// <summary>Certificate number from the accredited lab that calibrated this standard.</summary>
    public string CalibrationCertificateNumber { get; private set; } = string.Empty;
    
    /// <summary>Name of the accredited lab that performed this standard's calibration.</summary>
    public string CalibratingLaboratory { get; private set; } = string.Empty;
    
    // ── Computed Properties ──────────────────────────────────────────────────────
    
    /// <summary>
    /// Indicates whether this standard's calibration is currently valid.
    /// An expired standard CANNOT be used to calibrate client equipment.
    /// </summary>
    public bool IsCalibrationValid => DateTime.UtcNow.Date <= CalibrationDueDate.Date;
    
    /// <summary>Days remaining until calibration expiry. Negative means already expired.</summary>
    public int DaysUntilExpiry => (CalibrationDueDate.Date - DateTime.UtcNow.Date).Days;
    
    // EF Core constructor
    private StandardEquipment() { }
    
    public StandardEquipment(
        string labTag, string model, string manufacturer, string serialNumber,
        string instrumentType, decimal error, decimal expandedUncertainty,
        decimal coverageFactor, DateTime lastCalibrationDate, DateTime calibrationDueDate,
        string certificateNumber, string calibratingLaboratory,
        decimal resolution = 0m)
    {
        if (coverageFactor <= 0)
            throw new ArgumentException("Coverage factor must be positive.", nameof(coverageFactor));
        if (calibrationDueDate <= lastCalibrationDate)
            throw new ArgumentException("Due date must be after last calibration date.");
        if (resolution < 0)
            throw new ArgumentException("Resolution cannot be negative.", nameof(resolution));
            
        LabTag = labTag;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        Error = error;
        Correction = -error; // Correction is always the negative of error
        ExpandedUncertainty = expandedUncertainty;
        CoverageFactor = coverageFactor;
        LastCalibrationDate = lastCalibrationDate;
        CalibrationDueDate = calibrationDueDate;
        CalibrationCertificateNumber = certificateNumber;
        CalibratingLaboratory = calibratingLaboratory;
        Resolution = resolution;
    }
    
    /// <summary>
    /// Update metrological data after this standard receives a new calibration.
    /// </summary>
    public void UpdateCalibrationData(
        decimal newError, decimal newExpandedUncertainty, decimal coverageFactor,
        DateTime calibrationDate, DateTime dueDate,
        string newCertificateNumber, string calibratingLaboratory,
        decimal? newResolution = null)
    {
        if (dueDate <= calibrationDate)
            throw new ArgumentException("Due date must be after calibration date.");
        if (newResolution.HasValue && newResolution.Value < 0)
            throw new ArgumentException("Resolution cannot be negative.", nameof(newResolution));
            
        Error = newError;
        Correction = -newError;
        ExpandedUncertainty = newExpandedUncertainty;
        CoverageFactor = coverageFactor;
        LastCalibrationDate = calibrationDate;
        CalibrationDueDate = dueDate;
        CalibrationCertificateNumber = newCertificateNumber;
        CalibratingLaboratory = calibratingLaboratory;
        if (newResolution.HasValue)
            Resolution = newResolution.Value;

        MarkAsUpdated();
    }
}