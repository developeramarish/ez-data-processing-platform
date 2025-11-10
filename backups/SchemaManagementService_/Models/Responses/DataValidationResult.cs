namespace SchemaManagementService.Models.Responses;

/// <summary>
/// Result of data validation against schema
/// </summary>
public class DataValidationResult
{
    /// <summary>
    /// Whether the data is valid against the schema
    /// </summary>
    public bool IsValid { get; set; } = false;

    /// <summary>
    /// Validation error details
    /// </summary>
    public List<DataValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Summary message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Schema used for validation
    /// </summary>
    public string? SchemaId { get; set; }

    /// <summary>
    /// Schema name used for validation
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of records validated
    /// </summary>
    public int RecordsValidated { get; set; } = 1;

    /// <summary>
    /// Number of valid records
    /// </summary>
    public int ValidRecords { get; set; } = 0;

    /// <summary>
    /// Number of invalid records
    /// </summary>
    public int InvalidRecords { get; set; } = 0;
}

/// <summary>
/// Data validation error details
/// </summary>
public class DataValidationError
{
    /// <summary>
    /// Error message in Hebrew
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error message in English
    /// </summary>
    public string MessageEnglish { get; set; } = string.Empty;

    /// <summary>
    /// JSON path to the invalid field
    /// </summary>
    public string? FieldPath { get; set; }

    /// <summary>
    /// Field name that failed validation
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Actual value that failed validation
    /// </summary>
    public object? ActualValue { get; set; }

    /// <summary>
    /// Expected value or constraint
    /// </summary>
    public string? ExpectedConstraint { get; set; }

    /// <summary>
    /// Validation rule that failed
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// Record index (for batch validation)
    /// </summary>
    public int? RecordIndex { get; set; }
}
