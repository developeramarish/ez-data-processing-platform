// MongoDbFixture.cs - MongoDB Integration Test Fixture
// EZ Platform Integration Tests
// Version: 1.0
// Date: December 17, 2025

using MongoDB.Bson;
using MongoDB.Driver;

namespace DataProcessing.IntegrationTests.Fixtures;

/// <summary>
/// Provides MongoDB client and database access for integration tests
/// </summary>
public class MongoDbFixture : IDisposable
{
    private readonly MongoClient _client;
    private bool _disposed;

    public IMongoDatabase Database { get; }

    public MongoDbFixture()
    {
        _client = new MongoClient(TestConfiguration.MongoDbConnectionString);
        Database = _client.GetDatabase(TestConfiguration.MongoDbDatabase);
    }

    /// <summary>
    /// Gets a typed collection
    /// </summary>
    public IMongoCollection<T> GetCollection<T>(string collectionName) =>
        Database.GetCollection<T>(collectionName);

    /// <summary>
    /// Gets a BsonDocument collection
    /// </summary>
    public IMongoCollection<BsonDocument> GetCollection(string collectionName) =>
        Database.GetCollection<BsonDocument>(collectionName);

    /// <summary>
    /// Checks if MongoDB is reachable
    /// </summary>
    public bool IsMongoDbAvailable()
    {
        try
        {
            Database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Async wrapper for IsMongoDbAvailable
    /// </summary>
    public async Task<bool> IsMongoDbAvailableAsync()
    {
        try
        {
            await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for a document to appear in a collection
    /// </summary>
    public async Task<BsonDocument?> WaitForDocumentAsync(
        string collectionName,
        FilterDefinition<BsonDocument> filter,
        TimeSpan timeout)
    {
        var collection = GetCollection(collectionName);
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var doc = await collection.Find(filter).FirstOrDefaultAsync();
            if (doc != null)
                return doc;

            await Task.Delay(500);
        }

        return null;
    }

    /// <summary>
    /// Counts documents matching a filter
    /// </summary>
    public async Task<long> CountDocumentsAsync(
        string collectionName,
        FilterDefinition<BsonDocument> filter)
    {
        var collection = GetCollection(collectionName);
        return await collection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Gets all documents from a collection with optional filter
    /// </summary>
    public async Task<List<BsonDocument>> GetDocumentsAsync(
        string collectionName,
        FilterDefinition<BsonDocument>? filter = null,
        int limit = 100)
    {
        var collection = GetCollection(collectionName);
        filter ??= Builders<BsonDocument>.Filter.Empty;

        return await collection
            .Find(filter)
            .Limit(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts a test document
    /// </summary>
    public async Task<BsonDocument> InsertTestDocumentAsync(
        string collectionName,
        BsonDocument document)
    {
        var collection = GetCollection(collectionName);
        await collection.InsertOneAsync(document);
        return document;
    }

    /// <summary>
    /// Deletes test documents
    /// </summary>
    public async Task<long> DeleteTestDocumentsAsync(
        string collectionName,
        FilterDefinition<BsonDocument> filter)
    {
        var collection = GetCollection(collectionName);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // MongoClient manages its own connection pool
            _disposed = true;
        }
    }
}
