namespace InvalidRecordsService.Models.Responses;

public class CorrectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ValidationResultId { get; set; }  // If reprocessed
}
