namespace InvalidRecordsService.Models.Requests;

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;  // New, InProgress, Corrected, Ignored
    public string? Notes { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
