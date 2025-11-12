// KafkaOutputHandler.cs - Kafka Output Handler
// Task-20: Multi-Destination Output Service  
// Version: 1.0
// Date: November 12, 2025

using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using DataProcessing.Shared.Entities;

namespace OutputService.Handlers;

/// <summary>
/// Handles output to Kafka topics
/// </summary>
public class KafkaOutputHandler : IOutputHandler
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaOutputHandler> _logger;
    
    public KafkaOutputHandler(
        IProducer<string, string> producer,
        ILogger<KafkaOutputHandler> logger)
    {
        _producer = producer;
        _logger = logger;
    }
    
    public bool CanHandle(string destinationType) => destinationType == "kafka";
    
    public async Task<OutputResult> WriteAsync(
        OutputDestination destination,
        string content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (destination.KafkaConfig == null)
            {
                throw new ArgumentException("KafkaConfig is required for kafka destination type");
            }
            
            var config = destination.KafkaConfig;
            
            // Build message key with placeholders
            var messageKey = ReplacePlaceholders(
                config.MessageKey ?? fileName,
                fileName,
                destination.Name);
            
            // Build Kafka message
            var message = new Message<string, string>
            {
                Key = messageKey,
                Value = content
            };
            
            // Add custom headers
            if (config.Headers != null && config.Headers.Any())
            {
                message.Headers = new Headers();
                foreach (var header in config.Headers)
                {
                    message.Headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
                }
            }
            
            // Publish to Kafka with retry logic (3 attempts)
            DeliveryResult<string, string>? deliveryResult = null;
            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    deliveryResult = await _producer.ProduceAsync(
                        config.Topic,
                        message,
                        cancellationToken);
                    
                    _logger.LogInformation(
                        "Published to Kafka: topic={Topic}, partition={Partition}, offset={Offset}, destination={Destination}",
                        config.Topic,
                        deliveryResult.Partition.Value,
                        deliveryResult.Offset.Value,
                        destination.Name);
                    
                    stopwatch.Stop();
                    
                    return OutputResult.CreateSuccess(
                        destination.Name,
                        "kafka",
                        stopwatch.Elapsed);
                }
                catch (ProduceException<string, string> ex) when (attempt < 3)
                {
                    lastException = ex;
                    _logger.LogWarning(
                        "Kafka publish attempt {Attempt} failed: {Error}",
                        attempt,
                        ex.Error.Reason);
                    
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                }
            }
            
            // All retries failed
            stopwatch.Stop();
            throw lastException ?? new Exception("Failed to publish to Kafka after 3 attempts");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Failed to write to Kafka destination {Destination}, topic {Topic}",
                destination.Name,
                destination.KafkaConfig?.Topic);
            
            return OutputResult.CreateFailure(
                destination.Name,
                "kafka",
                ex.Message,
                stopwatch.Elapsed);
        }
    }
    
    private string ReplacePlaceholders(string pattern, string fileName, string datasourceName)
    {
        return pattern
            .Replace("{filename}", Path.GetFileNameWithoutExtension(fileName))
            .Replace("{datasource}", datasourceName)
            .Replace("{timestamp}", DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
            .Replace("{date}", DateTime.UtcNow.ToString("yyyyMMdd"));
    }
}
