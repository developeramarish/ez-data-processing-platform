using MassTransit;
using DataProcessing.Shared.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration and setup for MassTransit with Kafka transport
/// Implements topic naming conventions: dataprocessing.[service].[event]
/// </summary>
public static class MassTransitConfiguration
{
    /// <summary>
    /// Add MassTransit with Kafka transport to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">Name of the current service for topic prefixing</param>
    public static IServiceCollection AddDataProcessingMassTransit(
        this IServiceCollection services, 
        IConfiguration configuration,
        string serviceName)
    {
        services.AddMassTransit(x =>
        {
            // Set endpoint name formatter
            x.SetKebabCaseEndpointNameFormatter();

            // Add Kafka rider
            x.AddRider(rider =>
            {
                var kafkaHost = configuration.GetConnectionString("Kafka") 
                    ?? throw new InvalidOperationException("Kafka connection string is required");

                rider.UsingKafka((context, k) =>
                {
                    k.Host(kafkaHost);

                    // Configure topic endpoints with our naming convention
                    ConfigureKafkaTopics(k, context, serviceName);
                });
            });

            // Use in-memory bus for local message handling
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Configure Kafka topics with Data Processing Platform naming conventions
    /// </summary>
    private static void ConfigureKafkaTopics(IKafkaFactoryConfigurator kafka, IRiderRegistrationContext context, string serviceName)
    {
        // File Polling Event - Published by SchedulingService, Consumed by FilesReceiverService
        kafka.TopicEndpoint<FilePollingEvent>("dataprocessing.scheduling.filepolling", serviceName, e =>
        {
            e.CreateIfMissing(t =>
            {
                t.NumPartitions = 3;
                t.ReplicationFactor = 1;
            });
        });

        // Validation Request Event - Published by FilesReceiverService, Consumed by ValidationService
        kafka.TopicEndpoint<ValidationRequestEvent>("dataprocessing.filesreceiver.validationrequest", serviceName, e =>
        {
            e.CreateIfMissing(t =>
            {
                t.NumPartitions = 3;
                t.ReplicationFactor = 1;
            });
        });

        // Validation Completed Event - Published by ValidationService
        kafka.TopicEndpoint<ValidationCompletedEvent>("dataprocessing.validation.completed", serviceName, e =>
        {
            e.CreateIfMissing(t =>
            {
                t.NumPartitions = 3;
                t.ReplicationFactor = 1;
            });
        });

        // File Processing Failed Event - Published by any service
        kafka.TopicEndpoint<FileProcessingFailedEvent>("dataprocessing.global.processingfailed", serviceName, e =>
        {
            e.CreateIfMissing(t =>
            {
                t.NumPartitions = 3;
                t.ReplicationFactor = 1;
            });
        });
    }

    /// <summary>
    /// Add message publishing capabilities
    /// </summary>
    public static IServiceCollection AddDataProcessingPublisher(this IServiceCollection services)
    {
        services.AddScoped<IDataProcessingMessagePublisher, DataProcessingMessagePublisher>();
        return services;
    }
}

/// <summary>
/// Interface for publishing Data Processing Platform messages
/// </summary>
public interface IDataProcessingMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, IDataProcessingMessage;
    Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default) where T : class, IDataProcessingMessage;
}

/// <summary>
/// Implementation of Data Processing Platform message publisher
/// </summary>
public class DataProcessingMessagePublisher : IDataProcessingMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public DataProcessingMessagePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
        where T : class, IDataProcessingMessage
    {
        // Ensure timestamp and correlation ID are set
        if (message.Timestamp == default)
            message.Timestamp = DateTime.UtcNow;
            
        if (string.IsNullOrEmpty(message.CorrelationId))
            message.CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        await _publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default) 
        where T : class, IDataProcessingMessage
    {
        message.CorrelationId = correlationId;
        message.Timestamp = DateTime.UtcNow;

        await _publishEndpoint.Publish(message, cancellationToken);
    }
}
