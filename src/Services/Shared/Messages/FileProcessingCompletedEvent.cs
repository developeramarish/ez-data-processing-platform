using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when FilesReceiverService completes processing files for a data source
/// Published by: FilesReceiverService
/// Consumed by: SchedulingService
/// </summary>
public class FileProcessingCompletedEvent : IDataProcessingMessage
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
    public string PublishedBy { get; set; } = "FilesReceiver";

    /// <summary>
    /// Version of the message schema for compatibility tracking
    /// </summary>
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// ID of the data source that was processed
    /// </summary>
    [Required]
    [StringLength(24)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Number of files that were processed
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Total number of records across all files
    /// </summary>
    public long TotalRecords { get; set; }

    /// <summary>
    /// Whether processing completed successfully
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
