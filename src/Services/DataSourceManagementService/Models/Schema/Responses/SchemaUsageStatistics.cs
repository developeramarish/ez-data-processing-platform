namespace SchemaManagementService.Models.Responses;

/// <summary>
/// Usage statistics for a schema
/// </summary>
public class SchemaUsageStatistics
{
    /// <summary>
    /// Schema ID
    /// </summary>
    public string SchemaId { get; set; } = string.Empty;

    /// <summary>
    /// Schema name
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Schema display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Current usage count
    /// </summary>
    public int CurrentUsageCount { get; set; } = 0;

    /// <summary>
    /// Data sources using this schema
    /// </summary>
    public List<DataSourceUsage> DataSources { get; set; } = new();

    /// <summary>
    /// Total records processed with this schema
    /// </summary>
    public long TotalRecordsProcessed { get; set; } = 0;

    /// <summary>
    /// Total validation attempts
    /// </summary>
    public long TotalValidationAttempts { get; set; } = 0;

    /// <summary>
    /// Successful validations
    /// </summary>
    public long SuccessfulValidations { get; set; } = 0;

    /// <summary>
    /// Failed validations
    /// </summary>
    public long FailedValidations { get; set; } = 0;

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;

    /// <summary>
    /// Average validation time in milliseconds
    /// </summary>
    public double AverageValidationTimeMs { get; set; } = 0.0;

    /// <summary>
    /// Schema creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last validation date
    /// </summary>
    public DateTime? LastValidationAt { get; set; }

    /// <summary>
    /// Statistics collection date
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Usage trend over time
    /// </summary>
    public List<UsageTrendPoint> UsageTrend { get; set; } = new();
}

/// <summary>
/// Data source usage information
/// </summary>
public class DataSourceUsage
{
    /// <summary>
    /// Data source ID
    /// </summary>
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Data source name
    /// </summary>
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Records processed by this data source
    /// </summary>
    public long RecordsProcessed { get; set; } = 0;

    /// <summary>
    /// Last activity date
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool IsActive { get; set; } = false;
}

/// <summary>
/// Usage trend data point
/// </summary>
public class UsageTrendPoint
{
    /// <summary>
    /// Date of the measurement
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of validations on this date
    /// </summary>
    public int ValidationCount { get; set; } = 0;

    /// <summary>
    /// Records processed on this date
    /// </summary>
    public long RecordsCount { get; set; } = 0;

    /// <summary>
    /// Success rate on this date
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;
}
