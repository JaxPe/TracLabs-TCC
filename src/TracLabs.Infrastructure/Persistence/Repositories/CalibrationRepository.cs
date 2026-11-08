using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class CalibrationRepository
    : BaseRepository<CalibrationRecord>, ICalibrationRepository
{
    public CalibrationRepository(TracLabsDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CalibrationRecord>> GetHistoryByEquipmentIdAsync(
        Guid equipmentId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.EquipmentId == equipmentId)
            .Include(c => c.Certificate)
            .Include(c => c.StandardsUsed)
            .Include(c => c.StandardEquipment)
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetByTechnicianIdAsync(
        string technicianId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.TechnicianId == technicianId)
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.CalibrationDate >= from && c.CalibrationDate <= to)
            .Include(c => c.Equipment)
            .Include(c => c.Certificate)
            .Include(c => c.StandardsUsed)
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<CalibrationRecord?> GetWithDetailsAsync(
        Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Equipment)
                .ThenInclude(e => e!.Client)
            .Include(c => c.StandardEquipment)
            .Include(c => c.Certificate)
            .Include(c => c.StandardsUsed)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetByStandardEquipmentIdAsync(
        Guid standardId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Equipment)
                .ThenInclude(e => e!.Client)
            .Include(c => c.Certificate)
            .Include(c => c.StandardsUsed)
            .Where(c => c.StandardEquipmentId == standardId || c.StandardsUsed.Any(su => su.StandardEquipmentId == standardId))
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<int> GetActiveCalibrationsCountAsync(CancellationToken ct = default)
        => await _dbSet.CountAsync(c => c.Result == CalibrationResult.Pending, ct);

    public async Task<int> GetNonConformingCountAsync(CancellationToken ct = default)
        => await _dbSet.CountAsync(c => c.Result == CalibrationResult.NonConforming, ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetRecentCalibrationsAsync(int count = 10, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Equipment)
                .ThenInclude(e => e!.Client)
            .Include(c => c.Certificate)
            .OrderByDescending(c => c.CalibrationDate)
            .Take(count)
            .ToListAsync(ct);
}
