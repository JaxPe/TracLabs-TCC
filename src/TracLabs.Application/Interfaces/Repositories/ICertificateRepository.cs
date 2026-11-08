using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Repository contract for calibration Certificate entities.
/// Certificate queries are critical for the search and reprint features.
/// </summary>
public interface ICertificateRepository : IRepository<Certificate>
{
    /// <summary>
    /// Finds a certificate by its unique certificate number (e.g. "CERT-2025-00042").
    /// This is the primary way clients and auditors look up a specific certificate.
    /// </summary>
    Task<Certificate?> GetByCertificateNumberAsync(
        string certificateNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca um certificado pelo ID carregando todas as entidades associadas (calibração, equipamento, cliente e padrões).
    /// Essencial para a geração fiel do PDF do certificado de acordo com a ISO/IEC 17025.
    /// </summary>
    Task<Certificate?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all certificates issued for a specific equipment.
    /// </summary>
    Task<IReadOnlyList<Certificate>> GetByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all certificates issued to a specific client.
    /// Used on the client detail screen and for generating client reports.
    /// </summary>
    Task<IReadOnlyList<Certificate>> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all certificates issued within a date range.
    /// Drives the certificate issuance management report.
    /// </summary>
    Task<IReadOnlyList<Certificate>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all certificates, optionally filtered by status.
    /// </summary>
    Task<IReadOnlyList<Certificate>> GetAllAsync(
        CertificateStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns count of issued certificates since a given date.
    /// </summary>
    Task<int> GetIssuedCountSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and reserves the next sequential certificate number.
    /// This MUST be called inside a transaction to prevent duplicate numbers
    /// if two certificates are issued simultaneously.
    /// </summary>
    Task<string> GenerateNextCertificateNumberAsync(
        CancellationToken cancellationToken = default);
}
