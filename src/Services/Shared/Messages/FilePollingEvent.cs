using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when a data source should be polled for new files
/// Published by: SchedulingService
/// Consumed by: FilesReceiverService
/// </summary>
public class FilePollingEvent : IDataProcessingMessage
{
    /// <summary>
    /// Unique correlation ID for tracing requests across services
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the message was created
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Name of the service that published this message
    /// </summary>
    [Required]
    public string PublishedBy { get; set; } = "SchedulingService";

    /// <summary>
    /// Version of the message schema for compatibility tracking
    /// </summary>
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// ID of the data source to poll for files
    /// </summary>
    [Required]
    [StringLength(24)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// File path or directory to monitor for new files
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File pattern to match (e.g., "*.json", "*.xml")
    /// </summary>
    [StringLength(50)]
    public string FilePattern { get; set; } = "*.*";

    /// <summary>
    /// Name of the data source being polled
    /// </summary>
    [StringLength(200)]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name for logging and tracking
    /// </summary>
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum age of files to process (to avoid reprocessing old files)
    /// </summary>
    public TimeSpan? MaxFileAge { get; set; }

    /// <summary>
    /// Whether to process files recursively in subdirectories
    /// </summary>
    public bool ProcessRecursively { get; set; } = false;
}
