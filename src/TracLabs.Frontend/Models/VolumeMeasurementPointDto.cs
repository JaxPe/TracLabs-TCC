namespace TracLabs.Frontend.Models;

/// <summary>
/// Representa uma leitura pontual (repetição) do ensaio gravimétrico de calibração volumétrica,
/// incluindo massas, temperatura da água e condições ambientais específicas da corrida (ISO 4787 / ISO 8655).
/// </summary>
public class VolumeMeasurementPointDto
{
    public int RunNumber { get; set; }
    public decimal EmptyMassGrams { get; set; }
    public decimal FullMassGrams { get; set; }
    public decimal WaterTemperatureCelsius { get; set; } = 20.0m;

    // Condições ambientais registradas individualmente por ponto
    public decimal AmbientTemperatureCelsius { get; set; } = 20.0m;
    public decimal AtmosphericPressureHpa { get; set; } = 1013.25m;
    public decimal RelativeHumidityPercent { get; set; } = 50.0m;

    // Grandezas metrológicas calculadas individualmente por ponto
    public decimal CalculatedAirDensity { get; set; }
    public decimal CalculatedZFactor { get; set; }

    // Propriedades calculadas e de validação
    public decimal ApparentWaterMassGrams => FullMassGrams > EmptyMassGrams ? FullMassGrams - EmptyMassGrams : 0m;
    public decimal CalculatedVolumeMl { get; set; }
    public decimal DeviationMl { get; set; }
    public bool HasError => FullMassGrams <= EmptyMassGrams && FullMassGrams > 0;
    public string? ErrorMessage => HasError ? "Massa cheia deve ser superior à tara vazia" : null;
}
