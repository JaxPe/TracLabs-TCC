// src/TracLabs.Infrastructure/Persistence/Repositories/CalibrationRepository.cs

using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;

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
            .Include(c => c.StandardEquipment)
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetByTechnicianIdAsync(
        Guid technicianId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.TechnicianId == technicianId.ToString())
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CalibrationRecord>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.CalibrationDate >= from && c.CalibrationDate <= to)
            .Include(c => c.Equipment)
            .Include(c => c.Certificate)
            .OrderByDescending(c => c.CalibrationDate)
            .ToListAsync(ct);

    public async Task<CalibrationRecord?> GetWithDetailsAsync(
        Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Equipment)
                .ThenInclude(e => e!.Client)
            .Include(c => c.StandardEquipment)
            .Include(c => c.Certificate)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}