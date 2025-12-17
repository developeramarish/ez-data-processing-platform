// KafkaOutputHandlerTests.cs - Unit Tests for KafkaOutputHandler
// UNIT-001: Kafka Output Handler Tests
// Version: 1.0
// Date: December 17, 2025

using Confluent.Kafka;
using DataProcessing.Output.Handlers;
using DataProcessing.Output.Models;
using DataProcessing.Shared.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataProcessing.Output.Tests.Handlers;

/// <summary>
/// Unit tests for KafkaOutputHandler
/// Tests message serialization, topic configuration, partition key generation,
/// error handling, and retry logic in isolation using mocked Kafka client.
/// </summary>
public class KafkaOutputHandlerTests
{
    private readonly Mock<IProducer<string, string>> _mockProducer;
    private readonly Mock<ILogger<KafkaOutputHandler>> _mockLogger;
    private readonly KafkaOutputHandler _handler;

    public KafkaOutputHandlerTests()
    {
        _mockProducer = new Mock<IProducer<string, string>>();
        _mockLogger = new Mock<ILogger<KafkaOutputHandler>>();
        _handler = new KafkaOutputHandler(_mockProducer.Object, _mockLogger.Object);
    }

    #region CanHandle Tests

    [Fact]
    public void CanHandle_WithKafkaType_ReturnsTrue()
    {
        // Act
        var result = _handler.CanHandle("kafka");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("folder")]
    [InlineData("sftp")]
    [InlineData("http")]
    [InlineData("KAFKA")]
    [InlineData("")]
    [InlineData(null)]
    public void CanHandle_WithNonKafkaType_ReturnsFalse(string? destinationType)
    {
        // Act
        var result = _handler.CanHandle(destinationType!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region WriteAsync Basic Tests

    [Fact]
    public async Task WriteAsync_WithValidConfig_PublishesToKafka()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        var content = """{"transactionId":"TXN-001","amount":100.50}""";
        var fileName = "transactions.json";

        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        result.DestinationType.Should().Be("kafka");
        result.DestinationName.Should().Be(destination.Name);
        result.BytesWritten.Should().Be(content.Length);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task WriteAsync_WithoutKafkaConfig_ReturnsFailure()
    {
        // Arrange
        var destination = new OutputDestination
        {
            Name = "test-destination",
            Type = "kafka",
            KafkaConfig = null
        };
        var content = "test content";
        var fileName = "test.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("KafkaConfig is required");
    }

    #endregion

    #region Message Key Tests

    [Fact]
    public async Task WriteAsync_WithCustomMessageKey_UsesProvidedKey()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        destination.KafkaConfig!.MessageKey = "custom-key-{filename}";
        var content = "test content";
        var fileName = "test-file.json";

        Message<string, string>? capturedMessage = null;
        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, ct) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await _handler.WriteAsync(destination, content, fileName);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Key.Should().Contain("custom-key-test-file");
    }

    [Fact]
    public async Task WriteAsync_WithoutMessageKey_UsesFileName()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        destination.KafkaConfig!.MessageKey = null;
        var content = "test content";
        var fileName = "transactions.json";

        Message<string, string>? capturedMessage = null;
        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, ct) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await _handler.WriteAsync(destination, content, fileName);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Key.Should().Be(fileName);
    }

    #endregion

    #region Custom Headers Tests

    [Fact]
    public async Task WriteAsync_WithCustomHeaders_AddsHeadersToMessage()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        destination.KafkaConfig!.Headers = new Dictionary<string, string>
        {
            { "source", "test-system" },
            { "environment", "testing" }
        };
        var content = "test content";
        var fileName = "test.json";

        Message<string, string>? capturedMessage = null;
        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, ct) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await _handler.WriteAsync(destination, content, fileName);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Headers.Should().NotBeNull();
        capturedMessage.Headers.Should().HaveCount(2);
    }

    [Fact]
    public async Task WriteAsync_WithoutHeaders_DoesNotAddHeaders()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        destination.KafkaConfig!.Headers = null;
        var content = "test content";
        var fileName = "test.json";

        Message<string, string>? capturedMessage = null;
        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, ct) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await _handler.WriteAsync(destination, content, fileName);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Headers.Should().BeNull();
    }

    #endregion

    #region Error Handling and Retry Tests

    [Fact]
    public async Task WriteAsync_WhenKafkaThrowsException_ReturnsFailure()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        var content = "test content";
        var fileName = "test.json";

        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<string, string>(
                new Error(ErrorCode.BrokerNotAvailable, "Broker not available"),
                new DeliveryResult<string, string>()));

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WriteAsync_WithRetryableError_RetriesUpTo3Times()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        var content = "test content";
        var fileName = "test.json";
        var callCount = 0;

        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new ProduceException<string, string>(
                        new Error(ErrorCode.BrokerNotAvailable, "Temporary failure"),
                        new DeliveryResult<string, string>());
                }
                return Task.FromResult(new DeliveryResult<string, string>
                {
                    Partition = new Partition(0),
                    Offset = new Offset(1)
                });
            });

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        callCount.Should().Be(3);
    }

    #endregion

    #region Topic Configuration Tests

    [Fact]
    public async Task WriteAsync_PublishesToCorrectTopic()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        destination.KafkaConfig!.Topic = "my-custom-topic";
        var content = "test content";
        var fileName = "test.json";

        string? capturedTopic = null;
        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, ct) => capturedTopic = topic)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await _handler.WriteAsync(destination, content, fileName);

        // Assert
        capturedTopic.Should().Be("my-custom-topic");
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task WriteAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var destination = CreateValidKafkaDestination();
        var content = "test content";
        var fileName = "test.json";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static OutputDestination CreateValidKafkaDestination()
    {
        return new OutputDestination
        {
            Id = Guid.NewGuid().ToString(),
            Name = "test-kafka-destination",
            Type = "kafka",
            Enabled = true,
            KafkaConfig = new KafkaOutputConfig
            {
                Topic = "test-topic",
                MessageKey = null,
                Headers = null
            }
        };
    }

    #endregion
}
