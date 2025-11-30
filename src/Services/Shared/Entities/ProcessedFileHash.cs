using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Tracks a processed file hash to prevent duplicate processing
/// Used by FileDiscoveryService for deduplication
/// </summary>
public class ProcessedFileHash
{
    /// <summary>
    /// SHA256 hash of the file (filePath|fileSize|lastModified)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Original file name for debugging and logging
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File path for reference
    /// </summary>
    [StringLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the file was first discovered and processed
    /// </summary>
    [Required]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when this hash entry should expire and be cleaned up
    /// Default: 24 hours after ProcessedAt
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// File size in bytes (for debugging)
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Last modified UTC timestamp of the file (for debugging)
    /// </summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// Correlation ID of the discovery operation
    /// </summary>
    [StringLength(50)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Checks if this hash entry has expired based on current time
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Creates a new ProcessedFileHash with TTL
    /// </summary>
    public static ProcessedFileHash Create(
        string hash,
        string fileName,
        string filePath,
        long fileSizeBytes,
        DateTime lastModifiedUtc,
        TimeSpan ttl,
        string? correlationId = null)
    {
        return new ProcessedFileHash
        {
            Hash = hash,
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            LastModifiedUtc = lastModifiedUtc,
            ProcessedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            CorrelationId = correlationId
        };
    }
}
