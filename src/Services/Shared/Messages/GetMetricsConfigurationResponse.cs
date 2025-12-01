namespace DataProcessing.Shared.Messages;

/// <summary>
/// Response containing metric configurations from MetricsConfigurationService
/// Includes both global and datasource-specific metrics with alert rules
/// </summary>
public class GetMetricsConfigurationResponse : IDataProcessingMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PublishedBy { get; set; } = string.Empty;
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// List of metric configurations
    /// </summary>
    public List<MetricConfigurationDto> Metrics { get; set; } = new();

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Data Transfer Object for MetricConfiguration
/// Avoids circular dependencies between services
/// </summary>
public class MetricConfigurationDto
{
    /// <summary>
    /// Metric name (e.g., "total_price", "item_count")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Metric description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Metric category for grouping
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Scope: "global" or "datasource-specific"
    /// </summary>
    public string Scope { get; set; } = "global";

    /// <summary>
    /// Data source ID (null for global metrics)
    /// </summary>
    public string? DataSourceId { get; set; }

    /// <summary>
    /// JSON Path expression to extract values (e.g., "$.items[*].price")
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Prometheus metric type: counter, gauge, histogram, summary
    /// </summary>
    public string PrometheusType { get; set; } = "gauge";

    /// <summary>
    /// Alert rules associated with this metric
    /// </summary>
    public List<AlertRuleDto>? AlertRules { get; set; }

    /// <summary>
    /// Status: 0=Draft, 1=Active, 2=Inactive, 3=Error
    /// </summary>
    public int Status { get; set; }
}

/// <summary>
/// Data Transfer Object for AlertRule
/// </summary>
public class AlertRuleDto
{
    /// <summary>
    /// Alert rule ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Alert name (Prometheus-compatible)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alert description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// PromQL expression for alert condition
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Duration to wait before firing (e.g., "5m", "10m")
    /// </summary>
    public string? For { get; set; }

    /// <summary>
    /// Alert severity (critical, warning, info)
    /// </summary>
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Whether this alert is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Additional labels for the alert
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// Alert annotations
    /// </summary>
    public Dictionary<string, string> Annotations { get; set; } = new();
}
