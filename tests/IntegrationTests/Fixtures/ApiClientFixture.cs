// ApiClientFixture.cs - HTTP API Client Integration Test Fixture
// EZ Platform Integration Tests
// Version: 1.0
// Date: December 17, 2025

using System.Net.Http.Json;
using System.Text.Json;

namespace DataProcessing.IntegrationTests.Fixtures;

/// <summary>
/// Provides HTTP clients for testing service APIs
/// </summary>
public class ApiClientFixture : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public ApiClientFixture()
    {
        _httpClient = new HttpClient
        {
            Timeout = TestConfiguration.DefaultTimeout
        };
    }

    /// <summary>
    /// Creates a datasource via the management API
    /// </summary>
    public async Task<JsonElement?> CreateDatasourceAsync(object datasource)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{TestConfiguration.DatasourceManagementUrl}/api/v1/datasource",
            datasource);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("Data");
        }

        return null;
    }

    /// <summary>
    /// Gets a datasource by ID
    /// </summary>
    public async Task<JsonElement?> GetDatasourceAsync(string id)
    {
        var response = await _httpClient.GetAsync(
            $"{TestConfiguration.DatasourceManagementUrl}/api/v1/datasource/{id}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("Data");
        }

        return null;
    }

    /// <summary>
    /// Gets all datasources
    /// </summary>
    public async Task<JsonElement?> GetAllDatasourcesAsync()
    {
        var response = await _httpClient.GetAsync(
            $"{TestConfiguration.DatasourceManagementUrl}/api/v1/datasource");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("Data");
        }

        return null;
    }

    /// <summary>
    /// Deletes a datasource by ID
    /// </summary>
    public async Task<bool> DeleteDatasourceAsync(string id)
    {
        var response = await _httpClient.DeleteAsync(
            $"{TestConfiguration.DatasourceManagementUrl}/api/v1/datasource/{id}?deletedBy=IntegrationTest");

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets invalid records from the API
    /// </summary>
    public async Task<JsonElement?> GetInvalidRecordsAsync(string? datasourceId = null, int page = 1, int pageSize = 20)
    {
        var url = $"{TestConfiguration.InvalidRecordsUrl}/api/v1/invalid-records?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(datasourceId))
            url += $"&datasourceId={datasourceId}";

        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            return doc.RootElement;
        }

        return null;
    }

    /// <summary>
    /// Checks health of a service
    /// </summary>
    public async Task<bool> CheckHealthAsync(string serviceUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{serviceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks health of all services
    /// </summary>
    public async Task<Dictionary<string, bool>> CheckAllServicesHealthAsync()
    {
        var services = new Dictionary<string, string>
        {
            ["DatasourceManagement"] = TestConfiguration.DatasourceManagementUrl,
            ["Validation"] = TestConfiguration.ValidationUrl,
            ["Output"] = TestConfiguration.OutputUrl,
            ["InvalidRecords"] = TestConfiguration.InvalidRecordsUrl
        };

        var results = new Dictionary<string, bool>();

        foreach (var (name, url) in services)
        {
            results[name] = await CheckHealthAsync(url);
        }

        return results;
    }

    /// <summary>
    /// Waits for a service to become healthy
    /// </summary>
    public async Task<bool> WaitForServiceHealthAsync(string serviceUrl, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await CheckHealthAsync(serviceUrl))
                return true;

            await Task.Delay(1000);
        }

        return false;
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
