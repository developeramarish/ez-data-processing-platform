// KafkaFixture.cs - Kafka Integration Test Fixture
// EZ Platform Integration Tests
// Version: 1.0
// Date: December 17, 2025

using Confluent.Kafka;

namespace DataProcessing.IntegrationTests.Fixtures;

/// <summary>
/// Provides Kafka producer and consumer for integration tests
/// Manages connections and message verification
/// </summary>
public class KafkaFixture : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private bool _disposed;

    public KafkaFixture()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = TestConfiguration.KafkaBootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <summary>
    /// Creates a consumer for a specific topic with a unique group ID
    /// </summary>
    public IConsumer<string, string> CreateConsumer(string topic, string? groupIdSuffix = null)
    {
        var groupId = $"integration-test-{topic}-{groupIdSuffix ?? Guid.NewGuid().ToString("N")[..8]}";

        var config = new ConsumerConfig
        {
            BootstrapServers = TestConfiguration.KafkaBootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,  // Use Earliest to avoid race condition with producer
            EnableAutoCommit = false
        };

        var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);
        return consumer;
    }

    /// <summary>
    /// Publishes a message to a Kafka topic
    /// </summary>
    public async Task<DeliveryResult<string, string>> ProduceAsync(
        string topic,
        string key,
        string value)
    {
        var message = new Message<string, string> { Key = key, Value = value };
        return await _producer.ProduceAsync(topic, message);
    }

    /// <summary>
    /// Consumes messages from a topic until a condition is met or timeout
    /// </summary>
    public async Task<List<ConsumeResult<string, string>>> ConsumeUntilAsync(
        IConsumer<string, string> consumer,
        Func<ConsumeResult<string, string>, bool> predicate,
        TimeSpan timeout,
        int maxMessages = 100)
    {
        var results = new List<ConsumeResult<string, string>>();
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline && results.Count < maxMessages)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result != null)
            {
                results.Add(result);
                if (predicate(result))
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Waits for a specific message to appear on a topic
    /// </summary>
    public async Task<ConsumeResult<string, string>?> WaitForMessageAsync(
        string topic,
        Func<ConsumeResult<string, string>, bool> predicate,
        TimeSpan timeout)
    {
        using var consumer = CreateConsumer(topic);
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result != null && predicate(result))
                return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the latest messages from a topic
    /// </summary>
    public async Task<List<ConsumeResult<string, string>>> GetLatestMessagesAsync(
        string topic,
        int count,
        TimeSpan timeout)
    {
        var results = new List<ConsumeResult<string, string>>();
        using var consumer = CreateConsumer(topic);
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline && results.Count < count)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result != null)
                results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Checks if Kafka is reachable
    /// </summary>
    public bool IsKafkaAvailable()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = TestConfiguration.KafkaBootstrapServers })
                .Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            return metadata.Brokers.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Async wrapper for IsKafkaAvailable
    /// </summary>
    public Task<bool> IsKafkaAvailableAsync() => Task.FromResult(IsKafkaAvailable());

    public void Dispose()
    {
        if (!_disposed)
        {
            _producer?.Dispose();
            _disposed = true;
        }
    }
}
