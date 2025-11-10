namespace InvalidRecordsService.Models.Responses;

public class StatisticsDto
{
    public int TotalInvalidRecords { get; set; }
    public int ReviewedRecords { get; set; }
    public int IgnoredRecords { get; set; }
    public Dictionary<string, int> ByDataSource { get; set; } = new();
    public Dictionary<string, int> ByErrorType { get; set; } = new();
    public Dictionary<string, int> BySeverity { get; set; } = new();
}
