namespace TracLabs.Application.DTOs.Calibration;

/// <summary>
/// Dados brutos de uma repetição de pesagem inserida pelo técnico no painel de calibração volumétrica.
/// </summary>
public class VolumeMeasurementInputDto
{
    /// <summary>Índice da medição (1, 2, ..., n).</summary>
    public int RunNumber { get; set; }

    /// <summary>Massa bruta do frasco cheio de água pura ajustada ao menisco (MC em gramas).</summary>
    public decimal FullMassGrams { get; set; }

    /// <summary>Massa do frasco vazio e seco (MV em gramas).</summary>
    public decimal EmptyMassGrams { get; set; }

    /// <summary>Temperatura da água/líquido no momento do ensaio (T em °C).</summary>
    public decimal WaterTemperatureCelsius { get; set; }
}
