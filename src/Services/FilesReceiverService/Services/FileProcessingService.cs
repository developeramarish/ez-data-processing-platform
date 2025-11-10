using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Models;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Monitoring;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using System.Diagnostics;

namespace DataProcessing.FilesReceiver.Services;

/// <summary>
/// Service for processing files from data sources
/// </summary>
public class FileProcessingService : IFileProcessingService
{
    private readonly ILogger<FileProcessingService> _logger;
    private readonly IEnumerable<IFileReader> _fileReaders;
    private readonly DataProcessingMetrics _metrics;
    private readonly ActivitySource _activitySource;

    public FileProcessingService(
        ILogger<FileProcessingService> logger,
        IEnumerable<IFileReader> fileReaders,
        DataProcessingMetrics metrics,
        ActivitySource activitySource)
    {
        _logger = logger;
        _fileReaders = fileReaders;
        _metrics = metrics;
        _activitySource = activitySource;
    }

    public async Task<IReadOnlyList<ProcessedFileInfo>> ProcessFilesFromDataSourceAsync(string dataSourceId, string correlationId)
    {
        using var activity = _activitySource.StartDataProcessingActivity(
            "FileProcessingService.ProcessFilesFromDataSource",
            correlationId,
            dataSourceId);

        _logger.LogInformation("Processing files for data source: {DataSourceId}, CorrelationId: {CorrelationId}", 
            dataSourceId, correlationId);

        try
        {
            // Get data source configuration
            var dataSource = await DB.Find<DataProcessingDataSource>()
                .Match(ds => ds.ID == dataSourceId && ds.IsActive && !ds.IsDeleted)
                .ExecuteFirstAsync();

            if (dataSource == null)
            {
                _logger.LogWarning("Data source not found or inactive: {DataSourceId}", dataSourceId);
                return Array.Empty<ProcessedFileInfo>();
            }

            activity?.SetTag("data-source.name", dataSource.Name);
            activity?.SetTag("data-source.supplier", dataSource.SupplierName);
            activity?.SetTag("data-source.path", dataSource.FilePath);

            // Get files from the configured path
            var files = await GetFilesFromDataSourceAsync(dataSource, correlationId);
            
            if (!files.Any())
            {
                _logger.LogInformation("No files found for data source: {DataSourceId}", dataSourceId);
                return Array.Empty<ProcessedFileInfo>();
            }

            var processedFiles = new List<ProcessedFileInfo>();

            // Process each file
            foreach (var filePath in files)
            {
                try
                {
                    using var fileActivity = _activitySource.StartDataProcessingActivity(
                        "FileProcessingService.ProcessSingleFile",
                        correlationId,
                        dataSourceId);

                    var fileName = Path.GetFileName(filePath);
                    fileActivity?.SetFileProcessingContext(fileName, filePath, new FileInfo(filePath).Length);

                    var processedFile = await ProcessSingleFileAsync(filePath, correlationId);
                    processedFiles.Add(processedFile);

                    // Record metrics
                    _metrics.RecordFileSize(dataSourceId, Path.GetExtension(fileName), processedFile.FileSize);

                    _logger.LogInformation("Successfully processed file: {FileName} for data source: {DataSourceId}", 
                        fileName, dataSourceId);
                }
                catch (Exception ex)
                {
                    var fileName = Path.GetFileName(filePath);
                    _logger.LogError(ex, "Failed to process file: {FileName} for data source: {DataSourceId}", 
                        fileName, dataSourceId);
                    
                    // Record error but continue with other files
                    _metrics.RecordFileProcessingFailed(dataSourceId, "FilesReceiver", "individual_file_error");
                }
            }

            activity?.SetTag("processed.files.count", processedFiles.Count);
            _logger.LogInformation("Completed processing {ProcessedCount} out of {TotalCount} files for data source: {DataSourceId}", 
                processedFiles.Count, files.Count, dataSourceId);

            return processedFiles.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process files for data source: {DataSourceId}", dataSourceId);
            activity?.SetError(ex);
            throw;
        }
    }

    private Task<IList<string>> GetFilesFromDataSourceAsync(DataProcessingDataSource dataSource, string correlationId)
    {
        _logger.LogInformation("Scanning files in path: {FilePath} for data source: {DataSourceId}", 
            dataSource.FilePath, dataSource.ID);

        return Task.Run<IList<string>>(() =>
        {
            try
            {
                var files = new List<string>();

                if (Directory.Exists(dataSource.FilePath))
                {
                    // Get all files in directory (non-recursive for now)
                    var directoryFiles = Directory.GetFiles(dataSource.FilePath)
                        .Where(f => IsSupportedFileType(f))
                        .OrderBy(f => File.GetCreationTime(f)) // Process older files first
                        .ToList();

                    files.AddRange(directoryFiles);

                    _logger.LogInformation("Found {FileCount} supported files in directory: {FilePath}", 
                        files.Count, dataSource.FilePath);
                }
                else if (File.Exists(dataSource.FilePath))
                {
                    // Single file path
                    if (IsSupportedFileType(dataSource.FilePath))
                    {
                        files.Add(dataSource.FilePath);
                        _logger.LogInformation("Processing single file: {FilePath}", dataSource.FilePath);
                    }
                    else
                    {
                        _logger.LogWarning("File type not supported: {FilePath}", dataSource.FilePath);
                    }
                }
                else
                {
                    _logger.LogWarning("Path not found: {FilePath}", dataSource.FilePath);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan files in path: {FilePath}", dataSource.FilePath);
                throw;
            }
        });
    }

    private async Task<ProcessedFileInfo> ProcessSingleFileAsync(string filePath, string correlationId)
    {
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        // Find appropriate file reader
        var fileReader = _fileReaders.FirstOrDefault(reader => reader.CanRead(fileName, contentType));
        
        if (fileReader == null)
        {
            throw new NotSupportedException($"No file reader available for file type: {fileName}");
        }

        _logger.LogInformation("Processing file: {FileName} using reader: {ReaderType}", 
            fileName, fileReader.GetType().Name);

        return await fileReader.ReadFileAsync(filePath, correlationId);
    }

    private bool IsSupportedFileType(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        return _fileReaders.Any(reader => reader.CanRead(fileName, contentType));
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
