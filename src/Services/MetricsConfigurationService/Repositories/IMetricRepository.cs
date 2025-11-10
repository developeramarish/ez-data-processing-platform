using MetricsConfigurationService.Models;

namespace MetricsConfigurationService.Repositories;

public interface IMetricRepository
{
    Task<List<MetricConfiguration>> GetAllAsync();
    Task<MetricConfiguration?> GetByIdAsync(string id);
    Task<MetricConfiguration?> GetByNameAsync(string name);
    Task<MetricConfiguration> CreateAsync(MetricConfiguration metric);
    Task<MetricConfiguration> UpdateAsync(MetricConfiguration metric);
    Task<bool> DeleteAsync(string id);
    Task<MetricConfiguration> DuplicateAsync(string id, DuplicateMetricRequest request);
    Task<List<MetricConfiguration>> GetByDataSourceIdAsync(string dataSourceId);
    Task<List<MetricConfiguration>> GetGlobalMetricsAsync();
}
