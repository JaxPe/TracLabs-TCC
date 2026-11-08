using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
{
    public CertificateRepository(TracLabsDbContext context) : base(context) { }

    public async Task<Certificate?> GetByCertificateNumberAsync(
        string certificateNumber, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, ct);

    public async Task<Certificate?> GetWithDetailsAsync(
        Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.Equipment)
                    .ThenInclude(e => e!.Client)
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.StandardsUsed)
                    .ThenInclude(s => s.StandardEquipment)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Certificate>> GetByEquipmentIdAsync(
        Guid equipmentId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.Equipment)
                    .ThenInclude(e => e!.Client)
            .Where(c => c.CalibrationRecord!.EquipmentId == equipmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certificate>> GetByClientIdAsync(
        Guid clientId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.Equipment)
                    .ThenInclude(e => e!.Client)
            .Where(c => c.CalibrationRecord!.Equipment!.ClientId == clientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certificate>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.Equipment)
                    .ThenInclude(e => e!.Client)
            .Where(c => c.IssuedAt >= from && c.IssuedAt <= to)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Certificate>> GetAllAsync(
        CertificateStatus? status = null, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(c => c.CalibrationRecord)
                .ThenInclude(r => r!.Equipment)
                    .ThenInclude(e => e!.Client)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetIssuedCountSinceAsync(
        DateTime since, CancellationToken ct = default)
        => await _dbSet.CountAsync(c => c.Status == CertificateStatus.Issued && c.IssuedAt >= since, ct);

    /// <summary>
    /// Gera e reserva de forma atômica o próximo número sequencial de certificado no formato CERT-YYYY-NNNNN.
    /// Utiliza bloqueio pessimista (SELECT ... FOR UPDATE) na tabela 'certificate_sequences' para
    /// eliminar race conditions em ambientes com emissões simultâneas, atendendo aos requisitos
    /// de integridade e rastreabilidade da norma ABNT NBR ISO/IEC 17025:2017 item 7.8.
    /// Reseta automaticamente a sequência a cada virada de ano civil.
    /// </summary>
    public async Task<string> GenerateNextCertificateNumberAsync(
        CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        CertificateSequence? sequence = null;

        // Em PostgreSQL, aplicamos lock pessimista na linha para serializar acessos simultâneos
        if (_context.Database.IsNpgsql())
        {
            sequence = await _context.CertificateSequences
                .FromSqlInterpolated($"SELECT * FROM certificate_sequences WHERE \"Year\" = {year} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            sequence = await _context.CertificateSequences
                .FirstOrDefaultAsync(s => s.Year == year, ct);
        }

        if (sequence is null)
        {
            sequence = new CertificateSequence(year, 0);
            await _context.CertificateSequences.AddAsync(sequence, ct);
        }

        var nextNumber = sequence.Increment();
        return $"CERT-{year}-{nextNumber:D5}";
    }
}
