using System.Text.Json;
using MetricsConfigurationService.Models.Prometheus;

namespace MetricsConfigurationService.Services.Prometheus;

/// <summary>
/// Service for querying Prometheus HTTP API
/// </summary>
public class PrometheusQueryService : IPrometheusQueryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PrometheusQueryService> _logger;

    public PrometheusQueryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PrometheusQueryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PrometheusQueryResult> QueryInstantAsync(string query, PrometheusInstance instance)
    {
        var baseUrl = GetPrometheusUrl(instance);
        var url = $"{baseUrl}/api/v1/query?query={Uri.EscapeDataString(query)}";
        
        _logger.LogDebug("Executing instant query: {Query} on {Instance}", query, instance);
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("Prometheus");
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var promResponse = JsonSerializer.Deserialize<PrometheusResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (promResponse == null || promResponse.Status != "success")
            {
                throw new Exception($"Prometheus query failed: {promResponse?.Error ?? "Unknown error"}");
            }

            return ParseInstantResult(promResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Prometheus: {Query} on {Instance}", query, instance);
            throw;
        }
    }

    public async Task<PrometheusQueryResult> QueryRangeAsync(
        string query, DateTime start, DateTime end, string step, PrometheusInstance instance)
    {
        var baseUrl = GetPrometheusUrl(instance);
        var startTs = new DateTimeOffset(start).ToUnixTimeSeconds();
        var endTs = new DateTimeOffset(end).ToUnixTimeSeconds();
        
        var url = $"{baseUrl}/api/v1/query_range?" +
                  $"query={Uri.EscapeDataString(query)}&" +
                  $"start={startTs}&end={endTs}&step={step}";
        
        _logger.LogDebug("Executing range query: {Query} on {Instance} from {Start} to {End}", 
            query, instance, start, end);
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("Prometheus");
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var promResponse = JsonSerializer.Deserialize<PrometheusResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (promResponse == null || promResponse.Status != "success")
            {
                throw new Exception($"Prometheus query failed: {promResponse?.Error ?? "Unknown error"}");
            }

            return ParseRangeResult(promResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Prometheus range: {Query} on {Instance}", query, instance);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetMetricNamesAsync(PrometheusInstance instance)
    {
        var baseUrl = GetPrometheusUrl(instance);
        var url = $"{baseUrl}/api/v1/label/__name__/values";
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("Prometheus");
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var data = doc.RootElement.GetProperty("data");
            var metrics = new List<string>();
            
            foreach (var item in data.EnumerateArray())
            {
                metrics.Add(item.GetString() ?? string.Empty);
            }
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metric names from {Instance}", instance);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetLabelValuesAsync(string label, PrometheusInstance instance)
    {
        var baseUrl = GetPrometheusUrl(instance);
        var url = $"{baseUrl}/api/v1/label/{label}/values";
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("Prometheus");
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var data = doc.RootElement.GetProperty("data");
            var values = new List<string>();
            
            foreach (var item in data.EnumerateArray())
            {
                values.Add(item.GetString() ?? string.Empty);
            }
            
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting label values for {Label} from {Instance}", label, instance);
            return Enumerable.Empty<string>();
        }
    }

    private string GetPrometheusUrl(PrometheusInstance instance)
    {
        return instance switch
        {
            PrometheusInstance.System => _configuration["Prometheus:SystemUrl"] ?? "http://localhost:9090",
            PrometheusInstance.Business => _configuration["Prometheus:BusinessUrl"] ?? "http://localhost:9091",
            _ => throw new ArgumentException($"Unknown Prometheus instance: {instance}")
        };
    }

    private PrometheusQueryResult ParseInstantResult(PrometheusResponse response)
    {
        var result = new PrometheusQueryResult
        {
            ResultType = response.Data?.ResultType ?? "vector",
            Data = new List<MetricDataPoint>()
        };

        if (response.Data?.Result == null || !response.Data.Result.Any())
        {
            return result;
        }

        var firstResult = response.Data.Result.First();
        result.Labels = firstResult.Metric;

        if (firstResult.Value != null && firstResult.Value.Count == 2)
        {
            var timestamp = ExtractDouble(firstResult.Value[0]);
            var value = ExtractDouble(firstResult.Value[1]);
            
            result.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime;
            result.Value = value;
        }

        return result;
    }

    private PrometheusQueryResult ParseRangeResult(PrometheusResponse response)
    {
        var result = new PrometheusQueryResult
        {
            ResultType = response.Data?.ResultType ?? "matrix",
            Data = new List<MetricDataPoint>()
        };

        if (response.Data?.Result == null || !response.Data.Result.Any())
        {
            return result;
        }

        var firstResult = response.Data.Result.First();
        result.Labels = firstResult.Metric;

        if (firstResult.Values != null)
        {
            foreach (var valuePoint in firstResult.Values)
            {
                if (valuePoint.Count == 2)
                {
                    var timestamp = ExtractDouble(valuePoint[0]);
                    var value = ExtractDouble(valuePoint[1]);
                    
                    result.Data.Add(new MetricDataPoint
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime,
                        Value = value,
                        Labels = firstResult.Metric
                    });
                }
            }
        }

        return result;
    }

    private double ExtractDouble(object value)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.GetDouble();
            }
            if (element.ValueKind == JsonValueKind.String)
            {
                return double.Parse(element.GetString() ?? "0");
            }
        }
        return Convert.ToDouble(value);
    }
}
