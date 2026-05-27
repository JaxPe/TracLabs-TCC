namespace TracLabs.Application.Interfaces;

/// <summary>
/// Coordinates the work of multiple repositories by flushing all pending
/// changes to the database in a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Persists all changes tracked by the current context to the database.
    /// Returns the number of state entries written.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit database transaction.
    /// Use when you need finer control over commit/rollback than SaveChangesAsync provides.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current explicit transaction.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current explicit transaction.
    /// Always call this in a catch block if BeginTransactionAsync was called.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}