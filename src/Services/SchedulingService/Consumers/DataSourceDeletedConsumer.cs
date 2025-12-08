using DataProcessing.Shared.Messages;
using DataProcessing.Scheduling.Services;
using MassTransit;

namespace DataProcessing.Scheduling.Consumers;

/// <summary>
/// Consumes DataSourceDeletedEvent and removes polling schedule
/// </summary>
public class DataSourceDeletedConsumer : IConsumer<DataSourceDeletedEvent>
{
    private readonly ISchedulingManager _schedulingManager;
    private readonly ILogger<DataSourceDeletedConsumer> _logger;

    public DataSourceDeletedConsumer(
        ISchedulingManager schedulingManager,
        ILogger<DataSourceDeletedConsumer> logger)
    {
        _schedulingManager = schedulingManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DataSourceDeletedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received DataSourceDeletedEvent: {DataSourceId} - {DataSourceName}. CorrelationId: {CorrelationId}",
            message.DataSourceId, message.DataSourceName, message.CorrelationId);

        try
        {
            // Remove the schedule for this datasource
            var success = await _schedulingManager.UnscheduleDataSourcePollingAsync(message.DataSourceId, message.CorrelationId);

            if (success)
            {
                _logger.LogInformation(
                    "Successfully removed schedule for deleted datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "No schedule found to remove for datasource {DataSourceId}. CorrelationId: {CorrelationId}",
                    message.DataSourceId, message.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing DataSourceDeletedEvent for {DataSourceId}. CorrelationId: {CorrelationId}",
                message.DataSourceId, message.CorrelationId);
            throw;
        }
    }
}
