using MongoDB.Entities;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Extensions;

namespace DataProcessing.DataSourceManagement.Infrastructure;

/// <summary>
/// Base repository pattern leveraging existing MongoDbQueryExtensions
/// Provides common query operations for all entities
/// </summary>
/// <typeparam name="T">Entity type that inherits from DataProcessingBaseEntity</typeparam>
public abstract class BaseRepository<T> where T : DataProcessingBaseEntity
{
    /// <summary>
    /// Gets base query for active (non-deleted) entities
    /// </summary>
    /// <returns>Query with soft delete filter applied</returns>
    protected static Find<T, T> GetActiveQuery() => DB.Find<T>().ExcludeDeleted();

    /// <summary>
    /// Gets count of active entities
    /// </summary>
    /// <returns>Count of non-deleted entities</returns>
    protected static Task<long> CountActiveAsync() => MongoDbQueryExtensions.CountActiveAsync<T>();

    /// <summary>
    /// Gets count of deleted entities
    /// </summary>
    /// <returns>Count of soft-deleted entities</returns>
    protected static Task<long> CountDeletedAsync() => MongoDbQueryExtensions.CountDeletedAsync<T>();

    /// <summary>
    /// Applies pagination to a query using existing extensions
    /// </summary>
    /// <param name="query">The query to paginate</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Query with pagination applied</returns>
    protected static Find<T, T> ApplyPagination(Find<T, T> query, int pageNumber, int pageSize)
    {
        return query.Paginate(pageNumber, pageSize);
    }

    /// <summary>
    /// Applies common ordering (newest first) to a query
    /// </summary>
    /// <param name="query">The query to order</param>
    /// <returns>Query with newest-first ordering</returns>
    protected static Find<T, T> ApplyDefaultOrdering(Find<T, T> query)
    {
        return query.NewestFirst();
    }

    /// <summary>
    /// Soft deletes an entity using existing extension methods
    /// </summary>
    /// <param name="entity">Entity to soft delete</param>
    /// <param name="deletedBy">User or system performing the deletion</param>
    /// <returns>Task representing the soft delete operation</returns>
    protected static async Task SoftDeleteEntityAsync(T entity, string deletedBy = "System")
    {
        await entity.SoftDeleteAsync(deletedBy);
    }

    /// <summary>
    /// Restores a soft-deleted entity using existing extension methods
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    /// <param name="restoredBy">User or system performing the restoration</param>
    /// <returns>Task representing the restore operation</returns>
    protected static async Task RestoreEntityAsync(T entity, string restoredBy = "System")
    {
        await entity.RestoreAsync(restoredBy);
    }
}
