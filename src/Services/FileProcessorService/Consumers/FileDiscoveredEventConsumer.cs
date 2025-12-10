using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Converters;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using Hazelcast;
using MassTransit;
using MongoDB.Entities;

namespace DataProcessing.FileProcessor.Consumers;

/// <summary>
/// Consumes FileDiscoveredEvent messages from FileDiscoveryService
/// Reads file content, converts to JSON, stores in Hazelcast, and publishes ValidationRequestEvent
/// </summary>
public class FileDiscoveredEventConsumer : IConsumer<FileDiscoveredEvent>
{
    private readonly ILogger<FileDiscoveredEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHazelcastClient _hazelcastClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public FileDiscoveredEventConsumer(
        ILogger<FileDiscoveredEventConsumer> logger,
        IPublishEndpoint publishEndpoint,
        IHazelcastClient hazelcastClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _hazelcastClient = hazelcastClient;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<FileDiscoveredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "[{CorrelationId}] FileDiscoveredEventConsumer: Processing file {FileName} from datasource {DataSourceId}",
            message.CorrelationId, message.FileName, message.DataSourceId);

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

            // Read file content
            var fileContent = await ReadFileContentAsync(datasource, message.FilePath, message.CorrelationId);
            if (string.IsNullOrEmpty(fileContent))
            {
                _logger.LogWarning(
                    "[{CorrelationId}] File {FileName} is empty or could not be read",
                    message.CorrelationId, message.FileName);
                return;
            }

            // Detect and convert to JSON
            var (jsonContent, originalFormat, metadata) = await ConvertToJsonAsync(
                fileContent, 
                message.FileName, 
                message.CorrelationId);

            if (string.IsNullOrEmpty(jsonContent))
            {
                _logger.LogError(
                    "[{CorrelationId}] Failed to convert file {FileName} to JSON",
                    message.CorrelationId, message.FileName);
                return;
            }

            // Store in Hazelcast
            var hazelcastKey = await StoreInHazelcastAsync(
                jsonContent,
                message.CorrelationId);

            _logger.LogInformation(
                "[{CorrelationId}] File {FileName} converted to JSON ({Format}) and cached in Hazelcast with key {HazelcastKey}",
                message.CorrelationId, message.FileName, originalFormat, hazelcastKey);

            // Publish ValidationRequestEvent
            await PublishValidationRequestAsync(
                datasource,
                message,
                hazelcastKey,
                originalFormat,
                metadata);

            _logger.LogInformation(
                "[{CorrelationId}] Published ValidationRequestEvent for file {FileName}",
                message.CorrelationId, message.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error processing file {FileName} from datasource {DataSourceId}",
                message.CorrelationId, message.FileName, message.DataSourceId);
            throw; // Let MassTransit handle retry logic
        }
    }

    /// <summary>
    /// Reads file content using the appropriate connector
    /// </summary>
    private async Task<string> ReadFileContentAsync(
        DataProcessingDataSource datasource,
        string filePath,
        string correlationId)
    {
        using var scope = _scopeFactory.CreateScope();
        
        // Get appropriate connector (for now, LocalFileConnector)
        var connector = scope.ServiceProvider.GetRequiredService<LocalFileConnector>();

        try
        {
            using var stream = await connector.ReadFileAsync(datasource, filePath);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            _logger.LogDebug(
                "[{CorrelationId}] Read {Bytes} bytes from file {FilePath}",
                correlationId, content.Length, filePath);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error reading file {FilePath}",
                correlationId, filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Converts file content to JSON using appropriate format converter
    /// </summary>
    private async Task<(string jsonContent, string originalFormat, Dictionary<string, object> metadata)> ConvertToJsonAsync(
        string fileContent,
        string fileName,
        string correlationId)
    {
        using var scope = _scopeFactory.CreateScope();

        try
        {
            // Detect format from file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var originalFormat = extension switch
            {
                ".csv" => "csv",
                ".xml" => "xml",
                ".xlsx" or ".xls" => "excel",
                ".json" => "json",
                _ => "unknown"
            };

            IFormatConverter? converter = extension switch
            {
                ".csv" => scope.ServiceProvider.GetRequiredService<CsvToJsonConverter>(),
                ".xml" => scope.ServiceProvider.GetRequiredService<XmlToJsonConverter>(),
                ".xlsx" or ".xls" => scope.ServiceProvider.GetRequiredService<ExcelToJsonConverter>(),
                ".json" => scope.ServiceProvider.GetRequiredService<JsonToJsonConverter>(),
                _ => null
            };

            if (converter == null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Unsupported file format: {Extension}",
                    correlationId, extension);
                return (string.Empty, originalFormat, new Dictionary<string, object>());
            }

            // Convert content to JSON
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            using (var stream = new MemoryStream(contentBytes))
            {
                var jsonContent = await converter.ConvertToJsonAsync(stream);

                // Extract metadata using a fresh stream (converter may have disposed the first one)
                using var metadataStream = new MemoryStream(contentBytes);
                var metadata = await converter.ExtractMetadataAsync(metadataStream);

                _logger.LogDebug(
                    "[{CorrelationId}] Converted {Format} file to JSON ({JsonLength} characters)",
                    correlationId, originalFormat, jsonContent.Length);

                return (jsonContent, originalFormat, metadata);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error converting file {FileName} to JSON",
                correlationId, fileName);
            return (string.Empty, "unknown", new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Stores JSON content in Hazelcast with TTL
    /// </summary>
    private async Task<string> StoreInHazelcastAsync(
        string jsonContent,
        string correlationId)
    {
        try
        {
            var cacheKey = $"file:{Guid.NewGuid()}";
            var ttlHours = _configuration.GetValue("Hazelcast:CacheTTLHours", 1);
            
            var fileContentMap = await _hazelcastClient.GetMapAsync<string, string>("file-content");
            await fileContentMap.SetAsync(cacheKey, jsonContent, TimeSpan.FromHours(ttlHours));

            _logger.LogDebug(
                "[{CorrelationId}] Stored {Bytes} bytes in Hazelcast with key {CacheKey} (TTL: {TTLHours}h)",
                correlationId, jsonContent.Length, cacheKey, ttlHours);

            return cacheKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Error storing content in Hazelcast",
                correlationId);
            throw;
        }
    }

    /// <summary>
    /// Publishes ValidationRequestEvent for the ValidationService
    /// </summary>
    private async Task PublishValidationRequestAsync(
        DataProcessingDataSource datasource,
        FileDiscoveredEvent fileEvent,
        string hazelcastKey,
        string originalFormat,
        Dictionary<string, object> metadata)
    {
        var validationEvent = new ValidationRequestEvent
        {
            CorrelationId = fileEvent.CorrelationId,
            PublishedBy = "FileProcessorService",
            DataSourceId = datasource.ID!,
            FileName = fileEvent.FileName,
            HazelcastKey = hazelcastKey,
            OriginalFormat = originalFormat,
            FormatMetadata = metadata
        };

        await _publishEndpoint.Publish(validationEvent);

        _logger.LogDebug(
            "[{CorrelationId}] Published ValidationRequestEvent with Hazelcast key {HazelcastKey}",
            fileEvent.CorrelationId, hazelcastKey);
    }
}
