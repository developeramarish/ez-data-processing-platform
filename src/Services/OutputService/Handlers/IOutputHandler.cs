// IOutputHandler.cs - Output Handler Interface
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: November 12, 2025

using DataProcessing.Shared.Entities;
using DataProcessing.Output.Models;

namespace DataProcessing.Output.Handlers;

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
