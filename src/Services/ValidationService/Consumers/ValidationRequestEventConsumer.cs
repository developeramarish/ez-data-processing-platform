using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Consumers;
using DataProcessing.Shared.Messages;
using DataProcessing.Shared.Monitoring;
using DataProcessing.Validation.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Hazelcast;
using Newtonsoft.Json;
using System.Text;

namespace DataProcessing.Validation.Consumers;

/// <summary>
/// Consumer for ValidationRequestEvent messages from the Files Receiver Service
/// Validates data records using JSON Schema and publishes results
/// Enhanced with Hazelcast caching and business metrics calculation
/// </summary>
public class ValidationRequestEventConsumer : DataProcessingConsumerBase<ValidationRequestEvent>
{
    private readonly IValidationService _validationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly DataProcessingMetrics _metrics;
    private readonly ActivitySource _activitySource;
    private readonly IHazelcastClient _hazelcastClient;
    private readonly ValidationMetrics _validationMetrics;
    private readonly DataMetricsCalculator _metricsCalculator;
    private readonly IConfiguration _configuration;

    public ValidationRequestEventConsumer(
        ILogger<ValidationRequestEventConsumer> logger,
        IValidationService validationService,
        IPublishEndpoint publishEndpoint,
        DataProcessingMetrics metrics,
        ActivitySource activitySource,
        IHazelcastClient hazelcastClient,
        ValidationMetrics validationMetrics,
        DataMetricsCalculator metricsCalculator,
        IConfiguration configuration)
        : base(logger)
    {
        _validationService = validationService;
        _publishEndpoint = publishEndpoint;
        _metrics = metrics;
        _activitySource = activitySource;
        _hazelcastClient = hazelcastClient;
        _validationMetrics = validationMetrics;
        _metricsCalculator = metricsCalculator;
        _configuration = configuration;
    }

    protected override async Task ProcessMessage(ConsumeContext<ValidationRequestEvent> context)
    {
        var message = context.Message;
        var stopwatch = Stopwatch.StartNew();

        using var activity = _activitySource.StartDataProcessingActivity(
            "ValidationRequestEventConsumer.ProcessMessage",
            message.CorrelationId,
            message.DataSourceId);

        Logger.LogInformation(
            "[{CorrelationId}] Processing validation request for file: {FileName}, DataSource: {DataSourceId}, HazelcastKey: {HazelcastKey}",
            message.CorrelationId, message.FileName, message.DataSourceId, message.HazelcastKey);

        // Record message received metric
        _metrics.RecordMessageReceived("ValidationRequestEvent", "Validation", true);
        _validationMetrics.IncrementActiveValidations();

        try
        {
            // Set activity context
            activity?.SetFileProcessingContext(message.FileName, null, message.FileSizeBytes);
            activity?.SetTag("validation.data_source_id", message.DataSourceId);
            activity?.SetTag("validation.hazelcast_key", message.HazelcastKey);

            // Step 1: Retrieve file content from Hazelcast
            byte[] fileContent;
            if (!string.IsNullOrEmpty(message.HazelcastKey))
            {
                Logger.LogDebug(
                    "[{CorrelationId}] Retrieving file content from Hazelcast: {HazelcastKey}",
                    message.CorrelationId, message.HazelcastKey);

                fileContent = await RetrieveFromHazelcastAsync(message.HazelcastKey, message.CorrelationId);
            }
            else
            {
                Logger.LogWarning(
                    "[{CorrelationId}] No HazelcastKey provided, falling back to FileContent property",
                    message.CorrelationId);

                fileContent = message.FileContent;
            }

            // Step 2: Validate the file content using JSON Schema
            var validationResult = await _validationService.ValidateFileContentAsync(
                message.DataSourceId,
                message.FileName,
                fileContent,
                message.FileContentType,
                message.CorrelationId);

            stopwatch.Stop();

            // Step 3: Record validation metrics (OpenTelemetry)
            _validationMetrics.RecordValidation(
                isValid: validationResult.InvalidRecords == 0,
                dataSourceId: message.DataSourceId,
                fileName: message.FileName,
                duration: stopwatch.Elapsed);

            // Step 4: Record legacy Prometheus metrics
            _metrics.RecordValidationResults(
                message.DataSourceId,
                validationResult.TotalRecords,
                validationResult.ValidRecords,
                validationResult.InvalidRecords);

            Logger.LogInformation(
                "[{CorrelationId}] Validation completed: Total={TotalRecords}, Valid={ValidRecords}, Invalid={InvalidRecords}, Duration={Duration}ms",
                message.CorrelationId, validationResult.TotalRecords, validationResult.ValidRecords,
                validationResult.InvalidRecords, stopwatch.ElapsedMilliseconds);

            // Step 5: Calculate business metrics (if records are valid)
            string? validRecordsKey = null;
            if (validationResult.ValidRecords > 0 && validationResult.ValidRecordsData != null)
            {
                await CalculateAndRecordBusinessMetricsAsync(
                    validationResult.ValidRecordsData,
                    message.DataSourceId,
                    message.CorrelationId);

                // Step 6: Store valid records in Hazelcast
                validRecordsKey = await StoreValidRecordsInHazelcastAsync(
                    validationResult.ValidRecordsData,
                    message.CorrelationId);

                Logger.LogInformation(
                    "[{CorrelationId}] Stored {Count} valid records in Hazelcast: {HazelcastKey}",
                    message.CorrelationId, validationResult.ValidRecords, validRecordsKey);
            }

            // Step 7: Publish validation completed event
            var completedEvent = new ValidationCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                PublishedBy = "Validation",
                DataSourceId = message.DataSourceId,
                FileName = message.FileName,
                ValidationResultId = validationResult.ValidationResultId,
                TotalRecords = validationResult.TotalRecords,
                ValidRecords = validationResult.ValidRecords,
                InvalidRecords = validationResult.InvalidRecords,
                ValidationStatus = validationResult.InvalidRecords == 0 ? "Success" : "PartialFailure",
                HazelcastValidRecordsKey = validRecordsKey ?? string.Empty,  // NEW: Populated with cache key (or empty if no valid records)
                ProcessingDuration = stopwatch.Elapsed,                       // NEW: Actual duration
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(completedEvent, context.CancellationToken);

            // Record message sent metric
            _metrics.RecordMessageSent("ValidationCompletedEvent", "Validation", true);

            activity?.SetValidationContext(
                validationResult.TotalRecords,
                validationResult.ValidRecords,
                validationResult.InvalidRecords);

            Logger.LogInformation(
                "[{CorrelationId}] Published ValidationCompletedEvent: ValidationResultId={ValidationResultId}, ValidRecordsKey={ValidRecordsKey}",
                message.CorrelationId, validationResult.ValidationResultId, validRecordsKey);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex,
                "[{CorrelationId}] Failed to validate file: {FileName} for data source {DataSourceId}: {ErrorMessage}",
                message.CorrelationId, message.FileName, message.DataSourceId, ex.Message);

            // Record error metrics
            _validationMetrics.RecordValidation(
                isValid: false,
                dataSourceId: message.DataSourceId,
                fileName: message.FileName,
                duration: stopwatch.Elapsed);

            _metrics.RecordFileProcessingFailed(message.DataSourceId, "Validation", "validation_error");
            _metrics.RecordMessageReceived("ValidationRequestEvent", "Validation", false);

            // Publish failure event
            var failureEvent = new FileProcessingFailedEvent
            {
                CorrelationId = message.CorrelationId,
                PublishedBy = "Validation",
                DataSourceId = message.DataSourceId,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(failureEvent, context.CancellationToken);
            _metrics.RecordMessageSent("FileProcessingFailedEvent", "Validation", true);

            activity?.SetError(ex, "Validation failed");
            throw; // Re-throw to trigger message bus retry logic
        }
        finally
        {
            _validationMetrics.DecrementActiveValidations();
        }
    }

    /// <summary>
    /// Retrieves file content from Hazelcast distributed cache
    /// </summary>
    private async Task<byte[]> RetrieveFromHazelcastAsync(string cacheKey, string correlationId)
    {
        try
        {
            var fileContentMap = await _hazelcastClient.GetMapAsync<string, string>("file-content");
            var jsonContent = await fileContentMap.GetAsync(cacheKey);

            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new InvalidOperationException($"File content not found in Hazelcast for key: {cacheKey}");
            }

            return Encoding.UTF8.GetBytes(jsonContent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "[{CorrelationId}] Failed to retrieve content from Hazelcast: {CacheKey}",
                correlationId, cacheKey);
            throw;
        }
    }

    /// <summary>
    /// Calculates and records business metrics from valid records
    /// </summary>
    private async Task CalculateAndRecordBusinessMetricsAsync(
        List<Newtonsoft.Json.Linq.JObject> validRecords,
        string dataSourceId,
        string correlationId)
    {
        try
        {
            // Get metric definitions (for now, using hardcoded examples)
            // TODO: Query MetricConfiguration service for actual metric definitions
            var metricDefinitions = GetDefaultMetricDefinitions();

            // Calculate metrics
            var calculatedMetrics = _metricsCalculator.CalculateMetrics(
                validRecords,
                metricDefinitions,
                correlationId);

            // Record each metric to OpenTelemetry
            foreach (var (metricName, value) in calculatedMetrics)
            {
                var metricDef = metricDefinitions.First(m => m.MetricName == metricName);
                _validationMetrics.RecordBusinessMetric(
                    metricName,
                    value,
                    metricDef.Unit,
                    dataSourceId);

                Logger.LogDebug(
                    "[{CorrelationId}] Business metric calculated: {MetricName}={Value} {Unit}",
                    correlationId, metricName, value, metricDef.Unit);
            }

            // Calculate category-based metrics if enabled
            if (_configuration.GetValue<bool>("MetricConfiguration:Enabled", true))
            {
                var categoryPath = _configuration.GetValue<string>("MetricConfiguration:CategoryJsonPath")
                    ?? "$.category";

                var categoryMetrics = _metricsCalculator.CalculateCategoryMetrics(
                    validRecords,
                    categoryPath,
                    correlationId);

                _validationMetrics.RecordCategoryItems(categoryMetrics, dataSourceId);

                Logger.LogDebug(
                    "[{CorrelationId}] Category metrics calculated: {CategoryCount} categories",
                    correlationId, categoryMetrics.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "[{CorrelationId}] Failed to calculate business metrics (non-fatal)",
                correlationId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stores valid records in Hazelcast for downstream processing
    /// </summary>
    private async Task<string> StoreValidRecordsInHazelcastAsync(
        List<Newtonsoft.Json.Linq.JObject> validRecords,
        string correlationId)
    {
        try
        {
            var validRecordsKey = $"valid-records:{Guid.NewGuid()}";
            var ttlHours = _configuration.GetValue("Hazelcast:CacheTTLHours", 1);

            var validRecordsJson = JsonConvert.SerializeObject(validRecords);

            var validRecordsMap = await _hazelcastClient.GetMapAsync<string, string>("valid-records");
            await validRecordsMap.SetAsync(
                validRecordsKey,
                validRecordsJson,
                TimeSpan.FromHours(ttlHours));

            return validRecordsKey;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "[{CorrelationId}] Failed to store valid records in Hazelcast",
                correlationId);
            throw;
        }
    }

    /// <summary>
    /// Returns default metric definitions
    /// TODO: Replace with MetricConfiguration service query
    /// </summary>
    private List<MetricDefinition> GetDefaultMetricDefinitions()
    {
        return new List<MetricDefinition>
        {
            new MetricDefinition
            {
                MetricName = "total_price",
                JsonPath = "$..price",
                AggregationType = "sum",
                Unit = "currency",
                Description = "Sum of all price values"
            },
            new MetricDefinition
            {
                MetricName = "item_count",
                JsonPath = "$..items",
                AggregationType = "count",
                Unit = "items",
                Description = "Total number of items"
            },
            new MetricDefinition
            {
                MetricName = "avg_price",
                JsonPath = "$..price",
                AggregationType = "avg",
                Unit = "currency",
                Description = "Average price"
            }
        };
    }
}
