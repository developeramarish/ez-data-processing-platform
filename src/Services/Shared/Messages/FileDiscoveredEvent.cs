using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when a new file is discovered by the file discovery service
/// Published by: FileDiscoveryService
/// Consumed by: FileProcessorService
/// </summary>
public class FileDiscoveredEvent : IDataProcessingMessage
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
    public string PublishedBy { get; set; } = "FileDiscoveryService";

    /// <summary>
    /// Version of the message schema for compatibility tracking
    /// </summary>
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// ID of the data source this file belongs to
    /// </summary>
    [Required]
    [StringLength(24)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the discovered file
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the discovered file
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// UTC timestamp when the file was discovered
    /// </summary>
    [Required]
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sequence number for ordering files within a batch
    /// Used to maintain processing order when multiple files are discovered
    /// </summary>
    [Required]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Unique identifier for the batch of files discovered in a single poll
    /// Used for correlation and batch tracking across multiple files
    /// </summary>
    [Required]
    public Guid PollBatchId { get; set; }
}
