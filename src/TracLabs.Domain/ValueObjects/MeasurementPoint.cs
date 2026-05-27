namespace TracLabs.Domain.ValueObjects;

/// <summary>
/// Represents a single data point recorded during a calibration procedure.
/// For example, calibrating a caliper at the 50mm point:
///   NominalValue    = 50.000 mm  (the reference/pattern value)
///   IndicatedValue  = 50.012 mm  (what the instrument under test displayed)
///   Error           = +0.012 mm  (IndicatedValue - NominalValue)
///   Correction      = -0.012 mm  (value to add to correct the reading)
/// </summary>
public sealed record MeasurementPoint
{
    /// <summary>The reference value provided by the standard (valor nominal).</summary>
    public decimal NominalValue { get; init; }

    /// <summary>The value displayed by the instrument under test (valor indicado).</summary>
    public decimal IndicatedValue { get; init; }

    /// <summary>
    /// Systematic error at this point. Calculated as: IndicatedValue - NominalValue.
    /// Positive error means the instrument reads high.
    /// </summary>
    public decimal Error => IndicatedValue - NominalValue;

    /// <summary>
    /// Correction at this point. Calculated as: NominalValue - IndicatedValue.
    /// Always the negative of the error.
    /// </summary>
    public decimal Correction => NominalValue - IndicatedValue;

    /// <summary>Unit of measurement for this point (inherited from the equipment).</summary>
    public string Unit { get; init; } = string.Empty;

    public MeasurementPoint(decimal nominalValue, decimal indicatedValue, string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit cannot be empty.", nameof(unit));

        NominalValue = nominalValue;
        IndicatedValue = indicatedValue;
        Unit = unit;
    }
}