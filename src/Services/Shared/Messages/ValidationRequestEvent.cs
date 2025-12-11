namespace DataProcessing.Shared.Messages;

/// <summary>
/// Event published when a file needs validation
/// Published by FilesReceiverService, consumed by ValidationService
/// </summary>
public class ValidationRequestEvent : IDataProcessingMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PublishedBy { get; set; } = string.Empty;
    public int MessageVersion { get; set; } = 1;
    public string DataSourceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileContentType { get; set; } = "application/json";
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Hazelcast cache key for storing file content
    /// Used to reference the cached file data in Hazelcast distributed cache
    /// </summary>
    public string HazelcastKey { get; set; } = string.Empty;

    /// <summary>
    /// Original format of the file (CSV, XML, JSON, Excel)
    /// Used for format reconstruction after validation
    /// </summary>
    public string OriginalFormat { get; set; } = string.Empty;

    /// <summary>
    /// Metadata required for reconstructing the original file format
    /// Contains format-specific information like delimiters, encoding, etc.
    /// </summary>
    public Dictionary<string, object> FormatMetadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Indicates this is a reprocess request for a previously invalid record
    /// When true, ValidationService will delete or update the OriginalInvalidRecordId
    /// </summary>
    public bool IsReprocess { get; set; } = false;

    /// <summary>
    /// ID of the invalid record being reprocessed
    /// Used to delete record if validation passes or update if validation fails
    /// </summary>
    public string? OriginalInvalidRecordId { get; set; }
}
