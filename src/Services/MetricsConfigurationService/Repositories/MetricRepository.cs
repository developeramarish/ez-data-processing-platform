using MetricsConfigurationService.Models;
using MongoDB.Entities;

namespace MetricsConfigurationService.Repositories;

public class MetricRepository : IMetricRepository
{
    public async Task<List<MetricConfiguration>> GetAllAsync()
    {
        return await DB.Find<MetricConfiguration>()
            .Sort(m => m.UpdatedAt, Order.Descending)
            .ExecuteAsync();
    }

    public async Task<MetricConfiguration?> GetByIdAsync(string id)
    {
        return await DB.Find<MetricConfiguration>()
            .Match(m => m.ID == id)
            .ExecuteSingleAsync();
    }

    public async Task<MetricConfiguration?> GetByNameAsync(string name)
    {
        return await DB.Find<MetricConfiguration>()
            .Match(m => m.Name == name)
            .ExecuteSingleAsync();
    }

    public async Task<MetricConfiguration> CreateAsync(MetricConfiguration metric)
    {
        metric.CreatedAt = DateTime.UtcNow;
        metric.UpdatedAt = DateTime.UtcNow;
        await metric.SaveAsync();
        return metric;
    }

    public async Task<MetricConfiguration> UpdateAsync(MetricConfiguration metric)
    {
        metric.UpdatedAt = DateTime.UtcNow;
        
        await DB.Update<MetricConfiguration>()
            .Match(m => m.ID == metric.ID)
            .Modify(m => m.DisplayName, metric.DisplayName)
            .Modify(m => m.Description, metric.Description)
            .Modify(m => m.Category, metric.Category)
            .Modify(m => m.Scope, metric.Scope)
            .Modify(m => m.DataSourceId, metric.DataSourceId)
            .Modify(m => m.Formula, metric.Formula)
            .Modify(m => m.FormulaType, metric.FormulaType)
            .Modify(m => m.FieldPath, metric.FieldPath)
            .Modify(m => m.PrometheusType, metric.PrometheusType)
            .Modify(m => m.LabelNames, metric.LabelNames)
            .Modify(m => m.LabelsExpression, metric.LabelsExpression)
            .Modify(m => m.Labels, metric.Labels)
            .Modify(m => m.AlertRules, metric.AlertRules)
            .Modify(m => m.Retention, metric.Retention)
            .Modify(m => m.Status, metric.Status)
            .Modify(m => m.UpdatedAt, metric.UpdatedAt)
            .Modify(m => m.UpdatedBy, metric.UpdatedBy)
            .ExecuteAsync();

        return (await GetByIdAsync(metric.ID))!;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await DB.DeleteAsync<MetricConfiguration>(id);
        return result.DeletedCount > 0;
    }

    public async Task<MetricConfiguration> DuplicateAsync(string id, DuplicateMetricRequest request)
    {
        var original = await GetByIdAsync(id);
        if (original == null)
        {
            throw new Exception($"Metric with ID {id} not found");
        }

        // Check if name already exists
        var existingWithName = await GetByNameAsync(request.Name);
        if (existingWithName != null)
        {
            throw new Exception($"Metric with name {request.Name} already exists");
        }

        var duplicate = new MetricConfiguration
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Category = original.Category,
            Scope = original.Scope,
            DataSourceId = original.DataSourceId,
            Formula = original.Formula,
            FieldPath = original.FieldPath,
            Labels = original.Labels?.ToList(),
            Retention = original.Retention,
            Status = 0, // Draft
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        await duplicate.SaveAsync();
        return duplicate;
    }

    public async Task<List<MetricConfiguration>> GetByDataSourceIdAsync(string dataSourceId)
    {
        return await DB.Find<MetricConfiguration>()
            .Match(m => m.DataSourceId == dataSourceId)
            .Sort(m => m.UpdatedAt, Order.Descending)
            .ExecuteAsync();
    }

    public async Task<List<MetricConfiguration>> GetGlobalMetricsAsync()
    {
        return await DB.Find<MetricConfiguration>()
            .Match(m => m.Scope == "global")
            .Sort(m => m.UpdatedAt, Order.Descending)
            .ExecuteAsync();
    }
}
