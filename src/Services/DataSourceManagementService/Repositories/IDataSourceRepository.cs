using DataProcessing.Shared.Entities;
using DataProcessing.DataSourceManagement.Models.Queries;
using DataProcessing.DataSourceManagement.Models.Responses;
using MongoDB.Entities;

namespace DataProcessing.DataSourceManagement.Repositories;

/// <summary>
/// Repository interface for data source management operations
/// </summary>
public interface IDataSourceRepository
{
    /// <summary>
    /// Gets a data source by ID with correlation ID tracking
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Data source entity or null if not found</returns>
    Task<DataProcessingDataSource?> GetByIdAsync(string id, string correlationId);

    /// <summary>
    /// Gets a data source by name with correlation ID tracking
    /// </summary>
    /// <param name="name">Data source name</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Data source entity or null if not found</returns>
    Task<DataProcessingDataSource?> GetByNameAsync(string name, string correlationId);

    /// <summary>
    /// Gets paginated list of data sources with filtering and sorting
    /// </summary>
    /// <param name="query">Query parameters for filtering, pagination, and sorting</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Paginated result containing data sources and paging information</returns>
    Task<PagedResult<DataProcessingDataSource>> GetPagedAsync(DataSourceQuery query, string correlationId);

    /// <summary>
    /// Gets all active data sources for processing operations
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of active data sources</returns>
    Task<List<DataProcessingDataSource>> GetActiveAsync(string correlationId);

    /// <summary>
    /// Gets data sources by supplier name
    /// </summary>
    /// <param name="supplierName">Supplier name</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of data sources for the supplier</returns>
    Task<List<DataProcessingDataSource>> GetBySupplierAsync(string supplierName, string correlationId);

    /// <summary>
    /// Creates a new data source
    /// </summary>
    /// <param name="dataSource">Data source entity to create</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Created data source entity</returns>
    Task<DataProcessingDataSource> CreateAsync(DataProcessingDataSource dataSource, string correlationId);

    /// <summary>
    /// Updates an existing data source
    /// </summary>
    /// <param name="dataSource">Data source entity to update</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Task representing the update operation</returns>
    Task UpdateAsync(DataProcessingDataSource dataSource, string correlationId);

    /// <summary>
    /// Soft deletes a data source by ID
    /// </summary>
    /// <param name="id">Data source ID to delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if deletion was successful, false if entity not found</returns>
    Task<bool> SoftDeleteAsync(string id, string deletedBy, string correlationId);

    /// <summary>
    /// Restores a soft-deleted data source
    /// </summary>
    /// <param name="id">Data source ID to restore</param>
    /// <param name="restoredBy">User or system performing the restoration</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if restoration was successful, false if entity not found</returns>
    Task<bool> RestoreAsync(string id, string restoredBy, string correlationId);

    /// <summary>
    /// Checks if a data source name already exists (for uniqueness validation)
    /// </summary>
    /// <param name="name">Data source name to check</param>
    /// <param name="excludeId">ID to exclude from check (for updates)</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> ExistsAsync(string name, string? excludeId = null, string correlationId = "");

    /// <summary>
    /// Gets count of active data sources
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Number of active data sources</returns>
    Task<long> CountActiveAsync(string correlationId);

    /// <summary>
    /// Gets count of soft-deleted data sources
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Number of deleted data sources</returns>
    Task<long> CountDeletedAsync(string correlationId);

    /// <summary>
    /// Gets data sources that haven't been processed for a specified time period
    /// </summary>
    /// <param name="inactiveThreshold">Time threshold for considering a data source inactive</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of inactive data sources</returns>
    Task<List<DataProcessingDataSource>> GetInactiveAsync(TimeSpan inactiveThreshold, string correlationId);

    /// <summary>
    /// Updates processing statistics for a data source
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="filesProcessed">Number of files processed</param>
    /// <param name="errorRecords">Number of error records</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if update was successful, false if entity not found</returns>
    Task<bool> UpdateProcessingStatsAsync(string id, long filesProcessed, long errorRecords, string correlationId);
}
