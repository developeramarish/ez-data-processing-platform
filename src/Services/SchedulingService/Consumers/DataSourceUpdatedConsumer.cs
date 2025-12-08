using DataProcessing.Shared.Messages;
using DataProcessing.Scheduling.Services;
using MassTransit;

namespace DataProcessing.Scheduling.Consumers;

/// <summary>
/// Consumes DataSourceUpdatedEvent and updates polling schedule accordingly
/// </summary>
public class DataSourceUpdatedConsumer : IConsumer<DataSourceUpdatedEvent>
{
    private readonly ISchedulingManager _schedulingManager;
    private readonly ILogger<DataSourceUpdatedConsumer> _logger;

    public DataSourceUpdatedConsumer(
        ISchedulingManager schedulingManager,
        ILogger<DataSourceUpdatedConsumer> logger)
    {
        _schedulingManager = schedulingManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DataSourceUpdatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received DataSourceUpdatedEvent: {DataSourceId} - {DataSourceName}. CorrelationId: {CorrelationId}",
            message.DataSourceId, message.DataSourceName, message.CorrelationId);

        try
        {
            // If datasource became inactive, unschedule it
            if (!message.IsActive)
            {
                _logger.LogInformation(
                    "DataSource {DataSourceId} is inactive, unscheduling. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);

                await _schedulingManager.UnscheduleDataSourcePollingAsync(message.DataSourceId, message.CorrelationId);
                return;
            }

            // Datasource is active - recreate/update schedule
            var dataSource = new DataProcessing.Shared.Entities.DataProcessingDataSource
            {
                ID = message.DataSourceId,
                Name = message.DataSourceName,
                SupplierName = message.SupplierName,
                PollingRate = message.PollingRate,
                CronExpression = message.CronExpression,
                IsActive = message.IsActive,
                FilePath = string.Empty,
                JsonSchema = new MongoDB.Bson.BsonDocument(),
                UpdatedBy = message.UpdatedBy
            };

            // This will delete existing schedule and create new one
            var success = await _schedulingManager.ScheduleDataSourcePollingAsync(dataSource, message.CorrelationId);

            if (success)
            {
                _logger.LogInformation(
                    "Successfully updated schedule for datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update schedule for datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing DataSourceUpdatedEvent for {DataSourceId}. CorrelationId: {CorrelationId}",
                message.DataSourceId, message.CorrelationId);
            throw;
        }
    }
}
