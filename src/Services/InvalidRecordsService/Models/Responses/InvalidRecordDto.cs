namespace InvalidRecordsService.Models.Responses;

public class InvalidRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string DataSourceId { get; set; } = string.Empty;
    public string DataSourceName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new();
    public object? OriginalData { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Error";
    public bool IsReviewed { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public bool IsIgnored { get; set; }
}
