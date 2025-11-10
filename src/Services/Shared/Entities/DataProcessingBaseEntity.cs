using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Base entity class for all Data Processing Platform entities
/// Provides audit fields, soft delete functionality, and correlation ID tracking
/// </summary>
public abstract class DataProcessingBaseEntity : Entity
{
    /// <summary>
    /// Creation timestamp in UTC
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp in UTC
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag - when true, entity is considered deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Correlation ID for request tracing across services
    /// </summary>
    [Required]
    [StringLength(36)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Entity version for optimistic concurrency control
    /// </summary>
    public long Version { get; set; } = 1;

    /// <summary>
    /// User or system that created this entity
    /// </summary>
    [StringLength(100)]
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// User or system that last updated this entity
    /// </summary>
    [StringLength(100)]
    public string UpdatedBy { get; set; } = "System";

    /// <summary>
    /// Updates the UpdatedAt timestamp and increments version
    /// Should be called before saving changes
    /// </summary>
    public virtual void MarkAsModified(string modifiedBy = "System")
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = modifiedBy;
        Version++;
    }

    /// <summary>
    /// Marks the entity as soft deleted
    /// </summary>
    public virtual void MarkAsDeleted(string deletedBy = "System")
    {
        IsDeleted = true;
        MarkAsModified(deletedBy);
    }
}
