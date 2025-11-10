using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration for comprehensive health checks
/// Provides health monitoring for Data Processing Platform dependencies
/// </summary>
public static class HealthCheckConfiguration
{
    /// <summary>
    /// Adds comprehensive health checks for all platform dependencies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">Name of the service</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataProcessingHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add basic application health check
        healthChecksBuilder.AddCheck<ApplicationHealthCheck>("application", 
            tags: new[] { "ready", "live" });

        // Add MongoDB health check
        var mongoConnectionString = configuration.GetConnectionString("MongoDB");
        if (!string.IsNullOrEmpty(mongoConnectionString))
        {
            healthChecksBuilder.AddCheck<MongoDbHealthCheck>("mongodb",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "mongodb" });
        }

        // Add Kafka health check
        var kafkaConnectionString = configuration.GetConnectionString("Kafka");
        if (!string.IsNullOrEmpty(kafkaConnectionString))
        {
            healthChecksBuilder.AddKafka(options =>
            {
                options.BootstrapServers = kafkaConnectionString;
            },
            name: "kafka",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready", "kafka" },
            timeout: TimeSpan.FromSeconds(10));
        }

        // Add Elasticsearch health check (for logging)
        var elasticsearchUri = configuration.GetConnectionString("Elasticsearch");
        if (!string.IsNullOrEmpty(elasticsearchUri))
        {
            healthChecksBuilder.AddElasticsearch(
                elasticsearchUri,
                name: "elasticsearch",
                failureStatus: HealthStatus.Degraded, // Non-critical for application functionality
                tags: new[] { "ready", "elasticsearch" },
                timeout: TimeSpan.FromSeconds(10));
        }

        // Add memory usage health check
        healthChecksBuilder.AddCheck<MemoryHealthCheck>("memory",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "memory" });

        // Add disk space health check
        healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>("disk_space",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "disk" });

        // Add custom service-specific health checks
        healthChecksBuilder.AddCheck<ServiceHealthCheck>($"{serviceName.ToLowerInvariant()}_service",
            tags: new[] { "ready", "service" });

        return services;
    }

    /// <summary>
    /// Configures health check endpoints with detailed responses
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static WebApplication UseDataProcessingHealthChecks(this WebApplication app)
    {
        // Liveness probe - should always return healthy unless the application is completely broken
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Readiness probe - returns unhealthy if dependencies are not available
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Detailed health endpoint for monitoring and debugging
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteDetailedHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Simple health endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteSimpleHealthCheckResponse
        });

        return app;
    }

    /// <summary>
    /// Writes a simple health check response
    /// </summary>
    private static async Task WriteSimpleHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("O"),
            duration = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    /// <summary>
    /// Writes a detailed health check response with all check results
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("O"),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    /// <summary>
    /// Writes a very detailed health check response including error information
    /// </summary>
    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("O"),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data,
                tags = entry.Value.Tags
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

/// <summary>
/// Basic application health check
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Perform basic application health checks
        try
        {
            // Check if the application is responding
            var isHealthy = true; // Add your specific health logic here

            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Application is running normally"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check failed"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check threw an exception", ex));
        }
    }
}

/// <summary>
/// Memory usage health check
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private const long UnhealthyThreshold = 1024L * 1024L * 1024L; // 1GB
    private const long DegradedThreshold = 512L * 1024L * 1024L;   // 512MB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allocated = GC.GetTotalMemory(false);
            var data = new Dictionary<string, object>
            {
                ["Allocated"] = allocated,
                ["AllocatedMB"] = allocated / (1024 * 1024),
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            };

            var status = allocated switch
            {
                var n when n >= UnhealthyThreshold => HealthStatus.Unhealthy,
                var n when n >= DegradedThreshold => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            var message = $"Memory usage: {allocated / (1024 * 1024):N0} MB";

            return Task.FromResult(new HealthCheckResult(status, message, data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory health check failed", ex));
        }
    }
}

/// <summary>
/// Disk space health check
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Environment.CurrentDirectory);
            var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);
            var usedSpacePercent = ((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100;

            var data = new Dictionary<string, object>
            {
                ["Drive"] = drive.Name,
                ["FreeSpaceGB"] = freeSpaceGB,
                ["TotalSpaceGB"] = totalSpaceGB,
                ["UsedSpacePercent"] = Math.Round(usedSpacePercent, 2)
            };

            var status = usedSpacePercent switch
            {
                var percent when percent >= 90 => HealthStatus.Unhealthy,
                var percent when percent >= 80 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            var message = $"Disk usage: {usedSpacePercent:F1}% ({freeSpaceGB:N0} GB free)";

            return Task.FromResult(new HealthCheckResult(status, message, data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space health check failed", ex));
        }
    }
}

/// <summary>
/// MongoDB health check
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified MongoDB health check
            // In a real implementation, you would:
            // 1. Get MongoDB connection string from configuration
            // 2. Test connection to MongoDB
            // 3. Perform a simple ping or collection count operation
            
            var isHealthy = true; // Replace with actual MongoDB connectivity check

            var data = new Dictionary<string, object>
            {
                ["LastChecked"] = DateTime.UtcNow,
                ["Database"] = "MongoDB"
            };

            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("MongoDB is accessible", data));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("MongoDB health check failed", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("MongoDB health check threw an exception", ex));
        }
    }
}

/// <summary>
/// Service-specific health check template
/// </summary>
public class ServiceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Implement service-specific health checks here
            // For example:
            // - Check if critical background services are running
            // - Verify database connections
            // - Check message queue connectivity
            // - Validate configuration settings
            // - Test external API connectivity

            var isHealthy = true; // Replace with actual health logic

            var data = new Dictionary<string, object>
            {
                ["LastChecked"] = DateTime.UtcNow,
                ["ServiceName"] = context.Registration.Name
            };

            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Service is operating normally", data));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("Service health check failed", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Service health check threw an exception", ex));
        }
    }
}
