namespace SchemaManagementService.Models.Responses;

/// <summary>
/// Result of JSON Schema validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the JSON Schema is valid
    /// </summary>
    public bool IsValid { get; set; } = false;

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Success message
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
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
    /// JSON path where the error occurred
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Line number where the error occurred
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Column number where the error occurred
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? ErrorCode { get; set; }
}
