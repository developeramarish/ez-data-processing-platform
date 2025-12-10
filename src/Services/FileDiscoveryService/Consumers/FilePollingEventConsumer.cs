using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using DataProcessing.Shared.Utilities;
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
    private readonly IConfiguration _configuration;

    public FilePollingEventConsumer(
        ILogger<FilePollingEventConsumer> logger,
        IPublishEndpoint publishEndpoint,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
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
                _logger.LogInformation(
                    "[{CorrelationId}] No files discovered from datasource {DataSourceName}, releasing lock",
                    message.CorrelationId, datasource.Name);

                // Release lock even when no files found
                datasource.LastProcessedAt = DateTime.UtcNow;
                datasource.ReleaseProcessingLock("completed_no_files");
                await datasource.SaveAsync();

                _logger.LogInformation(
                    "[{CorrelationId}] Lock released for datasource {DataSourceName} (no files found)",
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

            // Update last processed timestamp and release lock
            datasource.LastProcessedAt = DateTime.UtcNow;
            datasource.ReleaseProcessingLock("completed");
            await datasource.SaveAsync();

            _logger.LogInformation(
                "[{CorrelationId}] Published {EventCount} FileDiscoveredEvent(s) for datasource {DataSourceName}. Lock released.",
                message.CorrelationId, files.Count, datasource.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error processing FilePollingEvent for datasource {DataSourceId}",
                message.CorrelationId, message.DataSourceId);

            // Release lock on error
            try
            {
                var datasource = await DB.Find<DataProcessingDataSource>()
                    .OneAsync(message.DataSourceId);
                if (datasource != null)
                {
                    datasource.ReleaseProcessingLock("error");
                    await datasource.SaveAsync();
                }
            }
            catch (Exception lockEx)
            {
                _logger.LogWarning(lockEx,
                    "[{CorrelationId}] Failed to release lock after error for datasource {DataSourceId}",
                    message.CorrelationId, message.DataSourceId);
            }

            throw; // Let MassTransit handle retry logic
        }
    }

    /// <summary>
    /// Discovers files from a datasource using the appropriate connector
    /// Implements deduplication to prevent processing the same file multiple times
    /// </summary>
    private async Task<List<FileMetadata>> DiscoverFilesAsync(
        DataProcessingDataSource datasource,
        string correlationId)
    {
        using var scope = _scopeFactory.CreateScope();

        // Get the appropriate connector (for now, using LocalFileConnector)
        // Future: Implement factory to select FTP, SFTP, etc. based on datasource.FilePath
        var connector = scope.ServiceProvider.GetRequiredService<LocalFileConnector>();

        // Get deduplication TTL from configuration (default: 24 hours)
        var ttlHours = _configuration.GetValue("FileDiscovery:DeduplicationTTLHours", 24);
        var ttl = TimeSpan.FromHours(ttlHours);

        try
        {
            // List files matching the pattern
            var filePaths = await connector.ListFilesAsync(datasource, datasource.FilePattern);

            _logger.LogDebug(
                "[{CorrelationId}] Connector found {FileCount} file(s) from {Path}",
                correlationId, filePaths.Count, datasource.FilePath);

            // Get metadata for each file and apply deduplication
            var newFiles = new List<FileMetadata>();
            var duplicateCount = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var metadata = await connector.GetFileMetadataAsync(datasource, filePath);

                    // Calculate file hash for deduplication
                    var fileHash = FileHashCalculator.CalculateHash(metadata);

                    // Check if file already processed (not expired)
                    if (datasource.IsFileAlreadyProcessed(fileHash))
                    {
                        _logger.LogDebug(
                            "[{CorrelationId}] Skipping already processed file: {FileName} (hash: {Hash})",
                            correlationId, metadata.FileName, fileHash.Substring(0, 8) + "...");
                        duplicateCount++;
                        continue;
                    }

                    // Add to processed hashes with TTL
                    datasource.AddProcessedFileHash(
                        fileHash,
                        metadata.FileName,
                        metadata.FilePath,
                        metadata.FileSizeBytes,
                        metadata.LastModifiedUtc,
                        ttl,
                        correlationId);

                    newFiles.Add(metadata);

                    _logger.LogDebug(
                        "[{CorrelationId}] File queued for processing: {FileName} (hash: {Hash}, expires: {Expires})",
                        correlationId, metadata.FileName, fileHash.Substring(0, 8) + "...",
                        DateTime.UtcNow.Add(ttl).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{CorrelationId}] Failed to get metadata for file {FilePath}",
                        correlationId, filePath);
                }
            }

            // Cleanup expired hashes
            var cleanedCount = datasource.CleanupExpiredFileHashes();
            if (cleanedCount > 0)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] Cleaned up {Count} expired file hash(es) for datasource {DataSourceId}",
                    correlationId, cleanedCount, datasource.ID);
            }

            // Save datasource with updated hashes
            await datasource.SaveAsync();

            _logger.LogInformation(
                "[{CorrelationId}] Deduplication results: {NewFiles} new file(s), {Duplicates} duplicate(s) skipped, {ActiveHashes} active hash(es) tracked",
                correlationId, newFiles.Count, duplicateCount, datasource.GetActiveFileHashCount());

            return newFiles;
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
