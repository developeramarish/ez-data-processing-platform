// KafkaFlowTests.cs - Kafka Message Flow Integration Tests
// INT-001 to INT-004: Service-to-Service Kafka Communication
// Version: 1.0
// Date: December 17, 2025

using System.Text.Json;
using Confluent.Kafka;
using DataProcessing.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DataProcessing.IntegrationTests.ServiceIntegration;

/// <summary>
/// Integration tests for Kafka message flows between services
/// Tests the complete pipeline: FileDiscovery → FileProcessor → Validation → Output
/// </summary>
[Collection("Integration")]
public class KafkaFlowTests : IClassFixture<KafkaFixture>, IAsyncLifetime
{
    private readonly KafkaFixture _kafka;
    private IConsumer<string, string>? _consumer;

    public KafkaFlowTests(KafkaFixture kafka)
    {
        _kafka = kafka;
    }

    public async Task InitializeAsync()
    {
        // Verify Kafka is available before running tests
        var isAvailable = await _kafka.IsKafkaAvailableAsync();
        if (!isAvailable)
        {
            throw new InvalidOperationException(
                "Kafka is not available. Ensure port-forwarding is active: kubectl port-forward svc/kafka 9092:9092 -n ez-platform");
        }
    }

    public Task DisposeAsync()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        return Task.CompletedTask;
    }

    #region INT-001: FileDiscovery → FileProcessor Flow

    [Fact]
    [Trait("Category", "INT-001")]
    public async Task FileDiscovery_PublishesFileDiscoveredMessage_ToKafka()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.FileDiscovered;
        var uniqueId = $"test-ds-001-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var testMessage = new
        {
            DatasourceId = uniqueId,
            FilePath = "/data/test/sample.csv",
            FileName = "sample.csv",
            FileSize = 1024L,
            DiscoveredAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(testMessage));

        // Assert - Find our specific message by key
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();
        result!.Message.Value.Should().Contain(uniqueId);
        result.Message.Value.Should().Contain("sample.csv");
    }

    [Fact]
    [Trait("Category", "INT-001")]
    public async Task FileDiscovery_MessageFormat_IsValid()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.FileDiscovered;
        var uniqueId = $"test-ds-format-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var testMessage = new
        {
            DatasourceId = uniqueId,
            FilePath = "/data/test/format-test.json",
            FileName = "format-test.json",
            FileSize = 2048L,
            DiscoveredAt = DateTime.UtcNow.ToString("o"),
            ContentType = "application/json"
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(testMessage));
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));

        // Assert - Verify JSON structure
        result.Should().NotBeNull();
        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.TryGetProperty("DatasourceId", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("FilePath", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("FileName", out _).Should().BeTrue();
    }

    #endregion

    #region INT-002: FileProcessor → ValidationService Flow

    [Fact]
    [Trait("Category", "INT-002")]
    public async Task FileProcessor_PublishesRecordBatch_ToValidationTopic()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.RecordValidated;
        var uniqueId = $"test-ds-002-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var recordBatch = new
        {
            DatasourceId = uniqueId,
            BatchId = Guid.NewGuid().ToString(),
            Records = new[]
            {
                new { TransactionId = "TXN-20251201-000001", Amount = 100.50 },
                new { TransactionId = "TXN-20251201-000002", Amount = 200.75 }
            },
            RecordCount = 2,
            ProcessedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(recordBatch));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("RecordCount").GetInt32().Should().Be(2);
    }

    [Fact]
    [Trait("Category", "INT-002")]
    public async Task FileProcessor_HandlesLargeBatch_Successfully()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.RecordValidated;
        var uniqueId = $"test-ds-large-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var records = Enumerable.Range(1, 100)
            .Select(i => new { TransactionId = $"TXN-20251201-{i:D6}", Amount = i * 10.0 })
            .ToArray();

        var largeBatch = new
        {
            DatasourceId = uniqueId,
            BatchId = Guid.NewGuid().ToString(),
            Records = records,
            RecordCount = 100,
            ProcessedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(largeBatch));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(15));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("RecordCount").GetInt32().Should().Be(100);
    }

    #endregion

    #region INT-003: ValidationService → OutputService Flow

    [Fact]
    [Trait("Category", "INT-003")]
    public async Task ValidationService_PublishesValidRecords_ToOutputTopic()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.RecordOutput;
        var uniqueId = $"test-ds-003-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var validatedRecord = new
        {
            DatasourceId = uniqueId,
            RecordId = Guid.NewGuid().ToString(),
            Data = new
            {
                TransactionId = "TXN-20251201-000001",
                CustomerId = "CUST-1001",
                Amount = 1500.50,
                Currency = "USD",
                Status = "Completed"
            },
            IsValid = true,
            ValidationErrors = Array.Empty<string>(),
            ValidatedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(validatedRecord));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("IsValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "INT-003")]
    public async Task ValidationService_IncludesValidationMetadata_InOutput()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.RecordOutput;
        var uniqueId = $"test-ds-meta-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var validatedRecord = new
        {
            DatasourceId = uniqueId,
            RecordId = Guid.NewGuid().ToString(),
            SourceFile = "transactions.csv",
            RowNumber = 42,
            Data = new { TransactionId = "TXN-20251201-000042" },
            IsValid = true,
            SchemaVersion = "1.0",
            ValidatedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(validatedRecord));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.TryGetProperty("SourceFile", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("RowNumber", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("SchemaVersion", out _).Should().BeTrue();
    }

    #endregion

    #region INT-004: Invalid Records → InvalidRecordsService Flow

    [Fact]
    [Trait("Category", "INT-004")]
    public async Task ValidationService_PublishesInvalidRecords_ToInvalidTopic()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.InvalidRecord;
        var uniqueId = $"test-ds-004-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var invalidRecord = new
        {
            DatasourceId = uniqueId,
            RecordId = Guid.NewGuid().ToString(),
            Data = new
            {
                TransactionId = "INVALID-FORMAT",
                CustomerId = "BAD",
                Amount = -100,
                Currency = "XXX"
            },
            IsValid = false,
            ValidationErrors = new[]
            {
                "TransactionId does not match pattern ^TXN-[0-9]{8}-[0-9]{6}$",
                "CustomerId does not match pattern ^CUST-[0-9]{4}$",
                "Amount must be >= 0",
                "Currency must be one of: USD, EUR, GBP, ILS"
            },
            ValidatedAt = DateTime.UtcNow.ToString("o")
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(invalidRecord));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        parsed.RootElement.GetProperty("IsValid").GetBoolean().Should().BeFalse();
        parsed.RootElement.GetProperty("ValidationErrors").GetArrayLength().Should().Be(4);
    }

    [Fact]
    [Trait("Category", "INT-004")]
    public async Task InvalidRecords_PreserveOriginalData_ForReview()
    {
        // Arrange
        var topic = TestConfiguration.KafkaTopics.InvalidRecord;
        var uniqueId = $"test-ds-preserve-{Guid.NewGuid():N}";
        _consumer = _kafka.CreateConsumer(topic);
        _consumer.Subscribe(topic);

        var originalData = new
        {
            TransactionId = "BAD-TXN",
            CustomerId = "X",
            Amount = -999,
            ExtraField = "should be preserved"
        };

        var invalidRecord = new
        {
            DatasourceId = uniqueId,
            RecordId = Guid.NewGuid().ToString(),
            OriginalData = originalData,
            Data = originalData,
            IsValid = false,
            ValidationErrors = new[] { "Multiple validation errors" },
            SourceFile = "bad-data.csv",
            RowNumber = 99
        };

        // Act
        await _kafka.ProduceAsync(topic, uniqueId, JsonSerializer.Serialize(invalidRecord));

        // Assert
        var result = await ConsumeByKeyAsync(_consumer, uniqueId, TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result!.Message.Value);
        var data = parsed.RootElement.GetProperty("OriginalData");
        data.GetProperty("ExtraField").GetString().Should().Be("should be preserved");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Consumes messages until finding one with the specified key, or timeout
    /// </summary>
    private static async Task<ConsumeResult<string, string>?> ConsumeByKeyAsync(
        IConsumer<string, string> consumer,
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
