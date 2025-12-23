using Prometheus;
using System.Diagnostics.Metrics;

namespace DataProcessing.Shared.Monitoring;

/// <summary>
/// [DEPRECATED] Centralized metrics collection for Data Processing Platform
/// Provides Prometheus metrics for monitoring file processing performance
///
/// DEPRECATION NOTICE (December 2025):
/// This class uses the legacy Prometheus-net library and is being replaced by
/// BusinessMetrics.cs which uses OpenTelemetry's System.Diagnostics.Metrics API.
///
/// Migration mapping:
/// - dataprocessing_files_processed_total → business_files_processed_total
/// - dataprocessing_records_processed_total → business_records_processed_total
/// - dataprocessing_validation_errors_total → business_validation_errors_total
/// - dataprocessing_processing_duration_seconds → business_processing_duration_seconds
/// - dataprocessing_file_size_bytes → business_file_size_bytes
/// - dataprocessing_validation_error_rate → business_validation_error_rate
/// - dataprocessing_active_datasources_total → business_active_datasources_total
/// - dataprocessing_pending_files_total → business_files_pending
/// - dataprocessing_messages_sent_total → business_messages_sent_total
/// - dataprocessing_messages_received_total → business_messages_received_total
///
/// New code should use BusinessMetrics instead. This class will be removed in a future release.
/// </summary>
[Obsolete("Use BusinessMetrics class instead. This legacy Prometheus-net class will be removed in a future release.")]
public class DataProcessingMetrics
{
    private readonly Meter _meter;
    
    // Prometheus metrics
    public static readonly Counter FilesProcessedTotal = Metrics
        .CreateCounter("dataprocessing_files_processed_total", 
            "Total number of files processed", 
            new[] { "data_source_id", "service", "status" });

    public static readonly Counter RecordsProcessedTotal = Metrics
        .CreateCounter("dataprocessing_records_processed_total", 
            "Total number of records processed", 
            new[] { "data_source_id", "service", "status" });

    public static readonly Counter ValidationErrorsTotal = Metrics
        .CreateCounter("dataprocessing_validation_errors_total", 
            "Total number of validation errors", 
            new[] { "data_source_id", "error_type", "severity" });

    public static readonly Histogram ProcessingDurationSeconds = Metrics
        .CreateHistogram("dataprocessing_processing_duration_seconds", 
            "File processing duration in seconds", 
            new[] { "data_source_id", "service", "operation" });

    public static readonly Histogram FileSizeBytes = Metrics
        .CreateHistogram("dataprocessing_file_size_bytes", 
            "Size of processed files in bytes", 
            new[] { "data_source_id", "file_extension" });

    public static readonly Gauge ValidationErrorRate = Metrics
        .CreateGauge("dataprocessing_validation_error_rate", 
            "Current validation error rate (0-1)", 
            new[] { "data_source_id" });

    public static readonly Gauge ActiveDataSources = Metrics
        .CreateGauge("dataprocessing_active_datasources_total", 
            "Number of active data sources");

    public static readonly Gauge PendingFiles = Metrics
        .CreateGauge("dataprocessing_pending_files_total", 
            "Number of files pending processing", 
            new[] { "data_source_id" });

    public static readonly Counter MessagesSentTotal = Metrics
        .CreateCounter("dataprocessing_messages_sent_total", 
            "Total number of messages sent via message bus", 
            new[] { "message_type", "service", "status" });

    public static readonly Counter MessagesReceivedTotal = Metrics
        .CreateCounter("dataprocessing_messages_received_total", 
            "Total number of messages received from message bus", 
            new[] { "message_type", "service", "status" });

    // .NET Metrics API for additional telemetry
    private readonly Counter<long> _filesProcessedCounter;
    private readonly Counter<long> _recordsProcessedCounter;
    private readonly Histogram<double> _processingDurationHistogram;
    private readonly Counter<long> _errorsCounter;

    public DataProcessingMetrics()
    {
        _meter = new Meter("DataProcessing.Platform", "1.0.0");
        
        // Initialize .NET metrics
        _filesProcessedCounter = _meter.CreateCounter<long>(
            "dataprocessing.files.processed",
            "count",
            "Number of files processed");

        _recordsProcessedCounter = _meter.CreateCounter<long>(
            "dataprocessing.records.processed",
            "count", 
            "Number of records processed");

        _processingDurationHistogram = _meter.CreateHistogram<double>(
            "dataprocessing.processing.duration",
            "seconds",
            "Processing duration in seconds");

        _errorsCounter = _meter.CreateCounter<long>(
            "dataprocessing.errors",
            "count",
            "Number of errors occurred");
    }

    /// <summary>
    /// Records successful file processing
    /// </summary>
    public void RecordFileProcessed(string dataSourceId, string service, int recordCount, double durationSeconds)
    {
        FilesProcessedTotal.WithLabels(dataSourceId, service, "success").Inc();
        RecordsProcessedTotal.WithLabels(dataSourceId, service, "success").Inc(recordCount);
        ProcessingDurationSeconds.WithLabels(dataSourceId, service, "file_processing").Observe(durationSeconds);

        // .NET metrics
        _filesProcessedCounter.Add(1, 
            new KeyValuePair<string, object?>("data_source_id", dataSourceId),
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("status", "success"));

        _recordsProcessedCounter.Add(recordCount,
            new KeyValuePair<string, object?>("data_source_id", dataSourceId),
            new KeyValuePair<string, object?>("service", service));

        _processingDurationHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("data_source_id", dataSourceId),
            new KeyValuePair<string, object?>("service", service));
    }

    /// <summary>
    /// Records file processing failure
    /// </summary>
    public void RecordFileProcessingFailed(string dataSourceId, string service, string errorType)
    {
        FilesProcessedTotal.WithLabels(dataSourceId, service, "failed").Inc();
        ValidationErrorsTotal.WithLabels(dataSourceId, errorType, "error").Inc();

        _errorsCounter.Add(1,
            new KeyValuePair<string, object?>("data_source_id", dataSourceId),
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    /// <summary>
    /// Records validation results
    /// </summary>
    public void RecordValidationResults(string dataSourceId, int totalRecords, int validRecords, int invalidRecords)
    {
        RecordsProcessedTotal.WithLabels(dataSourceId, "validation", "valid").Inc(validRecords);
        RecordsProcessedTotal.WithLabels(dataSourceId, "validation", "invalid").Inc(invalidRecords);
        
        // Update error rate
        var errorRate = totalRecords > 0 ? (double)invalidRecords / totalRecords : 0;
        ValidationErrorRate.WithLabels(dataSourceId).Set(errorRate);

        if (invalidRecords > 0)
        {
            ValidationErrorsTotal.WithLabels(dataSourceId, "schema_validation", "error").Inc(invalidRecords);
        }
    }

    /// <summary>
    /// Records message bus activity
    /// </summary>
    public void RecordMessageSent(string messageType, string service, bool success = true)
    {
        var status = success ? "success" : "failed";
        MessagesSentTotal.WithLabels(messageType, service, status).Inc();
    }

    /// <summary>
    /// Records message bus activity
    /// </summary>
    public void RecordMessageReceived(string messageType, string service, bool success = true)
    {
        var status = success ? "success" : "failed";
        MessagesReceivedTotal.WithLabels(messageType, service, status).Inc();
    }

    /// <summary>
    /// Updates active data source count
    /// </summary>
    public void UpdateActiveDataSources(int count)
    {
        ActiveDataSources.Set(count);
    }

    /// <summary>
    /// Updates pending files count
    /// </summary>
    public void UpdatePendingFiles(string dataSourceId, int count)
    {
        PendingFiles.WithLabels(dataSourceId).Set(count);
    }

    /// <summary>
    /// Records file size for analysis
    /// </summary>
    public void RecordFileSize(string dataSourceId, string fileExtension, long sizeBytes)
    {
        FileSizeBytes.WithLabels(dataSourceId, fileExtension).Observe(sizeBytes);
    }

    /// <summary>
    /// Creates a timer for measuring operation duration
    /// </summary>
    public IDisposable CreateTimer(string dataSourceId, string service, string operation)
    {
        return ProcessingDurationSeconds.WithLabels(dataSourceId, service, operation).NewTimer();
    }

    /// <summary>
    /// Dispose the meter
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}

/// <summary>
/// [DEPRECATED] Extension methods for easy metrics recording
/// Use BusinessMetrics methods instead.
/// </summary>
[Obsolete("Use BusinessMetrics class instead. This legacy class will be removed in a future release.")]
public static class MetricsExtensions
{
    /// <summary>
    /// Records processing metrics with correlation ID context
    /// </summary>
    public static void RecordProcessingWithCorrelation(this DataProcessingMetrics metrics,
        string correlationId, string dataSourceId, string service, int recordCount, double durationSeconds)
    {
        using var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("correlation-id", correlationId);
        
        metrics.RecordFileProcessed(dataSourceId, service, recordCount, durationSeconds);
    }

    /// <summary>
    /// Records error with correlation ID context
    /// </summary>
    public static void RecordErrorWithCorrelation(this DataProcessingMetrics metrics,
        string correlationId, string dataSourceId, string service, string errorType)
    {
        using var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("error", "true");
        
        metrics.RecordFileProcessingFailed(dataSourceId, service, errorType);
    }
}
