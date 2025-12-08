namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when a data source is deleted
/// </summary>
public class DataSourceDeletedEvent : IDataProcessingMessage
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
    /// User who deleted the datasource
    /// </summary>
    public string DeletedBy { get; set; } = string.Empty;
}
