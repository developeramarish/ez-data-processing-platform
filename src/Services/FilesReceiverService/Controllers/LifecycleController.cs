using DataProcessing.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace DataProcessing.FilesReceiver.Controllers;

/// <summary>
/// Controller for K8s lifecycle management (PreStop hooks)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LifecycleController : ControllerBase
{
    private readonly ILogger<LifecycleController> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public LifecycleController(
        ILogger<LifecycleController> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    /// <summary>
    /// Called by K8s PreStop hook before pod termination
    /// Releases all locks held by this pod instance
    /// </summary>
    [HttpPost("shutdown")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GracefulShutdown()
    {
        var podId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        
        _logger.LogWarning("PreStop hook triggered - releasing locks for pod: {PodId}", podId);

        try
        {
            // Find and release all locks held by this pod
            var lockedDataSources = await DB.Find<DataProcessingDataSource>()
                .Match(ds => ds.IsCurrentlyProcessing && ds.ProcessingPodId == podId)
                .ExecuteAsync();

            if (lockedDataSources.Any())
            {
                _logger.LogWarning(
                    "Found {Count} locked datasources for pod {PodId}. Releasing locks...",
                    lockedDataSources.Count, podId);

                foreach (var ds in lockedDataSources)
                {
                    ds.ReleaseProcessingLock("pod_shutdown");
                    await ds.SaveAsync();
                    
                    _logger.LogInformation(
                        "Released lock for datasource {DataSourceId} ({DataSourceName})",
                        ds.ID, ds.Name);
                }

                _logger.LogWarning(
                    "Successfully released {Count} locks for pod {PodId}",
                    lockedDataSources.Count, podId);
            }
            else
            {
                _logger.LogInformation("No locked datasources found for pod {PodId}", podId);
            }

            // Give time for cleanup before shutdown
            await Task.Delay(1000);

            return Ok(new
            {
                PodId = podId,
                LocksReleased = lockedDataSources.Count,
                Timestamp = DateTime.UtcNow,
                Message = "Graceful shutdown initiated"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown for pod {PodId}", podId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        var podId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        return Ok(new
        {
            PodId = podId,
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }
}
