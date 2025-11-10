using DataProcessing.FilesReceiver.Services;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Consumers;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using DataProcessing.Shared.Monitoring;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using System.Diagnostics;

namespace DataProcessing.FilesReceiver.Consumers;

/// <summary>
/// Consumer for FilePollingEvent messages from the Scheduling Service
/// Processes incoming files and publishes validation requests
/// </summary>
public class FilePollingEventConsumer : DataProcessingConsumerBase<FilePollingEvent>
{
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly DataProcessingMetrics _metrics;
    private readonly ActivitySource _activitySource;

    public FilePollingEventConsumer(
        ILogger<FilePollingEventConsumer> logger,
        IFileProcessingService fileProcessingService,
        IPublishEndpoint publishEndpoint,
        DataProcessingMetrics metrics,
        ActivitySource activitySource) 
        : base(logger)
    {
        _fileProcessingService = fileProcessingService;
        _publishEndpoint = publishEndpoint;
        _metrics = metrics;
        _activitySource = activitySource;
    }

    protected override async Task ProcessMessage(ConsumeContext<FilePollingEvent> context)
    {
        var message = context.Message;
        using var activity = _activitySource.StartDataProcessingActivity(
            "FilePollingEventConsumer.ProcessMessage", 
            message.CorrelationId, 
            message.DataSourceId);

        Logger.LogInformation("Processing file polling event for data source: {DataSourceId}, CorrelationId: {CorrelationId}", 
            message.DataSourceId, message.CorrelationId);

        // Record message received metric
        _metrics.RecordMessageReceived("FilePollingEvent", "FilesReceiver", true);

        try
        {
            // Process files from the data source
            var processedFiles = await _fileProcessingService.ProcessFilesFromDataSourceAsync(
                message.DataSourceId, 
                message.CorrelationId);

            if (processedFiles.Any())
            {
                Logger.LogInformation("Successfully processed {FileCount} files for data source {DataSourceId}", 
                    processedFiles.Count, message.DataSourceId);

                // Publish validation request events for each processed file
                foreach (var processedFile in processedFiles)
                {
                    var validationRequest = new ValidationRequestEvent
                    {
                        CorrelationId = message.CorrelationId,
                        PublishedBy = "FilesReceiver",
                        DataSourceId = message.DataSourceId,
                        FileName = processedFile.FileName,
                        FileContent = processedFile.FileContent,
                        FileContentType = processedFile.ContentType,
                        FileSizeBytes = processedFile.FileSize,
                        Timestamp = DateTime.UtcNow
                    };

                    await _publishEndpoint.Publish(validationRequest, context.CancellationToken);
                    
                    Logger.LogInformation("Published validation request for file: {FileName}, Size: {FileSize} bytes", 
                        processedFile.FileName, processedFile.FileSize);

                    // Record message sent metric
                    _metrics.RecordMessageSent("ValidationRequestEvent", "FilesReceiver", true);
                    
                    // Record file processing metric
                    _metrics.RecordFileProcessed(message.DataSourceId, "FilesReceiver", processedFile.RecordCount, 0);
                }

                activity?.SetTag("processed.files.count", processedFiles.Count);
                activity?.SetTag("total.records", processedFiles.Sum(f => f.RecordCount));
            }
            else
            {
                Logger.LogInformation("No new files to process for data source {DataSourceId}", message.DataSourceId);
            }

            // Release processing lock and publish completion event
            await ReleaseProcessingLockAsync(message.DataSourceId, message.CorrelationId, 
                processedFiles.Count, processedFiles.Sum(f => f.RecordCount), true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process files for data source {DataSourceId}: {ErrorMessage}", 
                message.DataSourceId, ex.Message);

            // Record error metrics
            _metrics.RecordFileProcessingFailed(message.DataSourceId, "FilesReceiver", "file_processing_error");
            _metrics.RecordMessageReceived("FilePollingEvent", "FilesReceiver", false);

            // Release lock on failure and publish completion event with error
            await ReleaseProcessingLockAsync(message.DataSourceId, message.CorrelationId, 
                0, 0, false, ex.Message);

            // Publish failure event
            var failureEvent = new FileProcessingFailedEvent
            {
                CorrelationId = message.CorrelationId,
                PublishedBy = "FilesReceiver",
                DataSourceId = message.DataSourceId,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(failureEvent, context.CancellationToken);
            _metrics.RecordMessageSent("FileProcessingFailedEvent", "FilesReceiver", true);

            activity?.SetError(ex, "File processing failed");
            throw; // Re-throw to trigger message bus retry logic
        }
    }

    /// <summary>
    /// Releases processing lock in MongoDB and publishes completion event
    /// </summary>
    private async Task ReleaseProcessingLockAsync(
        string dataSourceId, 
        string correlationId, 
        int filesProcessed, 
        long totalRecords,
        bool success, 
        string? errorMessage = null)
    {
        try
        {
            // Release lock in MongoDB
            await DB.Update<DataProcessingDataSource>()
                .Match(ds => ds.ID == dataSourceId)
                .Modify(ds => ds.IsCurrentlyProcessing, false)
                .Modify(ds => ds.ProcessingCompletedAt, DateTime.UtcNow)
                .Modify(ds => ds.ProcessingCorrelationId, null)
                .ExecuteAsync();

            // Publish completion event
            var completionEvent = new FileProcessingCompletedEvent
            {
                CorrelationId = correlationId,
                PublishedBy = "FilesReceiver",
                DataSourceId = dataSourceId,
                FilesProcessed = filesProcessed,
                TotalRecords = totalRecords,
                Success = success,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(completionEvent);
            _metrics.RecordMessageSent("FileProcessingCompletedEvent", "FilesReceiver", true);

            Logger.LogInformation(
                "Released processing lock for {DataSourceId}. Files: {FilesProcessed}, Success: {Success}",
                dataSourceId, filesProcessed, success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to release processing lock for {DataSourceId}", dataSourceId);
        }
    }
}
