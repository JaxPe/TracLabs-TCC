using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly TracLabsDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(TracLabsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void SoftDelete(T entity)
    {
        entity.Delete();
        _dbSet.Update(entity);
    }
}