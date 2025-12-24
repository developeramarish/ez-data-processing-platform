using DataProcessing.Shared.Entities;

namespace DataProcessing.Shared.Services;

/// <summary>
/// Service for managing file hash deduplication using distributed cache
/// Replaces MongoDB-embedded hash storage with Hazelcast for better scaling
/// </summary>
public interface IFileHashService
{
    /// <summary>
    /// Checks if a file with the given hash has already been processed
    /// </summary>
    /// <param name="datasourceId">The data source ID (used as namespace)</param>
    /// <param name="fileHash">SHA256 hash of file metadata (path|size|lastModified)</param>
    /// <returns>True if file was already processed and not expired</returns>
    Task<bool> IsFileAlreadyProcessedAsync(string datasourceId, string fileHash);

    /// <summary>
    /// Adds a file hash to the processed list with TTL-based expiration
    /// </summary>
    /// <param name="datasourceId">The data source ID (used as namespace)</param>
    /// <param name="fileHash">SHA256 hash of file metadata</param>
    /// <param name="metadata">File metadata for tracking</param>
    /// <param name="ttl">Time-to-live before hash expires</param>
    Task AddProcessedFileHashAsync(
        string datasourceId,
        string fileHash,
        ProcessedFileHashInfo metadata,
        TimeSpan ttl);

    /// <summary>
    /// Gets the count of active (non-expired) file hashes for a datasource
    /// </summary>
    /// <param name="datasourceId">The data source ID</param>
    /// <returns>Count of active hashes</returns>
    Task<int> GetActiveFileHashCountAsync(string datasourceId);

    /// <summary>
    /// Removes a specific file hash (for reprocessing scenarios)
    /// </summary>
    /// <param name="datasourceId">The data source ID</param>
    /// <param name="fileHash">The hash to remove</param>
    Task RemoveFileHashAsync(string datasourceId, string fileHash);
}

/// <summary>
/// Lightweight DTO for file hash metadata stored in Hazelcast
/// </summary>
public class ProcessedFileHashInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
