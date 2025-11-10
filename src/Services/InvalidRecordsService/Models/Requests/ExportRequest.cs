namespace InvalidRecordsService.Models.Requests;

public class ExportRequest
{
    public InvalidRecordListRequest Filters { get; set; } = new();
    public string Format { get; set; } = "CSV";  // CSV or Excel (future)
}
