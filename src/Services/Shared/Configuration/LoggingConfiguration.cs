using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;
using System.Text.Json;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration for structured logging with Elasticsearch integration
/// Provides comprehensive logging for Data Processing Platform
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with Elasticsearch sink and enrichers
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Host environment</param>
    /// <param name="serviceName">Name of the service for logging</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataProcessingLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        // Get configuration values
        var elasticsearchUri = configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
        var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";
        var enableFileLogging = configuration.GetValue<bool>("Logging:EnableFileLogging", true);
        var enableConsoleLogging = configuration.GetValue<bool>("Logging:EnableConsoleLogging", true);
        
        // Parse log level
        if (!Enum.TryParse<LogEventLevel>(logLevel, true, out var serilogLevel))
        {
            serilogLevel = LogEventLevel.Information;
        }

        // Configure Serilog
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(serilogLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
            
            // Add enrichers
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ThreadId", Environment.CurrentManagedThreadId)
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.WithProperty("Application", "DataProcessing.Platform")
            .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");

        // Add correlation ID enricher
        loggerConfiguration.Enrich.With<CorrelationIdEnricher>();

        // Add console logging
        if (enableConsoleLogging)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                restrictedToMinimumLevel: serilogLevel);
        }

        // Add file logging
        if (enableFileLogging)
        {
            var logPath = configuration.GetValue<string>("Logging:FilePath") ?? "logs/dataprocessing-.txt";
            loggerConfiguration.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: serilogLevel,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // Add Elasticsearch logging
        var elasticsearchOptions = new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
        {
            IndexFormat = $"dataprocessing-{serviceName.ToLowerInvariant()}-logs-{{0:yyyy.MM.dd}}",
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            TypeName = null, // For Elasticsearch 7.x compatibility
            BatchAction = ElasticOpType.Index,
            MinimumLogEventLevel = serilogLevel,
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                              EmitEventFailureHandling.WriteToFailureSink |
                              EmitEventFailureHandling.RaiseCallback,
            FailureCallback = e => 
            {
                // Log to file sink as alternative
                System.IO.File.AppendAllText("logs/elasticsearch-failures.txt", 
                    $"{DateTime.UtcNow:O}: {e.MessageTemplate} - {e.Exception?.Message}{Environment.NewLine}");
            },
            
            // Custom fields for better searching and analysis
            CustomFormatter = new ElasticsearchJsonFormatter(),
            
            // Connection settings
            ConnectionTimeout = TimeSpan.FromSeconds(30),
            NumberOfShards = 1,
            NumberOfReplicas = 0
        };

        loggerConfiguration.WriteTo.Elasticsearch(elasticsearchOptions);

        // Configure request/response logging for web applications
        loggerConfiguration.Enrich.With<RequestResponseEnricher>();

        // Create the logger
        Log.Logger = loggerConfiguration.CreateLogger();

        // Add Serilog to DI container
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        return services;
    }

    /// <summary>
    /// Configures request logging middleware for ASP.NET Core applications
    /// </summary>
    public static void UseDataProcessingRequestLogging(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            
            // Emit debug-level events instead of the defaults
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error 
                : httpContext.Response.StatusCode > 499 
                    ? LogEventLevel.Error 
                    : LogEventLevel.Information;

            // Attach additional properties to the request completion event
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                
                // Add correlation ID if present
                var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                                  httpContext.Items["CorrelationId"]?.ToString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId);
                }

                // Add user information if authenticated
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
                    diagnosticContext.Set("UserClaims", httpContext.User.Claims.ToDictionary(c => c.Type, c => c.Value));
                }
            };
        });
    }
}

/// <summary>
/// Custom enricher for correlation ID tracking
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }

    private static string? GetCorrelationId()
    {
        // Try to get correlation ID from current activity
        var activity = System.Diagnostics.Activity.Current;
        if (activity != null)
        {
            var correlationTag = activity.Tags.FirstOrDefault(t => t.Key == "correlation-id");
            if (!string.IsNullOrEmpty(correlationTag.Value))
            {
                return correlationTag.Value;
            }
        }

        // Try to get from AsyncLocal or Thread context
        return CallContext.LogicalGetData("CorrelationId")?.ToString();
    }
}

/// <summary>
/// Custom enricher for request/response information
/// </summary>
public class RequestResponseEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // This will be populated by middleware
        var httpContext = CallContext.LogicalGetData("HttpContext") as Microsoft.AspNetCore.Http.HttpContext;
        if (httpContext != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("StatusCode", httpContext.Response.StatusCode));
        }
    }
}

/// <summary>
/// Custom JSON formatter for Elasticsearch with structured fields
/// </summary>
public class ElasticsearchJsonFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var json = new
        {
            timestamp = logEvent.Timestamp.ToString("O"),
            level = logEvent.Level.ToString(),
            messageTemplate = logEvent.MessageTemplate.Text,
            message = logEvent.RenderMessage(),
            exception = logEvent.Exception?.ToString(),
            properties = logEvent.Properties.ToDictionary(
                p => p.Key,
                p => RenderPropertyValue(p.Value)),
            fields = new
            {
                service = logEvent.Properties.TryGetValue("Service", out var service) 
                    ? service.ToString()?.Trim('"') : null,
                environment = logEvent.Properties.TryGetValue("Environment", out var env) 
                    ? env.ToString()?.Trim('"') : null,
                correlationId = logEvent.Properties.TryGetValue("CorrelationId", out var corrId) 
                    ? corrId.ToString()?.Trim('"') : null,
                machineName = logEvent.Properties.TryGetValue("MachineName", out var machine) 
                    ? machine.ToString()?.Trim('"') : null,
                threadId = logEvent.Properties.TryGetValue("ThreadId", out var thread) 
                    ? thread.ToString()?.Trim('"') : null
            }
        };

        output.Write(System.Text.Json.JsonSerializer.Serialize(json, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        }));
        output.WriteLine();
    }

    private static object? RenderPropertyValue(LogEventPropertyValue propertyValue)
    {
        if (propertyValue is ScalarValue scalar)
        {
            return scalar.Value;
        }
        
        if (propertyValue is SequenceValue sequence)
        {
            return sequence.Elements.Select(RenderPropertyValue).ToArray();
        }
        
        if (propertyValue is StructureValue structure)
        {
            return structure.Properties.ToDictionary(
                p => p.Name,
                p => RenderPropertyValue(p.Value));
        }
        
        return propertyValue.ToString()?.Trim('"');
    }
}

/// <summary>
/// Helper class for managing call context data
/// </summary>
public static class CallContext
{
    private static readonly AsyncLocal<Dictionary<string, object?>> _data = new();

    public static void LogicalSetData(string name, object? data)
    {
        if (_data.Value == null)
        {
            _data.Value = new Dictionary<string, object?>();
        }
        _data.Value[name] = data;
    }

    public static object? LogicalGetData(string name)
    {
        return _data.Value?.TryGetValue(name, out var value) == true ? value : null;
    }

    public static void FreeNamedDataSlot(string name)
    {
        _data.Value?.Remove(name);
    }
}

/// <summary>
/// Extension methods for easier logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs with correlation ID context
    /// </summary>
    public static void LogInformationWithCorrelation(this Microsoft.Extensions.Logging.ILogger logger, string correlationId, string message, params object[] args)
    {
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs error with correlation ID context
    /// </summary>
    public static void LogErrorWithCorrelation(this Microsoft.Extensions.Logging.ILogger logger, Exception exception, string correlationId, string message, params object[] args)
    {
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Logs with file processing context
    /// </summary>
    public static void LogFileProcessing(this Microsoft.Extensions.Logging.ILogger logger, string correlationId, string fileName, string operation, params object[] args)
    {
        using (logger.BeginScope(new Dictionary<string, object> 
        { 
            ["CorrelationId"] = correlationId,
            ["FileName"] = fileName,
            ["Operation"] = operation
        }))
        {
            logger.LogInformation($"File processing {operation}: {fileName}", args);
        }
    }

    /// <summary>
    /// Logs with validation context
    /// </summary>
    public static void LogValidation(this Microsoft.Extensions.Logging.ILogger logger, string correlationId, string dataSourceId, int totalRecords, int validRecords, int invalidRecords)
    {
        using (logger.BeginScope(new Dictionary<string, object> 
        { 
            ["CorrelationId"] = correlationId,
            ["DataSourceId"] = dataSourceId,
            ["TotalRecords"] = totalRecords,
            ["ValidRecords"] = validRecords,
            ["InvalidRecords"] = invalidRecords
        }))
        {
            logger.LogInformation("Validation completed: {TotalRecords} total, {ValidRecords} valid, {InvalidRecords} invalid", 
                totalRecords, validRecords, invalidRecords);
        }
    }
}
