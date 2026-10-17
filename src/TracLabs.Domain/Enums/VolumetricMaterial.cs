namespace TracLabs.Domain.Enums;

/// <summary>
/// Materiais comuns utilizados na fabricação de vidrarias e instrumentos volumétricos de laboratório
/// (conforme ABNT NBR ISO 4787:2013 e DOC-CGCRE-027).
/// Define o coeficiente padrão de dilatação térmica volumétrica (gamma em °C^-1).
/// </summary>
public enum VolumetricMaterial
{
    /// <summary>Vidro Borossilicato 3.3 (gamma = 9.9 x 10^-6 °C^-1)</summary>
    BorosilicateGlass33 = 1,

    /// <summary>Vidro Borossilicato 5.0 (gamma = 1.5 x 10^-5 °C^-1)</summary>
    BorosilicateGlass50 = 2,

    /// <summary>Vidro Soda-Cal / Vidro Comum / Transparente (gamma = 2.5 x 10^-5 a 4.2 x 10^-5 °C^-1)</summary>
    SodaLimeGlass = 3,

    /// <summary>Polipropileno / Plástico laboratorial (gamma aprox. 4.5 x 10^-4 °C^-1)</summary>
    Polypropylene = 4,

    /// <summary>Material personalizado com coeficiente de dilatação térmica definido pelo usuário</summary>
    Custom = 99
}
