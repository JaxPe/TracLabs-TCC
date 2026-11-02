namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Dados brutos de uma repetição de pesagem e das condições ambientais inseridas pelo técnico
/// no painel de calibração volumétrica, conforme a ABNT NBR ISO 4787 e DOC-CGCRE-027.
/// </summary>
public class VolumeMeasurementInputDto
{
    /// <summary>Índice da medição/corrida (1, 2, ..., n).</summary>
    public int RunNumber { get; set; }

    /// <summary>Massa bruta do frasco cheio de água pura ajustada ao menisco (MC em gramas).</summary>
    public decimal FullMassGrams { get; set; }

    /// <summary>Massa do frasco vazio e seco (MV em gramas).</summary>
    public decimal EmptyMassGrams { get; set; }

    /// <summary>Temperatura da água/líquido no momento do ensaio (T em °C).</summary>
    public decimal WaterTemperatureCelsius { get; set; }

    /// <summary>Temperatura ambiente média na bancada no momento da corrida em °C.</summary>
    public decimal AmbientTemperatureCelsius { get; set; } = 20.0m;

    /// <summary>Pressão atmosférica no momento da corrida em hPa (mbar).</summary>
    public decimal AtmosphericPressureHpa { get; set; } = 1013.25m;

    /// <summary>Umidade relativa do ar no momento da corrida em %.</summary>
    public decimal RelativeHumidityPercent { get; set; } = 50.0m;
}
