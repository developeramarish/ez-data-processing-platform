namespace DataProcessing.Validation.Models;

/// <summary>
/// Result of file content validation
/// </summary>
public class ValidationResult
{
    public string ValidationResultId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
