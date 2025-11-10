using DataProcessing.FilesReceiver.Models;

namespace DataProcessing.FilesReceiver.Services;

/// <summary>
/// Service for processing files from data sources
/// </summary>
public interface IFileProcessingService
{
    /// <summary>
    /// Process files from the specified data source
    /// </summary>
    /// <param name="dataSourceId">ID of the data source to process files from</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>List of processed files ready for validation</returns>
    Task<IReadOnlyList<ProcessedFileInfo>> ProcessFilesFromDataSourceAsync(string dataSourceId, string correlationId);
}
