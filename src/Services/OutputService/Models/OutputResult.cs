namespace DataProcessing.Output.Models;

/// <summary>
/// Result of writing to an output destination
/// </summary>
public class OutputResult
{
    public string DestinationName { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
    public long? BytesWritten { get; set; }
    public TimeSpan Duration { get; set; }

    public static OutputResult Successful(string name, string type, long bytesWritten, TimeSpan duration, int retryCount = 0)
    {
        return new OutputResult
        {
            DestinationName = name,
            DestinationType = type,
            Success = true,
            BytesWritten = bytesWritten,
            Duration = duration,
            RetryCount = retryCount
        };
    }

    public static OutputResult Failure(string name, string type, string errorMessage, int retryCount = 0)
    {
        return new OutputResult
        {
            DestinationName = name,
            DestinationType = type,
            Success = false,
            ErrorMessage = errorMessage,
            RetryCount = retryCount
        };
    }
}
