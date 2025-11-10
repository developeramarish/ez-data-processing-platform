namespace InvalidRecordsService.Models.Requests;

public class BulkOperationRequest
{
    public List<string> RecordIds { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}
