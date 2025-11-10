namespace InvalidRecordsService.Models.Requests;

public class CorrectRecordRequest
{
    public object CorrectedData { get; set; } = new();
    public string CorrectedBy { get; set; } = string.Empty;
    public bool AutoReprocess { get; set; } = true;
}
