using System.ComponentModel.DataAnnotations;
using SchemaManagementService.Entities;

namespace SchemaManagementService.Models.Requests;

/// <summary>
/// Request model for creating a new schema
/// </summary>
public class CreateSchemaRequest
{
    /// <summary>
    /// Schema name (unique identifier)
    /// </summary>
    [Required(ErrorMessage = "שם Schema הוא חובה")]
    [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "שם Schema חייב להכיל רק אותיות אנגליות קטנות, מספרים וקו תחתון")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "שם Schema חייב להיות בין 3-50 תווים")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [Required(ErrorMessage = "שם תצוגה הוא חובה")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "שם תצוגה חייב להיות בין 3-100 תווים")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Schema description
    /// </summary>
    [StringLength(500, ErrorMessage = "תיאור חייב להיות עד 500 תווים")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Associated data source ID
    /// </summary>
    public string? DataSourceId { get; set; }

    /// <summary>
    /// JSON Schema content (JSON Schema 2020-12)
    /// </summary>
    [Required(ErrorMessage = "תוכן JSON Schema הוא חובה")]
    public string JsonSchemaContent { get; set; } = string.Empty;

    /// <summary>
    /// Schema tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Initial schema status
    /// </summary>
    public SchemaStatus Status { get; set; } = SchemaStatus.Draft;

    /// <summary>
    /// Schema version
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "גרסה חייבת להיות מספר חיובי")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// User who created the schema
    /// </summary>
    [StringLength(100, ErrorMessage = "שם משתמש חייב להיות עד 100 תווים")]
    public string CreatedBy { get; set; } = "System";
}
