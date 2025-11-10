namespace DataProcessing.FilesReceiver.Models;

/// <summary>
/// Information about a processed file ready for validation
/// </summary>
public class ProcessedFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/json";
    public long FileSize { get; set; }
    public int RecordCount { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
