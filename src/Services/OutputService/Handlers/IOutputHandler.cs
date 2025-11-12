// IOutputHandler.cs - Output Handler Interface
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: November 12, 2025

using DataProcessing.Shared.Entities;

namespace OutputService.Handlers;

/// <summary>
/// Interface for output destination handlers
/// Each destination type (Kafka, Folder, SFTP, HTTP) implements this interface
/// </summary>
public interface IOutputHandler
{
    /// <summary>
    /// Check if this handler can handle the specified destination type
    /// </summary>
    /// <param name="destinationType">Type: "kafka", "folder", "sftp", "http"</param>
    /// <returns>True if this handler supports the type</returns>
    bool CanHandle(string destinationType);
    
    /// <summary>
    /// Write content to the specified destination
    /// </summary>
    /// <param name="destination">Destination configuration</param>
    /// <param name="content">File content (in specified format)</param>
    /// <param name="fileName">Original filename for reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success/failure with details</returns>
    Task<OutputResult> WriteAsync(
        OutputDestination destination,
        string content,
        string fileName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of output write operation
/// </summary>
public class OutputResult
{
    public string DestinationName { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    
    public static OutputResult CreateSuccess(string name, string type, TimeSpan duration)
    {
        return new OutputResult
        {
            DestinationName = name,
            DestinationType = type,
            Success = true,
            Duration = duration
        };
    }
    
    public static OutputResult CreateFailure(string name, string type, string error, TimeSpan duration)
    {
        return new OutputResult
        {
            DestinationName = name,
            DestinationType = type,
            Success = false,
            ErrorMessage = error,
            Duration = duration
        };
    }
}
