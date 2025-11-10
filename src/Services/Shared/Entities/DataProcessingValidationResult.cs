using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Represents the result of validating a file against a data source schema
/// </summary>
public class DataProcessingValidationResult : DataProcessingBaseEntity
{
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
    /// Full path of the processed file
    /// </summary>
    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the processed file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; } = 0;

    /// <summary>
    /// Total number of records found in the file
    /// </summary>
    public int TotalRecords { get; set; } = 0;

    /// <summary>
    /// Number of records that passed validation
    /// </summary>
    public int ValidRecords { get; set; } = 0;

    /// <summary>
    /// Number of records that failed validation
    /// </summary>
    public int InvalidRecords { get; set; } = 0;

    /// <summary>
    /// Time taken to process and validate the file
    /// </summary>
    public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Processing status
    /// </summary>
    [StringLength(50)]
    public string Status { get; set; } = "Processing";

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Schema version used for validation
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Start time of validation process
    /// </summary>
    public DateTime ProcessingStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End time of validation process
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; }

    /// <summary>
    /// Whether the validation process completed successfully
    /// </summary>
    public bool IsProcessingComplete { get; set; } = false;

    /// <summary>
    /// Additional metadata about the processing
    /// </summary>
    [StringLength(2000)]
    public string? ProcessingNotes { get; set; }

    /// <summary>
    /// Calculates the validation success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalRecords > 0 ? (double)ValidRecords / TotalRecords * 100 : 0;

    /// <summary>
    /// Calculates the validation error rate as a percentage
    /// </summary>
    public double ErrorRate => TotalRecords > 0 ? (double)InvalidRecords / TotalRecords * 100 : 0;

    /// <summary>
    /// Marks the validation process as completed
    /// </summary>
    public void MarkAsCompleted(string status = "Completed", string? notes = null)
    {
        Status = status;
        ProcessingCompletedAt = DateTime.UtcNow;
        ProcessingDuration = ProcessingCompletedAt.Value - ProcessingStartedAt;
        IsProcessingComplete = true;
        ProcessingNotes = notes;
        MarkAsModified();
    }

    /// <summary>
    /// Marks the validation process as failed
    /// </summary>
    public void MarkAsFailed(string errorMessage, string? notes = null)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        ProcessingCompletedAt = DateTime.UtcNow;
        ProcessingDuration = ProcessingCompletedAt.Value - ProcessingStartedAt;
        IsProcessingComplete = true;
        ProcessingNotes = notes;
        MarkAsModified();
    }
}
