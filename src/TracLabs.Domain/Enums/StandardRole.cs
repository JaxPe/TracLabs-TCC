namespace TracLabs.Domain.Enums;

/// <summary>
/// Identifica a função/papel de um equipamento padrão na execução de uma calibração.
/// </summary>
public enum StandardRole
{
    /// <summary>Balança analítica utilizada para a determinação gravimétrica da massa.</summary>
    Balance = 0,

    /// <summary>Termômetro utilizado para medir a temperatura da água/líquido de ensaio.</summary>
    LiquidThermometer = 1,

    /// <summary>Termômetro utilizado para medir a temperatura ambiente do laboratório.</summary>
    AmbientThermometer = 2,

    /// <summary>Barômetro utilizado para medir a pressão atmosférica do laboratório.</summary>
    Barometer = 3,

    /// <summary>Higrômetro utilizado para medir a umidade relativa do ar no laboratório.</summary>
    Hygrometer = 4,

    /// <summary>Jogo de pesos-padrão de referência (classe E2/F1) para verificação da balança.</summary>
    ReferenceWeights = 5,

    /// <summary>Outro instrumento padrão auxiliar.</summary>
    Other = 99
}
