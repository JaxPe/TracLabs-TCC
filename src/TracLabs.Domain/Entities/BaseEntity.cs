namespace TracLabs.Domain.Entities;

/// <summary>
/// Base class for all domain entities.
/// Provides common auditing fields required by ISO/IEC 17025 traceability requirements —
/// you must be able to answer WHO did WHAT and WHEN for every record.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    /// <summary>When this record was first created in the system.</summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    
    /// <summary>When this record was last modified. Null if never modified after creation.</summary>
    public DateTime? UpdatedAt { get; protected set; }
    
    /// <summary>
    /// Soft delete flag. In a metrology context, you NEVER hard-delete calibration records.
    /// ISO/IEC 17025 clause 7.5 requires maintaining integrity of records.
    /// </summary>
    public bool IsDeleted { get; protected set; } = false;
    
    /// <summary>Mark this entity as updated. Call this in any mutating method.</summary>
    protected void MarkAsUpdated() => UpdatedAt = DateTime.UtcNow;
    
    /// <summary>Soft-delete this entity instead of removing from database.</summary>
    public void Delete() => IsDeleted = true;
}