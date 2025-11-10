using MassTransit;
using DataProcessing.Shared.Messages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;

namespace DataProcessing.Shared.Consumers;

/// <summary>
/// Base consumer class for all Data Processing Platform message consumers
/// Provides correlation ID tracking, logging, metrics, and error handling
/// </summary>
/// <typeparam name="T">Message type that implements IDataProcessingMessage</typeparam>
public abstract class DataProcessingConsumerBase<T> : IConsumer<T> 
    where T : class, IDataProcessingMessage
{
    protected readonly ILogger Logger;
    private readonly Meter _meter;
    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesErrored;
    private readonly Histogram<double> _processingDuration;
    private readonly ActivitySource _activitySource;

    protected DataProcessingConsumerBase(ILogger logger)
    {
        Logger = logger;
        
        // Initialize activity source
        _activitySource = new ActivitySource("DataProcessing.Consumer", "1.0.0");
        
        // Initialize metrics
        _meter = new Meter("DataProcessing.Consumer", "1.0.0");
        _messagesProcessed = _meter.CreateCounter<long>(
            "dataprocessing_messages_processed_total",
            "messages", 
            "Total number of messages processed");
        _messagesErrored = _meter.CreateCounter<long>(
            "dataprocessing_messages_errored_total",
            "messages",
            "Total number of messages that resulted in errors");
        _processingDuration = _meter.CreateHistogram<double>(
            "dataprocessing_message_processing_duration_seconds",
            "seconds",
            "Duration of message processing in seconds");
    }

    /// <summary>
    /// Consumes and processes the message with correlation ID tracking and metrics
    /// </summary>
    public async Task Consume(ConsumeContext<T> context)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;
        var messageType = typeof(T).Name;
        var consumerType = GetType().Name;

        using var activity = _activitySource.StartActivity($"Consume{messageType}");
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("message-type", messageType);
        activity?.SetTag("consumer-type", consumerType);
        activity?.SetTag("published-by", message.PublishedBy);
        activity?.SetTag("message-version", message.MessageVersion.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogInformation("Processing message {MessageType} with correlation ID {CorrelationId} from {PublishedBy}",
                messageType, correlationId, message.PublishedBy);

            // Validate message
            await ValidateMessage(message, context);

            // Process the message
            await ProcessMessage(context);

            // Record success metrics
            stopwatch.Stop();
            var tags = new[]
            {
                new KeyValuePair<string, object?>("message_type", messageType),
                new KeyValuePair<string, object?>("consumer_type", consumerType),
                new KeyValuePair<string, object?>("published_by", message.PublishedBy),
                new KeyValuePair<string, object?>("status", "success")
            };

            _messagesProcessed.Add(1, tags);
            _processingDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);

            Logger.LogInformation("Successfully processed message {MessageType} with correlation ID {CorrelationId} in {ProcessingTime}ms",
                messageType, correlationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record error metrics
            var errorTags = new[]
            {
                new KeyValuePair<string, object?>("message_type", messageType),
                new KeyValuePair<string, object?>("consumer_type", consumerType),
                new KeyValuePair<string, object?>("published_by", message.PublishedBy),
                new KeyValuePair<string, object?>("status", "error"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name)
            };

            _messagesErrored.Add(1, errorTags);
            _processingDuration.Record(stopwatch.Elapsed.TotalSeconds, errorTags);

            Logger.LogError(ex, "Failed to process message {MessageType} with correlation ID {CorrelationId} after {ProcessingTime}ms",
                messageType, correlationId, stopwatch.ElapsedMilliseconds);

            // Handle the error
            await HandleProcessingError(ex, message, context);

            // Decide whether to throw or consume the message
            if (ShouldRetryOnError(ex))
            {
                throw; // This will trigger MassTransit retry mechanisms
            }

            // Log as consumed with error (message won't be retried)
            Logger.LogWarning("Message {MessageType} with correlation ID {CorrelationId} consumed with error and will not be retried",
                messageType, correlationId);
        }
    }

    /// <summary>
    /// Abstract method to be implemented by concrete consumers
    /// </summary>
    /// <param name="context">Message consumption context</param>
    /// <returns>Task representing the processing operation</returns>
    protected abstract Task ProcessMessage(ConsumeContext<T> context);

    /// <summary>
    /// Validates the incoming message (can be overridden for specific validation logic)
    /// </summary>
    /// <param name="message">The message to validate</param>
    /// <param name="context">Message consumption context</param>
    /// <returns>Task representing the validation operation</returns>
    protected virtual Task ValidateMessage(T message, ConsumeContext<T> context)
    {
        // Basic validation
        if (string.IsNullOrEmpty(message.CorrelationId))
        {
            throw new ArgumentException("Correlation ID is required", nameof(message));
        }

        if (message.Timestamp == default)
        {
            throw new ArgumentException("Timestamp is required", nameof(message));
        }

        if (string.IsNullOrEmpty(message.PublishedBy))
        {
            throw new ArgumentException("PublishedBy is required", nameof(message));
        }

        // Check message age (optional - can be overridden)
        var messageAge = DateTime.UtcNow - message.Timestamp;
        if (messageAge > TimeSpan.FromHours(24))
        {
            Logger.LogWarning("Processing old message {MessageType} with correlation ID {CorrelationId}, age: {MessageAge}",
                typeof(T).Name, message.CorrelationId, messageAge);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles processing errors (can be overridden for specific error handling)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="message">The message being processed</param>
    /// <param name="context">Message consumption context</param>
    /// <returns>Task representing the error handling operation</returns>
    protected virtual Task HandleProcessingError(Exception exception, T message, ConsumeContext<T> context)
    {
        // Default implementation logs the error details
        Logger.LogError(exception, "Processing error details - Message: {MessageType}, CorrelationId: {CorrelationId}, PublishedBy: {PublishedBy}, Exception: {ExceptionType}",
            typeof(T).Name, message.CorrelationId, message.PublishedBy, exception.GetType().Name);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the message should be retried on error (can be overridden)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <returns>True if the message should be retried, false to consume with error</returns>
    protected virtual bool ShouldRetryOnError(Exception exception)
    {
        return exception switch
        {
            // Don't retry on validation errors - most specific exceptions first
            ArgumentNullException => false,
            ArgumentException => false,
            InvalidOperationException when exception.Message.Contains("validation") => false,
            
            // Don't retry on permanent failures
            NotSupportedException => false,
            NotImplementedException => false,
            
            // Retry on temporary failures
            TimeoutException => true,
            TaskCanceledException => true,
            HttpRequestException => true,
            
            // Default to retry for unknown exceptions
            _ => true
        };
    }

    /// <summary>
    /// Gets the correlation ID from the current message context
    /// </summary>
    protected string GetCorrelationId(ConsumeContext<T> context)
    {
        return context.Message.CorrelationId;
    }

    /// <summary>
    /// Creates a child activity for tracking sub-operations
    /// </summary>
    protected Activity? StartChildActivity(string operationName, string correlationId)
    {
        var activity = _activitySource.StartActivity(operationName);
        activity?.SetTag("correlation-id", correlationId);
        activity?.SetTag("parent-consumer", GetType().Name);
        return activity;
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public virtual void Dispose()
    {
        _meter?.Dispose();
    }
}
