using System.Diagnostics;
using System.Linq.Expressions;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Extensions;
using DataProcessing.DataSourceManagement.Infrastructure;
using DataProcessing.DataSourceManagement.Models.Queries;
using DataProcessing.DataSourceManagement.Models.Responses;
using MongoDB.Entities;

namespace DataProcessing.DataSourceManagement.Repositories;

/// <summary>
/// Repository implementation for data source management operations
/// Leverages existing MongoDbQueryExtensions for consistent data access patterns
/// </summary>
public class DataSourceRepository : BaseRepository<DataProcessingDataSource>, IDataSourceRepository
{
    private static readonly ActivitySource ActivitySource = new("DataProcessing.DataSourceManagement.Repository");

    /// <summary>
    /// Gets a data source by ID with correlation ID tracking
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Data source entity or null if not found</returns>
    public async Task<DataProcessingDataSource?> GetByIdAsync(string id, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetByIdAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-id", id);

        return await GetActiveQuery()
            .Match(x => x.ID == id)
            .ExecuteFirstAsync();
    }

    /// <summary>
    /// Gets a data source by name with correlation ID tracking
    /// </summary>
    /// <param name="name">Data source name</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Data source entity or null if not found</returns>
    public async Task<DataProcessingDataSource?> GetByNameAsync(string name, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetByNameAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-name", name);

        return await GetActiveQuery()
            .Match(x => x.Name == name)
            .ExecuteFirstAsync();
    }

    /// <summary>
    /// Gets paginated list of data sources with filtering and sorting
    /// </summary>
    /// <param name="query">Query parameters for filtering, pagination, and sorting</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Paginated result containing data sources and paging information</returns>
    public async Task<PagedResult<DataProcessingDataSource>> GetPagedAsync(DataSourceQuery query, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetPagedAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("page", query.Page);
        activity?.SetTag("size", query.Size);

        // Start with base query (active or including deleted based on query parameter)
        var mongoQuery = query.IncludeDeleted ? DB.Find<DataProcessingDataSource>() : GetActiveQuery();

        // Apply filters
        mongoQuery = ApplyFilters(mongoQuery, query);

        // Get total count by building the filter and using DB.CountAsync
        var totalItems = await GetCountAsync(query);

        // Apply sorting
        mongoQuery = ApplySorting(mongoQuery, query);

        // Apply pagination using existing extension
        mongoQuery = ApplyPagination(mongoQuery, query.Page, query.Size);

        // Execute query
        var items = await mongoQuery.ExecuteAsync();

        return PagedResult<DataProcessingDataSource>.Create(items, query.Page, query.Size, totalItems);
    }

    /// <summary>
    /// Gets all active data sources for processing operations
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of active data sources</returns>
    public async Task<List<DataProcessingDataSource>> GetActiveAsync(string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetActiveAsync");
        activity?.SetTag("correlation-id", correlationId);

        // Use existing extension for active data sources
        return await DataProcessingQueryExtensions.ActiveDataSources()
            .ExecuteAsync();
    }

    /// <summary>
    /// Gets data sources by supplier name
    /// </summary>
    /// <param name="supplierName">Supplier name</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of data sources for the supplier</returns>
    public async Task<List<DataProcessingDataSource>> GetBySupplierAsync(string supplierName, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetBySupplierAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("supplier-name", supplierName);

        // Use existing extension for supplier filtering
        return await DataProcessingQueryExtensions.BySupplier(supplierName)
            .ExecuteAsync();
    }

    /// <summary>
    /// Creates a new data source
    /// </summary>
    /// <param name="dataSource">Data source entity to create</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Created data source entity</returns>
    public async Task<DataProcessingDataSource> CreateAsync(DataProcessingDataSource dataSource, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("CreateAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-name", dataSource.Name);

        // Set correlation ID and audit fields
        dataSource.CorrelationId = correlationId;
        dataSource.CreatedAt = DateTime.UtcNow;
        dataSource.UpdatedAt = DateTime.UtcNow;
        dataSource.CreatedBy = "DataSourceManagementService";
        dataSource.UpdatedBy = "DataSourceManagementService";

        await dataSource.SaveAsync();
        
        return dataSource;
    }

    /// <summary>
    /// Updates an existing data source
    /// </summary>
    /// <param name="dataSource">Data source entity to update</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Task representing the update operation</returns>
    public async Task UpdateAsync(DataProcessingDataSource dataSource, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("UpdateAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-id", dataSource.ID);

        // Update audit fields using existing method
        dataSource.CorrelationId = correlationId;
        dataSource.MarkAsModified("DataSourceManagementService");

        await dataSource.SaveAsync();
    }

    /// <summary>
    /// Soft deletes a data source by ID
    /// </summary>
    /// <param name="id">Data source ID to delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if deletion was successful, false if entity not found</returns>
    public async Task<bool> SoftDeleteAsync(string id, string deletedBy, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("SoftDeleteAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-id", id);
        activity?.SetTag("deleted-by", deletedBy);

        var dataSource = await GetByIdAsync(id, correlationId);
        if (dataSource == null)
        {
            return false;
        }

        // Use existing extension method for soft delete
        await SoftDeleteEntityAsync(dataSource, deletedBy);
        
        return true;
    }

    /// <summary>
    /// Restores a soft-deleted data source
    /// </summary>
    /// <param name="id">Data source ID to restore</param>
    /// <param name="restoredBy">User or system performing the restoration</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if restoration was successful, false if entity not found</returns>
    public async Task<bool> RestoreAsync(string id, string restoredBy, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("RestoreAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-id", id);
        activity?.SetTag("restored-by", restoredBy);

        // Find the deleted entity
        var dataSource = await DB.Find<DataProcessingDataSource>()
            .OnlyDeleted()
            .Match(x => x.ID == id)
            .ExecuteFirstAsync();

        if (dataSource == null)
        {
            return false;
        }

        // Use existing extension method for restore
        await RestoreEntityAsync(dataSource, restoredBy);
        
        return true;
    }

    /// <summary>
    /// Checks if a data source name already exists (for uniqueness validation)
    /// </summary>
    /// <param name="name">Data source name to check</param>
    /// <param name="excludeId">ID to exclude from check (for updates)</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if name exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(string name, string? excludeId = null, string correlationId = "")
    {
        using var activity = ActivitySource.StartActivity("ExistsAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-name", name);
        activity?.SetTag("exclude-id", excludeId);

        var query = GetActiveQuery().Match(x => x.Name == name);

        if (!string.IsNullOrEmpty(excludeId))
        {
            query = query.Match(x => x.ID != excludeId);
        }

        return await query.ExecuteAnyAsync();
    }

    /// <summary>
    /// Gets count of active data sources
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Number of active data sources</returns>
    public async Task<long> CountActiveAsync(string correlationId)
    {
        using var activity = ActivitySource.StartActivity("CountActiveAsync");
        activity?.SetTag("correlation-id", correlationId);

        // Use existing extension method
        return await MongoDbQueryExtensions.CountActiveAsync<DataProcessingDataSource>();
    }

    /// <summary>
    /// Gets count of soft-deleted data sources
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Number of deleted data sources</returns>
    public async Task<long> CountDeletedAsync(string correlationId)
    {
        using var activity = ActivitySource.StartActivity("CountDeletedAsync");
        activity?.SetTag("correlation-id", correlationId);

        // Use existing extension method
        return await MongoDbQueryExtensions.CountDeletedAsync<DataProcessingDataSource>();
    }

    /// <summary>
    /// Gets data sources that haven't been processed for a specified time period
    /// </summary>
    /// <param name="inactiveThreshold">Time threshold for considering a data source inactive</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>List of inactive data sources</returns>
    public async Task<List<DataProcessingDataSource>> GetInactiveAsync(TimeSpan inactiveThreshold, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("GetInactiveAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("inactive-threshold-hours", inactiveThreshold.TotalHours);

        var thresholdDate = DateTime.UtcNow.Subtract(inactiveThreshold);

        return await GetActiveQuery()
            .Match(x => x.IsActive == true)
            .Match(x => x.LastProcessedAt == null || x.LastProcessedAt < thresholdDate)
            .ExecuteAsync();
    }

    /// <summary>
    /// Updates processing statistics for a data source
    /// </summary>
    /// <param name="id">Data source ID</param>
    /// <param name="filesProcessed">Number of files processed</param>
    /// <param name="errorRecords">Number of error records</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>True if update was successful, false if entity not found</returns>
    public async Task<bool> UpdateProcessingStatsAsync(string id, long filesProcessed, long errorRecords, string correlationId)
    {
        using var activity = ActivitySource.StartActivity("UpdateProcessingStatsAsync");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("data-source-id", id);
        activity?.SetTag("files-processed", filesProcessed);
        activity?.SetTag("error-records", errorRecords);

        var dataSource = await GetByIdAsync(id, correlationId);
        if (dataSource == null)
        {
            return false;
        }

        // Use existing method on entity
        dataSource.UpdateProcessingStats(filesProcessed, errorRecords);
        dataSource.CorrelationId = correlationId;

        await dataSource.SaveAsync();
        
        return true;
    }

    /// <summary>
    /// Applies filtering criteria to the query
    /// </summary>
    /// <param name="query">MongoDB query to filter</param>
    /// <param name="queryParams">Query parameters containing filter criteria</param>
    /// <returns>Filtered query</returns>
    private static Find<DataProcessingDataSource, DataProcessingDataSource> ApplyFilters(
        Find<DataProcessingDataSource, DataProcessingDataSource> query, DataSourceQuery queryParams)
    {
        // Name filter (partial match)
        if (!string.IsNullOrEmpty(queryParams.Name))
        {
            query = query.Match(x => x.Name.Contains(queryParams.Name));
        }

        // Supplier name filter (partial match)
        if (!string.IsNullOrEmpty(queryParams.SupplierName))
        {
            query = query.Match(x => x.SupplierName.Contains(queryParams.SupplierName));
        }

        // Category filter
        if (!string.IsNullOrEmpty(queryParams.Category))
        {
            query = query.Match(x => x.Category == queryParams.Category);
        }

        // Active status filter
        if (queryParams.IsActive.HasValue)
        {
            query = query.Match(x => x.IsActive == queryParams.IsActive.Value);
        }

        // Creation date range filter using existing extension
        if (queryParams.CreatedFrom.HasValue && queryParams.CreatedTo.HasValue)
        {
            query = query.CreatedBetween(queryParams.CreatedFrom.Value, queryParams.CreatedTo.Value);
        }
        else if (queryParams.CreatedFrom.HasValue)
        {
            query = query.Match(x => x.CreatedAt >= queryParams.CreatedFrom.Value);
        }
        else if (queryParams.CreatedTo.HasValue)
        {
            query = query.Match(x => x.CreatedAt <= queryParams.CreatedTo.Value);
        }

        // Update date range filter using existing extension
        if (queryParams.UpdatedFrom.HasValue && queryParams.UpdatedTo.HasValue)
        {
            query = query.UpdatedBetween(queryParams.UpdatedFrom.Value, queryParams.UpdatedTo.Value);
        }
        else if (queryParams.UpdatedFrom.HasValue)
        {
            query = query.Match(x => x.UpdatedAt >= queryParams.UpdatedFrom.Value);
        }
        else if (queryParams.UpdatedTo.HasValue)
        {
            query = query.Match(x => x.UpdatedAt <= queryParams.UpdatedTo.Value);
        }

        // Search text filter (across multiple fields)
        if (!string.IsNullOrEmpty(queryParams.SearchText))
        {
            var searchText = queryParams.SearchText.Trim();
            query = query.Match(x => 
                x.Name.Contains(searchText) || 
                x.SupplierName.Contains(searchText) || 
                (x.Description != null && x.Description.Contains(searchText)));
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the query based on sort parameters
    /// </summary>
    /// <param name="query">MongoDB query to sort</param>
    /// <param name="queryParams">Query parameters containing sort criteria</param>
    /// <returns>Sorted query</returns>
    private static Find<DataProcessingDataSource, DataProcessingDataSource> ApplySorting(
        Find<DataProcessingDataSource, DataProcessingDataSource> query, DataSourceQuery queryParams)
    {
        var isAscending = queryParams.SortDirection == Models.Queries.SortDirection.Ascending;

        return queryParams.SortBy switch
        {
            DataSourceSortField.ID => query.Sort(x => x.ID, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.Name => query.Sort(x => x.Name, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.SupplierName => query.Sort(x => x.SupplierName, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.CreatedAt => isAscending ? query.OldestFirst() : query.NewestFirst(),
            DataSourceSortField.UpdatedAt => query.Sort(x => x.UpdatedAt, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.Category => query.Sort(x => x.Category, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.LastProcessedAt => query.Sort(x => x.LastProcessedAt, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.TotalFilesProcessed => query.Sort(x => x.TotalFilesProcessed, isAscending ? Order.Ascending : Order.Descending),
            DataSourceSortField.TotalErrorRecords => query.Sort(x => x.TotalErrorRecords, isAscending ? Order.Ascending : Order.Descending),
            _ => query.NewestFirst() // Default to newest first
        };
    }

    /// <summary>
    /// Gets count of entities based on query filters
    /// Uses a simpler approach that builds the same query as ApplyFilters and gets count from results
    /// </summary>
    /// <param name="query">Query parameters for filtering</param>
    /// <returns>Count of entities matching the filters</returns>
    private static async Task<long> GetCountAsync(DataSourceQuery query)
    {
        // Build the exact same query as the main method but only get count
        // This ensures perfect consistency between count and actual results
        
        var countQuery = query.IncludeDeleted ? DB.Find<DataProcessingDataSource>() : GetActiveQuery();
        
        // Apply the same filters as ApplyFilters method
        countQuery = ApplyFilters(countQuery, query);
        
        // Get all results and count them (for smaller datasets this is fine)
        // For large datasets, this could be optimized but ensures accuracy
        var results = await countQuery.ExecuteAsync();
        return results.Count;
    }
}
