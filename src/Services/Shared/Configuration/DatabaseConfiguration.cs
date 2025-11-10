using MongoDB.Entities;
using MongoDB.Driver;
using DataProcessing.Shared.Entities;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration and setup for MongoDB database connection and indexes
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Initialize MongoDB connection and configure indexes
    /// </summary>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="environment">Environment name (development, staging, production)</param>
    /// <param name="applicationName">Name of the application for connection tracking</param>
    public static async Task InitializeAsync(string connectionString, string environment = "development", string? applicationName = null)
    {
        var databaseName = $"data_processing_{environment.ToLowerInvariant()}";
        
        // Initialize MongoDB.Entities with connection settings
        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        if (!string.IsNullOrEmpty(applicationName))
        {
            clientSettings.ApplicationName = applicationName;
        }
        
        await DB.InitAsync(databaseName, clientSettings);
        
        // Configure indexes for optimal performance
        await ConfigureIndexesAsync();
    }

    /// <summary>
    /// Configure database indexes for all entities
    /// </summary>
    private static async Task ConfigureIndexesAsync()
    {
        // DataProcessingDataSource indexes
        await DB.Index<DataProcessingDataSource>()
            .Key(x => x.Name, KeyType.Ascending)
            .Option(o => o.Unique = true)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingDataSource>()
            .Key(x => x.SupplierName, KeyType.Ascending)
            .Key(x => x.Category, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingDataSource>()
            .Key(x => x.IsActive, KeyType.Ascending)
            .Key(x => x.IsDeleted, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingDataSource>()
            .Key(x => x.LastProcessedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        // DataProcessingValidationResult indexes
        await DB.Index<DataProcessingValidationResult>()
            .Key(x => x.DataSourceId, KeyType.Ascending)
            .Key(x => x.CreatedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingValidationResult>()
            .Key(x => x.FileName, KeyType.Ascending)
            .Key(x => x.ProcessingStartedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingValidationResult>()
            .Key(x => x.Status, KeyType.Ascending)
            .Key(x => x.IsDeleted, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingValidationResult>()
            .Key(x => x.CorrelationId, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        // DataProcessingInvalidRecord indexes
        await DB.Index<DataProcessingInvalidRecord>()
            .Key(x => x.DataSourceId, KeyType.Ascending)
            .Key(x => x.ValidationResultId, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingInvalidRecord>()
            .Key(x => x.ErrorType, KeyType.Ascending)
            .Key(x => x.Severity, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingInvalidRecord>()
            .Key(x => x.IsReviewed, KeyType.Ascending)
            .Key(x => x.IsIgnored, KeyType.Ascending)
            .Key(x => x.IsDeleted, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<DataProcessingInvalidRecord>()
            .Key(x => x.FileName, KeyType.Ascending)
            .Key(x => x.CreatedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        // Common indexes for all entities (audit and soft delete)
        await CreateCommonIndexesAsync<DataProcessingDataSource>();
        await CreateCommonIndexesAsync<DataProcessingValidationResult>();
        await CreateCommonIndexesAsync<DataProcessingInvalidRecord>();
    }

    /// <summary>
    /// Create common indexes for audit fields and soft delete functionality
    /// </summary>
    private static async Task CreateCommonIndexesAsync<T>() where T : DataProcessingBaseEntity
    {
        // Correlation ID index for tracing
        await DB.Index<T>()
            .Key(x => x.CorrelationId, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        // Soft delete index
        await DB.Index<T>()
            .Key(x => x.IsDeleted, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();

        // Audit fields indexes
        await DB.Index<T>()
            .Key(x => x.CreatedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<T>()
            .Key(x => x.UpdatedAt, KeyType.Descending)
            .Option(o => o.Background = true)
            .CreateAsync();

        await DB.Index<T>()
            .Key(x => x.CreatedBy, KeyType.Ascending)
            .Option(o => o.Background = true)
            .CreateAsync();
    }

    /// <summary>
    /// Create database collections explicitly (optional, for better control)
    /// </summary>
    public static void CreateCollections()
    {
        // Collections will be created automatically when first document is inserted
        // This method can be used for explicit creation if needed
        _ = DB.Collection<DataProcessingDataSource>();
        _ = DB.Collection<DataProcessingValidationResult>();
        _ = DB.Collection<DataProcessingInvalidRecord>();
    }

    /// <summary>
    /// Get database statistics and health information
    /// </summary>
    public static async Task<object> GetDatabaseStatsAsync()
    {
        var db = DB.Database<DataProcessingBaseEntity>();
        var stats = await db.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("dbStats", 1));
            
        return new
        {
            DatabaseName = db.DatabaseNamespace.DatabaseName,
            Collections = stats["collections"].ToInt32(),
            DataSize = stats["dataSize"].ToInt64(),
            StorageSize = stats["storageSize"].ToInt64(),
            IndexSize = stats["indexSize"].ToInt64(),
            Objects = stats["objects"].ToInt64()
        };
    }
}
