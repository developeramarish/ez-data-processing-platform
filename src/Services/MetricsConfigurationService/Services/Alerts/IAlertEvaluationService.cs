using MetricsConfigurationService.Models;

namespace MetricsConfigurationService.Services.Alerts;

/// <summary>
/// Service for evaluating alert rules against collected metric values
/// </summary>
public interface IAlertEvaluationService
{
    /// <summary>
    /// Evaluate alert rules for a specific metric against its current value
    /// </summary>
    /// <param name="metric">The metric configuration with alert rules</param>
    /// <param name="currentValue">The current value of the metric</param>
    Task EvaluateAlertsAsync(MetricConfiguration metric, double currentValue);

    /// <summary>
    /// Evaluate all alert rules across all metrics
    /// </summary>
    /// <returns>List of evaluation results</returns>
    Task<List<AlertEvaluationResult>> EvaluateAllAsync();

    /// <summary>
    /// Substitute $variable patterns in a PromQL expression for global alerts.
    /// </summary>
    /// <param name="expression">The PromQL expression with potential $variables</param>
    /// <param name="globalAlert">The global alert configuration containing labels and context</param>
    /// <returns>The expression with all recognized variables substituted</returns>
    string SubstituteGlobalAlertVariables(string expression, GlobalAlertConfiguration globalAlert);
}

/// <summary>
/// Result of an alert evaluation
/// </summary>
public class AlertEvaluationResult
{
    public string MetricId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public bool Triggered { get; set; }
    public double CurrentValue { get; set; }
    public double Threshold { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public string? Message { get; set; }
}
