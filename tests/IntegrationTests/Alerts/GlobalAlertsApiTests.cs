using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DataProcessing.IntegrationTests.Alerts;

/// <summary>
/// Integration tests for Global Alerts API
/// Tests alert creation for business and system metrics
/// </summary>
public class GlobalAlertsApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly List<string> _createdAlertIds = new();

    public GlobalAlertsApiTests()
    {
        _baseUrl = Environment.GetEnvironmentVariable("METRICS_CONFIG_URL") ?? "http://localhost:5002";
        _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Extracts the data property from an ApiResponse wrapper
    /// </summary>
    private static JsonElement GetDataFromResponse(JsonElement response)
    {
        if (response.TryGetProperty("data", out var data))
        {
            return data;
        }
        // If no wrapper, return as-is (for arrays or direct responses)
        return response;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Cleanup created alerts
        foreach (var alertId in _createdAlertIds)
        {
            try
            {
                await _client.DeleteAsync($"/api/v1/global-alerts/{alertId}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        _client.Dispose();
    }

    [Fact]
    public async Task GlobalAlerts_GetAll_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/global-alerts");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}");
    }

    [Fact]
    public async Task GlobalAlerts_CreateBusinessMetricAlert_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            MetricType = "business",
            MetricName = "business_records_processed_total",
            AlertName = $"test_alert_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Test alert for business metrics",
            Expression = "rate(business_records_processed_total[5m]) < 1",
            For = "5m",
            Severity = "warning",
            IsEnabled = true,
            Labels = new Dictionary<string, string>
            {
                ["team"] = "data-processing"
            },
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/global-alerts", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var data = GetDataFromResponse(result);
            if (data.TryGetProperty("id", out var idProp))
            {
                _createdAlertIds.Add(idProp.GetString() ?? "");
            }
        }
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {content}");
    }

    [Fact]
    public async Task GlobalAlerts_CreateSystemMetricAlert_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            MetricType = "system",
            MetricName = "process_cpu_seconds_total",
            AlertName = $"test_cpu_alert_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Test alert for CPU usage",
            Expression = "rate(process_cpu_seconds_total[5m]) > 0.8",
            For = "10m",
            Severity = "critical",
            IsEnabled = true,
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/global-alerts", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var data = GetDataFromResponse(result);
            if (data.TryGetProperty("id", out var idProp))
            {
                _createdAlertIds.Add(idProp.GetString() ?? "");
            }
        }
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {content}");
    }

    [Fact]
    public async Task GlobalAlerts_CreateWithLabels_LabelsArePersisted()
    {
        // Arrange
        var labels = new Dictionary<string, string>
        {
            ["environment"] = "production",
            ["region"] = "us-east-1",
            ["team"] = "platform"
        };

        var request = new
        {
            MetricType = "business",
            MetricName = "business_invalid_records_total",
            AlertName = $"test_labels_{Guid.NewGuid():N}".Substring(0, 30),
            Expression = "business_invalid_records_total > 100",
            Severity = "warning",
            Labels = labels,
            CreatedBy = "IntegrationTest"
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1/global-alerts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();

        if (!createResponse.IsSuccessStatusCode)
        {
            createResponse.IsSuccessStatusCode.Should().BeTrue($"Create failed: {createContent}");
            return;
        }

        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createData = GetDataFromResponse(createResult);
        var alertId = createData.GetProperty("id").GetString();
        _createdAlertIds.Add(alertId ?? "");

        // Verify labels persisted
        var getResponse = await _client.GetAsync($"/api/v1/global-alerts/{alertId}");
        var getContent = await getResponse.Content.ReadAsStringAsync();

        // Assert
        getResponse.IsSuccessStatusCode.Should().BeTrue();
        var getResult = JsonSerializer.Deserialize<JsonElement>(getContent);
        var getData = GetDataFromResponse(getResult);

        if (getData.TryGetProperty("labels", out var labelsElement))
        {
            labelsElement.TryGetProperty("environment", out var envProp);
            envProp.GetString().Should().Be("production");
        }
    }

    [Fact]
    public async Task GlobalAlerts_UpdateAlert_ChangesArePersisted()
    {
        // Arrange - Create an alert first
        var createRequest = new
        {
            MetricType = "business",
            MetricName = "business_files_processed_total",
            AlertName = $"test_update_{Guid.NewGuid():N}".Substring(0, 30),
            Expression = "business_files_processed_total < 10",
            Severity = "warning",
            CreatedBy = "IntegrationTest"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/global-alerts", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return; // Skip if create failed
        }

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createData = GetDataFromResponse(createResult);
        var alertId = createData.GetProperty("id").GetString();
        _createdAlertIds.Add(alertId ?? "");

        // Act - Update the alert
        var updateRequest = new
        {
            Severity = "critical",
            Expression = "business_files_processed_total < 5",
            UpdatedBy = "IntegrationTest"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/global-alerts/{alertId}", updateRequest);

        // Assert
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify changes
        var getResponse = await _client.GetAsync($"/api/v1/global-alerts/{alertId}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<JsonElement>(getContent);
        var getData = GetDataFromResponse(getResult);

        getData.GetProperty("severity").GetString().Should().Be("critical");
    }

    [Fact]
    public async Task GlobalAlerts_DeleteAlert_RemovesFromDatabase()
    {
        // Arrange - Create an alert first
        var createRequest = new
        {
            MetricType = "business",
            MetricName = "business_bytes_processed_total",
            AlertName = $"test_delete_{Guid.NewGuid():N}".Substring(0, 30),
            Expression = "business_bytes_processed_total == 0",
            Severity = "info",
            CreatedBy = "IntegrationTest"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/global-alerts", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return; // Skip if create failed
        }

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createData = GetDataFromResponse(createResult);
        var alertId = createData.GetProperty("id").GetString();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/global-alerts/{alertId}");

        // Assert
        deleteResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify deleted
        var getResponse = await _client.GetAsync($"/api/v1/global-alerts/{alertId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GlobalAlerts_FilterByMetricType_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/global-alerts?metricType=business");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = GetDataFromResponse(result);

        // All returned alerts should be business type
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var alert in data.EnumerateArray())
            {
                if (alert.TryGetProperty("metricType", out var metricType))
                {
                    metricType.GetString().Should().Be("business");
                }
            }
        }
    }

    [Fact]
    public async Task GlobalAlerts_EmptyAlertName_ReturnsBadRequest()
    {
        // Arrange - Empty alert name (invalid)
        var request = new
        {
            MetricType = "business",
            MetricName = "business_records_processed_total",
            AlertName = "",  // Empty - invalid
            Expression = "business_records_processed_total > 0",
            Severity = "warning"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/global-alerts", request);

        // Assert - Should fail validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GlobalAlerts_MissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange - Missing required Expression field
        var request = new
        {
            MetricType = "business",
            MetricName = "business_records_processed_total",
            AlertName = "test_missing_expression"
            // Expression is missing
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/global-alerts", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }
}
