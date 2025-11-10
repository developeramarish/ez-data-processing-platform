using MetricsConfigurationService.Models.Prometheus;

namespace MetricsConfigurationService.Services.Prometheus;

/// <summary>
/// Service for querying Prometheus instances
/// </summary>
public interface IPrometheusQueryService
{
    /// <summary>
    /// Execute an instant query to get the current value
    /// </summary>
    Task<PrometheusQueryResult> QueryInstantAsync(string query, PrometheusInstance instance);
    
    /// <summary>
    /// Execute a range query to get time-series data
    /// </summary>
    Task<PrometheusQueryResult> QueryRangeAsync(
        string query, 
        DateTime start, 
        DateTime end, 
        string step, 
        PrometheusInstance instance);
    
    /// <summary>
    /// Get list of all metric names
    /// </summary>
    Task<IEnumerable<string>> GetMetricNamesAsync(PrometheusInstance instance);
    
    /// <summary>
    /// Get possible values for a label
    /// </summary>
    Task<IEnumerable<string>> GetLabelValuesAsync(string label, PrometheusInstance instance);
}

/// <summary>
/// Enum for selecting Prometheus instance
/// </summary>
public enum PrometheusInstance
{
    /// <summary>
    /// System Prometheus - Infrastructure metrics (port 9090)
    /// </summary>
    System,
    
    /// <summary>
    /// Business Prometheus - Business KPIs (port 9091)
    /// </summary>
    Business
}
