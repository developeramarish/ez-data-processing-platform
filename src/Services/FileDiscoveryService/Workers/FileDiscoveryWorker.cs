using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using DataProcessing.Shared.Utilities;
using MassTransit;
using MongoDB.Entities;
using Quartz;

namespace DataProcessing.FileDiscovery.Workers;

/// <summary>
/// Quartz job that discovers new files from configured data sources
/// Runs periodically to check all active datasources for new files
/// </summary>
[DisallowConcurrentExecution]
public class FileDiscoveryWorker : IJob
{
    private readonly ILogger<FileDiscoveryWorker> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public FileDiscoveryWorker(
        ILogger<FileDiscoveryWorker> logger,
        IPublishEndpoint publishEndpoint,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("[{CorrelationId}] FileDiscoveryWorker: Starting file discovery cycle", correlationId);

        try
        {
            // Query all active datasources
            var datasources = await DB.Find<DataProcessingDataSource>()
                .Match(ds => ds.IsActive)
                .ExecuteAsync();

            _logger.LogInformation("[{CorrelationId}] Found {Count} active datasources to check", 
                correlationId, datasources.Count);

            var discoveredCount = 0;

            foreach (var datasource in datasources)
            {
                try
                {
                    // Check if it's time to poll this datasource based on its cron expression
                    var cronExpression = datasource.GetEffectiveCronExpression();
                    
                    if (ShouldPollDatasource(datasource, cronExpression))
                    {
                        _logger.LogDebug("[{CorrelationId}] Checking datasource: {DataSourceName} ({DataSourceId})", 
                            correlationId, datasource.Name, datasource.ID);

                        var files = await DiscoverFilesAsync(datasource, correlationId);
                        
                        if (files.Any())
                        {
                            _logger.LogInformation(
                                "[{CorrelationId}] Discovered {FileCount} file(s) from datasource {DataSourceName}",
                                correlationId, files.Count, datasource.Name);

                            // Publish FileDiscoveredEvent for each file
                            foreach (var file in files)
                            {
                                await PublishFileDiscoveredEventAsync(datasource, file, correlationId);
                                discoveredCount++;
                            }

                            // Update last processed timestamp
                            datasource.LastProcessedAt = DateTime.UtcNow;
                            await datasource.SaveAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "[{CorrelationId}] Error discovering files from datasource {DataSourceName} ({DataSourceId})", 
                        correlationId, datasource.Name, datasource.ID);
                }
            }

            _logger.LogInformation(
                "[{CorrelationId}] FileDiscoveryWorker: Completed. Discovered {TotalFiles} file(s) from {DataSourceCount} datasource(s)",
                correlationId, discoveredCount, datasources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] FileDiscoveryWorker: Fatal error during discovery cycle", correlationId);
        }
    }

    /// <summary>
    /// Determines if a datasource should be polled based on its cron schedule
    /// </summary>
    private bool ShouldPollDatasource(DataProcessingDataSource datasource, string cronExpression)
    {
        // If LastProcessedAt is null, poll immediately
        if (!datasource.LastProcessedAt.HasValue)
            return true;

        // Simple time-based check: if more time has passed than the minimum interval
        // For production, you'd parse the cron expression and check next run time
        var timeSinceLastPoll = DateTime.UtcNow - datasource.LastProcessedAt.Value;
        var pollingRate = datasource.PollingRate;

        // Poll if enough time has passed
        return timeSinceLastPoll >= pollingRate;
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

        // Get the appropriate connector based on datasource type
        // For now, we'll use LocalFileConnector as the default
        // In a full implementation, you'd detect the type from datasource.FilePath
        var connector = scope.ServiceProvider.GetRequiredService<LocalFileConnector>();

        // Get deduplication TTL from configuration (default: 24 hours)
        var ttlHours = _configuration.GetValue("FileDiscovery:DeduplicationTTLHours", 24);
        var ttl = TimeSpan.FromHours(ttlHours);

        try
        {
            // List files using the connector
            var filePaths = await connector.ListFilesAsync(
                datasource,
                datasource.FilePattern);

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
                "[{CorrelationId}] Error using connector to discover files from {Path}",
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
        string correlationId)
    {
        var pollBatchId = Guid.NewGuid(); // Each discovery cycle gets a batch ID
        
        var @event = new FileDiscoveredEvent
        {
            CorrelationId = correlationId,
            DataSourceId = datasource.ID!,
            FilePath = file.FilePath,
            FileName = file.FileName,
            FileSizeBytes = file.FileSizeBytes,
            DiscoveredAt = DateTime.UtcNow,
            SequenceNumber = 1, // Will be improved in production to track actual sequence
            PollBatchId = pollBatchId
        };

        await _publishEndpoint.Publish(@event);

        _logger.LogInformation(
            "[{CorrelationId}] Published FileDiscoveredEvent for file {FileName} from datasource {DataSourceName}",
            correlationId, file.FileName, datasource.Name);
    }
}
