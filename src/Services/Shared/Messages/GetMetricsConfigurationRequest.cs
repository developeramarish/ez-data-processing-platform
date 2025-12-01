namespace DataProcessing.Shared.Messages;

/// <summary>
/// Request to get metric configurations for a specific data source
/// Used by ValidationService to query MetricsConfigurationService
/// Supports both global metrics (apply to all data sources) and datasource-specific metrics
/// </summary>
public class GetMetricsConfigurationRequest : IDataProcessingMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PublishedBy { get; set; } = string.Empty;
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// Data source ID to retrieve datasource-specific metrics for
    /// </summary>
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include global metrics that apply to all data sources
    /// Default: true
    /// </summary>
    public bool IncludeGlobal { get; set; } = true;

    /// <summary>
    /// Whether to only return active metrics (Status = 1)
    /// Default: true
    /// </summary>
    public bool OnlyActive { get; set; } = true;
}
