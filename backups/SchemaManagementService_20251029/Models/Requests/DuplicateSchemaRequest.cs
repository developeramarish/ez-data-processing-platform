using System.ComponentModel.DataAnnotations;

namespace SchemaManagementService.Models.Requests;

/// <summary>
/// Request model for duplicating an existing schema
/// </summary>
public class DuplicateSchemaRequest
{
    /// <summary>
    /// New schema name (unique identifier)
    /// </summary>
    [Required(ErrorMessage = "שם Schema החדש הוא חובה")]
    [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "שם Schema חייב להכיל רק אותיות אנגליות קטנות, מספרים וקו תחתון")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "שם Schema חייב להיות בין 3-50 תווים")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// New display name for UI
    /// </summary>
    [Required(ErrorMessage = "שם תצוגה החדש הוא חובה")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "שם תצוגה חייב להיות בין 3-100 תווים")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Additional description for the duplicate
    /// </summary>
    [StringLength(300, ErrorMessage = "תיאור נוסף חייב להיות עד 300 תווים")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who created the duplicate
    /// </summary>
    [StringLength(100, ErrorMessage = "שם משתמש חייב להיות עד 100 תווים")]
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// Whether to copy tags from original schema
    /// </summary>
    public bool CopyTags { get; set; } = true;

    /// <summary>
    /// Additional tags for the duplicate
    /// </summary>
    public List<string> AdditionalTags { get; set; } = new();
}
