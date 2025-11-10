using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessing.Shared.Monitoring;

/// <summary>
/// Helper class for emitting business metrics that will be routed to Business Prometheus
/// All metrics use the "business_" prefix for proper routing by OpenTelemetry Collector
/// </summary>
public class BusinessMetrics : IDisposable
{
    private readonly Meter _meter;
    private bool _disposed;

    // Common business metric instruments
    private readonly Counter<long> _recordsProcessedCounter;
    private readonly Counter<long> _invalidRecordsCounter;
    private readonly Histogram<double> _validationErrorRate;
    private readonly Histogram<double> _processingDuration;
    private readonly Counter<long> _filesProcessedCounter;
    private readonly Histogram<double> _fileSizeBytes;
    private readonly UpDownCounter<long> _activeJobsCounter;
    private readonly Counter<long> _jobCompletedCounter;
    private readonly Counter<long> _jobFailedCounter;

    public BusinessMetrics(Meter meter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));

        // Initialize metric instruments with business_ prefix
        _recordsProcessedCounter = _meter.CreateCounter<long>(
            "business_records_processed_total",
            description: "Total number of records processed by data source");

        _invalidRecordsCounter = _meter.CreateCounter<long>(
            "business_invalid_records_total",
            description: "Total number of invalid records by error type");

        _validationErrorRate = _meter.CreateHistogram<double>(
            "business_validation_error_rate",
            unit: "percentage",
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
            unit: "bytes",
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
    }

    /// <summary>
    /// Records the number of records processed
    /// </summary>
    public void RecordProcessedRecords(long count, string dataSource, string? status = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource }
        };

        if (!string.IsNullOrEmpty(status))
        {
            tags.Add("status", status);
        }

        _recordsProcessedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records invalid records with error classification
    /// </summary>
    public void RecordInvalidRecords(long count, string dataSource, string errorType)
    {
        _invalidRecordsCounter.Add(count, new TagList
        {
            { "data_source", dataSource },
            { "error_type", errorType }
        });
    }

    /// <summary>
    /// Records validation error rate for a batch
    /// </summary>
    public void RecordValidationErrorRate(double errorRatePercentage, string dataSource, string? fileName = null)
    {
        var tags = new TagList
        {
            { "data_source", dataSource }
        };

        if (!string.IsNullOrEmpty(fileName))
        {
            tags.Add("file_name", fileName);
        }

        _validationErrorRate.Record(errorRatePercentage, tags);
    }

    /// <summary>
    /// Records processing duration
    /// </summary>
    public void RecordProcessingDuration(double durationSeconds, string dataSource, string operationType)
    {
        _processingDuration.Record(durationSeconds, new TagList
        {
            { "data_source", dataSource },
            { "operation_type", operationType }
        });
    }

    /// <summary>
    /// Records a processed file
    /// </summary>
    public void RecordFileProcessed(string dataSource, string fileType, bool success)
    {
        _filesProcessedCounter.Add(1, new TagList
        {
            { "data_source", dataSource },
            { "file_type", fileType },
            { "status", success ? "success" : "failed" }
        });
    }

    /// <summary>
    /// Records file size
    /// </summary>
    public void RecordFileSize(long sizeBytes, string dataSource, string fileType)
    {
        _fileSizeBytes.Record(sizeBytes, new TagList
        {
            { "data_source", dataSource },
            { "file_type", fileType }
        });
    }

    /// <summary>
    /// Increments active jobs counter
    /// </summary>
    public void IncrementActiveJobs(string jobType)
    {
        _activeJobsCounter.Add(1, new TagList { { "job_type", jobType } });
    }

    /// <summary>
    /// Decrements active jobs counter
    /// </summary>
    public void DecrementActiveJobs(string jobType)
    {
        _activeJobsCounter.Add(-1, new TagList { { "job_type", jobType } });
    }

    /// <summary>
    /// Records a completed job
    /// </summary>
    public void RecordJobCompleted(string jobType, string dataSource)
    {
        _jobCompletedCounter.Add(1, new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource }
        });
    }

    /// <summary>
    /// Records a failed job
    /// </summary>
    public void RecordJobFailed(string jobType, string dataSource, string? errorReason = null)
    {
        var tags = new TagList
        {
            { "job_type", jobType },
            { "data_source", dataSource }
        };

        if (!string.IsNullOrEmpty(errorReason))
        {
            tags.Add("error_reason", errorReason);
        }

        _jobFailedCounter.Add(1, tags);
    }

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
    public static IServiceCollection AddBusinessMetrics(this IServiceCollection services)
    {
        services.AddSingleton<BusinessMetrics>();
        return services;
    }
}
