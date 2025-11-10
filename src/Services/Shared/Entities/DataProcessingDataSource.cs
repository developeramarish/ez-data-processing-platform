using MongoDB.Entities;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Represents a data source configuration for file processing
/// </summary>
public class DataProcessingDataSource : DataProcessingBaseEntity
{
    /// <summary>
    /// Display name for the data source
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name of the supplier providing this data source
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// File path or directory to monitor for new files
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval for checking new files (legacy - use CronExpression instead)
    /// </summary>
    public TimeSpan PollingRate { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Quartz cron expression for scheduling (6-field format with seconds support)
    /// Format: second minute hour day month dayOfWeek
    /// Example: "*/30 * * * * *" = every 30 seconds
    /// </summary>
    [StringLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets the effective cron expression (uses CronExpression if set, else converts PollingRate)
    /// </summary>
    public string GetEffectiveCronExpression()
    {
        if (!string.IsNullOrEmpty(CronExpression))
            return CronExpression;

        // Convert TimeSpan to 6-field cron expression (with seconds)
        var totalMinutes = (int)PollingRate.TotalMinutes;
        if (totalMinutes < 1)
            return "*/30 * * * * *"; // Default to 30 seconds

        // Generate minute-based cron
        if (totalMinutes == 1)
            return "0 * * * * *"; // Every minute at 0 seconds
        
        if (totalMinutes < 60)
            return $"0 */{totalMinutes} * * * *"; // Every N minutes
        
        var hours = totalMinutes / 60;
        return $"0 0 */{hours} * * *"; // Every N hours
    }

    /// <summary>
    /// JSON schema document for validating file content
    /// </summary>
    [Required]
    public BsonDocument JsonSchema { get; set; } = new();

    /// <summary>
    /// Category classification for the data source
    /// </summary>
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Current version of the schema (for schema evolution tracking)
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Indicates if the data source is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// File pattern or filter for selecting files (e.g., "*.json", "*.xml")
    /// </summary>
    [StringLength(50)]
    public string FilePattern { get; set; } = "*.*";

    /// <summary>
    /// Last time this data source was successfully processed
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// Number of files successfully processed from this source
    /// </summary>
    public long TotalFilesProcessed { get; set; } = 0;

    /// <summary>
    /// Number of records with validation errors from this source
    /// </summary>
    public long TotalErrorRecords { get; set; } = 0;

    /// <summary>
    /// Additional configuration options stored as JSON
    /// </summary>
    public BsonDocument? AdditionalConfiguration { get; set; }

    /// <summary>
    /// Description or notes about this data source
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if files from this datasource are currently being processed
    /// Used for distributed locking to prevent concurrent processing
    /// </summary>
    public bool IsCurrentlyProcessing { get; set; } = false;

    /// <summary>
    /// UTC timestamp when processing started (for timeout detection)
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Correlation ID of the current processing operation
    /// </summary>
    [StringLength(50)]
    public string? ProcessingCorrelationId { get; set; }

    /// <summary>
    /// K8s pod ID or hostname of the service instance currently processing
    /// </summary>
    [StringLength(100)]
    public string? ProcessingPodId { get; set; }

    /// <summary>
    /// Hostname of the service instance currently processing
    /// </summary>
    [StringLength(100)]
    public string? ProcessingHostname { get; set; }

    /// <summary>
    /// UTC timestamp when last processing completed
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; }

    /// <summary>
    /// Updates statistics after processing files
    /// </summary>
    public void UpdateProcessingStats(long filesProcessed, long errorRecords)
    {
        TotalFilesProcessed += filesProcessed;
        TotalErrorRecords += errorRecords;
        LastProcessedAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// Attempts to acquire processing lock (stateless distributed lock)
    /// </summary>
    public bool TryAcquireProcessingLock(string correlationId)
    {
        if (IsCurrentlyProcessing)
        {
            // Reduced timeout for K8s environments (5 minutes instead of 30)
            // PreStop hooks handle most graceful shutdowns faster
            if (ProcessingStartedAt.HasValue && 
                DateTime.UtcNow - ProcessingStartedAt.Value > TimeSpan.FromMinutes(5))
            {
                // Lock expired, force release
                ReleaseProcessingLock("timeout");
                return true;
            }
            return false; // Already locked
        }

        IsCurrentlyProcessing = true;
        ProcessingStartedAt = DateTime.UtcNow;
        ProcessingCorrelationId = correlationId;
        
        // Capture K8s pod identity for targeted cleanup
        ProcessingPodId = Environment.GetEnvironmentVariable("HOSTNAME") 
            ?? Environment.MachineName;
        ProcessingHostname = Environment.MachineName;
        
        MarkAsModified();
        return true;
    }

    /// <summary>
    /// Releases processing lock
    /// </summary>
    public void ReleaseProcessingLock(string reason)
    {
        IsCurrentlyProcessing = false;
        ProcessingCompletedAt = DateTime.UtcNow;
        ProcessingCorrelationId = null;
        ProcessingPodId = null;
        ProcessingHostname = null;
        MarkAsModified();
    }
}
