using DataProcessing.Shared.Messages;
using DataProcessing.Scheduling.Services;
using MassTransit;

namespace DataProcessing.Scheduling.Consumers;

/// <summary>
/// Consumes DataSourceCreatedEvent and automatically creates polling schedule
/// </summary>
public class DataSourceCreatedConsumer : IConsumer<DataSourceCreatedEvent>
{
    private readonly ISchedulingManager _schedulingManager;
    private readonly ILogger<DataSourceCreatedConsumer> _logger;

    public DataSourceCreatedConsumer(
        ISchedulingManager schedulingManager,
        ILogger<DataSourceCreatedConsumer> logger)
    {
        _schedulingManager = schedulingManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DataSourceCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received DataSourceCreatedEvent: {DataSourceId} - {DataSourceName}. CorrelationId: {CorrelationId}",
            message.DataSourceId, message.DataSourceName, message.CorrelationId);

        try
        {
            // Only create schedule if datasource is active
            if (!message.IsActive)
            {
                _logger.LogInformation(
                    "Skipping schedule creation for inactive datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
                return;
            }

            // Create datasource object for scheduling
            var dataSource = new DataProcessing.Shared.Entities.DataProcessingDataSource
            {
                ID = message.DataSourceId,
                Name = message.DataSourceName,
                SupplierName = message.SupplierName,
                PollingRate = message.PollingRate,
                CronExpression = message.CronExpression,
                IsActive = message.IsActive,
                FilePath = string.Empty, // Not needed for scheduling
                JsonSchema = new MongoDB.Bson.BsonDocument(), // Not needed for scheduling
                CreatedBy = message.CreatedBy
            };

            // Create schedule automatically
            var success = await _schedulingManager.ScheduleDataSourcePollingAsync(dataSource, message.CorrelationId);

            if (success)
            {
                _logger.LogInformation(
                    "Automatically created schedule for datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create schedule for datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing DataSourceCreatedEvent for {DataSourceId}. CorrelationId: {CorrelationId}",
                message.DataSourceId, message.CorrelationId);
            throw; // Let MassTransit handle retry
        }
    }
}
