using MetricsConfigurationService.Models;
using MetricsConfigurationService.Models.Prometheus;
using MetricsConfigurationService.Repositories;
using MetricsConfigurationService.Services.Alerts;
using MetricsConfigurationService.Services.Prometheus;

namespace MetricsConfigurationService.Services.Collection;

/// <summary>
/// Background service that periodically collects metric data from Prometheus
/// and evaluates alert rules
/// </summary>
public class MetricsCollectionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsCollectionBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public MetricsCollectionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MetricsCollectionBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if collection is enabled
        var enabled = _configuration.GetValue<bool>("MetricsCollection:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("Metrics collection is disabled in configuration");
            return;
        }

        var intervalSeconds = _configuration.GetValue<int>("MetricsCollection:IntervalSeconds", 60);
        _logger.LogInformation(
            "✅ Metrics collection background service started. Interval: {Interval}s",
            intervalSeconds);

        // Wait a bit before starting first collection to allow services to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in metrics collection cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Metrics collection background service stopped");
    }

    /// <summary>
    /// Collect metrics from Prometheus and evaluate alerts
    /// </summary>
    private async Task CollectMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var metricRepository = scope.ServiceProvider.GetRequiredService<IMetricRepository>();
        var prometheusService = scope.ServiceProvider.GetRequiredService<IPrometheusQueryService>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertEvaluationService>();

        try
        {
            // Get all active metrics (Status = 1)
            var allMetrics = await metricRepository.GetAllAsync();
            var activeMetrics = allMetrics.Where(m => m.Status == 1).ToList();

            if (!activeMetrics.Any())
            {
                _logger.LogDebug("No active metrics to collect");
                return;
            }

            _logger.LogInformation(
                "📊 Collecting data for {Count} active metrics",
                activeMetrics.Count);

            var successCount = 0;
            var errorCount = 0;

            foreach (var metric in activeMetrics)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await CollectSingleMetricAsync(metric, prometheusService, alertService);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "⚠️ Error collecting metric '{Name}' (ID: {Id})",
                        metric.Name,
                        metric.ID);
                    errorCount++;
                }
            }

            _logger.LogInformation(
                "✅ Collection cycle complete. Success: {Success}, Errors: {Errors}",
                successCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in collection cycle");
        }
    }

    /// <summary>
    /// Collect a single metric from Prometheus
    /// </summary>
    private async Task CollectSingleMetricAsync(
        MetricConfiguration metric,
        IPrometheusQueryService prometheusService,
        IAlertEvaluationService alertService)
    {
        // Determine which Prometheus instance to query
        var instance = metric.Scope == "global"
            ? PrometheusInstance.System
            : PrometheusInstance.Business;

        // Build PromQL query from metric configuration
        var query = BuildPromQLQuery(metric);

        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning(
                "Cannot build query for metric '{Name}' - empty formula/field path",
                metric.Name);
            return;
        }

        try
        {
            // Query Prometheus for current value
            var result = await prometheusService.QueryInstantAsync(query, instance);

            if (result?.Data != null && result.Data.Any())
            {
                var value = result.Data.First().Value;

                _logger.LogDebug(
                    "📈 Metric '{Name}': {Value:F2} (from {Instance})",
                    metric.Name,
                    value,
                    instance);

                // Update last value and timestamp in database
                await UpdateMetricLastValueAsync(metric, value);

                // Evaluate alert rules if any exist
                if (metric.AlertRules != null && metric.AlertRules.Any())
                {
                    await alertService.EvaluateAlertsAsync(metric, value);
                }
            }
            else
            {
                _logger.LogDebug(
                    "No data returned for metric '{Name}' query: {Query}",
                    metric.Name,
                    query);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                "Failed to query Prometheus for metric '{Name}': {Message}",
                metric.Name,
                ex.Message);
        }
    }

    /// <summary>
    /// Build PromQL query from metric configuration
    /// </summary>
    private string BuildPromQLQuery(MetricConfiguration metric)
    {
        // If formula type is PromQL, use the formula directly
        if (metric.FormulaType == FormulaType.PromQL && !string.IsNullOrEmpty(metric.Formula))
        {
            return metric.Formula;
        }

        // If formula type is Recording, use the recording rule name
        if (metric.FormulaType == FormulaType.Recording && !string.IsNullOrEmpty(metric.Formula))
        {
            return metric.Formula;
        }

        // For Simple type, build from metric name
        // Note: This assumes the metric is already being published to Prometheus
        // by the data processing services
        var query = metric.Name;

        // Add labels if configured
        if (!string.IsNullOrEmpty(metric.LabelNames))
        {
            // Simple approach: just use the metric name
            // Full label filtering would require label values
            query = metric.Name;
        }

        return query;
    }

    /// <summary>
    /// Update metric's last value and timestamp in database
    /// </summary>
    private async Task UpdateMetricLastValueAsync(MetricConfiguration metric, double value)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMetricRepository>();

            metric.LastValue = value;
            metric.LastCalculated = DateTime.UtcNow;
            metric.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(metric);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to update last value for metric '{Name}'",
                metric.Name);
        }
    }
}
