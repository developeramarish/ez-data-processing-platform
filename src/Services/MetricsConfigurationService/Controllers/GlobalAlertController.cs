using Microsoft.AspNetCore.Mvc;
using MetricsConfigurationService.Models;
using MetricsConfigurationService.Repositories;

namespace MetricsConfigurationService.Controllers;

[ApiController]
[Route("api/v1/global-alerts")]
public class GlobalAlertController : ControllerBase
{
    private readonly IGlobalAlertRepository _repository;
    private readonly ILogger<GlobalAlertController> _logger;

    public GlobalAlertController(IGlobalAlertRepository repository, ILogger<GlobalAlertController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all global alerts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? metricType = null, CancellationToken ct = default)
    {
        try
        {
            List<GlobalAlertConfiguration> alerts;

            if (!string.IsNullOrEmpty(metricType))
            {
                alerts = await _repository.GetByMetricTypeAsync(metricType, ct);
            }
            else
            {
                alerts = await _repository.GetAllAsync(ct);
            }

            return Ok(new
            {
                IsSuccess = true,
                Data = alerts,
                Message = $"Retrieved {alerts.Count} global alerts"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global alerts");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get a global alert by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct = default)
    {
        try
        {
            var alert = await _repository.GetByIdAsync(id, ct);
            if (alert == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Global alert with ID {id} not found" }
                });
            }

            return Ok(new
            {
                IsSuccess = true,
                Data = alert
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global alert {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Create a new global alert
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGlobalAlertRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate metric type
            if (request.MetricType != "business" && request.MetricType != "system")
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Error = new { Message = "MetricType must be 'business' or 'system'" }
                });
            }

            var alert = new GlobalAlertConfiguration
            {
                MetricType = request.MetricType,
                MetricName = request.MetricName,
                AlertName = request.AlertName,
                Description = request.Description,
                Expression = request.Expression,
                For = request.For,
                Severity = request.Severity,
                IsEnabled = request.IsEnabled,
                Labels = request.Labels ?? new Dictionary<string, string>(),
                Annotations = request.Annotations ?? new Dictionary<string, string>(),
                NotificationRecipients = request.NotificationRecipients,
                CreatedBy = request.CreatedBy
            };

            var created = await _repository.CreateAsync(alert, ct);

            _logger.LogInformation(
                "Created global alert {AlertName} for {MetricType} metric {MetricName}",
                alert.AlertName, alert.MetricType, alert.MetricName);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.ID },
                new
                {
                    IsSuccess = true,
                    Data = created,
                    Message = "Global alert created successfully"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating global alert");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Update an existing global alert
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGlobalAlertRequest request, CancellationToken ct = default)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Global alert with ID {id} not found" }
                });
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.AlertName))
                existing.AlertName = request.AlertName;
            if (request.Description != null)
                existing.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Expression))
                existing.Expression = request.Expression;
            if (request.For != null)
                existing.For = request.For;
            if (!string.IsNullOrEmpty(request.Severity))
                existing.Severity = request.Severity;
            if (request.IsEnabled.HasValue)
                existing.IsEnabled = request.IsEnabled.Value;
            if (request.Labels != null)
                existing.Labels = request.Labels;
            if (request.Annotations != null)
                existing.Annotations = request.Annotations;
            if (request.NotificationRecipients != null)
                existing.NotificationRecipients = request.NotificationRecipients;

            existing.UpdatedBy = request.UpdatedBy;

            var updated = await _repository.UpdateAsync(existing, ct);

            _logger.LogInformation("Updated global alert {AlertId} ({AlertName})", id, updated.AlertName);

            return Ok(new
            {
                IsSuccess = true,
                Data = updated,
                Message = "Global alert updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global alert {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete a global alert
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        try
        {
            var alert = await _repository.GetByIdAsync(id, ct);
            if (alert == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Error = new { Message = $"Global alert with ID {id} not found" }
                });
            }

            var deleted = await _repository.DeleteAsync(id, ct);
            if (!deleted)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = new { Message = "Failed to delete global alert" }
                });
            }

            _logger.LogInformation("Deleted global alert {AlertId} ({AlertName})", id, alert.AlertName);

            return Ok(new
            {
                IsSuccess = true,
                Message = "Global alert deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting global alert {Id}", id);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get alerts by metric name
    /// </summary>
    [HttpGet("metric/{metricName}")]
    public async Task<IActionResult> GetByMetricName(string metricName, CancellationToken ct = default)
    {
        try
        {
            var alerts = await _repository.GetByMetricNameAsync(metricName, ct);
            return Ok(new
            {
                IsSuccess = true,
                Data = alerts,
                Message = $"Retrieved {alerts.Count} alerts for metric {metricName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for metric {MetricName}", metricName);
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all enabled global alerts (for alert evaluation)
    /// </summary>
    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken ct = default)
    {
        try
        {
            var alerts = await _repository.GetEnabledAlertsAsync(ct);
            return Ok(new
            {
                IsSuccess = true,
                Data = alerts,
                Message = $"Retrieved {alerts.Count} enabled global alerts"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled global alerts");
            return StatusCode(500, new
            {
                IsSuccess = false,
                Error = new { Message = ex.Message }
            });
        }
    }
}
