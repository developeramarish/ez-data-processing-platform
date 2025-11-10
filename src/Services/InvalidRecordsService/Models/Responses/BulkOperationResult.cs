namespace InvalidRecordsService.Models.Responses;

public class BulkOperationResult
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
}

public class BulkOperationError
{
    public string RecordId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
