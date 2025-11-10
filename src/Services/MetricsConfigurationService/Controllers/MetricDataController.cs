using Microsoft.AspNetCore.Mvc;
using MetricsConfigurationService.Models.Prometheus;
using MetricsConfigurationService.Models.Requests;
using MetricsConfigurationService.Repositories;
using MetricsConfigurationService.Services.Prometheus;

namespace MetricsConfigurationService.Controllers;

/// <summary>
/// Controller for querying metric data from Prometheus
/// </summary>
[ApiController]
[Route("api/v1/metrics")]
public class MetricDataController : ControllerBase
{
    private readonly IPrometheusQueryService _prometheusService;
    private readonly IMetricRepository _metricRepository;
    private readonly ILogger<MetricDataController> _logger;

    public MetricDataController(
        IPrometheusQueryService prometheusService,
        IMetricRepository metricRepository,
        ILogger<MetricDataController> logger)
    {
        _prometheusService = prometheusService;
        _metricRepository = metricRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get time-series data for a specific metric
    /// </summary>
    [HttpGet("{metricId}/data")]
    public async Task<IActionResult> GetTimeSeriesData(
        string metricId,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] string step = "1m")
    {
        try
        {
            // Get metric configuration
            var metric = await _metricRepository.GetByIdAsync(metricId);
            if (metric == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with ID {metricId} not found" }
                });
            }

            // Default time range if not specified (last 24 hours)
            var endTime = end ?? DateTime.UtcNow;
            var startTime = start ?? endTime.AddHours(-24);

            // Determine which Prometheus instance to query
            var instance = metric.Scope == "global" 
                ? PrometheusInstance.System 
                : PrometheusInstance.Business;

            _logger.LogInformation(
                "Querying time-series data for metric {MetricName} from {Instance} ({Start} to {End})",
                metric.Name, instance, startTime, endTime);

            // Build PromQL query from metric configuration
            var query = BuildPromQLQuery(metric);

            // Query Prometheus for range data
            var result = await _prometheusService.QueryRangeAsync(
                query, startTime, endTime, step, instance);

            return Ok(new
            {
                IsSuccess = true,
                Data = new
                {
                    MetricId = metricId,
                    MetricName = metric.Name,
                    DisplayName = metric.DisplayName,
                    StartTime = startTime,
                    EndTime = endTime,
                    Step = step,
                    Instance = instance.ToString(),
                    Query = query,
                    TimeSeries = result.Data,
                    DataPoints = result.Data.Count,
                    ResultType = result.ResultType
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time-series data for metric {MetricId}", metricId);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get current value for a specific metric
    /// </summary>
    [HttpGet("{metricId}/current")]
    public async Task<IActionResult> GetCurrentValue(string metricId)
    {
        try
        {
            var metric = await _metricRepository.GetByIdAsync(metricId);
            if (metric == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with ID {metricId} not found" }
                });
            }

            var instance = metric.Scope == "global" 
                ? PrometheusInstance.System 
                : PrometheusInstance.Business;

            _logger.LogInformation(
                "Querying current value for metric {MetricName} from {Instance}",
                metric.Name, instance);

            var query = BuildPromQLQuery(metric);
            var result = await _prometheusService.QueryInstantAsync(query, instance);

            return Ok(new
            {
                IsSuccess = true,
                Data = new
                {
                    MetricId = metricId,
                    MetricName = metric.Name,
                    DisplayName = metric.DisplayName,
                    Timestamp = result.Timestamp,
                    Value = result.Value,
                    Labels = result.Labels,
                    Instance = instance.ToString(),
                    Query = query
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current value for metric {MetricId}", metricId);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Execute a custom PromQL query
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> ExecutePromQLQuery([FromBody] PromQLQueryRequest request)
    {
        try
        {
            var instance = request.Instance?.ToLower() == "business" 
                ? PrometheusInstance.Business 
                : PrometheusInstance.System;

            _logger.LogInformation(
                "Executing PromQL query on {Instance}: {Query}",
                instance, request.Query);

            PrometheusQueryResult result;
            
            if (request.QueryType == "range")
            {
                var startTime = request.Start ?? DateTime.UtcNow.AddHours(-1);
                var endTime = request.End ?? DateTime.UtcNow;
                var step = request.Step ?? "1m";

                result = await _prometheusService.QueryRangeAsync(
                    request.Query, startTime, endTime, step, instance);
            }
            else
            {
                result = await _prometheusService.QueryInstantAsync(request.Query, instance);
            }

            return Ok(new
            {
                IsSuccess = true,
                Data = new
                {
                    Query = request.Query,
                    QueryType = request.QueryType,
                    Instance = instance.ToString(),
                    Result = result
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PromQL query: {Query}", request.Query);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get available metrics from Prometheus
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableMetrics([FromQuery] string? instance = "business")
    {
        try
        {
            var promInstance = instance?.ToLower() == "system"
                ? PrometheusInstance.System
                : PrometheusInstance.Business;

            var metricNames = await _prometheusService.GetMetricNamesAsync(promInstance);

            return Ok(new
            {
                IsSuccess = true,
                Data = new
                {
                    Instance = promInstance.ToString(),
                    MetricNames = metricNames,
                    Count = metricNames.Count()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available metrics");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Build PromQL query from metric configuration
    /// </summary>
    private string BuildPromQLQuery(MetricsConfigurationService.Models.MetricConfiguration metric)
    {
        // If formula is provided, use it directly (it's already PromQL)
        if (!string.IsNullOrEmpty(metric.Formula))
        {
            return metric.Formula;
        }

        // If LabelsExpression is provided, use it
        if (!string.IsNullOrEmpty(metric.LabelsExpression))
        {
            return $"{metric.Name}{metric.LabelsExpression}";
        }

        // Otherwise just use the metric name
        return metric.Name;
    }
}
