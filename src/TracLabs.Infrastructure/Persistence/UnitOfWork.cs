using Microsoft.EntityFrameworkCore.Storage;
using TracLabs.Application.Interfaces;

namespace TracLabs.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TracLabsDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(TracLabsDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Nenhuma transação ativa para commitar.");

        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Nenhuma transação ativa para rollback.");

        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}