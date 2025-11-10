using DataProcessing.FilesReceiver.Models;

namespace DataProcessing.FilesReceiver.Infrastructure;

/// <summary>
/// Interface for reading different file formats
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Check if this reader supports the given file format
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>True if this reader can process the file</returns>
    bool CanRead(string fileName, string contentType);

    /// <summary>
    /// Read file content and convert to ProcessedFileInfo
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Processed file information</returns>
    Task<ProcessedFileInfo> ReadFileAsync(string filePath, string correlationId);
}
