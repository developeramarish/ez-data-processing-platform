using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Represents a scheduled data source polling configuration
/// Persisted by SchedulingService to track active schedules
/// </summary>
public class ScheduledDataSource : Entity
{
    /// <summary>
    /// Reference to the DataProcessingDataSource ID
    /// </summary>
    [Required]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Data source name (denormalized for quick lookup)
    /// </summary>
    [Required]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name (denormalized)
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Cron expression for Quartz scheduler
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Whether the schedule is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the schedule is paused
    /// </summary>
    public bool IsPaused { get; set; } = false;

    /// <summary>
    /// Quartz job key (for tracking)
    /// </summary>
    public string? QuartzJobKey { get; set; }

    /// <summary>
    /// Quartz trigger key (for tracking)
    /// </summary>
    public string? QuartzTriggerKey { get; set; }

    /// <summary>
    /// Last time the job executed
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// Total number of times this schedule has executed
    /// </summary>
    public long ExecutionCount { get; set; } = 0;

    /// <summary>
    /// Last error message if execution failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Timestamp when schedule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when schedule was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User/service that created this schedule
    /// </summary>
    public string CreatedBy { get; set; } = "SchedulingService";

    /// <summary>
    /// User/service that last updated this schedule
    /// </summary>
    public string UpdatedBy { get; set; } = "SchedulingService";

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
