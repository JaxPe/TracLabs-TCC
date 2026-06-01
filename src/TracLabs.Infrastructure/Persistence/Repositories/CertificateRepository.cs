using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
{
    public CertificateRepository(TracLabsDbContext context) : base(context) { }

    public async Task<Certificate?> GetByCertificateNumberAsync(
        string certificateNumber, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, ct);

    public async Task<IReadOnlyList<Certificate>> GetByEquipmentIdAsync(
        Guid equipmentId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.CalibrationRecord!.EquipmentId == equipmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certificate>> GetByClientIdAsync(
        Guid clientId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.CalibrationRecord!.Equipment!.ClientId == clientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certificate>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.IssuedAt >= from && c.IssuedAt <= to)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

    public async Task<string> GenerateNextCertificateNumberAsync(
        CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        // Conta quantos certificados já foram emitidos no ano corrente
        var count = await _dbSet
            .CountAsync(c => c.CreatedAt.Year == year, ct);

        // Formato: CERT-2025-00001
        return $"CERT-{year}-{(count + 1):D5}";
    }
}