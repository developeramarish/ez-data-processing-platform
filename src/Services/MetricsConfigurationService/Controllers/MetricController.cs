using Microsoft.AspNetCore.Mvc;
using MetricsConfigurationService.Models;
using MetricsConfigurationService.Repositories;
using DataProcessing.Shared.Entities;
using MongoDB.Entities;

namespace MetricsConfigurationService.Controllers;

[ApiController]
[Route("api/v1/metrics")]
public class MetricController : ControllerBase
{
    private readonly IMetricRepository _repository;
    private readonly ILogger<MetricController> _logger;

    public MetricController(IMetricRepository repository, ILogger<MetricController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to build DTO with DataSourceName populated via lookup
    /// </summary>
    private async Task<object> BuildMetricDtoAsync(MetricConfiguration metric)
    {
        string? dataSourceName = null;
        
        if (!string.IsNullOrEmpty(metric.DataSourceId))
        {
            var dataSource = await DB.Find<DataProcessingDataSource>()
                .Match(d => d.ID == metric.DataSourceId)
                .ExecuteFirstAsync();
            dataSourceName = dataSource?.Name;
        }

        return new
        {
            metric.ID,
            metric.Name,
            metric.DisplayName,
            metric.Description,
            metric.Category,
            metric.Scope,
            DataSourceId = metric.DataSourceId,
            DataSourceName = dataSourceName,
            metric.Formula,
            metric.FormulaType,
            metric.FieldPath,
            metric.PrometheusType,
            metric.LabelNames,
            metric.LabelsExpression,
            metric.Labels,
            metric.Retention,
            metric.AlertRules,
            metric.Status,
            metric.LastValue,
            metric.LastCalculated,
            metric.CreatedAt,
            metric.UpdatedAt,
            metric.CreatedBy,
            metric.UpdatedBy
        };
    }

    /// <summary>
    /// Helper method to build DTOs for a list of metrics (efficient batch lookup)
    /// </summary>
    private async Task<List<object>> BuildMetricDtosAsync(List<MetricConfiguration> metrics)
    {
        // Batch lookup all datasources
        var dataSourceIds = metrics
            .Where(m => !string.IsNullOrEmpty(m.DataSourceId))
            .Select(m => m.DataSourceId!)
            .Distinct()
            .ToList();

        Dictionary<string, string> dataSourceNames = new();
        
        if (dataSourceIds.Any())
        {
            var dataSources = await DB.Find<DataProcessingDataSource>()
                .Match(d => dataSourceIds.Contains(d.ID))
                .ExecuteAsync();
            dataSourceNames = dataSources.ToDictionary(d => d.ID, d => d.Name);
        }

        return metrics.Select(metric => new
        {
            metric.ID,
            metric.Name,
            metric.DisplayName,
            metric.Description,
            metric.Category,
            metric.Scope,
            DataSourceId = metric.DataSourceId,
            DataSourceName = !string.IsNullOrEmpty(metric.DataSourceId)
                ? dataSourceNames.GetValueOrDefault(metric.DataSourceId, "Unknown")
                : null,
            metric.Formula,
            metric.FormulaType,
            metric.FieldPath,
            metric.PrometheusType,
            metric.LabelNames,
            metric.LabelsExpression,
            metric.Labels,
            metric.Retention,
            metric.AlertRules,
            metric.Status,
            metric.LastValue,
            metric.LastCalculated,
            metric.CreatedAt,
            metric.UpdatedAt,
            metric.CreatedBy,
            metric.UpdatedBy
        }).Cast<object>().ToList();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var metrics = await _repository.GetAllAsync();
            var dtos = await BuildMetricDtosAsync(metrics);
            return Ok(new
            {
                IsSuccess = true,
                Data = dtos,
                Message = "Metrics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var metric = await _repository.GetByIdAsync(id);
            if (metric == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with ID {id} not found" }
                });
            }

            var dto = await BuildMetricDtoAsync(metric);
            return Ok(new
            {
                IsSuccess = true,
                Data = dto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metric {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMetricRequest request)
    {
        try
        {
            // Check if name already exists
            var existing = await _repository.GetByNameAsync(request.Name);
            if (existing != null)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with name '{request.Name}' already exists" }
                });
            }

            var metric = new MetricConfiguration
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Category = request.Category,
                Scope = request.Scope,
                DataSourceId = request.DataSourceId,
                Formula = request.Formula,
                FormulaType = request.FormulaType,
                FieldPath = request.FieldPath,
                PrometheusType = request.PrometheusType,
                LabelNames = request.LabelNames,
                LabelsExpression = request.LabelsExpression,
                Labels = request.Labels,
                AlertRules = request.AlertRules,
                Retention = request.Retention,
                Status = request.Status,
                CreatedBy = request.CreatedBy
            };

            var created = await _repository.CreateAsync(metric);
            var dto = await BuildMetricDtoAsync(created);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.ID },
                new
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "Metric created successfully"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating metric");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateMetricRequest request)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with ID {id} not found" }
                });
            }

            existing.DisplayName = request.DisplayName;
            existing.Description = request.Description;
            existing.Category = request.Category;
            existing.Scope = request.Scope;
            existing.DataSourceId = request.DataSourceId;
            existing.Formula = request.Formula;
            existing.FormulaType = request.FormulaType;
            existing.FieldPath = request.FieldPath;
            existing.PrometheusType = request.PrometheusType;
            existing.LabelNames = request.LabelNames;
            existing.LabelsExpression = request.LabelsExpression;
            existing.Labels = request.Labels;
            existing.AlertRules = request.AlertRules;
            existing.Retention = request.Retention;
            existing.Status = request.Status;
            existing.UpdatedBy = request.UpdatedBy;

            var updated = await _repository.UpdateAsync(existing);
            var dto = await BuildMetricDtoAsync(updated);

            return Ok(new
            {
                IsSuccess = true,
                Data = dto,
                Message = "Metric updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metric {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var metric = await _repository.GetByIdAsync(id);
            if (metric == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Metric with ID {id} not found" }
                });
            }

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = new { Message = "Failed to delete metric" }
                });
            }

            return Ok(new
            {
                IsSuccess = true,
                Message = "Metric deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metric {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(string id, [FromBody] DuplicateMetricRequest request)
    {
        try
        {
            var duplicated = await _repository.DuplicateAsync(id, request);
            var dto = await BuildMetricDtoAsync(duplicated);

            return CreatedAtAction(
                nameof(GetById),
                new { id = duplicated.ID },
                new
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "Metric duplicated successfully"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating metric {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpGet("datasource/{dataSourceId}")]
    public async Task<IActionResult> GetByDataSource(string dataSourceId)
    {
        try
        {
            var metrics = await _repository.GetByDataSourceIdAsync(dataSourceId);
            var dtos = await BuildMetricDtosAsync(metrics);
            return Ok(new
            {
                IsSuccess = true,
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for data source {DataSourceId}", dataSourceId);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal()
    {
        try
        {
            var metrics = await _repository.GetGlobalMetricsAsync();
            var dtos = await BuildMetricDtosAsync(metrics);
            return Ok(new
            {
                IsSuccess = true,
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global metrics");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }
}
