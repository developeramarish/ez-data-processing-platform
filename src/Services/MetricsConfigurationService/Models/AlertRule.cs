using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MetricsConfigurationService.Models;

/// <summary>
/// Represents a Prometheus alert rule associated with a metric
/// </summary>
public class AlertRule
{
    /// <summary>
    /// Unique identifier for the alert rule (used for UI tracking, not as MongoDB document ID)
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Alert name (must be valid Prometheus alert name)
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]*$", ErrorMessage = "Alert name must match Prometheus naming convention")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hebrew description of the alert
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// PromQL expression for the alert condition
    /// </summary>
    [Required]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Duration to wait before firing (e.g., "5m", "10m")
    /// </summary>
    public string? For { get; set; }

    /// <summary>
    /// Duration to keep alert firing after condition is no longer met
    /// </summary>
    public string? KeepFiringFor { get; set; }

    /// <summary>
    /// Alert severity (e.g., "critical", "warning", "info")
    /// </summary>
    [Required]
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Additional labels for the alert
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// Alert annotations (summary, description, runbook_url, etc.)
    /// </summary>
    public Dictionary<string, string> Annotations { get; set; } = new();

    /// <summary>
    /// Template used to generate this alert (for UI helper)
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Template parameters (for UI helper)
    /// </summary>
    public Dictionary<string, string>? TemplateParameters { get; set; }

    /// <summary>
    /// Whether this alert is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Timestamp when the alert rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the alert rule was last modified
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Alert expression template for UI helper
/// </summary>
public class AlertExpressionTemplate
{
    public string Id { get; set; } = string.Empty;
    public string NameHebrew { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public string DescriptionHebrew { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public List<TemplateParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Parameter for alert expression template
/// </summary>
public class TemplateParameter
{
    public string Name { get; set; } = string.Empty;
    public string LabelHebrew { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, number, duration, metric
    public bool Required { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? PlaceholderHebrew { get; set; }
}
