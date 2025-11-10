using MongoDB.Entities;
using DataProcessing.Shared.Entities;
using MongoDB.Driver;

namespace DataProcessing.Shared.Extensions;

/// <summary>
/// Extension methods for MongoDB queries with soft delete and audit field support
/// </summary>
public static class MongoDbQueryExtensions
{
    /// <summary>
    /// Excludes soft deleted entities from query results
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <returns>Query with soft delete filter applied</returns>
    public static Find<T, T> ExcludeDeleted<T>(this Find<T, T> query) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.IsDeleted == false);
    }

    /// <summary>
    /// Includes only soft deleted entities in query results
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <returns>Query with deleted-only filter applied</returns>
    public static Find<T, T> OnlyDeleted<T>(this Find<T, T> query) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.IsDeleted == true);
    }

    /// <summary>
    /// Filters entities by correlation ID
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="correlationId">Correlation ID to filter by</param>
    /// <returns>Query with correlation ID filter applied</returns>
    public static Find<T, T> ByCorrelationId<T>(this Find<T, T> query, string correlationId) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.CorrelationId == correlationId);
    }

    /// <summary>
    /// Filters entities created by specific user or system
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="createdBy">Creator name to filter by</param>
    /// <returns>Query with creator filter applied</returns>
    public static Find<T, T> CreatedBy<T>(this Find<T, T> query, string createdBy) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.CreatedBy == createdBy);
    }

    /// <summary>
    /// Filters entities created within a specific date range
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Query with date range filter applied</returns>
    public static Find<T, T> CreatedBetween<T>(this Find<T, T> query, DateTime startDate, DateTime endDate) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate);
    }

    /// <summary>
    /// Filters entities updated within a specific date range
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Query with updated date range filter applied</returns>
    public static Find<T, T> UpdatedBetween<T>(this Find<T, T> query, DateTime startDate, DateTime endDate) where T : DataProcessingBaseEntity
    {
        return query.Match(x => x.UpdatedAt >= startDate && x.UpdatedAt <= endDate);
    }

    /// <summary>
    /// Orders entities by creation date (newest first)
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to order</param>
    /// <returns>Query ordered by creation date descending</returns>
    public static Find<T, T> NewestFirst<T>(this Find<T, T> query) where T : DataProcessingBaseEntity
    {
        return query.Sort(x => x.CreatedAt, Order.Descending);
    }

    /// <summary>
    /// Orders entities by creation date (oldest first)
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to order</param>
    /// <returns>Query ordered by creation date ascending</returns>
    public static Find<T, T> OldestFirst<T>(this Find<T, T> query) where T : DataProcessingBaseEntity
    {
        return query.Sort(x => x.CreatedAt, Order.Ascending);
    }

    /// <summary>
    /// Orders entities by last update date (most recently updated first)
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="query">The query to order</param>
    /// <returns>Query ordered by update date descending</returns>
    public static Find<T, T> RecentlyUpdatedFirst<T>(this Find<T, T> query) where T : DataProcessingBaseEntity
    {
        return query.Sort(x => x.UpdatedAt, Order.Descending);
    }

    /// <summary>
    /// Applies pagination to the query
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query to paginate</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Query with pagination applied</returns>
    public static Find<T, T> Paginate<T>(this Find<T, T> query, int pageNumber, int pageSize) where T : Entity
    {
        var skip = (pageNumber - 1) * pageSize;
        return query.Skip(skip).Limit(pageSize);
    }

    /// <summary>
    /// Soft deletes an entity (sets IsDeleted = true)
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="entity">Entity to soft delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <returns>Task representing the soft delete operation</returns>
    public static async Task SoftDeleteAsync<T>(this T entity, string deletedBy = "System") where T : DataProcessingBaseEntity
    {
        entity.MarkAsDeleted(deletedBy);
        await entity.SaveAsync();
    }

    /// <summary>
    /// Soft deletes multiple entities by IDs
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="ids">List of entity IDs to soft delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <returns>Number of entities that were soft deleted</returns>
    public static async Task<long> SoftDeleteManyAsync<T>(IEnumerable<string> ids, string deletedBy = "System") where T : DataProcessingBaseEntity
    {
        var updateDefinition = Builders<T>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy)
            .Inc(x => x.Version, 1);

        var filter = Builders<T>.Filter.In(x => x.ID, ids);
        
        var result = await DB.Collection<T>().UpdateManyAsync(filter, updateDefinition);
        return result.ModifiedCount;
    }

    /// <summary>
    /// Restores a soft deleted entity (sets IsDeleted = false)
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <param name="entity">Entity to restore</param>
    /// <param name="restoredBy">User or system performing the restoration</param>
    /// <returns>Task representing the restore operation</returns>
    public static async Task RestoreAsync<T>(this T entity, string restoredBy = "System") where T : DataProcessingBaseEntity
    {
        entity.IsDeleted = false;
        entity.MarkAsModified(restoredBy);
        await entity.SaveAsync();
    }

    /// <summary>
    /// Gets count of entities excluding soft deleted ones
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <returns>Count of non-deleted entities</returns>
    public static async Task<long> CountActiveAsync<T>() where T : DataProcessingBaseEntity
    {
        return await DB.CountAsync<T>(x => x.IsDeleted == false);
    }

    /// <summary>
    /// Gets count of soft deleted entities
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
    /// <returns>Count of deleted entities</returns>
    public static async Task<long> CountDeletedAsync<T>() where T : DataProcessingBaseEntity
    {
        return await DB.CountAsync<T>(x => x.IsDeleted == true);
    }
}

/// <summary>
/// Repository-like extensions for common query patterns
/// </summary>
public static class DataProcessingQueryExtensions
{
    /// <summary>
    /// Gets active data sources (not deleted and active flag = true)
    /// </summary>
    public static Find<DataProcessingDataSource, DataProcessingDataSource> ActiveDataSources()
    {
        return DB.Find<DataProcessingDataSource>()
            .ExcludeDeleted()
            .Match(x => x.IsActive == true);
    }

    /// <summary>
    /// Gets validation results by data source ID
    /// </summary>
    public static Find<DataProcessingValidationResult, DataProcessingValidationResult> ByDataSourceId(string dataSourceId)
    {
        return DB.Find<DataProcessingValidationResult>()
            .ExcludeDeleted()
            .Match(x => x.DataSourceId == dataSourceId);
    }

    /// <summary>
    /// Gets invalid records that haven't been reviewed
    /// </summary>
    public static Find<DataProcessingInvalidRecord, DataProcessingInvalidRecord> UnreviewedInvalidRecords()
    {
        return DB.Find<DataProcessingInvalidRecord>()
            .ExcludeDeleted()
            .Match(x => x.IsReviewed == false);
    }

    /// <summary>
    /// Gets invalid records by validation result ID
    /// </summary>
    public static Find<DataProcessingInvalidRecord, DataProcessingInvalidRecord> ByValidationResultId(string validationResultId)
    {
        return DB.Find<DataProcessingInvalidRecord>()
            .ExcludeDeleted()
            .Match(x => x.ValidationResultId == validationResultId);
    }

    /// <summary>
    /// Gets validation results by status and time range
    /// </summary>
    public static Find<DataProcessingValidationResult, DataProcessingValidationResult> ByStatusAndDateRange(string status, DateTime startDate, DateTime endDate)
    {
        return DB.Find<DataProcessingValidationResult>()
            .ExcludeDeleted()
            .Match(x => x.Status == status)
            .CreatedBetween(startDate, endDate);
    }

    /// <summary>
    /// Gets data sources by supplier name
    /// </summary>
    public static Find<DataProcessingDataSource, DataProcessingDataSource> BySupplier(string supplierName)
    {
        return DB.Find<DataProcessingDataSource>()
            .ExcludeDeleted()
            .Match(x => x.SupplierName == supplierName);
    }
}
