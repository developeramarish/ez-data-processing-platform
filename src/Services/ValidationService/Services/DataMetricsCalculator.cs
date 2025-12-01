using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace DataProcessing.Validation.Services;

/// <summary>
/// Calculates business metrics from validated JSON records
/// Uses JSON Path expressions from MetricConfiguration to extract values
/// </summary>
public class DataMetricsCalculator
{
    private readonly ILogger<DataMetricsCalculator> _logger;

    public DataMetricsCalculator(ILogger<DataMetricsCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates business metrics for a collection of validated records
    /// </summary>
    /// <param name="validRecords">List of valid JSON records as JObjects</param>
    /// <param name="metricDefinitions">Metric definitions from MetricConfiguration</param>
    /// <param name="correlationId">Correlation ID for logging</param>
    /// <returns>Dictionary of metric names to calculated values</returns>
    public Dictionary<string, double> CalculateMetrics(
        List<JObject> validRecords,
        List<MetricDefinition> metricDefinitions,
        string correlationId)
    {
        var results = new Dictionary<string, double>();

        if (!validRecords.Any())
        {
            _logger.LogDebug(
                "[{CorrelationId}] No valid records to calculate metrics",
                correlationId);
            return results;
        }

        foreach (var metricDef in metricDefinitions)
        {
            try
            {
                var value = CalculateMetric(validRecords, metricDef, correlationId);
                results[metricDef.MetricName] = value;

                _logger.LogDebug(
                    "[{CorrelationId}] Calculated metric '{MetricName}': {Value} (aggregation: {Aggregation})",
                    correlationId, metricDef.MetricName, value, metricDef.AggregationType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[{CorrelationId}] Failed to calculate metric '{MetricName}' with path '{JsonPath}'",
                    correlationId, metricDef.MetricName, metricDef.JsonPath);

                // Store 0 for failed metrics
                results[metricDef.MetricName] = 0;
            }
        }

        return results;
    }

    /// <summary>
    /// Calculates a single metric based on its definition
    /// </summary>
    private double CalculateMetric(
        List<JObject> records,
        MetricDefinition metricDef,
        string correlationId)
    {
        var values = ExtractValues(records, metricDef.JsonPath, correlationId);

        return metricDef.AggregationType.ToLowerInvariant() switch
        {
            "sum" => values.Sum(),
            "count" => values.Count,
            "avg" or "average" => values.Any() ? values.Average() : 0,
            "min" => values.Any() ? values.Min() : 0,
            "max" => values.Any() ? values.Max() : 0,
            _ => throw new ArgumentException($"Unsupported aggregation type: {metricDef.AggregationType}")
        };
    }

    /// <summary>
    /// Extracts numeric values from JSON records using JSON Path expression
    /// </summary>
    private List<double> ExtractValues(
        List<JObject> records,
        string jsonPath,
        string correlationId)
    {
        var values = new List<double>();

        foreach (var record in records)
        {
            try
            {
                // Use SelectTokens to handle JSON Path expressions
                var tokens = record.SelectTokens(jsonPath);

                foreach (var token in tokens)
                {
                    if (TryConvertToDouble(token, out var numericValue))
                    {
                        values.Add(numericValue);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex,
                    "[{CorrelationId}] Failed to extract value from record using path '{JsonPath}'",
                    correlationId, jsonPath);
            }
        }

        return values;
    }

    /// <summary>
    /// Tries to convert a JSON token to a double value
    /// </summary>
    private bool TryConvertToDouble(JToken token, out double value)
    {
        value = 0;

        if (token == null || token.Type == JTokenType.Null)
            return false;

        switch (token.Type)
        {
            case JTokenType.Integer:
            case JTokenType.Float:
                value = token.Value<double>();
                return true;

            case JTokenType.String:
                // Try to parse string as number
                var strValue = token.Value<string>();
                if (!string.IsNullOrEmpty(strValue))
                {
                    // Remove currency symbols and whitespace
                    strValue = Regex.Replace(strValue, @"[^\d.,\-]", "");

                    if (double.TryParse(strValue, out value))
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Calculates category-based metrics (e.g., count of items per category)
    /// </summary>
    /// <param name="validRecords">List of valid JSON records</param>
    /// <param name="categoryPath">JSON path to category field (e.g., "$.category")</param>
    /// <param name="correlationId">Correlation ID for logging</param>
    /// <returns>Dictionary of category names to counts</returns>
    public Dictionary<string, int> CalculateCategoryMetrics(
        List<JObject> validRecords,
        string categoryPath,
        string correlationId)
    {
        var categoryCounts = new Dictionary<string, int>();

        foreach (var record in validRecords)
        {
            try
            {
                var tokens = record.SelectTokens(categoryPath);

                foreach (var token in tokens)
                {
                    if (token != null && token.Type == JTokenType.String)
                    {
                        var category = token.Value<string>() ?? "unknown";

                        if (!categoryCounts.ContainsKey(category))
                            categoryCounts[category] = 0;

                        categoryCounts[category]++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex,
                    "[{CorrelationId}] Failed to extract category from record using path '{CategoryPath}'",
                    correlationId, categoryPath);
            }
        }

        return categoryCounts;
    }
}

/// <summary>
/// Metric definition from MetricConfiguration service
/// </summary>
public class MetricDefinition
{
    /// <summary>
    /// Metric name (e.g., "total_price", "item_count")
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// JSON Path expression to extract values (e.g., "$.items[*].price")
    /// </summary>
    public string JsonPath { get; set; } = string.Empty;

    /// <summary>
    /// Aggregation type: sum, count, avg, min, max
    /// </summary>
    public string AggregationType { get; set; } = "sum";

    /// <summary>
    /// Unit of measurement (e.g., "currency", "items", "kg")
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
