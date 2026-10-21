namespace TracLabs.Domain.Entities;

/// <summary>
/// Controla o número sequencial atômico de certificados emitidos por ano civil (ex: 2026).
/// Projetado para garantir unicidade e integridade transacional contra concorrência via locks pessimistas (SELECT FOR UPDATE).
/// Em auditorias de metrologia (ABNT NBR ISO/IEC 17025:2017 item 7.8), a numeração de certificados deve ser única,
/// sequencial e sem risco de duplicação em ambientes concorrentes.
/// </summary>
public class CertificateSequence
{
    /// <summary>Ano civil de referência da numeração (ex: 2026).</summary>
    public int Year { get; private set; }

    /// <summary>Último número sequencial reservado para o respectivo ano (inicia em 0).</summary>
    public int LastNumber { get; private set; }

    /// <summary>Data e hora da última emissão/incremento registrado.</summary>
    public DateTime UpdatedAt { get; private set; }

    // Construtor protegido exigido pelo EF Core
    private CertificateSequence() { }

    public CertificateSequence(int year, int initialNumber = 0)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Ano de sequência inválido.");

        Year = year;
        LastNumber = initialNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa atômica e sequencialmente o próximo número para o ano corrente.
    /// Retorna o novo número gerado.
    /// </summary>
    public int Increment()
    {
        LastNumber++;
        UpdatedAt = DateTime.UtcNow;
        return LastNumber;
    }
}
