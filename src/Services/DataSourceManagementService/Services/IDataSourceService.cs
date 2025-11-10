using DataProcessing.Shared.Entities;
using DataProcessing.DataSourceManagement.Models.Requests;
using DataProcessing.DataSourceManagement.Models.Responses;
using DataProcessing.DataSourceManagement.Models.Queries;

namespace DataProcessing.DataSourceManagement.Services;

/// <summary>
/// Service interface for data source management operations
/// Provides business logic layer with validation, error handling, and Hebrew UI support
/// </summary>
public interface IDataSourceService
{
    /// <summary>
    /// Gets a data source by ID with comprehensive error handling
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing data source or error details</returns>
    Task<ApiResponse<DataProcessingDataSource>> GetByIdAsync(string id, string correlationId);

    /// <summary>
    /// Gets paginated list of data sources with filtering and sorting
    /// </summary>
    /// <param name="query">Query parameters for filtering, pagination, and sorting</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing paginated data sources or error details</returns>
    Task<ApiResponse<PagedResult<DataProcessingDataSource>>> GetPagedAsync(DataSourceQuery query, string correlationId);

    /// <summary>
    /// Gets all active data sources for processing operations
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing list of active data sources</returns>
    Task<ApiResponse<List<DataProcessingDataSource>>> GetActiveAsync(string correlationId);

    /// <summary>
    /// Gets data sources by supplier name
    /// </summary>
    /// <param name="supplierName">Supplier name</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing list of data sources for the supplier</returns>
    Task<ApiResponse<List<DataProcessingDataSource>>> GetBySupplierAsync(string supplierName, string correlationId);

    /// <summary>
    /// Creates a new data source with business validation
    /// </summary>
    /// <param name="request">Create request containing data source details</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing created data source or validation errors</returns>
    Task<ApiResponse<DataProcessingDataSource>> CreateAsync(CreateDataSourceRequest request, string correlationId);

    /// <summary>
    /// Updates an existing data source with business validation
    /// </summary>
    /// <param name="request">Update request containing data source details</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating success or error details</returns>
    Task<ApiResponse<object>> UpdateAsync(UpdateDataSourceRequest request, string correlationId);

    /// <summary>
    /// Soft deletes a data source by ID
    /// </summary>
    /// <param name="id">Data source ID to delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating success or error details</returns>
    Task<ApiResponse<object>> SoftDeleteAsync(string id, string deletedBy, string correlationId);

    /// <summary>
    /// Restores a soft-deleted data source
    /// </summary>
    /// <param name="id">Data source ID to restore</param>
    /// <param name="restoredBy">User or system performing the restoration</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating success or error details</returns>
    Task<ApiResponse<object>> RestoreAsync(string id, string restoredBy, string correlationId);

    /// <summary>
    /// Validates a data source name for uniqueness
    /// </summary>
    /// <param name="name">Data source name to validate</param>
    /// <param name="excludeId">ID to exclude from validation (for updates)</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating if name is available</returns>
    Task<ApiResponse<bool>> ValidateNameAsync(string name, string? excludeId, string correlationId);

    /// <summary>
    /// Gets data source statistics and counts
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing statistics</returns>
    Task<ApiResponse<object>> GetStatisticsAsync(string correlationId);

    /// <summary>
    /// Gets data sources that haven't been processed for a specified time period
    /// </summary>
    /// <param name="inactiveThreshold">Time threshold for considering a data source inactive</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing list of inactive data sources</returns>
    Task<ApiResponse<List<DataProcessingDataSource>>> GetInactiveAsync(TimeSpan inactiveThreshold, string correlationId);

    /// <summary>
    /// Updates processing statistics for a data source
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="filesProcessed">Number of files processed</param>
    /// <param name="errorRecords">Number of error records</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating success or error details</returns>
    Task<ApiResponse<object>> UpdateProcessingStatsAsync(string id, long filesProcessed, long errorRecords, string correlationId);

    /// <summary>
    /// Tests connection to a data source
    /// </summary>
    /// <param name="id">Data source ID to test</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating connection test result</returns>
    Task<ApiResponse<object>> TestConnectionAsync(string id, string correlationId);

    /// <summary>
    /// Gets detailed processing statistics for a data source
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response containing detailed statistics</returns>
    Task<ApiResponse<object>> GetProcessingStatisticsAsync(string id, string correlationId);

    /// <summary>
    /// Updates schedule configuration for a data source
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="scheduleConfig">Schedule configuration JSON</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>API response indicating success or error details</returns>
    Task<ApiResponse<object>> UpdateScheduleAsync(string id, string scheduleConfig, string correlationId);
}
