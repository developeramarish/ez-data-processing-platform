using MetricsConfigurationService.Models;
using MetricsConfigurationService.Models.Prometheus;
using MetricsConfigurationService.Services.Prometheus;

namespace MetricsConfigurationService.Services.Alerts;

/// <summary>
/// Service for evaluating alert rules against collected metric values
/// </summary>
public class AlertEvaluationService : IAlertEvaluationService
{
    private readonly ILogger<AlertEvaluationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, DateTime> _alertCooldowns = new();
    private readonly Dictionary<string, DateTime> _alertLastFired = new();

    public AlertEvaluationService(
        ILogger<AlertEvaluationService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Evaluate alert rules for a specific metric against its current value
    /// </summary>
    public async Task EvaluateAlertsAsync(MetricConfiguration metric, double currentValue)
    {
        if (metric.AlertRules == null || !metric.AlertRules.Any())
            return;

        var cooldownSeconds = _configuration.GetValue<int>("AlertEvaluation:CooldownSeconds", 300);
        var enabled = _configuration.GetValue<bool>("AlertEvaluation:Enabled", true);

        if (!enabled)
        {
            _logger.LogDebug("Alert evaluation is disabled in configuration");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var prometheusService = scope.ServiceProvider.GetRequiredService<IPrometheusQueryService>();

        foreach (var alert in metric.AlertRules.Where(a => a.IsEnabled))
        {
            try
            {
                // Check cooldown period to prevent alert storms
                var cooldownKey = $"{metric.ID}:{alert.Name}";
                if (_alertCooldowns.TryGetValue(cooldownKey, out var lastTriggered))
                {
                    var timeSinceLastTrigger = DateTime.UtcNow - lastTriggered;
                    if (timeSinceLastTrigger.TotalSeconds < cooldownSeconds)
                    {
                        _logger.LogDebug(
                            "Alert {AlertName} for metric {MetricName} still in cooldown ({Remaining}s remaining)",
                            alert.Name, metric.Name, cooldownSeconds - timeSinceLastTrigger.TotalSeconds);
                        continue;
                    }
                }

                // Evaluate the PromQL expression
                var triggered = await EvaluateAlertExpressionAsync(
                    alert, 
                    metric, 
                    currentValue, 
                    prometheusService);

                if (triggered)
                {
                    // Alert condition met - fire the alert
                    _logger.LogWarning(
                        "🔥 ALERT TRIGGERED: '{AlertName}' for metric '{MetricName}' | " +
                        "Current value: {Value:F2} | Expression: {Expression} | Severity: {Severity}",
                        alert.Name, metric.Name, currentValue, alert.Expression, alert.Severity);

                    // Update cooldown
                    _alertCooldowns[cooldownKey] = DateTime.UtcNow;
                    _alertLastFired[cooldownKey] = DateTime.UtcNow;

                    // TODO: In future, integrate with NotificationService to send alerts
                    // For now, logging is sufficient to demonstrate the functionality
                }
                else
                {
                    _logger.LogDebug(
                        "Alert {AlertName} for metric {MetricName} condition not met. Current value: {Value:F2}",
                        alert.Name, metric.Name, currentValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alert {AlertName} for metric {MetricName}", alert.Name, metric.Name);
            }
        }
    }

    /// <summary>
    /// Evaluate a PromQL alert expression
    /// </summary>
    private async Task<bool> EvaluateAlertExpressionAsync(
        AlertRule alert,
        MetricConfiguration metric,
        double currentValue,
        IPrometheusQueryService prometheusService)
    {
        try
        {
            // Determine which Prometheus instance to query
            var instance = metric.Scope == "global"
                ? PrometheusInstance.System
                : PrometheusInstance.Business;

            // Execute the PromQL expression
            var result = await prometheusService.QueryInstantAsync(alert.Expression, instance);

            if (result?.Data == null || !result.Data.Any())
            {
                _logger.LogDebug(
                    "Alert expression '{Expression}' returned no data",
                    alert.Expression);
                return false;
            }

            // Get the first result value
            var expressionValue = result.Data.First().Value;

            // For PromQL expressions, any non-zero result typically means the alert condition is met
            // Common patterns:
            // - (metric > threshold) returns 1 if true, 0 if false
            // - absent(metric) returns 1 if metric is missing
            // - rate(metric[5m]) > threshold returns the value if true
            
            var triggered = expressionValue > 0;

            _logger.LogDebug(
                "Alert expression '{Expression}' evaluated to {Value}. Triggered: {Triggered}",
                alert.Expression, expressionValue, triggered);

            return triggered;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                "Failed to evaluate alert expression '{Expression}': {Message}",
                alert.Expression, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error evaluating alert expression '{Expression}'",
                alert.Expression);
            return false;
        }
    }

    /// <summary>
    /// Evaluate all alert rules across all metrics
    /// </summary>
    public async Task<List<AlertEvaluationResult>> EvaluateAllAsync()
    {
        var results = new List<AlertEvaluationResult>();

        _logger.LogInformation("Evaluating all alerts across all metrics");

        // This would require iterating through all metrics and their alert rules
        // For now, return empty list as this is called by background service per-metric
        // Future enhancement: could be used for on-demand full system alert check

        return await Task.FromResult(results);
    }
}
