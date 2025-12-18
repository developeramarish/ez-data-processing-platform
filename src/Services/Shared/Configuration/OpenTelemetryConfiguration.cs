using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration for OpenTelemetry distributed tracing
/// Provides comprehensive tracing for Data Processing Platform
/// </summary>
public static class OpenTelemetryConfiguration
{
    // ActivitySource for Data Processing Platform
    public static readonly ActivitySource DataProcessingActivitySource = new("DataProcessing.Platform", "1.0.0");
    
    /// <summary>
    /// Configures comprehensive OpenTelemetry (Metrics, Traces, Logs) with OTLP export to Collector
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">Name of the service for telemetry</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataProcessingOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        // Get configuration values
        var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";
        var enableMetrics = configuration.GetValue<bool>("OpenTelemetry:EnableMetrics", true);
        var enableTraces = configuration.GetValue<bool>("OpenTelemetry:EnableTraces", true);
        var enableLogs = configuration.GetValue<bool>("OpenTelemetry:EnableLogs", true);
        var samplingRatio = configuration.GetValue<double>("OpenTelemetry:SamplingRatio", 1.0);
        var enableConsoleExporter = configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter", false);

        var openTelemetryBuilder = services.AddOpenTelemetry();

        // Configure resource attributes
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, "DataProcessing.Platform", "1.0.0")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration.GetValue<string>("Environment") ?? "development",
                ["service.namespace"] = "dataprocessing",
                ["service.instance.id"] = Environment.MachineName,
                ["service.version"] = "1.0.0"
            });

        // Configure Metrics
        if (enableMetrics)
        {
            openTelemetryBuilder.WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    
                    // System metrics (auto-instrumented) - will be routed to System Prometheus
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    
                    // Business metrics meter - will be routed to Business Prometheus (business_* prefix)
                    .AddMeter("DataProcessing.Business.Metrics")
                    .AddMeter("DataProcessing.Business.*")

                    // Service-specific meters (validation, processing, etc.)
                    .AddMeter("DataProcessing.Validation")
                    .AddMeter("DataProcessing.FileProcessor")
                    .AddMeter("DataProcessing.Output")
                    .AddMeter($"{serviceName}.Metrics")
                    
                    // View to customize metric behavior
                    .AddView("http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                        });

                // Export to OpenTelemetry Collector
                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });

                if (enableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }
            });
        }

        // Configure Logging
        if (enableLogs)
        {
            openTelemetryBuilder.WithLogging(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });

                if (enableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }
            });
        }

        // Configure Tracing
        if (enableTraces)
        {
            openTelemetryBuilder.WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    
                    // Add instrumentation
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out health check endpoints from tracing
                        options.Filter = (httpContext) =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLowerInvariant();
                            return !path?.Contains("/health") == true && 
                                   !path?.Contains("/metrics") == true;
                        };

                        // Enrich traces with additional data
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            var correlationId = httpRequest.Headers["X-Correlation-ID"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(correlationId))
                            {
                                activity.SetTag("correlation-id", correlationId);
                            }
                            
                            activity.SetTag("http.request.header.user_agent", 
                                httpRequest.Headers.UserAgent.ToString());
                        };

                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.status_code", httpResponse.StatusCode);
                            if (httpResponse.StatusCode >= 400)
                            {
                                activity.SetStatus(ActivityStatusCode.Error, $"HTTP {httpResponse.StatusCode}");
                            }
                        };

                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("exception.type", exception.GetType().FullName);
                            activity.SetTag("exception.message", exception.Message);
                            activity.SetTag("exception.stacktrace", exception.StackTrace);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Enrich HTTP client traces
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("http.request.method", httpRequestMessage.Method.ToString());
                            activity.SetTag("http.request.uri", httpRequestMessage.RequestUri?.ToString());
                        };

                        options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        {
                            activity.SetTag("http.response.status_code", (int)httpResponseMessage.StatusCode);
                            if (!httpResponseMessage.IsSuccessStatusCode)
                            {
                                activity.SetStatus(ActivityStatusCode.Error, $"HTTP {httpResponseMessage.StatusCode}");
                            }
                        };

                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("exception.type", exception.GetType().FullName);
                            activity.SetTag("exception.message", exception.Message);
                        };
                    })
                    
                    // Add custom activity sources
                    .AddSource("DataProcessing.Platform")
                    .AddSource("DataProcessing.Consumer")
                    .AddSource("DataProcessing.Validation")
                    .AddSource("DataProcessing.FileProcessor")
                    .AddSource("MassTransit")
                    .AddSource($"{serviceName}.*")
                    
                    // Set sampling
                    .SetSampler(new TraceIdRatioBasedSampler(samplingRatio));

                // Export to OpenTelemetry Collector
                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });

                if (enableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }
            });
        }

        // Register the ActivitySource and Meter as singletons
        services.AddSingleton(DataProcessingActivitySource);
        
        // Register Business Metrics Meter
        services.AddSingleton<Meter>(sp => 
            new Meter("DataProcessing.Business.Metrics", "1.0.0"));
        
        // Register Service-specific Meter
        services.AddSingleton(sp => 
            new Meter($"{serviceName}.Metrics", "1.0.0"));

        return services;
    }

    /// <summary>
    /// Extension method to start a new activity with standard tags
    /// </summary>
    public static Activity? StartDataProcessingActivity(this ActivitySource activitySource, 
        string operationName, 
        string? correlationId = null,
        string? dataSourceId = null,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = activitySource.StartActivity(operationName, kind);
        
        if (activity != null)
        {
            activity.SetTag("service.name", "DataProcessing.Platform");
            activity.SetTag("service.version", "1.0.0");
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("correlation-id", correlationId);
            }
            
            if (!string.IsNullOrEmpty(dataSourceId))
            {
                activity.SetTag("data-source-id", dataSourceId);
            }

            activity.SetTag("timestamp", DateTimeOffset.UtcNow.ToString("O"));
        }

        return activity;
    }

    /// <summary>
    /// Extension method to add standard error information to activity
    /// </summary>
    public static void SetError(this Activity activity, Exception exception, string? additionalContext = null)
    {
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("error", true);
        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);
        
        if (!string.IsNullOrEmpty(additionalContext))
        {
            activity.SetTag("error.context", additionalContext);
        }
    }

    /// <summary>
    /// Extension method to add file processing context to activity
    /// </summary>
    public static void SetFileProcessingContext(this Activity activity, 
        string fileName, 
        string? filePath = null,
        long? fileSizeBytes = null)
    {
        activity.SetTag("file.name", fileName);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            activity.SetTag("file.path", filePath);
        }
        
        if (fileSizeBytes.HasValue)
        {
            activity.SetTag("file.size.bytes", fileSizeBytes.Value);
        }

        activity.SetTag("file.extension", Path.GetExtension(fileName));
    }

    /// <summary>
    /// Extension method to add message processing context to activity
    /// </summary>
    public static void SetMessageProcessingContext(this Activity activity,
        string messageType,
        string? correlationId = null,
        string? source = null)
    {
        activity.SetTag("message.type", messageType);
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag("correlation-id", correlationId);
        }
        
        if (!string.IsNullOrEmpty(source))
        {
            activity.SetTag("message.source", source);
        }

        activity.SetTag("message.timestamp", DateTimeOffset.UtcNow.ToString("O"));
    }

    /// <summary>
    /// Extension method to add validation context to activity
    /// </summary>
    public static void SetValidationContext(this Activity activity,
        int totalRecords,
        int validRecords,
        int invalidRecords,
        string? schemaVersion = null)
    {
        activity.SetTag("validation.total_records", totalRecords);
        activity.SetTag("validation.valid_records", validRecords);
        activity.SetTag("validation.invalid_records", invalidRecords);
        activity.SetTag("validation.success_rate", 
            totalRecords > 0 ? (double)validRecords / totalRecords : 1.0);
        
        if (!string.IsNullOrEmpty(schemaVersion))
        {
            activity.SetTag("validation.schema_version", schemaVersion);
        }
    }
}

/// <summary>
/// Helper class for creating child activities with proper context propagation
/// </summary>
public static class ActivityHelper
{
    /// <summary>
    /// Creates a child activity with automatic context propagation
    /// </summary>
    public static Activity? CreateChildActivity(string operationName, string? correlationId = null)
    {
        var activity = OpenTelemetryConfiguration.DataProcessingActivitySource
            .StartDataProcessingActivity(operationName, correlationId);
        
        return activity;
    }

    /// <summary>
    /// Safely executes an operation within a traced activity
    /// </summary>
    public static async Task<T> ExecuteWithTracing<T>(
        string operationName,
        Func<Activity?, Task<T>> operation,
        string? correlationId = null,
        string? dataSourceId = null)
    {
        using var activity = OpenTelemetryConfiguration.DataProcessingActivitySource
            .StartDataProcessingActivity(operationName, correlationId, dataSourceId);
        
        try
        {
            var result = await operation(activity);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
    }

    /// <summary>
    /// Safely executes a void operation within a traced activity
    /// </summary>
    public static async Task ExecuteWithTracing(
        string operationName,
        Func<Activity?, Task> operation,
        string? correlationId = null,
        string? dataSourceId = null)
    {
        using var activity = OpenTelemetryConfiguration.DataProcessingActivitySource
            .StartDataProcessingActivity(operationName, correlationId, dataSourceId);
        
        try
        {
            await operation(activity);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
    }
}
