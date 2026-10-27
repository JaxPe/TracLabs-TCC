using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class EquipmentRepository : BaseRepository<Equipment>, IEquipmentRepository
{
    public EquipmentRepository(TracLabsDbContext context) : base(context) { }

    public async Task<Equipment?> GetByLabTagAsync(string labTag, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.LabTag == labTag, ct);

    public async Task<IReadOnlyList<Equipment>> GetByClientIdAsync(
        Guid clientId, CancellationToken ct = default)
        => await _dbSet
            .Where(e => e.ClientId == clientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Equipment>> GetByStatusAsync(
        EquipmentStatus status, CancellationToken ct = default)
        => await _dbSet
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    public async Task<Equipment?> GetWithCalibrationHistoryAsync(
        Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(e => e.CalibrationHistory)
                .ThenInclude(c => c.Certificate)
            .Include(e => e.Client)
            .Include(e => e.MaintenanceHistory)
            .Include(e => e.OccurrenceHistory)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Equipment>> SearchAsync(
        string? labTag, string? serialNumber, string? model,
        string? clientName, EquipmentStatus? status,
        CancellationToken ct = default)
    {
        // IQueryable permite compor filtros sem executar a query a cada passo
        var query = _dbSet.Include(e => e.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(labTag))
            query = query.Where(e => e.LabTag.Contains(labTag));

        if (!string.IsNullOrWhiteSpace(serialNumber))
            query = query.Where(e => e.SerialNumber.Contains(serialNumber));

        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(e => e.Model.Contains(model));

        if (!string.IsNullOrWhiteSpace(clientName))
            query = query.Where(e => e.Client!.Name.Contains(clientName));

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }
}
