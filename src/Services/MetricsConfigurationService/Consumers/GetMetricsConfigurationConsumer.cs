using DataProcessing.Shared.Messages;
using MassTransit;
using MetricsConfigurationService.Repositories;

namespace MetricsConfigurationService.Consumers;

/// <summary>
/// Consumer for GetMetricsConfigurationRequest messages
/// Queries metric configurations from MongoDB and responds with both global and datasource-specific metrics
/// Used by ValidationService to retrieve metrics during validation process
/// </summary>
public class GetMetricsConfigurationConsumer : IConsumer<GetMetricsConfigurationRequest>
{
    private readonly ILogger<GetMetricsConfigurationConsumer> _logger;
    private readonly IMetricRepository _metricRepository;

    public GetMetricsConfigurationConsumer(
        ILogger<GetMetricsConfigurationConsumer> logger,
        IMetricRepository metricRepository)
    {
        _logger = logger;
        _metricRepository = metricRepository;
    }

    public async Task Consume(ConsumeContext<GetMetricsConfigurationRequest> context)
    {
        var request = context.Message;

        _logger.LogInformation(
            "[{CorrelationId}] Received GetMetricsConfigurationRequest for DataSourceId: {DataSourceId}, IncludeGlobal: {IncludeGlobal}, OnlyActive: {OnlyActive}",
            request.CorrelationId, request.DataSourceId, request.IncludeGlobal, request.OnlyActive);

        try
        {
            var metrics = new List<MetricConfigurationDto>();

            // Step 1: Retrieve global metrics if requested
            if (request.IncludeGlobal)
            {
                var globalMetrics = await _metricRepository.GetGlobalMetricsAsync();

                _logger.LogDebug(
                    "[{CorrelationId}] Retrieved {Count} global metrics from database",
                    request.CorrelationId, globalMetrics.Count);

                // Filter by active status if requested
                if (request.OnlyActive)
                {
                    globalMetrics = globalMetrics.Where(m => m.Status == 1).ToList();

                    _logger.LogDebug(
                        "[{CorrelationId}] Filtered to {Count} active global metrics",
                        request.CorrelationId, globalMetrics.Count);
                }

                // Convert to DTOs
                metrics.AddRange(globalMetrics.Select(MapToDto));
            }

            // Step 2: Retrieve datasource-specific metrics
            if (!string.IsNullOrEmpty(request.DataSourceId))
            {
                var datasourceMetrics = await _metricRepository.GetByDataSourceIdAsync(request.DataSourceId);

                _logger.LogDebug(
                    "[{CorrelationId}] Retrieved {Count} datasource-specific metrics for DataSourceId: {DataSourceId}",
                    request.CorrelationId, datasourceMetrics.Count, request.DataSourceId);

                // Filter by active status if requested
                if (request.OnlyActive)
                {
                    datasourceMetrics = datasourceMetrics.Where(m => m.Status == 1).ToList();

                    _logger.LogDebug(
                        "[{CorrelationId}] Filtered to {Count} active datasource-specific metrics",
                        request.CorrelationId, datasourceMetrics.Count);
                }

                // Convert to DTOs
                metrics.AddRange(datasourceMetrics.Select(MapToDto));
            }

            // Step 3: Respond with metrics
            var response = new GetMetricsConfigurationResponse
            {
                CorrelationId = request.CorrelationId,
                PublishedBy = "MetricsConfigurationService",
                Timestamp = DateTime.UtcNow,
                MessageVersion = 1,
                Metrics = metrics,
                Success = true
            };

            await context.RespondAsync(response);

            _logger.LogInformation(
                "[{CorrelationId}] Responded with {Count} metrics ({GlobalCount} global, {DatasourceCount} datasource-specific)",
                request.CorrelationId,
                metrics.Count,
                metrics.Count(m => m.Scope == "global"),
                metrics.Count(m => m.Scope == "datasource-specific"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{CorrelationId}] Failed to retrieve metric configurations: {ErrorMessage}",
                request.CorrelationId, ex.Message);

            // Respond with error
            var errorResponse = new GetMetricsConfigurationResponse
            {
                CorrelationId = request.CorrelationId,
                PublishedBy = "MetricsConfigurationService",
                Timestamp = DateTime.UtcNow,
                MessageVersion = 1,
                Metrics = new List<MetricConfigurationDto>(),
                Success = false,
                ErrorMessage = ex.Message
            };

            await context.RespondAsync(errorResponse);
        }
    }

    /// <summary>
    /// Maps MetricConfiguration entity to DTO
    /// </summary>
    private MetricConfigurationDto MapToDto(Models.MetricConfiguration metric)
    {
        return new MetricConfigurationDto
        {
            Name = metric.Name,
            DisplayName = metric.DisplayName,
            Description = metric.Description,
            Category = metric.Category,
            Scope = metric.Scope,
            DataSourceId = metric.DataSourceId,
            FieldPath = metric.FieldPath,
            PrometheusType = metric.PrometheusType,
            Status = metric.Status,
            AlertRules = metric.AlertRules?.Select(alert => new AlertRuleDto
            {
                Id = alert.Id,
                Name = alert.Name,
                Description = alert.Description,
                Expression = alert.Expression,
                For = alert.For,
                Severity = alert.Severity,
                IsEnabled = alert.IsEnabled,
                Labels = alert.Labels,
                Annotations = alert.Annotations
            }).ToList()
        };
    }
}
