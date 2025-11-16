using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using MassTransit;
using MongoDB.Entities;

namespace DataProcessing.FileDiscovery.Consumers;

/// <summary>
/// Consumes FilePollingEvent messages from SchedulingService
/// Discovers files from the specified datasource and publishes FileDiscoveredEvent for each
/// </summary>
public class FilePollingEventConsumer : IConsumer<FilePollingEvent>
{
    private readonly ILogger<FilePollingEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IServiceScopeFactory _scopeFactory;

    public FilePollingEventConsumer(
        ILogger<FilePollingEventConsumer> logger,
        IPublishEndpoint publishEndpoint,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _scopeFactory = scopeFactory;
    }

    public async Task Consume(ConsumeContext<FilePollingEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "[{CorrelationId}] FilePollingEventConsumer: Received polling request for datasource {DataSourceId} ({DataSourceName})",
            message.CorrelationId, message.DataSourceId, message.DataSourceName);

        try
        {
            // Get datasource from MongoDB
            var datasource = await DB.Find<DataProcessingDataSource>()
                .OneAsync(message.DataSourceId);

            if (datasource == null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Datasource {DataSourceId} not found",
                    message.CorrelationId, message.DataSourceId);
                return;
            }

            // Discover files using connector
            var files = await DiscoverFilesAsync(datasource, message.CorrelationId);

            if (!files.Any())
            {
                _logger.LogDebug(
                    "[{CorrelationId}] No files discovered from datasource {DataSourceName}",
                    message.CorrelationId, datasource.Name);
                return;
            }

            _logger.LogInformation(
                "[{CorrelationId}] Discovered {FileCount} file(s) from datasource {DataSourceName}",
                message.CorrelationId, files.Count, datasource.Name);

            // Publish FileDiscoveredEvent for each file
            var pollBatchId = Guid.NewGuid();
            for (int i = 0; i < files.Count; i++)
            {
                await PublishFileDiscoveredEventAsync(
                    datasource, 
                    files[i], 
                    message.CorrelationId, 
                    pollBatchId, 
                    i);
            }

            // Update last processed timestamp
            datasource.LastProcessedAt = DateTime.UtcNow;
            await datasource.SaveAsync();

            _logger.LogInformation(
                "[{CorrelationId}] Published {EventCount} FileDiscoveredEvent(s) for datasource {DataSourceName}",
                message.CorrelationId, files.Count, datasource.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error processing FilePollingEvent for datasource {DataSourceId}",
                message.CorrelationId, message.DataSourceId);
            throw; // Let MassTransit handle retry logic
        }
    }

    /// <summary>
    /// Discovers files from a datasource using the appropriate connector
    /// </summary>
    private async Task<List<FileMetadata>> DiscoverFilesAsync(
        DataProcessingDataSource datasource,
        string correlationId)
    {
        using var scope = _scopeFactory.CreateScope();

        // Get the appropriate connector (for now, using LocalFileConnector)
        // Future: Implement factory to select FTP, SFTP, etc. based on datasource.FilePath
        var connector = scope.ServiceProvider.GetRequiredService<LocalFileConnector>();

        try
        {
            // List files matching the pattern
            var filePaths = await connector.ListFilesAsync(datasource, datasource.FilePattern);

            // Get metadata for each file
            var fileMetadataList = new List<FileMetadata>();
            foreach (var filePath in filePaths)
            {
                try
                {
                    var metadata = await connector.GetFileMetadataAsync(datasource, filePath);
                    fileMetadataList.Add(metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{CorrelationId}] Failed to get metadata for file {FilePath}",
                        correlationId, filePath);
                }
            }

            _logger.LogDebug(
                "[{CorrelationId}] Connector discovered {FileCount} files from {Path}",
                correlationId, fileMetadataList.Count, datasource.FilePath);

            return fileMetadataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error discovering files from {Path}",
                correlationId, datasource.FilePath);
            return new List<FileMetadata>();
        }
    }

    /// <summary>
    /// Publishes a FileDiscoveredEvent for downstream processing
    /// </summary>
    private async Task PublishFileDiscoveredEventAsync(
        DataProcessingDataSource datasource,
        FileMetadata file,
        string correlationId,
        Guid pollBatchId,
        int sequenceNumber)
    {
        var fileEvent = new FileDiscoveredEvent
        {
            CorrelationId = correlationId,
            DataSourceId = datasource.ID!,
            FilePath = file.FilePath,
            FileName = file.FileName,
            FileSizeBytes = file.FileSizeBytes,
            DiscoveredAt = DateTime.UtcNow,
            SequenceNumber = sequenceNumber,
            PollBatchId = pollBatchId
        };

        await _publishEndpoint.Publish(fileEvent);

        _logger.LogDebug(
            "[{CorrelationId}] Published FileDiscoveredEvent for file {FileName} (sequence {SequenceNumber})",
            correlationId, file.FileName, sequenceNumber);
    }
}
