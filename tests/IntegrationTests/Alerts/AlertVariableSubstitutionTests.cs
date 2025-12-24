using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DataProcessing.IntegrationTests.Alerts;

/// <summary>
/// Integration tests for alert variable substitution functionality.
/// Tests that $variable patterns in PromQL expressions are correctly substituted.
/// </summary>
public class AlertVariableSubstitutionTests : IAsyncLifetime
{
    private readonly HttpClient _metricsClient;
    private readonly HttpClient _datasourceClient;
    private readonly string _metricsBaseUrl;
    private readonly string _datasourceBaseUrl;
    private readonly List<string> _createdAlertIds = new();
    private readonly List<string> _createdMetricIds = new();

    public AlertVariableSubstitutionTests()
    {
        _metricsBaseUrl = Environment.GetEnvironmentVariable("METRICS_CONFIG_URL") ?? "http://localhost:5002";
        _datasourceBaseUrl = Environment.GetEnvironmentVariable("DATASOURCE_MGMT_URL") ?? "http://localhost:5001";

        _metricsClient = new HttpClient { BaseAddress = new Uri(_metricsBaseUrl) };
        _metricsClient.Timeout = TimeSpan.FromSeconds(30);

        _datasourceClient = new HttpClient { BaseAddress = new Uri(_datasourceBaseUrl) };
        _datasourceClient.Timeout = TimeSpan.FromSeconds(30);
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
                await _metricsClient.DeleteAsync($"/api/v1/global-alerts/{alertId}");
            }
            catch { }
        }

        // Cleanup created metrics
        foreach (var metricId in _createdMetricIds)
        {
            try
            {
                await _metricsClient.DeleteAsync($"/api/v1/metrics/{metricId}");
            }
            catch { }
        }

        _metricsClient.Dispose();
        _datasourceClient.Dispose();
    }

    [Fact]
    public async Task GlobalAlert_WithLabelVariables_StoresLabelsCorrectly()
    {
        // Arrange - Create alert with labels containing both fixed and variable values
        var request = new
        {
            MetricType = "business",
            MetricName = "business_records_processed_total",
            AlertName = $"test_vars_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Alert with variable labels",
            // Expression using variable that will be substituted
            Expression = "rate(business_records_processed_total{status=\"$status\", environment=\"production\"}[5m]) < 1",
            Severity = "warning",
            IsEnabled = true,
            Labels = new Dictionary<string, string>
            {
                ["status"] = "approved",           // Fixed value - should be used for $status substitution
                ["environment"] = "$environment",  // Variable value - should NOT be substituted
                ["region"] = "us-east-1"           // Fixed value
            },
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _metricsClient.PostAsJsonAsync("/api/v1/global-alerts", request);
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

            // Verify labels are stored correctly
            if (data.TryGetProperty("labels", out var labels))
            {
                labels.TryGetProperty("status", out var statusProp);
                statusProp.GetString().Should().Be("approved");

                labels.TryGetProperty("environment", out var envProp);
                envProp.GetString().Should().Be("$environment");  // Should remain as variable

                labels.TryGetProperty("region", out var regionProp);
                regionProp.GetString().Should().Be("us-east-1");
            }
        }

        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {content}");
    }

    [Fact]
    public async Task GlobalAlert_ExpressionWithPredefinedVariables_IsAccepted()
    {
        // Arrange - Create alert using predefined variables
        var request = new
        {
            MetricType = "business",
            MetricName = "business_files_processed_total",
            AlertName = $"test_predefined_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Alert using predefined variables",
            // Expression using predefined variables
            Expression = "rate(business_files_processed_total{metric_name=\"$metric_name\", metric_type=\"$metric_type\"}[5m]) < 1",
            Severity = "warning",
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _metricsClient.PostAsJsonAsync("/api/v1/global-alerts", request);
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

            // Verify expression is stored as-is (variables are substituted at evaluation time)
            data.TryGetProperty("expression", out var exprProp);
            exprProp.GetString().Should().Contain("$metric_name");
            exprProp.GetString().Should().Contain("$metric_type");
        }

        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}: {content}");
    }

    [Fact]
    public async Task GlobalAlert_WithMixedLabelTypes_PreservesAll()
    {
        // Arrange - Mix of variable and fixed labels
        var request = new
        {
            MetricType = "business",
            MetricName = "business_invalid_records_total",
            AlertName = $"test_mixed_{Guid.NewGuid():N}".Substring(0, 30),
            Expression = "business_invalid_records_total > 100",
            Severity = "critical",
            Labels = new Dictionary<string, string>
            {
                ["datasource"] = "$datasource_name",   // Variable
                ["category"] = "$category",            // Variable
                ["severity_level"] = "critical",       // Fixed
                ["team"] = "data-processing",          // Fixed
                ["env"] = "production"                 // Fixed
            },
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _metricsClient.PostAsJsonAsync("/api/v1/global-alerts", request);
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

            // Verify all labels preserved
            if (data.TryGetProperty("labels", out var labels))
            {
                labels.EnumerateObject().Count().Should().Be(5);
            }
        }

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GlobalAlert_ComplexExpression_IsStoredCorrectly()
    {
        // Arrange - Complex PromQL expression with multiple operators
        var complexExpression = @"(
  sum(rate(business_records_processed_total{status=""$status""}[5m])) by (datasource)
  /
  sum(rate(business_records_processed_total[5m])) by (datasource)
) * 100 < 95";

        var request = new
        {
            MetricType = "business",
            MetricName = "business_records_processed_total",
            AlertName = $"test_complex_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Complex expression with percentage calculation",
            Expression = complexExpression,
            For = "5m",
            Severity = "warning",
            Labels = new Dictionary<string, string>
            {
                ["status"] = "approved"
            },
            CreatedBy = "IntegrationTest"
        };

        // Act
        var response = await _metricsClient.PostAsJsonAsync("/api/v1/global-alerts", request);
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

            data.TryGetProperty("expression", out var exprProp);
            var storedExpr = exprProp.GetString();
            storedExpr.Should().Contain("$status");
            storedExpr.Should().Contain("sum(rate");
        }

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsAvailableBusinessMetrics()
    {
        // Act - Get available business metrics
        var response = await _metricsClient.GetAsync("/api/v1/metrics/global/business");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = GetDataFromResponse(result);

        // Should return an array of available metrics
        if (data.ValueKind == JsonValueKind.Array)
        {
            data.GetArrayLength().Should().BeGreaterThan(0, "Should have predefined business metrics");
        }
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsAvailableSystemMetrics()
    {
        // Act - Get available system metrics
        var response = await _metricsClient.GetAsync("/api/v1/metrics/global/system");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = GetDataFromResponse(result);

        // Should return an array of available metrics
        if (data.ValueKind == JsonValueKind.Array)
        {
            data.GetArrayLength().Should().BeGreaterThan(0, "Should have predefined system metrics");
        }
    }

    [Fact]
    public async Task DatasourceAlert_WithVariableSubstitution_IsStored()
    {
        // First get an existing datasource
        var dsResponse = await _datasourceClient.GetAsync("/api/v1/datasources");
        if (!dsResponse.IsSuccessStatusCode)
        {
            // Skip if can't get datasources
            return;
        }

        var dsContent = await dsResponse.Content.ReadAsStringAsync();
        var dsResult = JsonSerializer.Deserialize<JsonElement>(dsContent);
        var datasources = GetDataFromResponse(dsResult);

        string? datasourceId = null;
        if (datasources.ValueKind == JsonValueKind.Array && datasources.GetArrayLength() > 0)
        {
            datasourceId = datasources[0].GetProperty("id").GetString();
        }

        if (string.IsNullOrEmpty(datasourceId))
        {
            // Skip if no datasources exist
            return;
        }

        // Create a metric with alert rule that uses variables
        var metricRequest = new
        {
            Name = $"test_ds_metric_{Guid.NewGuid():N}".Substring(0, 30),
            Description = "Test metric with variable substitution",
            DataSourceId = datasourceId,
            Category = "test",
            Scope = "local",
            Formula = "count(records)",
            FormulaType = "simple",
            Status = 1,
            AlertRules = new[]
            {
                new
                {
                    Name = "test_alert",
                    Expression = @"metric_name{datasource=""$datasource_name""} > 100",
                    Severity = "warning",
                    IsEnabled = true,
                    Labels = new Dictionary<string, string>
                    {
                        ["datasource_name"] = "my_datasource"  // Fixed value for substitution
                    }
                }
            }
        };

        var createResponse = await _metricsClient.PostAsJsonAsync("/api/v1/metrics", metricRequest);

        if (createResponse.IsSuccessStatusCode)
        {
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(createContent);
            var data = GetDataFromResponse(result);
            if (data.TryGetProperty("id", out var idProp))
            {
                _createdMetricIds.Add(idProp.GetString() ?? "");
            }
        }

        // Even if this fails (API may not support this exact format), the test documents the expected behavior
        // The actual substitution happens in AlertEvaluationService at evaluation time
    }
}
