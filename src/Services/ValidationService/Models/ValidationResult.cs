using Newtonsoft.Json.Linq;

namespace DataProcessing.Validation.Models;

/// <summary>
/// Result of file content validation
/// </summary>
public class ValidationResult
{
    public string ValidationResultId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of valid JSON records for business metrics calculation and caching
    /// </summary>
    public List<JObject>? ValidRecordsData { get; set; }
}
