using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public class ClientRepository : BaseRepository<Client>, IClientRepository
{
    public ClientRepository(TracLabsDbContext context) : base(context) { }

    public async Task<Client?> GetByTaxIdAsync(string taxId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(c => c.TaxId == taxId, ct);

    public async Task<IReadOnlyList<Client>> SearchByNameAsync(
        string name, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.Name.Contains(name))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Client?> GetWithEquipmentAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}