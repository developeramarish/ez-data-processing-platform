using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace MetricsConfigurationService.Models;

/// <summary>
/// Represents a global alert configuration for business or system metrics.
/// Unlike datasource-specific alerts (stored in MetricConfiguration.AlertRules),
/// these alerts are stored as top-level entities because business/system metrics
/// are hardcoded constants, not database entities.
/// </summary>
public class GlobalAlertConfiguration : Entity
{
    /// <summary>
    /// Type of metric this alert is for: "business" or "system"
    /// </summary>
    [Required]
    public string MetricType { get; set; } = "business";

    /// <summary>
    /// The metric name (e.g., "business_records_processed_total", "process_cpu_seconds_total")
    /// </summary>
    [Required]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Alert name (must be valid Prometheus alert name)
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]*$", ErrorMessage = "Alert name must match Prometheus naming convention")]
    public string AlertName { get; set; } = string.Empty;

    /// <summary>
    /// Hebrew description of the alert
    /// </summary>
    public string? Description { get; set; }

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
    /// Alert severity (e.g., "critical", "warning", "info")
    /// </summary>
    [Required]
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
    /// Alert annotations (summary, description, runbook_url, etc.)
    /// </summary>
    public Dictionary<string, string> Annotations { get; set; } = new();

    /// <summary>
    /// List of notification recipients (email addresses)
    /// </summary>
    public List<string>? NotificationRecipients { get; set; }

    /// <summary>
    /// Timestamp when the alert was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the alert was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this alert
    /// </summary>
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// Who last updated this alert
    /// </summary>
    public string UpdatedBy { get; set; } = "System";
}

/// <summary>
/// Request to create a new global alert
/// </summary>
public class CreateGlobalAlertRequest
{
    [Required]
    public string MetricType { get; set; } = "business";

    [Required]
    public string MetricName { get; set; } = string.Empty;

    [Required]
    public string AlertName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Expression { get; set; } = string.Empty;

    public string? For { get; set; }

    [Required]
    public string Severity { get; set; } = "warning";

    public bool IsEnabled { get; set; } = true;

    public Dictionary<string, string>? Labels { get; set; }

    public Dictionary<string, string>? Annotations { get; set; }

    public List<string>? NotificationRecipients { get; set; }

    public string CreatedBy { get; set; } = "User";
}

/// <summary>
/// Request to update an existing global alert
/// </summary>
public class UpdateGlobalAlertRequest
{
    public string? AlertName { get; set; }

    public string? Description { get; set; }

    public string? Expression { get; set; }

    public string? For { get; set; }

    public string? Severity { get; set; }

    public bool? IsEnabled { get; set; }

    public Dictionary<string, string>? Labels { get; set; }

    public Dictionary<string, string>? Annotations { get; set; }

    public List<string>? NotificationRecipients { get; set; }

    public string UpdatedBy { get; set; } = "User";
}
