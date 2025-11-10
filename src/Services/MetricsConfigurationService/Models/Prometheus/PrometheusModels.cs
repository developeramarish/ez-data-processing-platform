using System.Text.Json.Serialization;

namespace MetricsConfigurationService.Models.Prometheus;

/// <summary>
/// Prometheus HTTP API response wrapper
/// </summary>
public class PrometheusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PrometheusData? Data { get; set; }

    [JsonPropertyName("errorType")]
    public string? ErrorType { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Prometheus response data section
/// </summary>
public class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public List<PrometheusResult> Result { get; set; } = new();
}

/// <summary>
/// Individual Prometheus result
/// </summary>
public class PrometheusResult
{
    [JsonPropertyName("metric")]
    public Dictionary<string, string> Metric { get; set; } = new();

    [JsonPropertyName("value")]
    public List<object>? Value { get; set; } // [timestamp, value]

    [JsonPropertyName("values")]
    public List<List<object>>? Values { get; set; } // [[timestamp, value], ...]
}

/// <summary>
/// Parsed Prometheus query result
/// </summary>
public class PrometheusQueryResult
{
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<MetricDataPoint> Data { get; set; } = new();
    public string ResultType { get; set; } = string.Empty;
}

/// <summary>
/// Single metric data point
/// </summary>
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
}
