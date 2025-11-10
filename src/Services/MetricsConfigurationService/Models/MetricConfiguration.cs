using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace MetricsConfigurationService.Models;

/// <summary>
/// Formula type for metric calculation
/// </summary>
public enum FormulaType
{
    /// <summary>
    /// Simple field value (e.g., "$.amount")
    /// </summary>
    Simple = 0,
    
    /// <summary>
    /// PromQL expression (e.g., "rate(requests[5m])")
    /// </summary>
    PromQL = 1,
    
    /// <summary>
    /// Reference to recording rule (e.g., "job:requests:rate5m")
    /// </summary>
    Recording = 2
}

public class MetricConfiguration : Entity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    // Scope: "global" applies to ALL data sources, "datasource-specific" is one-to-one
    public string Scope { get; set; } = "global";
    
    // For custom metrics: specific data source ID (one-to-one mapping)
    // For global metrics: null (applies to all)
    public string? DataSourceId { get; set; }
    
    // For display purposes: datasource name (populated for datasource-specific metrics)
    public string? DataSourceName { get; set; }
    
    public string Formula { get; set; } = string.Empty;
    
    // Formula type: Simple, PromQL, or Recording
    public FormulaType FormulaType { get; set; } = FormulaType.Simple;
    
    // REQUIRED: Every metric must relate to exactly one schema field
    [Required]
    public string FieldPath { get; set; } = string.Empty;  // JSON path like $.amount
    
    // Prometheus metric type: counter, gauge, histogram, summary
    [Required]
    public string PrometheusType { get; set; } = "gauge";
    
    // Simple label names entered by user (comma-separated)
    public string? LabelNames { get; set; }
    
    // Generated PromQL labels expression (e.g., "{status=\"$status\", region=\"$region\"}")
    public string? LabelsExpression { get; set; }
    
    // DEPRECATED: Old labels field kept for backward compatibility
    public List<string>? Labels { get; set; }
    
    public string? Retention { get; set; }  // e.g., "7d", "30d", "90d"
    
    // Alert rules associated with this metric
    public List<AlertRule>? AlertRules { get; set; }
    
    // Status: 0=Draft, 1=Active, 2=Inactive, 3=Error
    public int Status { get; set; } = 0;
    
    public double? LastValue { get; set; }
    public DateTime? LastCalculated { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
    public string UpdatedBy { get; set; } = "System";
}

public class CreateMetricRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Scope { get; set; } = "global";
    public string? DataSourceId { get; set; }
    public string Formula { get; set; } = string.Empty;
    public FormulaType FormulaType { get; set; } = FormulaType.Simple;
    
    [Required]
    public string FieldPath { get; set; } = string.Empty;
    
    [Required]
    public string PrometheusType { get; set; } = "gauge";
    
    public string? LabelNames { get; set; }
    public string? LabelsExpression { get; set; }
    public List<string>? Labels { get; set; }
    public string? Retention { get; set; }
    public List<AlertRule>? AlertRules { get; set; }
    public int Status { get; set; } = 0;
    public string CreatedBy { get; set; } = "User";
}

public class UpdateMetricRequest
{
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Scope { get; set; } = "global";
    public string? DataSourceId { get; set; }
    public string Formula { get; set; } = string.Empty;
    public FormulaType FormulaType { get; set; } = FormulaType.Simple;
    
    [Required]
    public string FieldPath { get; set; } = string.Empty;
    
    [Required]
    public string PrometheusType { get; set; } = "gauge";
    
    public string? LabelNames { get; set; }
    public string? LabelsExpression { get; set; }
    public List<string>? Labels { get; set; }
    public string? Retention { get; set; }
    public List<AlertRule>? AlertRules { get; set; }
    public int Status { get; set; }
    public string UpdatedBy { get; set; } = "User";
}

public class DuplicateMetricRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "User";
}
