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
    /// Substitute $variable patterns in a PromQL expression with actual values from the metric configuration
    /// and optionally from alert labels.
    ///
    /// Predefined variables: $datasource_name, $datasource_id, $metric_name, $category, $scope
    /// Dynamic labels: Any $labelName where labelName is defined in the alert's Labels dictionary
    /// </summary>
    private string SubstituteVariables(string expression, MetricConfiguration metric, AlertRule? alert = null)
    {
        if (string.IsNullOrEmpty(expression) || !expression.Contains('$'))
            return expression;

        // Start with predefined metric variables
        var variables = new Dictionary<string, string?>
        {
            ["$datasource_name"] = metric.DataSourceName,
            ["$datasource_id"] = metric.DataSourceId,
            ["$metric_name"] = metric.Name,
            ["$category"] = metric.Category,
            ["$scope"] = metric.Scope
        };

        // Add alert label variables (where value is not itself a $variable)
        if (alert?.Labels != null)
        {
            foreach (var (labelName, labelValue) in alert.Labels)
            {
                // Skip if the value is itself a variable (e.g., "$status")
                // Only add labels with fixed values for substitution
                if (!string.IsNullOrEmpty(labelValue) && !labelValue.StartsWith("$"))
                {
                    var variableKey = $"${labelName}";
                    if (!variables.ContainsKey(variableKey))
                    {
                        variables[variableKey] = labelValue;
                    }
                }
            }
        }

        var result = expression;
        var substitutedCount = 0;
        var substitutedVars = new List<string>();

        foreach (var (variable, value) in variables)
        {
            if (!string.IsNullOrEmpty(value) && result.Contains(variable))
            {
                result = result.Replace(variable, value);
                substitutedCount++;
                substitutedVars.Add($"{variable}={value}");
            }
        }

        // Log any unsubstituted variables (potential configuration issues)
        if (result.Contains('$'))
        {
            // Extract remaining variable patterns for logging
            var remainingVars = System.Text.RegularExpressions.Regex.Matches(result, @"\$[a-zA-Z_][a-zA-Z0-9_]*")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            if (remainingVars.Any())
            {
                _logger.LogWarning(
                    "Unsubstituted variables in PromQL expression for metric '{MetricName}': {Variables}. " +
                    "Supported: $datasource_name, $datasource_id, $metric_name, $category, $scope, " +
                    "and any label name defined in the alert's Labels dictionary with a fixed value",
                    metric.Name, string.Join(", ", remainingVars));
            }
        }

        if (substitutedCount > 0)
        {
            _logger.LogDebug(
                "Substituted {Count} variable(s) in PromQL expression for metric '{MetricName}': [{Substitutions}]. " +
                "Original: '{Original}' → Resolved: '{Resolved}'",
                substitutedCount, metric.Name, string.Join(", ", substitutedVars), expression, result);
        }

        return result;
    }

    /// <summary>
    /// Substitute $variable patterns in a PromQL expression for global alerts.
    /// Supports alert labels and predefined metric context variables.
    /// </summary>
    public string SubstituteGlobalAlertVariables(string expression, GlobalAlertConfiguration globalAlert)
    {
        if (string.IsNullOrEmpty(expression) || !expression.Contains('$'))
            return expression;

        // Start with global alert context variables
        var variables = new Dictionary<string, string?>
        {
            ["$metric_name"] = globalAlert.MetricName,
            ["$metric_type"] = globalAlert.MetricType,
            ["$alert_name"] = globalAlert.AlertName,
            ["$severity"] = globalAlert.Severity
        };

        // Add alert label variables (where value is not itself a $variable)
        if (globalAlert.Labels != null)
        {
            foreach (var (labelName, labelValue) in globalAlert.Labels)
            {
                if (!string.IsNullOrEmpty(labelValue) && !labelValue.StartsWith("$"))
                {
                    var variableKey = $"${labelName}";
                    if (!variables.ContainsKey(variableKey))
                    {
                        variables[variableKey] = labelValue;
                    }
                }
            }
        }

        var result = expression;
        var substitutedCount = 0;
        var substitutedVars = new List<string>();

        foreach (var (variable, value) in variables)
        {
            if (!string.IsNullOrEmpty(value) && result.Contains(variable))
            {
                result = result.Replace(variable, value);
                substitutedCount++;
                substitutedVars.Add($"{variable}={value}");
            }
        }

        // Log any unsubstituted variables
        if (result.Contains('$'))
        {
            var remainingVars = System.Text.RegularExpressions.Regex.Matches(result, @"\$[a-zA-Z_][a-zA-Z0-9_]*")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            if (remainingVars.Any())
            {
                _logger.LogWarning(
                    "Unsubstituted variables in global alert '{AlertName}': {Variables}. " +
                    "Supported: $metric_name, $metric_type, $alert_name, $severity, " +
                    "and any label name defined in the alert's Labels dictionary with a fixed value",
                    globalAlert.AlertName, string.Join(", ", remainingVars));
            }
        }

        if (substitutedCount > 0)
        {
            _logger.LogDebug(
                "Substituted {Count} variable(s) in global alert '{AlertName}': [{Substitutions}]. " +
                "Original: '{Original}' → Resolved: '{Resolved}'",
                substitutedCount, globalAlert.AlertName, string.Join(", ", substitutedVars), expression, result);
        }

        return result;
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

            // Substitute $variable patterns with actual values from metric configuration and alert labels
            var resolvedExpression = SubstituteVariables(alert.Expression, metric, alert);

            // Execute the resolved PromQL expression
            var result = await prometheusService.QueryInstantAsync(resolvedExpression, instance);

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
