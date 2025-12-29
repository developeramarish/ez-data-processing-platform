using Hazelcast;
using Hazelcast.Core;
using Hazelcast.DistributedObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace DataProcessing.Shared.Configuration;

/// <summary>
/// Configuration and setup for Hazelcast distributed cache with resilience patterns
/// </summary>
public static class HazelcastConfiguration
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5)
    ];

    /// <summary>
    /// Add Hazelcast client with resilience configuration to DI container
    /// </summary>
    public static IServiceCollection AddResilientHazelcast(
        this IServiceCollection services,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HazelcastConfiguration");

        // Register the resilient wrapper as singleton
        // Note: Hazelcast client creation uses the passed loggerFactory (startup logs)
        // But ResilientHazelcastClient gets logger from DI so operational logs go through OTEL
        services.AddSingleton<IResilientHazelcastClient>(sp =>
        {
            var client = CreateHazelcastClientAsync(configuration, loggerFactory).GetAwaiter().GetResult();
            var serviceLogger = sp.GetRequiredService<ILogger<ResilientHazelcastClient>>();
            return new ResilientHazelcastClient(client, serviceLogger);
        });

        // Also register raw IHazelcastClient for backward compatibility
        services.AddSingleton<IHazelcastClient>(sp =>
        {
            var resilientClient = sp.GetRequiredService<IResilientHazelcastClient>();
            return resilientClient.Client;
        });

        logger.LogInformation("Hazelcast resilient client registered");
        return services;
    }

    /// <summary>
    /// Create a Hazelcast client with retry and reconnection support
    /// </summary>
    public static async Task<IHazelcastClient> CreateHazelcastClientAsync(
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HazelcastConfiguration");
        var server = configuration["Hazelcast:Server"] ?? "hazelcast:5701";
        var clusterName = configuration["Hazelcast:ClusterName"] ?? "data-processing-cluster";

        logger.LogInformation("Connecting to Hazelcast cluster {ClusterName} at {Server}", clusterName, server);

        var options = new HazelcastOptionsBuilder()
            .With(opts =>
            {
                opts.ClusterName = clusterName;
                opts.Networking.Addresses.Add(server);

                // Connection retry configuration
                opts.Networking.ConnectionRetry.ClusterConnectionTimeoutMilliseconds = 60000; // 60 seconds total
                opts.Networking.ConnectionRetry.InitialBackoffMilliseconds = 1000;
                opts.Networking.ConnectionRetry.MaxBackoffMilliseconds = 30000;
                opts.Networking.ConnectionRetry.Multiplier = 2.0;
                opts.Networking.ConnectionRetry.Jitter = 0.2;

                // Reconnection configuration
                opts.Networking.ReconnectMode = Hazelcast.Networking.ReconnectMode.ReconnectAsync;
                opts.Networking.ConnectionTimeoutMilliseconds = 30000;
                opts.Networking.SmartRouting = true;

                // Heartbeat to detect dead connections
                opts.Heartbeat.TimeoutMilliseconds = 60000;
                opts.Heartbeat.PeriodMilliseconds = 10000;

                // Use provided logger factory
                opts.LoggerFactory.Creator = () => loggerFactory;
            })
            .Build();

        // Retry client creation with exponential backoff
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                RetryDelays,
                (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(exception,
                        "Hazelcast connection attempt {RetryCount} failed. Retrying in {Delay}ms",
                        retryCount, timeSpan.TotalMilliseconds);
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var client = await HazelcastClientFactory.StartNewClientAsync(options);
            logger.LogInformation("Hazelcast client connected successfully to {Server}", server);
            return client;
        });
    }

    /// <summary>
    /// Create resilience policy for Hazelcast operations
    /// </summary>
    public static AsyncRetryPolicy CreateOperationRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<Hazelcast.Exceptions.HazelcastException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                RetryDelays,
                (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(exception,
                        "Hazelcast operation failed (attempt {RetryCount}). Retrying in {Delay}ms. Operation: {Operation}",
                        retryCount, timeSpan.TotalMilliseconds, context.OperationKey ?? "unknown");
                });
    }

    /// <summary>
    /// Create circuit breaker policy for Hazelcast operations
    /// </summary>
    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .Handle<Hazelcast.Exceptions.HazelcastException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    logger.LogError(exception,
                        "Hazelcast circuit breaker opened for {Duration}s due to repeated failures",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("Hazelcast circuit breaker reset - connections restored");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Hazelcast circuit breaker half-open - testing connection");
                });
    }
}

/// <summary>
/// Interface for resilient Hazelcast operations
/// </summary>
public interface IResilientHazelcastClient : IAsyncDisposable
{
    /// <summary>
    /// The underlying Hazelcast client
    /// </summary>
    IHazelcastClient Client { get; }

    /// <summary>
    /// Check if the client is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get a map with resilient operations
    /// </summary>
    Task<IHMap<TKey, TValue>> GetMapAsync<TKey, TValue>(string mapName) where TKey : notnull;

    /// <summary>
    /// Get value from map with retry logic
    /// </summary>
    Task<TValue?> GetAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull;

    /// <summary>
    /// Set value in map with retry logic
    /// </summary>
    Task SetAsync<TKey, TValue>(string mapName, TKey key, TValue value, TimeSpan? ttl = null) where TKey : notnull;

    /// <summary>
    /// Delete value from map with retry logic
    /// </summary>
    Task<bool> DeleteAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull;

    /// <summary>
    /// Check if key exists with retry logic
    /// </summary>
    Task<bool> ContainsKeyAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull;

    /// <summary>
    /// Reconnect the client if disconnected
    /// </summary>
    Task ReconnectAsync();
}

/// <summary>
/// Resilient wrapper around Hazelcast client with automatic retry and reconnection
/// </summary>
public class ResilientHazelcastClient : IResilientHazelcastClient
{
    private readonly IHazelcastClient _client;
    private readonly ILogger<ResilientHazelcastClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    public ResilientHazelcastClient(IHazelcastClient client, ILogger<ResilientHazelcastClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = HazelcastConfiguration.CreateOperationRetryPolicy(_logger);
        _circuitBreaker = HazelcastConfiguration.CreateCircuitBreakerPolicy(_logger);

        _logger.LogInformation("Resilient Hazelcast client initialized with retry and circuit breaker policies");
    }

    public IHazelcastClient Client => _client;

    public bool IsConnected => _client.State == ClientState.Connected;

    public async Task<IHMap<TKey, TValue>> GetMapAsync<TKey, TValue>(string mapName) where TKey : notnull
    {
        return await ExecuteWithResilienceAsync(
            () => _client.GetMapAsync<TKey, TValue>(mapName),
            $"GetMap:{mapName}");
    }

    public async Task<TValue?> GetAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull
    {
        return await ExecuteWithResilienceAsync(async () =>
        {
            var map = await _client.GetMapAsync<TKey, TValue>(mapName);
            return await map.GetAsync(key);
        }, $"Get:{mapName}:{key}");
    }

    public async Task SetAsync<TKey, TValue>(string mapName, TKey key, TValue value, TimeSpan? ttl = null) where TKey : notnull
    {
        await ExecuteWithResilienceAsync(async () =>
        {
            var map = await _client.GetMapAsync<TKey, TValue>(mapName);
            if (ttl.HasValue)
            {
                await map.SetAsync(key, value, ttl.Value);
            }
            else
            {
                await map.SetAsync(key, value);
            }
            return true;
        }, $"Set:{mapName}:{key}");
    }

    public async Task<bool> DeleteAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull
    {
        return await ExecuteWithResilienceAsync(async () =>
        {
            var map = await _client.GetMapAsync<TKey, TValue>(mapName);
            var removed = await map.RemoveAsync(key);
            return removed != null;
        }, $"Delete:{mapName}:{key}");
    }

    public async Task<bool> ContainsKeyAsync<TKey, TValue>(string mapName, TKey key) where TKey : notnull
    {
        return await ExecuteWithResilienceAsync(async () =>
        {
            var map = await _client.GetMapAsync<TKey, TValue>(mapName);
            return await map.ContainsKeyAsync(key);
        }, $"ContainsKey:{mapName}:{key}");
    }

    public async Task ReconnectAsync()
    {
        if (!await _reconnectLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("Reconnect already in progress, skipping");
            return;
        }

        try
        {
            if (IsConnected)
            {
                _logger.LogDebug("Client already connected, skipping reconnect");
                return;
            }

            _logger.LogInformation("Initiating Hazelcast reconnection...");

            // The Hazelcast client should auto-reconnect based on configuration
            // This method is for manual reconnection if needed
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;

            while (!IsConnected && DateTime.UtcNow - startTime < timeout)
            {
                await Task.Delay(1000);
                _logger.LogDebug("Waiting for Hazelcast reconnection... State: {State}", _client.State);
            }

            if (IsConnected)
            {
                _logger.LogInformation("Hazelcast reconnected successfully");
            }
            else
            {
                _logger.LogError("Hazelcast reconnection timed out after {Timeout}s", timeout.TotalSeconds);
            }
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        _reconnectLock.Dispose();
    }

    private async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, string operationKey)
    {
        var context = new Context(operationKey);

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                // Check connection state before operation
                if (!IsConnected)
                {
                    _logger.LogWarning("Hazelcast not connected, attempting reconnection before {Operation}", operationKey);
                    await ReconnectAsync();
                }

                return await operation();
            }, context);
        });
    }

}
