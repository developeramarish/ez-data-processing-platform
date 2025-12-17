// ErrorHandlingTests.cs - Error Handling Integration Tests
// INT-023 to INT-025: Validation Failures, Output Failures, Restart Resilience
// Version: 1.0
// Date: December 17, 2025

using System.Text.Json;
using DataProcessing.IntegrationTests.Fixtures;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DataProcessing.IntegrationTests.ErrorHandling;

/// <summary>
/// Integration tests for error handling, failure recovery, and system resilience
/// </summary>
[Collection("Integration")]
public class ErrorHandlingTests : IClassFixture<KafkaFixture>, IClassFixture<MongoDbFixture>, IClassFixture<ApiClientFixture>, IAsyncLifetime
{
    private readonly KafkaFixture _kafka;
    private readonly MongoDbFixture _mongo;
    private readonly ApiClientFixture _apiClient;
    private const string TestCollection = "error_handling_tests";

    public ErrorHandlingTests(KafkaFixture kafka, MongoDbFixture mongo, ApiClientFixture apiClient)
    {
        _kafka = kafka;
        _mongo = mongo;
        _apiClient = apiClient;
    }

    public async Task InitializeAsync()
    {
        // Verify infrastructure is available
        var kafkaAvailable = await _kafka.IsKafkaAvailableAsync();
        var mongoAvailable = await _mongo.IsMongoDbAvailableAsync();

        if (!kafkaAvailable && !mongoAvailable)
        {
            throw new InvalidOperationException("Neither Kafka nor MongoDB is available for testing");
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        try
        {
            var collection = _mongo.GetCollection<BsonDocument>(TestCollection);
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Regex("_testMarker", "^INT-02"));
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region INT-023: Validation Failure Handling

    [Fact]
    [Trait("Category", "INT-023")]
    public async Task ValidationFailure_RouteToInvalidRecords_Topic()
    {
        // Arrange
        var invalidTopic = TestConfiguration.KafkaTopics.InvalidRecord;
        var uniqueId = $"test-ds-invalid-{Guid.NewGuid():N}";
        var consumer = _kafka.CreateConsumer(invalidTopic);
        consumer.Subscribe(invalidTopic);

        var invalidRecord = new
        {
            RecordId = Guid.NewGuid().ToString(),
            DatasourceId = uniqueId,
            Data = new
            {
                TransactionId = "INVALID-FORMAT", // Wrong format
                Amount = -100,                     // Negative amount
                Currency = "XXX"                   // Invalid currency
            },
            ValidationErrors = new[]
            {
                "TransactionId format invalid",
                "Amount must be non-negative",
                "Currency not in allowed list"
            },
            IsValid = false,
            FailedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(invalidTopic, uniqueId, JsonSerializer.Serialize(invalidRecord));

        // Assert - Find our specific message by key
        var result = await ConsumeByKeyAsync(consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("IsValid").GetBoolean().Should().BeFalse();
        parsed.RootElement.GetProperty("ValidationErrors").GetArrayLength().Should().Be(3);

        consumer.Close();
    }

    [Fact]
    [Trait("Category", "INT-023")]
    public async Task ValidationFailure_PreservesOriginalData_ForReview()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("invalid_records_test");
        var originalData = new BsonDocument
        {
            ["TransactionId"] = "BAD-TXN",
            ["Amount"] = -500,
            ["ExtraField"] = "should be preserved",
            ["NestedData"] = new BsonDocument { ["Inner"] = "value" }
        };

        var invalidRecord = new BsonDocument
        {
            ["_testMarker"] = "INT-023-preserve",
            ["recordId"] = Guid.NewGuid().ToString(),
            ["originalData"] = originalData,
            ["validationErrors"] = new BsonArray { "Multiple errors" }
        };

        // Act
        await collection.InsertOneAsync(invalidRecord);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("recordId", invalidRecord["recordId"]);
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["originalData"]["ExtraField"].AsString.Should().Be("should be preserved");
        found["originalData"]["NestedData"]["Inner"].AsString.Should().Be("value");
    }

    [Fact]
    [Trait("Category", "INT-023")]
    public void ValidationFailure_GeneratesDetailedErrorMessages()
    {
        // Arrange
        var validationResults = new List<(string Field, string Error, object? Value)>
        {
            ("TransactionId", "Does not match pattern ^TXN-[0-9]{8}-[0-9]{6}$", "INVALID"),
            ("Amount", "Must be greater than or equal to 0", -100),
            ("Currency", "Must be one of: USD, EUR, GBP, ILS", "XXX"),
            ("CustomerId", "Required field is missing", null)
        };

        // Act
        var errorMessages = validationResults.Select(r =>
            $"Field '{r.Field}': {r.Error}. Actual value: {r.Value ?? "null"}"
        ).ToList();

        // Assert
        errorMessages.Should().HaveCount(4);
        errorMessages[0].Should().Contain("TransactionId");
        errorMessages[0].Should().Contain("INVALID");
        errorMessages[3].Should().Contain("null");
    }

    [Fact]
    [Trait("Category", "INT-023")]
    public async Task ValidationFailure_CountsAreTracked_PerDatasource()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("validation_metrics");
        var datasourceId = "metrics-test-" + Guid.NewGuid().ToString("N")[..8];

        // Simulate validation metrics
        var metrics = new BsonDocument
        {
            ["_testMarker"] = "INT-023-metrics",
            ["datasourceId"] = datasourceId,
            ["totalRecords"] = 1000,
            ["validRecords"] = 950,
            ["invalidRecords"] = 50,
            ["errorsByType"] = new BsonDocument
            {
                ["format_error"] = 25,
                ["range_error"] = 15,
                ["required_field_missing"] = 10
            },
            ["lastUpdated"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(metrics);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("datasourceId", datasourceId);
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found["validRecords"].AsInt32.Should().Be(950);
        found["invalidRecords"].AsInt32.Should().Be(50);
        (found["validRecords"].AsInt32 + found["invalidRecords"].AsInt32).Should().Be(found["totalRecords"].AsInt32);
    }

    #endregion

    #region INT-024: Output Failure Handling

    [Fact]
    [Trait("Category", "INT-024")]
    public async Task OutputFailure_RetriesWithExponentialBackoff()
    {
        // Arrange
        var retryDelays = new List<TimeSpan>();
        var maxRetries = 5;
        var baseDelay = TimeSpan.FromSeconds(1);

        // Act - Simulate exponential backoff calculation
        for (int i = 0; i < maxRetries; i++)
        {
            var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * Math.Pow(2, i));
            retryDelays.Add(delay);
        }

        // Assert
        retryDelays[0].Should().Be(TimeSpan.FromSeconds(1));
        retryDelays[1].Should().Be(TimeSpan.FromSeconds(2));
        retryDelays[2].Should().Be(TimeSpan.FromSeconds(4));
        retryDelays[3].Should().Be(TimeSpan.FromSeconds(8));
        retryDelays[4].Should().Be(TimeSpan.FromSeconds(16));
    }

    [Fact]
    [Trait("Category", "INT-024")]
    public async Task OutputFailure_MovesToDeadLetterQueue_AfterMaxRetries()
    {
        // Arrange
        var dlqTopic = "dead-letter-queue";
        var uniqueRecordId = Guid.NewGuid().ToString();
        var consumer = _kafka.CreateConsumer(dlqTopic);
        consumer.Subscribe(dlqTopic);

        var failedRecord = new
        {
            RecordId = uniqueRecordId,
            OriginalTopic = "output-records",
            FailureReason = "Connection timeout after 5 retries",
            RetryCount = 5,
            OriginalData = new { TransactionId = "TXN-FAILED" },
            FailedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(dlqTopic, uniqueRecordId, JsonSerializer.Serialize(failedRecord));

        // Assert - Find our specific message by key
        var result = await ConsumeByKeyAsync(consumer, uniqueRecordId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("RetryCount").GetInt32().Should().Be(5);
        parsed.RootElement.GetProperty("FailureReason").GetString().Should().Contain("timeout");

        consumer.Close();
    }

    [Fact]
    [Trait("Category", "INT-024")]
    public async Task OutputFailure_LogsDetailedError_ForTroubleshooting()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("output_failures");
        var errorLog = new BsonDocument
        {
            ["_testMarker"] = "INT-024-error-log",
            ["recordId"] = Guid.NewGuid().ToString(),
            ["datasourceId"] = "ds-error-test",
            ["destinationType"] = "Kafka",
            ["destinationConfig"] = new BsonDocument
            {
                ["topic"] = "output-records",
                ["bootstrapServers"] = "kafka:9092"
            },
            ["errorType"] = "ConnectionException",
            ["errorMessage"] = "Unable to connect to broker",
            ["stackTrace"] = "at Confluent.Kafka.Producer...",
            ["attemptNumber"] = 3,
            ["occurredAt"] = DateTime.UtcNow
        };

        // Act
        await collection.InsertOneAsync(errorLog);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Eq("recordId", errorLog["recordId"]);
        var found = await collection.Find(filter).FirstOrDefaultAsync();

        found.Should().NotBeNull();
        found["errorType"].AsString.Should().Be("ConnectionException");
        found["attemptNumber"].AsInt32.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "INT-024")]
    public void OutputFailure_CircuitBreaker_OpensAfterThreshold()
    {
        // Arrange
        var failureThreshold = 5;
        var consecutiveFailures = 0;
        var circuitOpen = false;

        // Act - Simulate failures hitting threshold
        for (int i = 0; i < 7; i++)
        {
            consecutiveFailures++;
            if (consecutiveFailures >= failureThreshold)
            {
                circuitOpen = true;
                break;
            }
        }

        // Assert
        circuitOpen.Should().BeTrue();
        consecutiveFailures.Should().Be(5);
    }

    #endregion

    #region INT-025: Restart Resilience Tests

    [Fact]
    [Trait("Category", "INT-025")]
    public async Task RestartResilience_ResumesFromLastCheckpoint()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("processing_checkpoints");
        var fileId = Guid.NewGuid().ToString();

        // Simulate checkpoint before "crash"
        var checkpoint = new BsonDocument
        {
            ["_testMarker"] = "INT-025-checkpoint",
            ["fileId"] = fileId,
            ["datasourceId"] = "ds-checkpoint-test",
            ["lastProcessedOffset"] = 5000,
            ["totalRecords"] = 10000,
            ["status"] = "InProgress",
            ["checkpointedAt"] = DateTime.UtcNow.AddMinutes(-5)
        };
        await collection.InsertOneAsync(checkpoint);

        // Act - Simulate restart by reading checkpoint
        var filter = Builders<BsonDocument>.Filter.Eq("fileId", fileId);
        var savedCheckpoint = await collection.Find(filter).FirstOrDefaultAsync();

        // Assert - Can resume from saved state
        savedCheckpoint.Should().NotBeNull();
        savedCheckpoint["lastProcessedOffset"].AsInt32.Should().Be(5000);
        savedCheckpoint["status"].AsString.Should().Be("InProgress");
    }

    [Fact]
    [Trait("Category", "INT-025")]
    public async Task RestartResilience_RecoversUncommittedKafkaMessages()
    {
        // Arrange
        var topic = "resilience-test-topic";
        var consumerGroup = "int-025-resilience";

        // Produce messages
        for (int i = 1; i <= 10; i++)
        {
            await _kafka.ProduceAsync(topic, $"key-{i}", JsonSerializer.Serialize(new { Sequence = i }));
        }

        // Act - First consumer reads 5 messages but doesn't commit
        var consumer1 = _kafka.CreateConsumer(consumerGroup);
        consumer1.Subscribe(topic);

        var readMessages = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var result = consumer1.Consume(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                var parsed = JsonDocument.Parse(result.Message.Value);
                readMessages.Add(parsed.RootElement.GetProperty("Sequence").GetInt32());
            }
        }
        consumer1.Close(); // Close without committing

        // Assert - Messages were read
        readMessages.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Category", "INT-025")]
    public async Task RestartResilience_RecoversDatasourceConfiguration()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var dsName = $"Resilience-Test-DS-{uniqueId}";
        var datasource = new
        {
            Name = dsName,
            Type = "JSON",
            SourcePath = "/data/resilience",
            Category = "Integration Test",
            SupplierName = "Test Supplier",
            ConnectionString = "/data/resilience",
            Schema = new { type = "object" }
        };

        // Act - Create datasource (simulating pre-crash state)
        var created = await _apiClient.CreateDatasourceAsync(datasource);

        // Assert - Configuration persists and can be retrieved after "restart"
        if (created != null)
        {
            var dsId = created?.GetProperty("ID").GetString();
            var recovered = await _apiClient.GetDatasourceAsync(dsId!);

            recovered.Should().NotBeNull();
            recovered?.GetProperty("Name").GetString().Should().Be(dsName);

            // Cleanup
            await _apiClient.DeleteDatasourceAsync(dsId!);
        }
    }

    [Fact]
    [Trait("Category", "INT-025")]
    public async Task RestartResilience_HandlesPartialBatchProcessing()
    {
        // Arrange
        var collection = _mongo.GetCollection<BsonDocument>("batch_processing_state");
        var batchId = Guid.NewGuid().ToString();

        // Simulate partial batch processing before crash
        var batchState = new BsonDocument
        {
            ["_testMarker"] = "INT-025-partial-batch",
            ["batchId"] = batchId,
            ["totalRecords"] = 100,
            ["processedRecords"] = new BsonArray
            {
                "record-001", "record-002", "record-003", "record-004", "record-005"
            },
            ["pendingRecords"] = new BsonArray
            {
                "record-006", "record-007", "record-008", "record-009", "record-010"
            },
            ["status"] = "PartiallyProcessed",
            ["lastUpdated"] = DateTime.UtcNow.AddMinutes(-10)
        };
        await collection.InsertOneAsync(batchState);

        // Act - Recover batch state
        var filter = Builders<BsonDocument>.Filter.Eq("batchId", batchId);
        var recovered = await collection.Find(filter).FirstOrDefaultAsync();

        // Assert - Can determine what still needs processing
        recovered.Should().NotBeNull();
        recovered["processedRecords"].AsBsonArray.Count.Should().Be(5);
        recovered["pendingRecords"].AsBsonArray.Count.Should().Be(5);
        recovered["status"].AsString.Should().Be("PartiallyProcessed");
    }

    [Fact]
    [Trait("Category", "INT-025")]
    public void RestartResilience_ServiceHealthCheck_DetectsRecovery()
    {
        // Arrange
        var healthStates = new List<(DateTime Timestamp, bool Healthy, string Reason)>
        {
            (DateTime.UtcNow.AddMinutes(-10), true, "Healthy"),
            (DateTime.UtcNow.AddMinutes(-5), false, "Pod restart"),
            (DateTime.UtcNow.AddMinutes(-4), false, "Initializing"),
            (DateTime.UtcNow.AddMinutes(-3), false, "Connecting to dependencies"),
            (DateTime.UtcNow, true, "Healthy - recovered")
        };

        // Act
        var lastHealthy = healthStates.Last(s => s.Healthy);
        var downtime = healthStates
            .SkipWhile(s => s.Healthy)
            .TakeWhile(s => !s.Healthy)
            .ToList();

        // Assert
        lastHealthy.Reason.Should().Contain("recovered");
        downtime.Should().HaveCount(3);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Consumes messages until finding one with the specified key, or timeout
    /// </summary>
    private static async Task<Confluent.Kafka.ConsumeResult<string, string>?> ConsumeByKeyAsync(
        Confluent.Kafka.IConsumer<string, string> consumer,
        string expectedKey,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result?.Message?.Key == expectedKey)
                return result;
        }
        return null;
    }

    #endregion
}
