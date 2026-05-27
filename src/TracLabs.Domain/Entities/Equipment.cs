namespace TracLabs.Domain.Entities;

/// <summary>
/// Represents a piece of equipment submitted to the laboratory for calibration service.
/// This is the CLIENT's instrument, not the lab's own standard.
/// </summary>
public class Equipment : BaseEntity
{
    // ── Identification ──────────────────────────────────────────────────────────
    
    /// <summary>Internal tag/code assigned by the lab (e.g. "EQ-2025-001").</summary>
    public string LabTag { get; private set; } = string.Empty;
    
    /// <summary>Manufacturer's model or part number.</summary>
    public string Model { get; private set; } = string.Empty;
    
    /// <summary>Equipment manufacturer/brand name.</summary>
    public string Manufacturer { get; private set; } = string.Empty;
    
    /// <summary>Serial number as stamped on the physical instrument.</summary>
    public string SerialNumber { get; private set; } = string.Empty;
    
    /// <summary>
    /// Type/category of the instrument (e.g. "Torque Wrench", "Caliper", "Thermometer").
    /// In a production system this would reference a lookup table.
    /// </summary>
    public string InstrumentType { get; private set; } = string.Empty;
    
    // ── Measurement Range ───────────────────────────────────────────────────────
    
    /// <summary>Minimum value of the instrument's measurement range.</summary>
    public decimal RangeMin { get; private set; }
    
    /// <summary>Maximum value of the instrument's measurement range.</summary>
    public decimal RangeMax { get; private set; }
    
    /// <summary>Unit of measurement (e.g. "mm", "N.m", "°C").</summary>
    public string Unit { get; private set; } = string.Empty;
    
    /// <summary>Instrument resolution/scale division (menor divisão de escala).</summary>
    public decimal Resolution { get; private set; }
    
    // ── Ownership ───────────────────────────────────────────────────────────────
    
    /// <summary>The client/owner of this equipment. Foreign key to Client entity.</summary>
    public Guid ClientId { get; private set; }
    
    /// <summary>Navigation property — EF Core will populate this.</summary>
    public Client? Client { get; private set; }
    
    // ── Calibration Tracking ────────────────────────────────────────────────────
    
    /// <summary>Current calibration status of this equipment.</summary>
    public EquipmentStatus Status { get; private set; } = EquipmentStatus.Received;
    
    /// <summary>
    /// Collection of all calibration records for this equipment.
    /// This maintains the full calibration history required by ISO/IEC 17025 clause 7.5.
    /// </summary>
    public IReadOnlyCollection<CalibrationRecord> CalibrationHistory => 
        _calibrationHistory.AsReadOnly();
    
    // Backing field — EF Core requires this pattern for encapsulated collections
    private readonly List<CalibrationRecord> _calibrationHistory = new();
    
    // ── Constructor ─────────────────────────────────────────────────────────────
    
    // Private parameterless constructor required by EF Core
    private Equipment() { }
    
    public Equipment(
        string labTag, string model, string manufacturer, string serialNumber,
        string instrumentType, decimal rangeMin, decimal rangeMax,
        string unit, decimal resolution, Guid clientId)
    {
        // Domain validation — do not allow invalid state to ever exist
        if (string.IsNullOrWhiteSpace(labTag))
            throw new ArgumentException("Lab tag cannot be empty.", nameof(labTag));
        if (rangeMin >= rangeMax)
            throw new ArgumentException("Range minimum must be less than range maximum.");
        if (resolution <= 0)
            throw new ArgumentException("Resolution must be a positive value.");
            
        LabTag = labTag;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        Unit = unit;
        Resolution = resolution;
        ClientId = clientId;
    }
    
    // ── Domain Methods ───────────────────────────────────────────────────────────
    
    /// <summary>
    /// Transitions the equipment to "In Calibration" status.
    /// Status transitions should always go through domain methods, never set directly.
    /// </summary>
    public void StartCalibration()
    {
        if (Status != EquipmentStatus.Received && Status != EquipmentStatus.Approved)
            throw new InvalidOperationException(
                $"Cannot start calibration when equipment is in status '{Status}'.");
        
        Status = EquipmentStatus.InCalibration;
        MarkAsUpdated();
    }
    
    /// <summary>Marks calibration as complete and adds the record to history.</summary>
    public void CompleteCalibration(CalibrationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        
        _calibrationHistory.Add(record);
        Status = EquipmentStatus.Calibrated;
        MarkAsUpdated();
    }
    
    /// <summary>Returns the most recent calibration record, or null if never calibrated.</summary>
    public CalibrationRecord? GetLatestCalibration() =>
        _calibrationHistory.OrderByDescending(c => c.CalibrationDate).FirstOrDefault();
}