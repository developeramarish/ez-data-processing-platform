namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when a data source is updated
/// </summary>
public class DataSourceUpdatedEvent : IDataProcessingMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PublishedBy { get; set; } = "DataSourceManagementService";
    public int MessageVersion { get; set; } = 1;

    /// <summary>
    /// Data source identifier
    /// </summary>
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Data source name
    /// </summary>
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier providing the data
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval for file discovery
    /// </summary>
    public TimeSpan PollingRate { get; set; }

    /// <summary>
    /// Cron expression for scheduling (if specified)
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether the datasource is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// User who updated the datasource
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}
