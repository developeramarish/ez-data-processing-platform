using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when file processing fails at any stage
/// Published by: Any service (FilesReceiverService, ValidationService, etc.)
/// Consumed by: Monitoring services, notification services, UI services
/// </summary>
public class FileProcessingFailedEvent : IDataProcessingMessage
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
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Version of the message schema for compatibility tracking
    /// </summary>
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// ID of the data source being processed
    /// </summary>
    [Required]
    [StringLength(24)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the file that failed processing
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full path of the file that failed processing
    /// </summary>
    [StringLength(1000)]
    public string? FilePath { get; set; }

    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Stage of processing where the failure occurred
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FailureStage { get; set; } = string.Empty;

    /// <summary>
    /// Type or category of error (FileAccess, Validation, Network, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the error (Critical, Error, Warning)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Full exception details including stack trace (for debugging)
    /// </summary>
    [StringLength(8000)]
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Name of the data source for context
    /// </summary>
    [StringLength(200)]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name for reporting and escalation
    /// </summary>
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Time when processing started (to calculate processing duration)
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Number of retry attempts made before giving up
    /// </summary>
    public int RetryAttempts { get; set; } = 0;

    /// <summary>
    /// Maximum number of retries that will be attempted
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether this failure should trigger an alert/notification
    /// </summary>
    public bool ShouldAlert { get; set; } = true;

    /// <summary>
    /// Whether automatic retry is recommended for this type of failure
    /// </summary>
    public bool IsRetryable { get; set; } = false;

    /// <summary>
    /// Size of the file that failed processing (if known)
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Schema version that was being used (if applicable)
    /// </summary>
    public int? SchemaVersion { get; set; }

    /// <summary>
    /// Additional context or metadata about the failure
    /// </summary>
    [StringLength(2000)]
    public string? AdditionalContext { get; set; }

    /// <summary>
    /// Calculates the processing duration if start time is available
    /// </summary>
    public TimeSpan? GetProcessingDuration()
    {
        return ProcessingStartedAt.HasValue ? Timestamp - ProcessingStartedAt.Value : null;
    }

    /// <summary>
    /// Indicates if this failure is critical and requires immediate attention
    /// </summary>
    public bool IsCritical => Severity == "Critical";

    /// <summary>
    /// Indicates if retry attempts are exhausted
    /// </summary>
    public bool IsRetryExhausted => RetryAttempts >= MaxRetries;
}
