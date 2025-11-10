using DataProcessing.Shared.Messages;

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
}
