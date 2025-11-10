namespace InvalidRecordsService.Models.Requests;

public class InvalidRecordListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? DataSourceId { get; set; }
    public string? ErrorType { get; set; }  // schema, format, required, range
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }  // New, InProgress, Corrected, Ignored
}
