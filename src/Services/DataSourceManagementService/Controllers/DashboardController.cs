using DataSourceManagementService.Models.Dashboard;
using DataProcessing.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace DataSourceManagementService.Controllers;

/// <summary>
/// Controller for dashboard statistics and aggregations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview statistics
    /// </summary>
    /// <returns>Dashboard statistics including file counts and error rates</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewResponse), 200)]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview()
    {
        try
        {
            _logger.LogInformation("Fetching dashboard overview statistics");

            // Count total data sources (representing files to process)
            var totalFiles = await DB.CountAsync<DataProcessingDataSource>();

            // Count invalid records
            var invalidRecords = await DB.CountAsync<DataProcessingInvalidRecord>();

            // For now, we'll estimate valid records based on data sources
            // In a real scenario, this would come from a processed records collection
            // or from aggregating validation results
            // Temporary calculation: assume average of 100 records per datasource
            var estimatedTotalRecords = totalFiles * 100;
            var validRecords = estimatedTotalRecords - invalidRecords;

            // Calculate error rate
            var errorRate = estimatedTotalRecords > 0
                ? (double)invalidRecords / estimatedTotalRecords * 100.0
                : 0.0;

            var response = new DashboardOverviewResponse
            {
                TotalFiles = (int)totalFiles,
                ValidRecords = validRecords,
                InvalidRecords = invalidRecords,
                ErrorRate = Math.Round(errorRate, 2),
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Dashboard stats: {Files} files, {Valid} valid, {Invalid} invalid, {ErrorRate}% error rate",
                response.TotalFiles,
                response.ValidRecords,
                response.InvalidRecords,
                response.ErrorRate);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard overview");
            return StatusCode(500, new { error = "Failed to fetch dashboard statistics" });
        }
    }

    /// <summary>
    /// Get dashboard health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Dashboard",
            Timestamp = DateTime.UtcNow
        });
    }
}
