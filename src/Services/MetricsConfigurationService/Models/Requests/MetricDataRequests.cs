namespace MetricsConfigurationService.Models.Requests;

/// <summary>
/// Request for querying metric data over a time range
/// </summary>
public class TimeRangeRequest
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string Step { get; set; } = "1m"; // 1m, 5m, 15m, 1h, etc.
}

/// <summary>
/// Request for executing a PromQL query
/// </summary>
public class PromQLQueryRequest
{
    public required string Query { get; set; }
    public string QueryType { get; set; } = "instant"; // instant or range
    public string Instance { get; set; } = "business"; // business or system
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string Step { get; set; } = "1m";
}

/// <summary>
/// Request for duplicate metric (already exists from MetricController)
/// </summary>
public class DuplicateMetricRequest
{
    public required string NewName { get; set; }
    public string? NewDisplayName { get; set; }
}

/// <summary>
/// Request for creating a new metric configuration
/// </summary>
public class CreateMetricRequest
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public required string Scope { get; set; }
    public string? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    public string? Formula { get; set; }
    public string? FormulaType { get; set; }
    public string? FieldPath { get; set; }
    public string PrometheusType { get; set; } = "gauge";
    public List<string>? LabelNames { get; set; }
    public string? LabelsExpression { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public List<AlertRule>? AlertRules { get; set; }
    public int Retention { get; set; } = 30;
    public string Status { get; set; } = "active";
    public required string CreatedBy { get; set; }
}

/// <summary>
/// Request for updating a metric configuration
/// </summary>
public class UpdateMetricRequest
{
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public required string Scope { get; set; }
    public string? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    public string? Formula { get; set; }
    public string? FormulaType { get; set; }
    public string? FieldPath { get; set; }
    public string PrometheusType { get; set; } = "gauge";
    public List<string>? LabelNames { get; set; }
    public string? LabelsExpression { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public List<AlertRule>? AlertRules { get; set; }
    public int Retention { get; set; } = 30;
    public string Status { get; set; } = "active";
    public required string UpdatedBy { get; set; }
}
