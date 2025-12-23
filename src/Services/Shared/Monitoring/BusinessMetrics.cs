using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessing.Shared.Monitoring;

/// <summary>
/// Helper class for emitting business metrics that will be routed to Business Prometheus.
/// All metrics use the "business_" prefix for proper routing by OpenTelemetry Collector.
///
/// STANDARD LABELS (used across all metrics where applicable):
/// - data_source: The datasource identifier (e.g., "customer-transactions")
/// - service: The emitting service (e.g., "FileProcessor", "Validation", "Output")
/// - pipeline_stage: Processing stage (discovery, processing, validation, output)
/// - status: Outcome status (success, failed, skipped, partial)
/// - error_type: Error classification (schema, type_mismatch, missing_field, format)
/// - file_type: File format (csv, json, xml, parquet)
/// - priority: Job priority (critical, high, medium, low)
/// - operation_type: Operation kind (full_file, incremental, retry, reprocess)
/// - validation_mode: Validation strictness (strict, lenient, schema_only)
/// - output_destination: Output target (kafka, file, api, database)
///
/// BUSINESS CONTEXT LABELS (from DataSource properties):
/// - category: Business category of datasource (e.g., "financial", "sales", "inventory")
/// - supplier_name: Data supplier/provider (e.g., "bank_hapoalim", "supplier_a")
/// - file_format: Expected file format derived from pattern (csv, json, xml, parquet)
/// - schedule_type: Processing schedule (hourly, daily, weekly, monthly, on_demand)
/// </summary>
public class BusinessMetrics : IDisposable
{
    private readonly Meter _meter;
    private bool _disposed;

    #region Business Context Labels Helper

    /// <summary>
    /// Business context labels derived from DataSource properties.
    /// These provide business-level grouping and filtering capabilities.
    /// </summary>
    public record BusinessContext(
        string? Category = null,         // From DataSource.Category (e.g., "financial", "sales")
        string? SupplierName = null,     // From DataSource.SupplierName (e.g., "bank_hapoalim")
        string? FileFormat = null,       // Derived from DataSource.FilePattern (csv, json, xml)
        string? ScheduleType = null      // From DataSource.ScheduleFrequency (hourly, daily, etc.)
    )
    {
        /// <summary>
        /// Adds business context labels to an existing TagList
        /// </summary>
        public void AddToTags(ref TagList tags)
        {
            if (!string.IsNullOrEmpty(Category))
                tags.Add("category", Category);
            if (!string.IsNullOrEmpty(SupplierName))
                tags.Add("supplier_name", SupplierName);
            if (!string.IsNullOrEmpty(FileFormat))
                tags.Add("file_format", FileFormat);
            if (!string.IsNullOrEmpty(ScheduleType))
                tags.Add("schedule_type", ScheduleType);
        }

        /// <summary>
        /// Creates a BusinessContext from DataSource properties
        /// </summary>
        public static BusinessContext FromDataSource(
            string? category = null,
            string? supplierName = null,
            string? filePattern = null,
            string? scheduleFrequency = null)
        {
            // Derive file format from pattern (e.g., "*.csv" -> "csv")
            string? fileFormat = null;
            if (!string.IsNullOrEmpty(filePattern))
            {
                var ext = filePattern.ToLowerInvariant();
                if (ext.Contains(".csv")) fileFormat = "csv";
                else if (ext.Contains(".json")) fileFormat = "json";
                else if (ext.Contains(".xml")) fileFormat = "xml";
                else if (ext.Contains(".parquet")) fileFormat = "parquet";
                else if (ext.Contains(".xlsx") || ext.Contains(".xls")) fileFormat = "excel";
            }

            // Normalize schedule type
            string? scheduleType = scheduleFrequency?.ToLowerInvariant() switch
            {
                "hourly" or "1h" or "60m" => "hourly",
                "daily" or "1d" or "24h" => "daily",
                "weekly" or "7d" => "weekly",
                "monthly" or "30d" => "monthly",
                "on_demand" or "manual" => "on_demand",
                _ => scheduleFrequency?.ToLowerInvariant()
            };

            return new BusinessContext(category, supplierName, fileFormat, scheduleType);
        }
    }

    #endregion

    #region Original 9 Metrics

    // 1. Records processed counter
    private readonly Counter<long> _recordsProcessedCounter;

    // 2. Invalid records counter
    private readonly Counter<long> _invalidRecordsCounter;

    // 3. Validation error rate histogram
    private readonly Histogram<double> _validationErrorRate;

    // 4. Processing duration histogram
    private readonly Histogram<double> _processingDuration;

    // 5. Files processed counter
    private readonly Counter<long> _filesProcessedCounter;

    // 6. File size histogram
    private readonly Histogram<double> _fileSizeBytes;

    // 7. Active jobs gauge (up/down counter)
    private readonly UpDownCounter<long> _activeJobsCounter;

    // 8. Jobs completed counter
    private readonly Counter<long> _jobCompletedCounter;

    // 9. Jobs failed counter
    private readonly Counter<long> _jobFailedCounter;

    #endregion

    #region New 11 Metrics (Total 20)

    // 10. Bytes processed counter - Total data volume
    private readonly Counter<long> _bytesProcessedCounter;

    // 11. Output records counter - Records successfully output
    private readonly Counter<long> _outputRecordsCounter;

    // 12. Output bytes counter - Bytes written to output
    private readonly Counter<long> _outputBytesCounter;

    // 13. End-to-end latency histogram - Full pipeline latency
    private readonly Histogram<double> _endToEndLatency;

    // 14. Queue wait time histogram - Time in Kafka queues
    private readonly Histogram<double> _queueWaitTime;

    // 15. Validation latency histogram - Validation stage time
    private readonly Histogram<double> _validationLatency;

    // 16. Retry attempts counter - Number of retry attempts
    private readonly Counter<long> _retryAttemptsCounter;

    // 17. Dead letter records counter - Records sent to DLQ
    private readonly Counter<long> _deadLetterRecordsCounter;

    // 18. Records skipped counter - Records filtered/skipped
    private readonly Counter<long> _recordsSkippedCounter;

    // 19. Files pending gauge - Files awaiting processing
    private readonly UpDownCounter<long> _filesPendingCounter;

    // 20. Batches processed counter - Processing batch count
    private readonly Counter<long> _batchesProcessedCounter;

    #endregion

    #region New 4 Metrics (Migrated from Legacy DataProcessingMetrics)

    // 21. Validation errors counter - Total validation errors (migrated from dataprocessing_validation_errors_total)
    private readonly Counter<long> _validationErrorsCounter;

    // 22. Active datasources gauge - Number of active data sources (migrated from dataprocessing_active_datasources_total)
    private readonly UpDownCounter<long> _activeDatasourcesCounter;

    // 23. Messages sent counter - Messages sent via message bus (migrated from dataprocessing_messages_sent_total)
    private readonly Counter<long> _messagesSentCounter;

    // 24. Messages received counter - Messages received from message bus (migrated from dataprocessing_messages_received_total)
    private readonly Counter<long> _messagesReceivedCounter;

    #endregion

    public BusinessMetrics(Meter meter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));

        #region Initialize Original 9 Metrics

        _recordsProcessedCounter = _meter.CreateCounter<long>(
            "business_records_processed_total",
            description: "Total number of records processed by data source");

        _invalidRecordsCounter = _meter.CreateCounter<long>(
            "business_invalid_records_total",
            description: "Total number of invalid records by error type");

        _validationErrorRate = _meter.CreateHistogram<double>(
            "business_validation_error_rate",
            unit: "percent",
            description: "Validation error rate percentage per batch");

        _processingDuration = _meter.CreateHistogram<double>(
            "business_processing_duration_seconds",
            unit: "s",
            description: "Time taken to process files or batches");

        _filesProcessedCounter = _meter.CreateCounter<long>(
            "business_files_processed_total",
            description: "Total number of files processed");

        _fileSizeBytes = _meter.CreateHistogram<double>(
            "business_file_size_bytes",
            unit: "By",
            description: "Size of processed files in bytes");

        _activeJobsCounter = _meter.CreateUpDownCounter<long>(
            "business_active_jobs",
            description: "Number of currently active jobs");

        _jobCompletedCounter = _meter.CreateCounter<long>(
            "business_jobs_completed_total",
            description: "Total number of completed jobs");

        _jobFailedCounter = _meter.CreateCounter<long>(
            "business_jobs_failed_total",
            description: "Total number of failed jobs");

        #endregion

        #region Initialize New 11 Metrics

        _bytesProcessedCounter = _meter.CreateCounter<long>(
            "business_bytes_processed_total",
            unit: "By",
            description: "Total bytes processed across all files");

        _outputRecordsCounter = _meter.CreateCounter<long>(
            "business_output_records_total",
            description: "Total number of records successfully written to output");

        _outputBytesCounter = _meter.CreateCounter<long>(
            "business_output_bytes_total",
            unit: "By",
            description: "Total bytes written to output destinations");

        _endToEndLatency = _meter.CreateHistogram<double>(
            "business_end_to_end_latency_seconds",
            unit: "s",
            description: "End-to-end pipeline latency from file discovery to output");

        _queueWaitTime = _meter.CreateHistogram<double>(
            "business_queue_wait_time_seconds",
            unit: "s",
            description: "Time spent waiting in Kafka message queues");

        _validationLatency = _meter.CreateHistogram<double>(
            "business_validation_latency_seconds",
            unit: "s",
            description: "Time spent in validation stage");

        _retryAttemptsCounter = _meter.CreateCounter<long>(
            "business_retry_attempts_total",
            description: "Total number of retry attempts for failed operations");

        _deadLetterRecordsCounter = _meter.CreateCounter<long>(
            "business_dead_letter_records_total",
            description: "Total number of records sent to dead letter queue");

        _recordsSkippedCounter = _meter.CreateCounter<long>(
            "business_records_skipped_total",
            description: "Total number of records skipped/filtered during processing");

        _filesPendingCounter = _meter.CreateUpDownCounter<long>(
            "business_files_pending",
            description: "Number of files pending/awaiting processing");

        _batchesProcessedCounter = _meter.CreateCounter<long>(
            "business_batches_processed_total",
            description: "Total number of processing batches completed");

        #endregion

        #region Initialize 4 New Metrics (Migrated from Legacy)

        _validationErrorsCounter = _meter.CreateCounter<long>(
            "business_validation_errors_total",
            description: "Total number of validation errors by error type and severity");

        _activeDatasourcesCounter = _meter.CreateUpDownCounter<long>(
            "business_active_datasources_total",
            description: "Number of currently active data sources");

        _messagesSentCounter = _meter.CreateCounter<long>(
            "business_messages_sent_total",
            description: "Total number of messages sent via message bus");

        _messagesReceivedCounter = _meter.CreateCounter<long>(
            "business_messages_received_total",
            description: "Total number of messages received from message bus");

        #endregion
    }

    #region Original Methods (Updated with service label)

    /// <summary>
    /// Records the number of records processed
    /// Labels: data_source, service, status, pipeline_stage
    /// </summary>
    public void RecordProcessedRecords(long count, string dataSource, string service,
        string? status = null, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);
        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        _recordsProcessedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records invalid records with error classification
    /// Labels: data_source, service, error_type, validation_mode
    /// </summary>
    public void RecordInvalidRecords(long count, string dataSource, string service,
        string errorType, string? validationMode = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "error_type", errorType }
        };

        if (!string.IsNullOrEmpty(validationMode))
            tags.Add("validation_mode", validationMode);

        _invalidRecordsCounter.Add(count, tags);
    }

    /// <summary>
    /// Records validation error rate for a batch
    /// Labels: data_source, service, validation_mode
    /// </summary>
    public void RecordValidationErrorRate(double errorRatePercentage, string dataSource,
        string service, string? validationMode = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(validationMode))
            tags.Add("validation_mode", validationMode);

        _validationErrorRate.Record(errorRatePercentage, tags);
    }

    /// <summary>
    /// Records processing duration
    /// Labels: data_source, service, operation_type, pipeline_stage
    /// </summary>
    public void RecordProcessingDuration(double durationSeconds, string dataSource,
        string service, string operationType, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "operation_type", operationType }
        };

        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        _processingDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records a processed file
    /// Labels: data_source, service, file_type, status
    /// </summary>
    public void RecordFileProcessed(string dataSource, string service, string fileType,
        bool success, string? operationType = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "file_type", fileType },
            { "status", success ? "success" : "failed" }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);

        _filesProcessedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records file size
    /// Labels: data_source, service, file_type
    /// </summary>
    public void RecordFileSize(long sizeBytes, string dataSource, string service, string fileType)
    {
        _fileSizeBytes.Record(sizeBytes, new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "file_type", fileType }
        });
    }

    /// <summary>
    /// Increments active jobs counter
    /// Labels: job_type, service, priority
    /// </summary>
    public void IncrementActiveJobs(string jobType, string service, string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _activeJobsCounter.Add(1, tags);
    }

    /// <summary>
    /// Decrements active jobs counter
    /// Labels: job_type, service, priority
    /// </summary>
    public void DecrementActiveJobs(string jobType, string service, string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _activeJobsCounter.Add(-1, tags);
    }

    /// <summary>
    /// Records a completed job
    /// Labels: job_type, data_source, service, priority
    /// </summary>
    public void RecordJobCompleted(string jobType, string dataSource, string service,
        string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _jobCompletedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a failed job
    /// Labels: job_type, data_source, service, error_reason, priority
    /// </summary>
    public void RecordJobFailed(string jobType, string dataSource, string service,
        string? errorReason = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(errorReason))
            tags.Add("error_reason", errorReason);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _jobFailedCounter.Add(1, tags);
    }

    #endregion

    #region New Methods for 11 Additional Metrics

    /// <summary>
    /// Records bytes processed
    /// Labels: data_source, service, file_type, pipeline_stage
    /// </summary>
    public void RecordBytesProcessed(long bytes, string dataSource, string service,
        string? fileType = null, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(fileType))
            tags.Add("file_type", fileType);
        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        _bytesProcessedCounter.Add(bytes, tags);
    }

    /// <summary>
    /// Records output records successfully written
    /// Labels: data_source, service, output_destination, status
    /// </summary>
    public void RecordOutputRecords(long count, string dataSource, string service,
        string outputDestination, string? status = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "output_destination", outputDestination }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);

        _outputRecordsCounter.Add(count, tags);
    }

    /// <summary>
    /// Records output bytes written
    /// Labels: data_source, service, output_destination
    /// </summary>
    public void RecordOutputBytes(long bytes, string dataSource, string service,
        string outputDestination)
    {
        _outputBytesCounter.Add(bytes, new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "output_destination", outputDestination }
        });
    }

    /// <summary>
    /// Records end-to-end pipeline latency
    /// Labels: data_source, service, operation_type, priority
    /// </summary>
    public void RecordEndToEndLatency(double latencySeconds, string dataSource, string service,
        string? operationType = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _endToEndLatency.Record(latencySeconds, tags);
    }

    /// <summary>
    /// Records queue wait time
    /// Labels: data_source, service, pipeline_stage, queue_name
    /// </summary>
    public void RecordQueueWaitTime(double waitTimeSeconds, string dataSource, string service,
        string pipelineStage, string? queueName = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "pipeline_stage", pipelineStage }
        };

        if (!string.IsNullOrEmpty(queueName))
            tags.Add("queue_name", queueName);

        _queueWaitTime.Record(waitTimeSeconds, tags);
    }

    /// <summary>
    /// Records validation latency
    /// Labels: data_source, service, validation_mode
    /// </summary>
    public void RecordValidationLatency(double latencySeconds, string dataSource, string service,
        string? validationMode = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(validationMode))
            tags.Add("validation_mode", validationMode);

        _validationLatency.Record(latencySeconds, tags);
    }

    /// <summary>
    /// Records retry attempts
    /// Labels: data_source, service, operation_type, pipeline_stage, error_type
    /// </summary>
    public void RecordRetryAttempt(string dataSource, string service, string operationType,
        string? pipelineStage = null, string? errorType = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "operation_type", operationType }
        };

        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);
        if (!string.IsNullOrEmpty(errorType))
            tags.Add("error_type", errorType);

        _retryAttemptsCounter.Add(1, tags);
    }

    /// <summary>
    /// Records dead letter queue records
    /// Labels: data_source, service, error_type, pipeline_stage
    /// </summary>
    public void RecordDeadLetterRecords(long count, string dataSource, string service,
        string errorType, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "error_type", errorType }
        };

        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        _deadLetterRecordsCounter.Add(count, tags);
    }

    /// <summary>
    /// Records skipped records
    /// Labels: data_source, service, skip_reason, pipeline_stage
    /// </summary>
    public void RecordSkippedRecords(long count, string dataSource, string service,
        string skipReason, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "skip_reason", skipReason }
        };

        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        _recordsSkippedCounter.Add(count, tags);
    }

    /// <summary>
    /// Increments files pending counter
    /// Labels: data_source, service, file_type, priority
    /// </summary>
    public void IncrementFilesPending(string dataSource, string service,
        string? fileType = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(fileType))
            tags.Add("file_type", fileType);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _filesPendingCounter.Add(1, tags);
    }

    /// <summary>
    /// Decrements files pending counter
    /// Labels: data_source, service, file_type, priority
    /// </summary>
    public void DecrementFilesPending(string dataSource, string service,
        string? fileType = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(fileType))
            tags.Add("file_type", fileType);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        _filesPendingCounter.Add(-1, tags);
    }

    /// <summary>
    /// Records a processed batch
    /// Labels: data_source, service, status, operation_type
    /// </summary>
    public void RecordBatchProcessed(string dataSource, string service, bool success,
        string? operationType = null, int? batchSize = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "status", success ? "success" : "failed" }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);

        _batchesProcessedCounter.Add(1, tags);
    }

    #endregion

    #region Backward Compatibility Methods (without service parameter)

    // These methods maintain backward compatibility with existing code
    // They use "unknown" as the default service name

    public void RecordProcessedRecords(long count, string dataSource, string? status = null)
        => RecordProcessedRecords(count, dataSource, "unknown", status);

    public void RecordInvalidRecords(long count, string dataSource, string errorType)
        => RecordInvalidRecords(count, dataSource, "unknown", errorType);

    public void RecordValidationErrorRate(double errorRatePercentage, string dataSource, string? fileName = null)
        => RecordValidationErrorRate(errorRatePercentage, dataSource, "unknown");

    public void RecordProcessingDuration(double durationSeconds, string dataSource, string operationType)
        => RecordProcessingDuration(durationSeconds, dataSource, "unknown", operationType);

    public void RecordFileProcessed(string dataSource, string fileType, bool success)
        => RecordFileProcessed(dataSource, "unknown", fileType, success);

    public void RecordFileSize(long sizeBytes, string dataSource, string fileType)
        => RecordFileSize(sizeBytes, dataSource, "unknown", fileType);

    public void IncrementActiveJobs(string jobType)
        => IncrementActiveJobs(jobType, "unknown");

    public void DecrementActiveJobs(string jobType)
        => DecrementActiveJobs(jobType, "unknown");

    public void RecordJobCompleted(string jobType, string dataSource)
        => RecordJobCompleted(jobType, dataSource, "unknown");

    public void RecordJobFailed(string jobType, string dataSource, string? errorReason = null)
        => RecordJobFailed(jobType, dataSource, "unknown", errorReason);

    #endregion

    #region Methods with Business Context Labels

    /// <summary>
    /// Records processed records with full business context
    /// Labels: data_source, service, status, pipeline_stage + business context (category, supplier_name, etc.)
    /// </summary>
    public void RecordProcessedRecords(long count, string dataSource, string service,
        BusinessContext context, string? status = null, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);
        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        context.AddToTags(ref tags);
        _recordsProcessedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records invalid records with full business context
    /// Labels: data_source, service, error_type, validation_mode + business context
    /// </summary>
    public void RecordInvalidRecords(long count, string dataSource, string service,
        string errorType, BusinessContext context, string? validationMode = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "error_type", errorType }
        };

        if (!string.IsNullOrEmpty(validationMode))
            tags.Add("validation_mode", validationMode);

        context.AddToTags(ref tags);
        _invalidRecordsCounter.Add(count, tags);
    }

    /// <summary>
    /// Records a processed file with full business context
    /// Labels: data_source, service, file_type, status + business context
    /// </summary>
    public void RecordFileProcessed(string dataSource, string service, string fileType,
        bool success, BusinessContext context, string? operationType = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "file_type", fileType },
            { "status", success ? "success" : "failed" }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);

        context.AddToTags(ref tags);
        _filesProcessedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records processing duration with full business context
    /// Labels: data_source, service, operation_type, pipeline_stage + business context
    /// </summary>
    public void RecordProcessingDuration(double durationSeconds, string dataSource,
        string service, string operationType, BusinessContext context, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "operation_type", operationType }
        };

        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        context.AddToTags(ref tags);
        _processingDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records bytes processed with full business context
    /// Labels: data_source, service, file_type, pipeline_stage + business context
    /// </summary>
    public void RecordBytesProcessed(long bytes, string dataSource, string service,
        BusinessContext context, string? fileType = null, string? pipelineStage = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(fileType))
            tags.Add("file_type", fileType);
        if (!string.IsNullOrEmpty(pipelineStage))
            tags.Add("pipeline_stage", pipelineStage);

        context.AddToTags(ref tags);
        _bytesProcessedCounter.Add(bytes, tags);
    }

    /// <summary>
    /// Records output records with full business context
    /// Labels: data_source, service, output_destination, status + business context
    /// </summary>
    public void RecordOutputRecords(long count, string dataSource, string service,
        string outputDestination, BusinessContext context, string? status = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "output_destination", outputDestination }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);

        context.AddToTags(ref tags);
        _outputRecordsCounter.Add(count, tags);
    }

    /// <summary>
    /// Records end-to-end latency with full business context
    /// Labels: data_source, service, operation_type, priority + business context
    /// </summary>
    public void RecordEndToEndLatency(double latencySeconds, string dataSource, string service,
        BusinessContext context, string? operationType = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        context.AddToTags(ref tags);
        _endToEndLatency.Record(latencySeconds, tags);
    }

    /// <summary>
    /// Records a completed job with full business context
    /// Labels: job_type, data_source, service, priority + business context
    /// </summary>
    public void RecordJobCompleted(string jobType, string dataSource, string service,
        BusinessContext context, string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        context.AddToTags(ref tags);
        _jobCompletedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a failed job with full business context
    /// Labels: job_type, data_source, service, error_reason, priority + business context
    /// </summary>
    public void RecordJobFailed(string jobType, string dataSource, string service,
        BusinessContext context, string? errorReason = null, string? priority = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(errorReason))
            tags.Add("error_reason", errorReason);
        if (!string.IsNullOrEmpty(priority))
            tags.Add("priority", priority);

        context.AddToTags(ref tags);
        _jobFailedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a processed batch with full business context
    /// Labels: data_source, service, status, operation_type + business context
    /// </summary>
    public void RecordBatchProcessed(string dataSource, string service, bool success,
        BusinessContext context, string? operationType = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "status", success ? "success" : "failed" }
        };

        if (!string.IsNullOrEmpty(operationType))
            tags.Add("operation_type", operationType);

        context.AddToTags(ref tags);
        _batchesProcessedCounter.Add(1, tags);
    }

    #endregion

    #region Methods for 4 New Metrics (Migrated from Legacy)

    /// <summary>
    /// Records validation errors
    /// Labels: data_source, service, error_type, severity
    /// </summary>
    public void RecordValidationErrors(long count, string dataSource, string service,
        string errorType, string? severity = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource },
            { "service", service },
            { "error_type", errorType }
        };

        if (!string.IsNullOrEmpty(severity))
            tags.Add("severity", severity);

        _validationErrorsCounter.Add(count, tags);
    }

    /// <summary>
    /// Increments active datasources counter
    /// Labels: (none - global count)
    /// </summary>
    public void IncrementActiveDatasources()
    {
        _activeDatasourcesCounter.Add(1);
    }

    /// <summary>
    /// Decrements active datasources counter
    /// Labels: (none - global count)
    /// </summary>
    public void DecrementActiveDatasources()
    {
        _activeDatasourcesCounter.Add(-1);
    }

    /// <summary>
    /// Sets active datasources count (delta from current)
    /// Labels: (none - global count)
    /// </summary>
    public void SetActiveDatasources(long delta)
    {
        _activeDatasourcesCounter.Add(delta);
    }

    /// <summary>
    /// Records a message sent via message bus
    /// Labels: message_type, service, status
    /// </summary>
    public void RecordMessageSent(string messageType, string service, string? status = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);
        else
            tags.Add("status", "success");

        _messagesSentCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a message received from message bus
    /// Labels: message_type, service, status
    /// </summary>
    public void RecordMessageReceived(string messageType, string service, string? status = null)
    {
        var tags = new TagList
        {
            { "message_type", messageType },
            { "service", service }
        };

        if (!string.IsNullOrEmpty(status))
            tags.Add("status", status);
        else
            tags.Add("status", "success");

        _messagesReceivedCounter.Add(1, tags);
    }

    #endregion

    #region Custom Metric Factory Methods

    /// <summary>
    /// Creates a custom counter for domain-specific business metrics
    /// </summary>
    public Counter<T> CreateBusinessCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateCounter<T>(
            $"business_{name}",
            unit,
            description ?? $"Business metric: {name}");
    }

    /// <summary>
    /// Creates a custom histogram for domain-specific business metrics
    /// </summary>
    public Histogram<T> CreateBusinessHistogram<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateHistogram<T>(
            $"business_{name}",
            unit,
            description ?? $"Business metric: {name}");
    }

    /// <summary>
    /// Creates a custom gauge for domain-specific business metrics
    /// </summary>
    public ObservableGauge<T> CreateBusinessGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateObservableGauge(
            $"business_{name}",
            observeValue,
            unit,
            description ?? $"Business metric: {name}");
    }

    /// <summary>
    /// Creates a custom gauge with measurements for domain-specific business metrics
    /// </summary>
    public ObservableGauge<T> CreateBusinessGauge<T>(string name, Func<Measurement<T>> observeMeasurement, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateObservableGauge(
            $"business_{name}",
            observeMeasurement,
            unit,
            description ?? $"Business metric: {name}");
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _meter?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Extension methods for IServiceCollection to register BusinessMetrics
/// </summary>
public static class BusinessMetricsExtensions
{
    /// <summary>
    /// Adds BusinessMetrics with proper meter for routing to Prometheus Business
    /// </summary>
    public static IServiceCollection AddBusinessMetrics(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var meter = new Meter("DataProcessing.Business.Metrics", "1.0.0");
            return new BusinessMetrics(meter);
        });
        return services;
    }
}
