using System.Text.Json;
using Hazelcast;
using Microsoft.Extensions.Logging;

namespace DataProcessing.Shared.Services;

/// <summary>
/// Hazelcast-based implementation of file hash deduplication service
/// Uses distributed maps with native TTL support for automatic expiration
/// </summary>
public class HazelcastFileHashService : IFileHashService
{
    private readonly IHazelcastClient _hazelcastClient;
    private readonly ILogger<HazelcastFileHashService> _logger;

    /// <summary>
    /// Hazelcast map name prefix for file hashes
    /// Each datasource gets its own map: file-hashes-{datasourceId}
    /// </summary>
    private const string MapNamePrefix = "file-hashes";

    public HazelcastFileHashService(
        IHazelcastClient hazelcastClient,
        ILogger<HazelcastFileHashService> logger)
    {
        _hazelcastClient = hazelcastClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsFileAlreadyProcessedAsync(string datasourceId, string fileHash)
    {
        try
        {
            var mapName = GetMapName(datasourceId);
            var map = await _hazelcastClient.GetMapAsync<string, string>(mapName);

            var exists = await map.ContainsKeyAsync(fileHash);

            if (exists)
            {
                _logger.LogDebug(
                    "File hash {Hash} found in cache for datasource {DatasourceId}",
                    fileHash.Substring(0, 8) + "...", datasourceId);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to check file hash in Hazelcast for datasource {DatasourceId}",
                datasourceId);

            // On error, return false to allow processing (fail-open for availability)
            return false;
        }
    }

    /// <inheritdoc />
    public async Task AddProcessedFileHashAsync(
        string datasourceId,
        string fileHash,
        ProcessedFileHashInfo metadata,
        TimeSpan ttl)
    {
        try
        {
            var mapName = GetMapName(datasourceId);
            var map = await _hazelcastClient.GetMapAsync<string, string>(mapName);

            // Serialize metadata to JSON for storage
            var metadataJson = JsonSerializer.Serialize(metadata);

            // SetAsync with TTL - Hazelcast handles expiration automatically
            await map.SetAsync(fileHash, metadataJson, ttl);

            _logger.LogDebug(
                "Added file hash {Hash} to cache for datasource {DatasourceId} with TTL {TTL}",
                fileHash.Substring(0, 8) + "...", datasourceId, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add file hash to Hazelcast for datasource {DatasourceId}",
                datasourceId);

            // Don't throw - deduplication is optimization, not critical path
            // File might get reprocessed, but won't break the pipeline
        }
    }

    /// <inheritdoc />
    public async Task<int> GetActiveFileHashCountAsync(string datasourceId)
    {
        try
        {
            var mapName = GetMapName(datasourceId);
            var map = await _hazelcastClient.GetMapAsync<string, string>(mapName);

            return await map.GetSizeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get file hash count from Hazelcast for datasource {DatasourceId}",
                datasourceId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task RemoveFileHashAsync(string datasourceId, string fileHash)
    {
        try
        {
            var mapName = GetMapName(datasourceId);
            var map = await _hazelcastClient.GetMapAsync<string, string>(mapName);

            await map.RemoveAsync(fileHash);

            _logger.LogDebug(
                "Removed file hash {Hash} from cache for datasource {DatasourceId}",
                fileHash.Substring(0, 8) + "...", datasourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove file hash from Hazelcast for datasource {DatasourceId}",
                datasourceId);
        }
    }

    /// <summary>
    /// Generates the Hazelcast map name for a datasource
    /// </summary>
    private static string GetMapName(string datasourceId)
    {
        return $"{MapNamePrefix}-{datasourceId}";
    }
}
