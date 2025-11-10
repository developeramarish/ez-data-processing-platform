using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Consumers;
using DataProcessing.Shared.Messages;
using DataProcessing.Shared.Monitoring;
using DataProcessing.Validation.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DataProcessing.Validation.Consumers;

/// <summary>
/// Consumer for ValidationRequestEvent messages from the Files Receiver Service
/// Validates data records using JSON Schema and publishes results
/// </summary>
public class ValidationRequestEventConsumer : DataProcessingConsumerBase<ValidationRequestEvent>
{
    private readonly IValidationService _validationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly DataProcessingMetrics _metrics;
    private readonly ActivitySource _activitySource;

    public ValidationRequestEventConsumer(
        ILogger<ValidationRequestEventConsumer> logger,
        IValidationService validationService,
        IPublishEndpoint publishEndpoint,
        DataProcessingMetrics metrics,
        ActivitySource activitySource) 
        : base(logger)
    {
        _validationService = validationService;
        _publishEndpoint = publishEndpoint;
        _metrics = metrics;
        _activitySource = activitySource;
    }

    protected override async Task ProcessMessage(ConsumeContext<ValidationRequestEvent> context)
    {
        var message = context.Message;
        using var activity = _activitySource.StartDataProcessingActivity(
            "ValidationRequestEventConsumer.ProcessMessage", 
            message.CorrelationId, 
            message.DataSourceId);

        Logger.LogInformation("Processing validation request for file: {FileName}, DataSource: {DataSourceId}, CorrelationId: {CorrelationId}", 
            message.FileName, message.DataSourceId, message.CorrelationId);

        // Record message received metric
        _metrics.RecordMessageReceived("ValidationRequestEvent", "Validation", true);

        try
        {
            // Set activity context
            activity?.SetFileProcessingContext(message.FileName, null, message.FileSizeBytes);
            activity?.SetTag("validation.data_source_id", message.DataSourceId);

            // Validate the file content using JSON Schema
            var validationResult = await _validationService.ValidateFileContentAsync(
                message.DataSourceId,
                message.FileName,
                message.FileContent,
                message.FileContentType,
                message.CorrelationId);

            // Record validation metrics
            _metrics.RecordValidationResults(
                message.DataSourceId,
                validationResult.TotalRecords,
                validationResult.ValidRecords,
                validationResult.InvalidRecords);

            Logger.LogInformation("Validation completed for file: {FileName}, Total: {TotalRecords}, Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                message.FileName, validationResult.TotalRecords, validationResult.ValidRecords, validationResult.InvalidRecords);

            // Publish validation completed event
            var completedEvent = new ValidationCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                PublishedBy = "Validation",
                DataSourceId = message.DataSourceId,
                FileName = message.FileName,
                ValidationResultId = validationResult.ValidationResultId,
                TotalRecords = validationResult.TotalRecords,
                ValidRecords = validationResult.ValidRecords,
                InvalidRecords = validationResult.InvalidRecords,
                ValidationStatus = validationResult.InvalidRecords == 0 ? "Success" : "PartialFailure",
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(completedEvent, context.CancellationToken);

            // Record message sent metric
            _metrics.RecordMessageSent("ValidationCompletedEvent", "Validation", true);

            activity?.SetValidationContext(
                validationResult.TotalRecords,
                validationResult.ValidRecords,
                validationResult.InvalidRecords);

            Logger.LogInformation("Published validation completed event for file: {FileName}, ValidationResult: {ValidationResultId}", 
                message.FileName, validationResult.ValidationResultId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate file: {FileName} for data source {DataSourceId}: {ErrorMessage}", 
                message.FileName, message.DataSourceId, ex.Message);

            // Record error metrics
            _metrics.RecordFileProcessingFailed(message.DataSourceId, "Validation", "validation_error");
            _metrics.RecordMessageReceived("ValidationRequestEvent", "Validation", false);

            // Publish failure event
            var failureEvent = new FileProcessingFailedEvent
            {
                CorrelationId = message.CorrelationId,
                PublishedBy = "Validation",
                DataSourceId = message.DataSourceId,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(failureEvent, context.CancellationToken);
            _metrics.RecordMessageSent("FileProcessingFailedEvent", "Validation", true);

            activity?.SetError(ex, "Validation failed");
            throw; // Re-throw to trigger message bus retry logic
        }
    }
}
