using MetricsConfigurationService.Models;

namespace MetricsConfigurationService.Repositories;

public interface IGlobalAlertRepository
{
    Task<List<GlobalAlertConfiguration>> GetAllAsync(CancellationToken ct = default);
    Task<List<GlobalAlertConfiguration>> GetByMetricTypeAsync(string metricType, CancellationToken ct = default);
    Task<List<GlobalAlertConfiguration>> GetByMetricNameAsync(string metricName, CancellationToken ct = default);
    Task<GlobalAlertConfiguration?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<GlobalAlertConfiguration> CreateAsync(GlobalAlertConfiguration alert, CancellationToken ct = default);
    Task<GlobalAlertConfiguration> UpdateAsync(GlobalAlertConfiguration alert, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<List<GlobalAlertConfiguration>> GetEnabledAlertsAsync(CancellationToken ct = default);
}
