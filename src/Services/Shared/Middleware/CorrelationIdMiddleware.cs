using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MassTransit;

namespace DataProcessing.Shared.Middleware;

/// <summary>
/// Middleware for managing correlation IDs across HTTP requests
/// Ensures consistent correlation ID tracking for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdActivityTagName = "correlation-id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrGenerateCorrelationId(context);
        
        // Set correlation ID in response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        
        // Add to HttpContext items for access throughout the request
        context.Items["CorrelationId"] = correlationId;
        
        // Add to current activity for distributed tracing
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag(CorrelationIdActivityTagName, correlationId);
            activity.SetTag("http.request.correlation_id", correlationId);
        }
        
        // Set in call context for logging enrichment
        CallContext.LogicalSetData("CorrelationId", correlationId);
        CallContext.LogicalSetData("HttpContext", context);
        
        try
        {
            // Log request start with correlation ID
            _logger.LogInformation("HTTP Request started: {Method} {Path} [CorrelationId: {CorrelationId}]", 
                context.Request.Method, context.Request.Path, correlationId);
            
            await _next(context);
            
            // Log request completion with correlation ID
            _logger.LogInformation("HTTP Request completed: {Method} {Path} {StatusCode} [CorrelationId: {CorrelationId}]", 
                context.Request.Method, context.Request.Path, context.Response.StatusCode, correlationId);
        }
        catch (Exception ex)
        {
            // Log error with correlation ID
            _logger.LogError(ex, "HTTP Request failed: {Method} {Path} [CorrelationId: {CorrelationId}]", 
                context.Request.Method, context.Request.Path, correlationId);
            throw;
        }
        finally
        {
            // Clean up call context
            CallContext.FreeNamedDataSlot("CorrelationId");
            CallContext.FreeNamedDataSlot("HttpContext");
        }
    }

    /// <summary>
    /// Extracts correlation ID from request headers or generates a new one
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Correlation ID</returns>
    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) && 
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }
        
        // Try to get from trace ID if available
        var activity = Activity.Current;
        if (activity != null && !string.IsNullOrWhiteSpace(activity.TraceId.ToString()))
        {
            return activity.TraceId.ToString();
        }
        
        // Generate new correlation ID
        return Guid.NewGuid().ToString("D");
    }
}

/// <summary>
/// Extensions for registering correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the request pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}

/// <summary>
/// Helper class for managing call context data (duplicate from LoggingConfiguration for independence)
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
/// Service for managing correlation IDs in non-HTTP contexts
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string? GetCorrelationId();
    
    /// <summary>
    /// Sets the correlation ID for the current context
    /// </summary>
    void SetCorrelationId(string correlationId);
    
    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    string GenerateCorrelationId();
}

/// <summary>
/// Implementation of correlation ID service
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current correlation ID from various sources
    /// </summary>
    public string? GetCorrelationId()
    {
        // Try to get from HTTP context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        {
            return correlationId?.ToString();
        }

        // Try to get from current activity
        var activity = Activity.Current;
        if (activity != null)
        {
            var correlationTag = activity.Tags.FirstOrDefault(t => t.Key == "correlation-id");
            if (!string.IsNullOrEmpty(correlationTag.Value))
            {
                return correlationTag.Value;
            }
        }

        // Try to get from call context
        var contextCorrelationId = CallContext.LogicalGetData("CorrelationId");
        if (contextCorrelationId != null)
        {
            return contextCorrelationId.ToString();
        }

        return null;
    }

    /// <summary>
    /// Sets the correlation ID in various contexts
    /// </summary>
    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        // Set in HTTP context if available
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["CorrelationId"] = correlationId;
        }

        // Set in current activity
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("correlation-id", correlationId);
        }

        // Set in call context
        CallContext.LogicalSetData("CorrelationId", correlationId);
    }

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    public string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("D");
    }
}

/// <summary>
/// Extension methods for correlation ID service registration
/// </summary>
public static class CorrelationIdServiceExtensions
{
    /// <summary>
    /// Adds correlation ID service to DI container
    /// </summary>
    public static IServiceCollection AddCorrelationIdService(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();
        return services;
    }
}

/// <summary>
/// Correlation ID extensions for message publishing
/// </summary>
public static class CorrelationIdMessageExtensions
{
    /// <summary>
    /// Sets correlation ID on MassTransit message headers
    /// </summary>
    public static void SetCorrelationId(this SendContext context, string correlationId)
    {
        context.Headers.Set("X-Correlation-ID", correlationId);
        context.CorrelationId = Guid.TryParse(correlationId, out var guid) ? guid : Guid.NewGuid();
    }

    /// <summary>
    /// Gets correlation ID from MassTransit message headers
    /// </summary>
    public static string? GetCorrelationId(this ConsumeContext context)
    {
        // Try to get from headers
        if (context.Headers.TryGetHeader("X-Correlation-ID", out var headerValue))
        {
            return headerValue?.ToString();
        }

        // Fall back to MassTransit correlation ID
        return context.CorrelationId?.ToString("D");
    }
}
