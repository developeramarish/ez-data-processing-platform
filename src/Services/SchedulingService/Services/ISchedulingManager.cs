using DataProcessing.Shared.Entities;

namespace DataProcessing.Scheduling.Services;

/// <summary>
/// Interface for managing Quartz.NET scheduling operations
/// </summary>
public interface ISchedulingManager
{
    /// <summary>
    /// Creates or updates a polling schedule for a data source
    /// </summary>
    Task<bool> ScheduleDataSourcePollingAsync(DataProcessingDataSource dataSource, string correlationId);

    /// <summary>
    /// Removes a polling schedule for a data source
    /// </summary>
    Task<bool> UnscheduleDataSourcePollingAsync(string dataSourceId, string correlationId);

    /// <summary>
    /// Updates the polling interval for an existing data source schedule
    /// </summary>
    Task<bool> UpdatePollingIntervalAsync(string dataSourceId, TimeSpan newInterval, string correlationId);

    /// <summary>
    /// Pauses polling for a data source temporarily
    /// </summary>
    Task<bool> PauseDataSourcePollingAsync(string dataSourceId, string correlationId);

    /// <summary>
    /// Resumes polling for a previously paused data source
    /// </summary>
    Task<bool> ResumeDataSourcePollingAsync(string dataSourceId, string correlationId);

    /// <summary>
    /// Gets the current schedule status for a data source
    /// </summary>
    Task<ScheduleStatus?> GetScheduleStatusAsync(string dataSourceId);

    /// <summary>
    /// Gets all active schedules
    /// </summary>
    Task<List<ScheduleInfo>> GetAllSchedulesAsync();

    /// <summary>
    /// Triggers immediate polling for a data source (manual trigger)
    /// </summary>
    Task<bool> TriggerImmediatePollingAsync(string dataSourceId, string correlationId);

    /// <summary>
    /// Validates if a schedule configuration is valid
    /// </summary>
    Task<bool> ValidateScheduleConfigurationAsync(TimeSpan pollingInterval);
}

/// <summary>
/// Schedule status information
/// </summary>
public class ScheduleStatus
{
    public string DataSourceId { get; set; } = string.Empty;
    public string DataSourceName { get; set; } = string.Empty;
    public TimeSpan PollingInterval { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public string? LastError { get; set; }
    public int ExecutionCount { get; set; }
}

/// <summary>
/// Detailed schedule information
/// </summary>
public class ScheduleInfo
{
    public string DataSourceId { get; set; } = string.Empty;
    public string DataSourceName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public TimeSpan PollingInterval { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public string JobKey { get; set; } = string.Empty;
    public string TriggerKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
