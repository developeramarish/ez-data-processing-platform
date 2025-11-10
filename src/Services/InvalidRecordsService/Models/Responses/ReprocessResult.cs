namespace InvalidRecordsService.Models.Responses;

public class ReprocessResult
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public List<string>? ValidationErrors { get; set; }
    public string Message { get; set; } = string.Empty;
}
