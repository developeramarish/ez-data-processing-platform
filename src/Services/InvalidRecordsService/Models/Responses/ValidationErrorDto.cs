namespace InvalidRecordsService.Models.Responses;

public class ValidationErrorDto
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;  // schema, format, required, range
    public string? ExpectedValue { get; set; }
    public string? ActualValue { get; set; }
}
