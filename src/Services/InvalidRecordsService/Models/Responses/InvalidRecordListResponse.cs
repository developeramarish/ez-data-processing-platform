namespace InvalidRecordsService.Models.Responses;

public class InvalidRecordListResponse
{
    public List<InvalidRecordDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
