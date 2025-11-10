using System.ComponentModel.DataAnnotations;

namespace SchemaManagementService.Models.Requests;

/// <summary>
/// Request model for validating JSON Schema syntax
/// </summary>
public class ValidateJsonSchemaRequest
{
    /// <summary>
    /// JSON Schema content to validate
    /// </summary>
    [Required(ErrorMessage = "תוכן JSON Schema הוא חובה")]
    [StringLength(100000, ErrorMessage = "JSON Schema גדול מדי - מקסימום 100KB")]
    public string JsonSchemaContent { get; set; } = string.Empty;

    /// <summary>
    /// Schema specification version to validate against
    /// </summary>
    [StringLength(20, ErrorMessage = "גרסת Schema חייבת להיות עד 20 תווים")]
    public string? SchemaVersion { get; set; }

    /// <summary>
    /// Whether to perform strict validation
    /// </summary>
    public bool StrictMode { get; set; } = true;
}
