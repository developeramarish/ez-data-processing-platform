using MetricsConfigurationService.Models;
using MongoDB.Entities;

namespace MetricsConfigurationService.Repositories;

public class GlobalAlertRepository : IGlobalAlertRepository
{
    private readonly ILogger<GlobalAlertRepository> _logger;

    public GlobalAlertRepository(ILogger<GlobalAlertRepository> logger)
    {
        _logger = logger;
    }

    public async Task<List<GlobalAlertConfiguration>> GetAllAsync(CancellationToken ct = default)
    {
        return await DB.Find<GlobalAlertConfiguration>()
            .Sort(a => a.UpdatedAt, Order.Descending)
            .ExecuteAsync(ct);
    }

    public async Task<List<GlobalAlertConfiguration>> GetByMetricTypeAsync(string metricType, CancellationToken ct = default)
    {
        return await DB.Find<GlobalAlertConfiguration>()
            .Match(a => a.MetricType == metricType)
            .Sort(a => a.UpdatedAt, Order.Descending)
            .ExecuteAsync(ct);
    }

    public async Task<List<GlobalAlertConfiguration>> GetByMetricNameAsync(string metricName, CancellationToken ct = default)
    {
        return await DB.Find<GlobalAlertConfiguration>()
            .Match(a => a.MetricName == metricName)
            .Sort(a => a.UpdatedAt, Order.Descending)
            .ExecuteAsync(ct);
    }

    public async Task<GlobalAlertConfiguration?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await DB.Find<GlobalAlertConfiguration>()
            .Match(a => a.ID == id)
            .ExecuteSingleAsync(ct);
    }

    public async Task<GlobalAlertConfiguration> CreateAsync(GlobalAlertConfiguration alert, CancellationToken ct = default)
    {
        alert.CreatedAt = DateTime.UtcNow;
        alert.UpdatedAt = DateTime.UtcNow;
        await alert.SaveAsync(cancellation: ct);

        _logger.LogInformation(
            "Created global alert {AlertName} for {MetricType} metric {MetricName}",
            alert.AlertName, alert.MetricType, alert.MetricName);

        return alert;
    }

    public async Task<GlobalAlertConfiguration> UpdateAsync(GlobalAlertConfiguration alert, CancellationToken ct = default)
    {
        alert.UpdatedAt = DateTime.UtcNow;

        await DB.Update<GlobalAlertConfiguration>()
            .Match(a => a.ID == alert.ID)
            .Modify(a => a.AlertName, alert.AlertName)
            .Modify(a => a.Description, alert.Description)
            .Modify(a => a.Expression, alert.Expression)
            .Modify(a => a.For, alert.For)
            .Modify(a => a.Severity, alert.Severity)
            .Modify(a => a.IsEnabled, alert.IsEnabled)
            .Modify(a => a.Labels, alert.Labels)
            .Modify(a => a.Annotations, alert.Annotations)
            .Modify(a => a.NotificationRecipients, alert.NotificationRecipients)
            .Modify(a => a.UpdatedAt, alert.UpdatedAt)
            .Modify(a => a.UpdatedBy, alert.UpdatedBy)
            .ExecuteAsync(ct);

        _logger.LogInformation(
            "Updated global alert {AlertId} ({AlertName})",
            alert.ID, alert.AlertName);

        return (await GetByIdAsync(alert.ID, ct))!;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var alert = await GetByIdAsync(id, ct);
        if (alert == null)
        {
            return false;
        }

        var result = await DB.DeleteAsync<GlobalAlertConfiguration>(id, cancellation: ct);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation(
                "Deleted global alert {AlertId} ({AlertName})",
                id, alert.AlertName);
        }

        return result.DeletedCount > 0;
    }

    public async Task<List<GlobalAlertConfiguration>> GetEnabledAlertsAsync(CancellationToken ct = default)
    {
        return await DB.Find<GlobalAlertConfiguration>()
            .Match(a => a.IsEnabled == true)
            .Sort(a => a.MetricType, Order.Ascending)
            .Sort(a => a.MetricName, Order.Ascending)
            .ExecuteAsync(ct);
    }
}
