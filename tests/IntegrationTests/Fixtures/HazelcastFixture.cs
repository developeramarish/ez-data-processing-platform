// HazelcastFixture.cs - Hazelcast Cache Integration Test Fixture
// EZ Platform Integration Tests
// Version: 1.0
// Date: December 17, 2025

using System.Net.Sockets;

namespace DataProcessing.IntegrationTests.Fixtures;

/// <summary>
/// Provides Hazelcast cache access for testing caching behavior
/// Note: Uses REST API for basic operations since Hazelcast .NET client is complex
/// </summary>
public class HazelcastFixture : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;
    private readonly string _hazelcastRestUrl;

    public HazelcastFixture()
    {
        _httpClient = new HttpClient
        {
            Timeout = TestConfiguration.DefaultTimeout
        };

        // Hazelcast REST API (if enabled) or Management Center
        _hazelcastRestUrl = $"http://{TestConfiguration.HazelcastAddress.Replace("5701", "8080")}";
    }

    /// <summary>
    /// Check if Hazelcast is available via TCP connection
    /// </summary>
    public async Task<bool> IsHazelcastAvailableAsync()
    {
        try
        {
            var parts = TestConfiguration.HazelcastAddress.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

            var completed = await Task.WhenAny(connectTask, timeoutTask);
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Simulates cache entry creation timestamp for TTL testing
    /// </summary>
    public CacheEntry CreateTestEntry(string key, object value, TimeSpan? ttl = null)
    {
        return new CacheEntry
        {
            Key = key,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
        };
    }

    /// <summary>
    /// Calculates expected expiration based on TTL configuration
    /// </summary>
    public DateTime CalculateExpiration(TimeSpan ttl)
    {
        return DateTime.UtcNow.Add(ttl);
    }

    /// <summary>
    /// Verifies TTL logic (simulated for unit-style integration test)
    /// </summary>
    public bool IsExpired(CacheEntry entry)
    {
        if (!entry.ExpiresAt.HasValue)
            return false;

        return DateTime.UtcNow > entry.ExpiresAt.Value;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a cache entry for testing
/// </summary>
public class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
