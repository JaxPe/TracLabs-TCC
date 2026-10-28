namespace TracLabs.Domain.ValueObjects;

/// <summary>
/// Representa uma pesagem individual de calibração volumétrica pelo método gravimétrico
/// segundo a norma ABNT NBR ISO 4787:2013, ISO 8655 e DOC-CGCRE-027.
/// </summary>
public sealed record VolumeGravimetricPoint
{
    /// <summary>Número sequencial da repetição (1, 2, ..., n).</summary>
    public int RunNumber { get; init; }

    /// <summary>Massa do frasco/recipiente cheio de água pura com o menisco ajustado (MC em gramas).</summary>
    public decimal FullMassGrams { get; init; }

    /// <summary>Massa do frasco/recipiente vazio e seco (MV em gramas).</summary>
    public decimal EmptyMassGrams { get; init; }

    /// <summary>Massa aparente da água obtida por diferença de pesagem (mL = MC - MV em gramas).</summary>
    public decimal ApparentWaterMassGrams => FullMassGrams - EmptyMassGrams;

    /// <summary>Temperatura da água/líquido no momento do ensaio (T em °C).</summary>
    public decimal WaterTemperatureCelsius { get; init; }

    /// <summary>Temperatura ambiente média na bancada durante a corrida em °C.</summary>
    public decimal AmbientTemperatureCelsius { get; init; }

    /// <summary>Pressão atmosférica durante a corrida em hPa.</summary>
    public decimal AtmosphericPressureHpa { get; init; }

    /// <summary>Umidade relativa do ar durante a corrida em %.</summary>
    public decimal RelativeHumidityPercent { get; init; }

    /// <summary>Massa específica do ar calculada para esta corrida (ra em kg/m³).</summary>
    public decimal CalculatedAirDensityKgPerM3 { get; init; }

    /// <summary>Volume a 20°C calculado para esta repetição (V20 em mL ou cm³).</summary>
    public decimal CalculatedVolumeMl { get; init; }

    public VolumeGravimetricPoint(
        int runNumber,
        decimal fullMassGrams,
        decimal emptyMassGrams,
        decimal waterTemperatureCelsius,
        decimal calculatedVolumeMl = 0m,
        decimal ambientTemperatureCelsius = 20.0m,
        decimal atmosphericPressureHpa = 1013.25m,
        decimal relativeHumidityPercent = 50.0m,
        decimal calculatedAirDensityKgPerM3 = 0m)
    {
        if (runNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(runNumber), "Número da medição deve ser maior que zero.");
        if (fullMassGrams <= emptyMassGrams)
            throw new ArgumentException("Massa cheia deve ser estritamente maior que a massa vazia.");

        RunNumber = runNumber;
        FullMassGrams = fullMassGrams;
        EmptyMassGrams = emptyMassGrams;
        WaterTemperatureCelsius = waterTemperatureCelsius;
        CalculatedVolumeMl = calculatedVolumeMl;
        AmbientTemperatureCelsius = ambientTemperatureCelsius;
        AtmosphericPressureHpa = atmosphericPressureHpa;
        RelativeHumidityPercent = relativeHumidityPercent;
        CalculatedAirDensityKgPerM3 = calculatedAirDensityKgPerM3;
    }
}
