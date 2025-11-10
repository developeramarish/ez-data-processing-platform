using System.ComponentModel.DataAnnotations;
using SchemaManagementService.Entities;

namespace SchemaManagementService.Models.Requests;

/// <summary>
/// Request model for updating an existing schema
/// </summary>
public class UpdateSchemaRequest
{
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
    /// Schema status
    /// </summary>
    public SchemaStatus Status { get; set; } = SchemaStatus.Draft;

    /// <summary>
    /// User who updated the schema
    /// </summary>
    [StringLength(100, ErrorMessage = "שם משתמש חייב להיות עד 100 תווים")]
    public string UpdatedBy { get; set; } = "System";

    /// <summary>
    /// Optional version increment for breaking changes
    /// </summary>
    public bool IncrementVersion { get; set; } = false;
}
