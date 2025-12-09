using MongoDB.Entities;
using Quartz;
using DataProcessing.Shared.Entities;
using DataProcessing.Scheduling.Jobs;

namespace DataProcessing.Scheduling.Services;

/// <summary>
/// Hosted service that reloads persisted schedules from MongoDB on startup
/// This ensures schedules survive pod restarts and maintain continuity
/// </summary>
public class ScheduleReloadService : IHostedService
{
    private readonly IScheduler _scheduler;
    private readonly ILogger<ScheduleReloadService> _logger;
    private const string JobGroupName = "DataSourcePolling";
    private const string TriggerGroupName = "DataSourcePollingTriggers";

    public ScheduleReloadService(
        IScheduler scheduler,
        ILogger<ScheduleReloadService> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScheduleReloadService starting - reloading persisted schedules from MongoDB");

        try
        {
            // Query all persisted schedules from MongoDB
            var persistedSchedules = await DB.Find<ScheduledDataSource>()
                .Match(s => s.IsActive && !s.IsPaused)
                .ExecuteAsync(cancellationToken);

            _logger.LogInformation("Found {Count} persisted schedules to reload", persistedSchedules.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var schedule in persistedSchedules)
            {
                try
                {
                    // Create job key from persisted schedule
                    var jobKey = new JobKey($"polling-{schedule.DataSourceId}", JobGroupName);
                    var triggerKey = new TriggerKey($"trigger-{schedule.DataSourceId}", TriggerGroupName);

                    // Skip if job already exists (shouldn't happen on startup, but safe to check)
                    if (await _scheduler.CheckExists(jobKey, cancellationToken))
                    {
                        _logger.LogDebug("Job {JobKey} already exists, skipping reload", jobKey);
                        continue;
                    }

                    // Recreate the Quartz job from persisted data
                    var job = JobBuilder.Create<DataSourcePollingJob>()
                        .WithIdentity(jobKey)
                        .WithDescription($"Polling job for data source: {schedule.DataSourceName} (reloaded from MongoDB)")
                        .UsingJobData("dataSourceId", schedule.DataSourceId)
                        .UsingJobData("dataSourceName", schedule.DataSourceName)
                        .UsingJobData("supplierName", schedule.SupplierName ?? "Unknown")
                        .UsingJobData("createdAt", schedule.CreatedAt.ToString("O"))
                        .Build();

                    // Recreate the trigger using the persisted Quartz.NET cron expression
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithDescription($"Trigger for data source: {schedule.DataSourceName} - Cron: {schedule.CronExpression} (reloaded)")
                        .StartNow()
                        .WithCronSchedule(schedule.CronExpression) // Use stored Quartz format
                        .Build();

                    // Schedule the job in Quartz
                    await _scheduler.ScheduleJob(job, trigger, cancellationToken);

                    // Update next execution time in MongoDB
                    schedule.NextExecutionTime = trigger.GetNextFireTimeUtc()?.UtcDateTime;
                    schedule.UpdatedAt = DateTime.UtcNow;
                    await schedule.SaveAsync(cancellation: cancellationToken);

                    successCount++;

                    _logger.LogInformation(
                        "Successfully reloaded schedule for datasource {DataSourceName} ({DataSourceId}). Next execution: {NextExecution}",
                        schedule.DataSourceName, schedule.DataSourceId, schedule.NextExecutionTime);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex,
                        "Failed to reload schedule for datasource {DataSourceName} ({DataSourceId})",
                        schedule.DataSourceName, schedule.DataSourceId);
                }
            }

            _logger.LogInformation(
                "ScheduleReloadService completed. Successfully reloaded {SuccessCount} schedules, {FailureCount} failures",
                successCount, failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during schedule reload - some schedules may not be restored");
        }

        return;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScheduleReloadService stopping");
        return Task.CompletedTask;
    }
}
