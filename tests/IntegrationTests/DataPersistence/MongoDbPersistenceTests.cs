// MongoDbPersistenceTests.cs - MongoDB Data Persistence Integration Tests
// INT-009 to INT-014: CRUD Operations, Data Recovery, Collection Management
// Version: 1.0
// Date: December 17, 2025

using System.Text.Json;
using DataProcessing.IntegrationTests.Fixtures;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DataProcessing.IntegrationTests.DataPersistence;

/// <summary>
/// Integration tests for MongoDB data persistence across services
/// </summary>
[Collection("Integration")]
public class MongoDbPersistenceTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly MongoDbFixture _mongo;
    private const string TestCollection = "integration_test_records";

    public MongoDbPersistenceTests(MongoDbFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        var isAvailable = await _mongo.IsMongoDbAvailableAsync();
        if (!isAvailable)
        {
            throw new InvalidOperationException(
                "MongoDB is not available. Ensure port-forwarding is active: kubectl port-forward svc/mongodb 27017:27017 -n ez-platform");
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up test collection
        try
        {
            var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Regex("_testMarker", "^INT-"));
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region INT-009: Basic CRUD Operations

    [Fact]
    [Trait("Category", "INT-009")]
    public async Task MongoDB_Insert_SingleDocument_Succeeds()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var document = new BsonDocument
        {
            ["_testMarker"] = "INT-009-insert",
            ["transactionId"] = "TXN-20251201-000001",
            ["amount"] = 1500.50,
            ["currency"] = "USD",
            ["createdAt"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(document);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("transactionId", "TXN-20251201-000001");
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["amount"].AsDouble.Should().Be(1500.50);
    }

    [Fact]
    [Trait("Category", "INT-009")]
    public async Task MongoDB_Insert_BatchDocuments_Succeeds()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var documents = Enumerable.Range(1, 10).Select(i => new BsonDocument
        {
            ["_testMarker"] = "INT-009-batch",
            ["transactionId"] = $"TXN-20251201-{i:D6}",
            ["amount"] = i * 100.0,
            ["batchId"] = "test-batch-001"
        }).ToList();

        // Act
        await collection.InsertManyAsync(documents);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("batchId", "test-batch-001");
        var count = await collection.CountDocumentsAsync(filter);

        count.Should().Be(10);
    }

    [Fact]
    [Trait("Category", "INT-009")]
    public async Task MongoDB_Read_ByFilter_ReturnsCorrectDocuments()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_testMarker"] = "INT-009-read",
            ["transactionId"] = "TXN-READ-TEST",
            ["status"] = "Completed",
            ["amount"] = 500.0
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("status", "Completed"),
            Builders<BsonDocument>.Filter.Gte("amount", 100.0)
        );
        var results = await collection.Find(filter).ToListAsync();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().OnlyContain(doc => doc["status"].AsString == "Completed");
    }

    [Fact]
    [Trait("Category", "INT-009")]
    public async Task MongoDB_Update_SingleDocument_Succeeds()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var docId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = docId,
            ["_testMarker"] = "INT-009-update",
            ["status"] = "Pending",
            ["amount"] = 100.0
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("_id", docId);
        var update = Builders<BsonDocument>.Update
            .Set("status", "Completed")
            .Set("completedAt", DateTime.UtcNow);

        var result = await collection.UpdateOneAsync(filter, update);

        // Assert
        result.ModifiedCount.Should().Be(1);

        var updated = await collection.Find(filter).FirstOrDefaultAsync();
        updated["status"].AsString.Should().Be("Completed");
        updated.Contains("completedAt").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "INT-009")]
    public async Task MongoDB_Delete_SingleDocument_Succeeds()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var docId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = docId,
            ["_testMarker"] = "INT-009-delete",
            ["toDelete"] = true
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("_id", docId);
        var result = await collection.DeleteOneAsync(filter);

        // Assert
        result.DeletedCount.Should().Be(1);

        var afterDelete = await collection.Find(filter).FirstOrDefaultAsync();
        afterDelete.Should().BeNull();
    }

    #endregion

    #region INT-010: Invalid Records Persistence

    [Fact]
    [Trait("Category", "INT-010")]
    public async Task InvalidRecords_ArePersisted_ToMongoDB()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("invalid_records");
        var invalidRecord = new BsonDocument
        {
            ["_testMarker"] = "INT-010-invalid",
            ["datasourceId"] = "test-ds-invalid",
            ["recordId"] = Guid.NewGuid().ToString(),
            ["originalData"] = new BsonDocument
            {
                ["TransactionId"] = "INVALID",
                ["Amount"] = -100
            },
            ["validationErrors"] = new BsonArray { "Invalid TransactionId format", "Amount cannot be negative" },
            ["createdAt"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(invalidRecord);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("recordId", invalidRecord["recordId"]);
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["validationErrors"].AsBsonArray.Count.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "INT-010")]
    public async Task InvalidRecords_CanBeQueried_ByDatasource()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("invalid_records");
        var datasourceId = "test-ds-query-" + Guid.NewGuid().ToString("N")[..8];

        for (int i = 0; i < 5; i++)
        {
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_testMarker"] = "INT-010-query",
                ["datasourceId"] = datasourceId,
                ["recordId"] = Guid.NewGuid().ToString(),
                ["validationErrors"] = new BsonArray { $"Error {i}" }
            });
        }

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("datasourceId", datasourceId);
        var results = await collection.Find(filter).ToListAsync();

        // Assert
        results.Should().HaveCount(5);
    }

    #endregion

    #region INT-011: Datasource Configuration Persistence

    [Fact]
    [Trait("Category", "INT-011")]
    public async Task DatasourceConfig_IsPersisted_ToMongoDB()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("datasources");
        var datasource = new BsonDocument
        {
            ["_testMarker"] = "INT-011-config",
            ["name"] = "Test Datasource",
            ["type"] = "CSV",
            ["sourcePath"] = "/data/test",
            ["schema"] = new BsonDocument
            {
                ["type"] = "object",
                ["properties"] = new BsonDocument
                {
                    ["id"] = new BsonDocument { ["type"] = "string" }
                }
            },
            ["createdAt"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(datasource);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("name", "Test Datasource");
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["schema"]["type"].AsString.Should().Be("object");
    }

    [Fact]
    [Trait("Category", "INT-011")]
    public async Task DatasourceConfig_SchemaUpdates_ArePersisted()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("datasources");
        var docId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = docId,
            ["_testMarker"] = "INT-011-schema-update",
            ["name"] = "Schema Update Test",
            ["schema"] = new BsonDocument { ["version"] = "1.0" }
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("_id", docId);
        var update = Builders<BsonDocument>.Update.Set("schema", new BsonDocument
        {
            ["version"] = "2.0",
            ["type"] = "object",
            ["required"] = new BsonArray { "id", "name" }
        });
        await collection.UpdateOneAsync(filter, update);

        // Assert
        var updated = await collection.Find(filter).FirstOrDefaultAsync();
        updated["schema"]["version"].AsString.Should().Be("2.0");
    }

    #endregion

    #region INT-012: Processing State Persistence

    [Fact]
    [Trait("Category", "INT-012")]
    public async Task ProcessingState_IsPersisted_ForRecovery()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("processing_state");
        var state = new BsonDocument
        {
            ["_testMarker"] = "INT-012-state",
            ["datasourceId"] = "test-ds-state",
            ["fileId"] = Guid.NewGuid().ToString(),
            ["fileName"] = "large-file.csv",
            ["totalRecords"] = 10000,
            ["processedRecords"] = 5000,
            ["lastProcessedOffset"] = 5000,
            ["status"] = "InProgress",
            ["startedAt"] = DateTime.UtcNow.AddMinutes(-5),
            ["lastUpdated"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(state);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("fileId", state["fileId"]);
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["processedRecords"].AsInt32.Should().Be(5000);
        found["status"].AsString.Should().Be("InProgress");
    }

    [Fact]
    [Trait("Category", "INT-012")]
    public async Task ProcessingState_CanBeResumed_AfterInterruption()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("processing_state");
        var fileId = Guid.NewGuid().ToString();

        // Simulate interrupted processing
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_testMarker"] = "INT-012-resume",
            ["fileId"] = fileId,
            ["processedRecords"] = 3000,
            ["totalRecords"] = 10000,
            ["status"] = "Interrupted",
            ["lastUpdated"] = DateTime.UtcNow.AddMinutes(-10)
        });

        // Act - Simulate resume
        var filter = Builders<BsonDocument>.Filter.Eq("fileId", fileId);
        var update = Builders<BsonDocument>.Update
            .Set("status", "Resumed")
            .Set("resumedAt", DateTime.UtcNow);
        await collection.UpdateOneAsync(filter, update);

        // Assert
        var resumed = await collection.Find(filter).FirstOrDefaultAsync();
        resumed["status"].AsString.Should().Be("Resumed");
        resumed["processedRecords"].AsInt32.Should().Be(3000); // Should start from where it left off
    }

    #endregion

    #region INT-013: Data Integrity

    [Fact]
    [Trait("Category", "INT-013")]
    public async Task MongoDB_MaintainsDataIntegrity_OnConcurrentWrites()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var docId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = docId,
            ["_testMarker"] = "INT-013-concurrent",
            ["counter"] = 0
        });

        // Act - Concurrent increments
        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", docId);
            var update = Builders<BsonDocument>.Update.Inc("counter", 1);
            return collection.UpdateOneAsync(filter, update);
        });

        await Task.WhenAll(tasks);

        // Assert
        var filter2 = Builders<BsonDocument>.Filter.Eq("_id", docId);
        var result = await collection.Find(filter2).FirstOrDefaultAsync();
        result["counter"].AsInt32.Should().Be(10);
    }

    [Fact]
    [Trait("Category", "INT-013")]
    public async Task MongoDB_PreservesDocumentStructure_OnUpdate()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var docId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = docId,
            ["_testMarker"] = "INT-013-structure",
            ["nested"] = new BsonDocument
            {
                ["level1"] = new BsonDocument
                {
                    ["level2"] = "original"
                }
            },
            ["array"] = new BsonArray { 1, 2, 3 }
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("_id", docId);
        var update = Builders<BsonDocument>.Update.Set("nested.level1.level2", "updated");
        await collection.UpdateOneAsync(filter, update);

        // Assert
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        result["nested"]["level1"]["level2"].AsString.Should().Be("updated");
        result["array"].AsBsonArray.Should().HaveCount(3); // Array unchanged
    }

    #endregion

    #region INT-014: Query Performance

    [Fact]
    [Trait("Category", "INT-014")]
    public async Task MongoDB_IndexedQuery_PerformsWell()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var datasourceId = "perf-test-" + Guid.NewGuid().ToString("N")[..8];

        // Insert test data
        var documents = Enumerable.Range(1, 100).Select(i => new BsonDocument
        {
            ["_testMarker"] = "INT-014-perf",
            ["datasourceId"] = datasourceId,
            ["recordNumber"] = i,
            ["timestamp"] = DateTime.UtcNow.AddSeconds(-i)
        }).ToList();

        await collection.InsertManyAsync(documents);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var filter = Builders<BsonDocument>.Filter.Eq("datasourceId", datasourceId);
        var results = await collection.Find(filter).ToListAsync();
        sw.Stop();

        // Assert
        results.Should().HaveCount(100);
        sw.ElapsedMilliseconds.Should().BeLessThan(5000, "Query should complete within 5 seconds");
    }

    [Fact]
    [Trait("Category", "INT-014")]
    public async Task MongoDB_PaginatedQuery_ReturnsCorrectResults()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
        var batchId = "pagination-test-" + Guid.NewGuid().ToString("N")[..8];

        var documents = Enumerable.Range(1, 50).Select(i => new BsonDocument
        {
            ["_testMarker"] = "INT-014-pagination",
            ["batchId"] = batchId,
            ["sortOrder"] = i
        }).ToList();

        await collection.InsertManyAsync(documents);

        // Act - Get page 2 (items 11-20)
        var filter = Builders<BsonDocument>.Filter.Eq("batchId", batchId);
        var page2 = await collection.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("sortOrder"))
            .Skip(10)
            .Limit(10)
            .ToListAsync();

        // Assert
        page2.Should().HaveCount(10);
        page2.First()["sortOrder"].AsInt32.Should().Be(11);
        page2.Last()["sortOrder"].AsInt32.Should().Be(20);
    }

    #endregion
}
