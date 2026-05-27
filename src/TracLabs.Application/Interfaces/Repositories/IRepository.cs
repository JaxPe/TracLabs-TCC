namespace TracLabs.Application.Interfaces.Repositories;

/// <summary>
/// Generic base repository interface defining the standard data access
/// contract for all domain entities.
/// 
/// Notice this interface lives in Application, not Domain. The Domain layer
/// should not know anything about persistence concerns — that distinction
/// matters when someone reads your graduation defense.
/// </summary>
/// <typeparam name="T">A domain entity that inherits from BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    // ── Queries (reads) ───────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a single entity by its primary key.
    /// Returns null if not found — callers must handle the null case.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-deleted entities of this type.
    /// Use with caution on large tables — prefer specific query methods
    /// on the concrete interfaces instead.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if at least one entity matches the given predicate.
    /// Useful for uniqueness checks before inserting (e.g. duplicate serial numbers).
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    // ── Commands (writes) ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new entity to the persistence context.
    /// Does NOT save to the database — call IUnitOfWork.SaveChangesAsync() after.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity as modified in the persistence context.
    /// Does NOT save to the database — call IUnitOfWork.SaveChangesAsync() after.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Performs a soft delete by setting IsDeleted = true.
    /// In a metrology system we never hard-delete records — ISO/IEC 17025
    /// clause 7.5 requires that records remain traceable and intact.
    /// </summary>
    void SoftDelete(T entity);
}