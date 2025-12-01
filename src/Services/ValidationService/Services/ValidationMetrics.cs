using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataProcessing.Validation.Services;

/// <summary>
/// OpenTelemetry metrics for ValidationService using System.Diagnostics.Metrics
/// Exports metrics via OTLP gRPC to OpenTelemetry Collector
/// </summary>
public class ValidationMetrics
{
    private readonly Counter<long> _recordsValidatedCounter;
    private readonly Counter<long> _validationErrorsCounter;
    private readonly Histogram<double> _validationDurationHistogram;
    private readonly Histogram<double> _businessMetricHistogram;
    private readonly Counter<long> _categoryItemsCounter;
    private readonly ObservableGauge<int> _activeValidationsGauge;

    private int _activeValidations = 0;

    public ValidationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("DataProcessing.Validation", "1.0.0");

        // Counter: Total validated records
        _recordsValidatedCounter = meter.CreateCounter<long>(
            name: "validation.records.validated.total",
            unit: "records",
            description: "Total number of records validated"
        );

        // Counter: Validation errors
        _validationErrorsCounter = meter.CreateCounter<long>(
            name: "validation.errors.total",
            unit: "errors",
            description: "Total number of validation errors encountered"
        );

        // Histogram: Validation duration
        _validationDurationHistogram = meter.CreateHistogram<double>(
            name: "validation.duration",
            unit: "ms",
            description: "Duration of validation operations in milliseconds"
        );

        // Histogram: Business metrics (sum, avg, etc.)
        _businessMetricHistogram = meter.CreateHistogram<double>(
            name: "business.metric.value",
            unit: "{value}",
            description: "Distribution of calculated business metric values"
        );

        // Counter: Category-based items
        _categoryItemsCounter = meter.CreateCounter<long>(
            name: "business.category.items.total",
            unit: "items",
            description: "Count of items per category"
        );

        // Gauge: Active validations
        _activeValidationsGauge = meter.CreateObservableGauge<int>(
            name: "validation.active.count",
            observeValue: () => _activeValidations,
            unit: "validations",
            description: "Number of currently active validation operations"
        );
    }

    /// <summary>
    /// Records validation result metrics
    /// </summary>
    public void RecordValidation(
        bool isValid,
        string dataSourceId,
        string fileName,
        TimeSpan duration)
    {
        var tags = new TagList
        {
            { "data_source_id", dataSourceId },
            { "result", isValid ? "success" : "failure" },
            { "file_name", SanitizeFileName(fileName) }
        };

        _recordsValidatedCounter.Add(1, tags);
        _validationDurationHistogram.Record(duration.TotalMilliseconds, tags);

        if (!isValid)
        {
            _validationErrorsCounter.Add(1, new TagList
            {
                { "data_source_id", dataSourceId },
                { "file_name", SanitizeFileName(fileName) }
            });
        }
    }

    /// <summary>
    /// Records business metric calculations
    /// </summary>
    public void RecordBusinessMetric(
        string metricName,
        double value,
        string unit,
        string dataSourceId)
    {
        var tags = new TagList
        {
            { "metric_name", metricName },
            { "unit", unit },
            { "data_source_id", dataSourceId }
        };

        _businessMetricHistogram.Record(value, tags);
    }

    /// <summary>
    /// Records category-based metrics
    /// </summary>
    public void RecordCategoryItems(
        Dictionary<string, int> categoryItems,
        string dataSourceId)
    {
        foreach (var (category, count) in categoryItems)
        {
            var tags = new TagList
            {
                { "category", category },
                { "data_source_id", dataSourceId }
            };

            _categoryItemsCounter.Add(count, tags);
        }
    }

    /// <summary>
    /// Increments active validations counter
    /// </summary>
    public void IncrementActiveValidations()
    {
        Interlocked.Increment(ref _activeValidations);
    }

    /// <summary>
    /// Decrements active validations counter
    /// </summary>
    public void DecrementActiveValidations()
    {
        Interlocked.Decrement(ref _activeValidations);
    }

    /// <summary>
    /// Sanitizes file name for use in metric tags (removes high-cardinality parts)
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        // Extract file extension only to keep cardinality low
        var extension = Path.GetExtension(fileName)?.TrimStart('.') ?? "unknown";
        return extension;
    }
}
