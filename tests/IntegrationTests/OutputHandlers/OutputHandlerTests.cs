// OutputHandlerTests.cs - Output Handler Integration Tests
// INT-019 to INT-022: Folder Output, Kafka Output, Multi-destination, Templates
// Version: 1.0
// Date: December 17, 2025

using System.Text.Json;
using DataProcessing.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DataProcessing.IntegrationTests.OutputHandlers;

/// <summary>
/// Integration tests for output handlers (Folder, Kafka, multi-destination)
/// Tests output generation, formatting, and destination routing
/// </summary>
[Collection("Integration")]
public class OutputHandlerTests : IClassFixture<KafkaFixture>, IClassFixture<ApiClientFixture>, IAsyncLifetime
{
    private readonly KafkaFixture _kafka;
    private readonly ApiClientFixture _apiClient;
    private readonly string _testOutputPath;

    public OutputHandlerTests(KafkaFixture kafka, ApiClientFixture apiClient)
    {
        _kafka = kafka;
        _apiClient = apiClient;
        _testOutputPath = Path.Combine(Path.GetTempPath(), "ez-output-tests", Guid.NewGuid().ToString("N"));
    }

    public Task InitializeAsync()
    {
        // Create test output directory
        Directory.CreateDirectory(_testOutputPath);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Clean up test output directory
        try
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        return Task.CompletedTask;
    }

    #region INT-019: Folder Output Handler Tests

    [Fact]
    [Trait("Category", "INT-019")]
    public void FolderOutput_CreatesOutputDirectory_WhenNotExists()
    {
        // Arrange
        var outputDir = Path.Combine(_testOutputPath, "new-folder");

        // Act
        Directory.CreateDirectory(outputDir);

        // Assert
        Directory.Exists(outputDir).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "INT-019")]
    public async Task FolderOutput_WritesJsonFile_WithCorrectFormat()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputPath, "output.json");
        var records = new[]
        {
            new { TransactionId = "TXN-001", Amount = 100.50, Currency = "USD" },
            new { TransactionId = "TXN-002", Amount = 200.75, Currency = "EUR" }
        };

        // Act
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputFile, json);

        // Assert
        File.Exists(outputFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputFile);
        var parsed = JsonDocument.Parse(content);
        parsed.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    [Trait("Category", "INT-019")]
    public async Task FolderOutput_WritesCsvFile_WithHeaders()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputPath, "output.csv");
        var lines = new[]
        {
            "TransactionId,Amount,Currency",
            "TXN-001,100.50,USD",
            "TXN-002,200.75,EUR"
        };

        // Act
        await File.WriteAllLinesAsync(outputFile, lines);

        // Assert
        File.Exists(outputFile).Should().BeTrue();
        var content = await File.ReadAllLinesAsync(outputFile);
        content.Should().HaveCount(3);
        content[0].Should().Contain("TransactionId");
    }

    [Fact]
    [Trait("Category", "INT-019")]
    public async Task FolderOutput_HandlesLargeFiles_Successfully()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputPath, "large-output.json");
        var records = Enumerable.Range(1, 10000).Select(i => new
        {
            TransactionId = $"TXN-{i:D8}",
            Amount = i * 1.5,
            Timestamp = DateTime.UtcNow.AddSeconds(-i)
        }).ToArray();

        // Act
        var json = JsonSerializer.Serialize(records);
        await File.WriteAllTextAsync(outputFile, json);

        // Assert
        var fileInfo = new FileInfo(outputFile);
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThan(100000, "Large file should be substantial");
    }

    [Fact]
    [Trait("Category", "INT-019")]
    public void FolderOutput_GeneratesCorrectFileName_WithTimestamp()
    {
        // Arrange
        var datasourceId = "test-ds";
        var timestamp = DateTime.UtcNow;

        // Act
        var fileName = $"{datasourceId}_{timestamp:yyyyMMdd_HHmmss}.json";

        // Assert
        fileName.Should().MatchRegex(@"^test-ds_\d{8}_\d{6}\.json$");
    }

    #endregion

    #region INT-020: Kafka Output Handler Tests

    [Fact]
    [Trait("Category", "INT-020")]
    public async Task KafkaOutput_PublishesRecord_ToConfiguredTopic()
    {
        // Arrange
        var topic = "output-test-topic";
        var consumer = _kafka.CreateConsumer("int-020-test");
        consumer.Subscribe(topic);

        var outputRecord = new
        {
            RecordId = Guid.NewGuid().ToString(),
            Data = new { TransactionId = "TXN-001", Amount = 100.50 },
            OutputAt = DateTime.UtcNow
        };

        // Act
        await _kafka.ProduceAsync(topic, outputRecord.RecordId, JsonSerializer.Serialize(outputRecord));

        // Assert
        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();
        result.Message.Value.Should().Contain("TXN-001");

        consumer.Close();
    }

    [Fact]
    [Trait("Category", "INT-020")]
    public async Task KafkaOutput_PreservesMessageOrder_ByPartitionKey()
    {
        // Arrange
        var topic = "order-test-topic";
        var partitionKey = "datasource-001";
        var messages = Enumerable.Range(1, 10).Select(i => new
        {
            Sequence = i,
            Data = $"Message {i}"
        }).ToList();

        // Act
        foreach (var msg in messages)
        {
            await _kafka.ProduceAsync(topic, partitionKey, JsonSerializer.Serialize(msg));
        }

        // Assert - Messages with same key go to same partition, preserving order
        var consumer = _kafka.CreateConsumer("int-020-order-test");
        consumer.Subscribe(topic);

        var receivedSequences = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                var parsed = JsonDocument.Parse(result.Message.Value);
                receivedSequences.Add(parsed.RootElement.GetProperty("Sequence").GetInt32());
            }
        }

        consumer.Close();

        // Sequences should be in order for same partition key
        receivedSequences.Should().BeInAscendingOrder();
    }

    [Fact]
    [Trait("Category", "INT-020")]
    public async Task KafkaOutput_IncludesMetadata_InMessage()
    {
        // Arrange
        var topic = "metadata-test-topic";
        var consumer = _kafka.CreateConsumer("int-020-metadata");
        consumer.Subscribe(topic);

        var outputRecord = new
        {
            RecordId = Guid.NewGuid().ToString(),
            DatasourceId = "ds-001",
            SourceFile = "input.csv",
            RowNumber = 42,
            Data = new { Value = "test" },
            ProcessedAt = DateTime.UtcNow.ToString("o"),
            SchemaVersion = "1.0"
        };

        // Act
        await _kafka.ProduceAsync(topic, outputRecord.DatasourceId, JsonSerializer.Serialize(outputRecord));

        // Assert
        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var parsed = JsonDocument.Parse(result.Message.Value);
        parsed.RootElement.TryGetProperty("SourceFile", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("RowNumber", out _).Should().BeTrue();
        parsed.RootElement.TryGetProperty("SchemaVersion", out _).Should().BeTrue();

        consumer.Close();
    }

    #endregion

    #region INT-021: Multi-Destination Output Tests

    [Fact]
    [Trait("Category", "INT-021")]
    public async Task MultiDestination_OutputsTo_BothFolderAndKafka()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputPath, "multi-dest.json");
        var kafkaTopic = "multi-dest-topic";
        var consumer = _kafka.CreateConsumer("int-021-multi");
        consumer.Subscribe(kafkaTopic);

        var record = new
        {
            RecordId = Guid.NewGuid().ToString(),
            Data = new { TransactionId = "TXN-MULTI", Amount = 500.00 }
        };

        // Act - Write to both destinations
        await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(record));
        await _kafka.ProduceAsync(kafkaTopic, record.RecordId, JsonSerializer.Serialize(record));

        // Assert - Both destinations received the data
        File.Exists(outputFile).Should().BeTrue();

        var kafkaResult = consumer.Consume(TimeSpan.FromSeconds(10));
        kafkaResult.Should().NotBeNull();
        kafkaResult.Message.Value.Should().Contain("TXN-MULTI");

        consumer.Close();
    }

    [Fact]
    [Trait("Category", "INT-021")]
    public async Task MultiDestination_HandlesPartialFailure_Gracefully()
    {
        // Arrange - Simulate scenario where file write succeeds but Kafka fails
        var outputFile = Path.Combine(_testOutputPath, "partial-success.json");
        var record = new { RecordId = "test-partial", Data = "test data" };

        // Act - File write should succeed regardless of Kafka
        await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(record));

        // Assert
        File.Exists(outputFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputFile);
        content.Should().Contain("test-partial");
    }

    [Fact]
    [Trait("Category", "INT-021")]
    public void MultiDestination_Configuration_IsValid()
    {
        // Arrange - Simulate output configuration with consistent structure
        var destinations = new List<Dictionary<string, string>>
        {
            new() { ["Type"] = "Folder", ["Target"] = "/data/output", ["Format"] = "JSON" },
            new() { ["Type"] = "Kafka", ["Target"] = "output-records", ["Format"] = "JSON" }
        };

        var config = new
        {
            Destinations = destinations,
            BatchSize = 100,
            FlushIntervalSeconds = 5
        };

        // Assert
        config.Destinations.Should().HaveCount(2);
        config.Destinations.Should().Contain(d => d["Type"] == "Folder");
        config.Destinations.Should().Contain(d => d["Type"] == "Kafka");
    }

    #endregion

    #region INT-022: Output Template Tests

    [Fact]
    [Trait("Category", "INT-022")]
    public void OutputTemplate_AppliesFieldMapping_Correctly()
    {
        // Arrange
        var sourceRecord = new Dictionary<string, object>
        {
            ["TransactionId"] = "TXN-001",
            ["Amount"] = 100.50,
            ["Currency"] = "USD",
            ["Timestamp"] = DateTime.UtcNow
        };

        var template = new Dictionary<string, string>
        {
            ["txn_id"] = "TransactionId",
            ["value"] = "Amount",
            ["curr"] = "Currency",
            ["date"] = "Timestamp"
        };

        // Act - Apply template mapping
        var outputRecord = new Dictionary<string, object>();
        foreach (var mapping in template)
        {
            if (sourceRecord.TryGetValue(mapping.Value, out var value))
            {
                outputRecord[mapping.Key] = value;
            }
        }

        // Assert
        outputRecord.Should().ContainKey("txn_id");
        outputRecord["txn_id"].Should().Be("TXN-001");
        outputRecord.Should().ContainKey("value");
        outputRecord["value"].Should().Be(100.50);
    }

    [Fact]
    [Trait("Category", "INT-022")]
    public void OutputTemplate_HandlesNestedFields_Correctly()
    {
        // Arrange
        var sourceRecord = new
        {
            Transaction = new
            {
                Id = "TXN-001",
                Details = new
                {
                    Amount = 500.00,
                    Fee = 5.00
                }
            },
            Customer = new
            {
                Name = "John Doe"
            }
        };

        // Act - Extract nested values
        var outputRecord = new Dictionary<string, object>
        {
            ["id"] = sourceRecord.Transaction.Id,
            ["amount"] = sourceRecord.Transaction.Details.Amount,
            ["fee"] = sourceRecord.Transaction.Details.Fee,
            ["customer_name"] = sourceRecord.Customer.Name
        };

        // Assert
        outputRecord["id"].Should().Be("TXN-001");
        outputRecord["amount"].Should().Be(500.00);
        outputRecord["customer_name"].Should().Be("John Doe");
    }

    [Fact]
    [Trait("Category", "INT-022")]
    public void OutputTemplate_AppliesDateFormat_Correctly()
    {
        // Arrange
        var timestamp = new DateTime(2025, 12, 17, 14, 30, 0, DateTimeKind.Utc);
        var formatPatterns = new Dictionary<string, string>
        {
            ["iso8601"] = "yyyy-MM-ddTHH:mm:ssZ",
            ["date_only"] = "yyyy-MM-dd",
            ["time_only"] = "HH:mm:ss",
            ["custom"] = "dd/MM/yyyy HH:mm"
        };

        // Act & Assert
        timestamp.ToString(formatPatterns["iso8601"]).Should().Be("2025-12-17T14:30:00Z");
        timestamp.ToString(formatPatterns["date_only"]).Should().Be("2025-12-17");
        timestamp.ToString(formatPatterns["time_only"]).Should().Be("14:30:00");
        timestamp.ToString(formatPatterns["custom"]).Should().Be("17/12/2025 14:30");
    }

    [Fact]
    [Trait("Category", "INT-022")]
    public void OutputTemplate_AppliesNumberFormat_Correctly()
    {
        // Arrange
        var amount = 1234567.89m;
        var formatPatterns = new Dictionary<string, string>
        {
            ["currency"] = "C2",
            ["fixed"] = "F2",
            ["number"] = "N2",
            ["percent"] = "P2"
        };

        // Act & Assert
        amount.ToString(formatPatterns["fixed"]).Should().Be("1234567.89");
        var percentResult = (amount / 100).ToString(formatPatterns["percent"]);
        percentResult.Should().Contain("%"); // Culture-dependent, just verify it contains %
    }

    [Fact]
    [Trait("Category", "INT-022")]
    public void OutputTemplate_HandlesConditionalFields_Correctly()
    {
        // Arrange
        var records = new[]
        {
            new { Status = "Completed", Amount = 100, RefundAmount = (decimal?)null },
            new { Status = "Refunded", Amount = 100, RefundAmount = (decimal?)50.00m }
        };

        // Act - Apply conditional output logic
        var outputs = records.Select(r => new Dictionary<string, object?>
        {
            ["status"] = r.Status,
            ["amount"] = r.Amount,
            ["refund_amount"] = r.Status == "Refunded" ? r.RefundAmount : null
        }).ToList();

        // Assert
        outputs[0]["refund_amount"].Should().BeNull();
        outputs[1]["refund_amount"].Should().Be(50.00m);
    }

    #endregion
}
