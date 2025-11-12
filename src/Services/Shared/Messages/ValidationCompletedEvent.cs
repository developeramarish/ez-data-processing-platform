using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when file validation is completed
/// Published by: ValidationService
/// Consumed by: DataSourceManagementService, UI notification services
/// </summary>
public class ValidationCompletedEvent : IDataProcessingMessage
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
    public string PublishedBy { get; set; } = "ValidationService";

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
    /// Name of the file that was validated
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the validation result record created
    /// </summary>
    [Required]
    [StringLength(24)]
    public string ValidationResultId { get; set; } = string.Empty;

    /// <summary>
    /// Hazelcast cache key for valid records
    /// Used to reference the cached valid records in Hazelcast distributed cache
    /// </summary>
    public string HazelcastValidRecordsKey { get; set; } = string.Empty;

    /// <summary>
    /// Total number of records found in the file
    /// </summary>
    [Required]
    public int TotalRecords { get; set; } = 0;

    /// <summary>
    /// Number of records that passed validation
    /// </summary>
    [Required]
    public int ValidRecords { get; set; } = 0;

    /// <summary>
    /// Number of records that failed validation
    /// </summary>
    [Required]
    public int InvalidRecords { get; set; } = 0;

    /// <summary>
    /// Time taken to process and validate the file
    /// </summary>
    [Required]
    public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Validation status (Completed, Failed, PartiallyCompleted)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ValidationStatus { get; set; } = "Completed";

    /// <summary>
    /// Error message if validation failed completely
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Size of the processed file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; } = 0;

    /// <summary>
    /// Name of the data source for display purposes
    /// </summary>
    [StringLength(200)]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name for reporting and notifications
    /// </summary>
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Schema version used for validation
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Processing start time for duration calculation
    /// </summary>
    public DateTime ProcessingStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the validation success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalRecords > 0 ? (double)ValidRecords / TotalRecords * 100 : 0;

    /// <summary>
    /// Calculates the validation error rate as a percentage
    /// </summary>
    public double ErrorRate => TotalRecords > 0 ? (double)InvalidRecords / TotalRecords * 100 : 0;

    /// <summary>
    /// Indicates if validation was successful (no errors occurred during processing)
    /// </summary>
    public bool IsSuccessful => ValidationStatus == "Completed" && string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Indicates if validation found any invalid records
    /// </summary>
    public bool HasValidationErrors => InvalidRecords > 0;
}
