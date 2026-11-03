using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class StandardEquipmentRepository
    : BaseRepository<StandardEquipment>, IStandardEquipmentRepository
{
    public StandardEquipmentRepository(TracLabsDbContext context) : base(context) { }

    public async Task<IReadOnlyList<StandardEquipment>> GetExpiringSoonAsync(
        int withinDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return await _dbSet
            .Include(s => s.Certificates)
            .Where(s => s.Status == StandardEquipmentStatus.Calibrated &&
                        s.CalibrationDueDate <= cutoff &&
                        s.CalibrationDueDate >= DateTime.UtcNow)
            .OrderBy(s => s.CalibrationDueDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StandardEquipment>> GetExpiredAsync(
        CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Certificates)
            .Where(s => s.CalibrationDueDate < DateTime.UtcNow ||
                        s.Status == StandardEquipmentStatus.PendingCalibration)
            .OrderBy(s => s.CalibrationDueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StandardEquipment>> GetValidStandardsAsync(
        CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Certificates)
            .Where(s => s.Status == StandardEquipmentStatus.Calibrated &&
                        s.CalibrationDueDate >= DateTime.UtcNow)
            .OrderBy(s => s.InstrumentType)
            .ToListAsync(ct);

    public async Task<StandardEquipment?> GetByLabTagAsync(
        string labTag, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Certificates)
            .FirstOrDefaultAsync(s => s.LabTag == labTag, ct);

    public async Task<StandardEquipment?> GetWithCertificatesAsync(
        Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Certificates)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}
