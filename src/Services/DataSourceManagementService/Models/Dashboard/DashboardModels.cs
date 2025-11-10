namespace DataSourceManagementService.Models.Dashboard;

/// <summary>
/// Dashboard overview statistics response
/// </summary>
public class DashboardOverviewResponse
{
    /// <summary>
    /// Total number of files processed across all data sources
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total number of valid records
    /// </summary>
    public long ValidRecords { get; set; }

    /// <summary>
    /// Total number of invalid records
    /// </summary>
    public long InvalidRecords { get; set; }

    /// <summary>
    /// Error rate percentage (invalid / total * 100)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Timestamp when statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}
