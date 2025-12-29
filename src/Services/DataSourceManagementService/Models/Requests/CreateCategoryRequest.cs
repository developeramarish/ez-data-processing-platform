using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Requests;

/// <summary>
/// Request model for creating a new category
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>
    /// Category name in Hebrew (required)
    /// </summary>
    [Required(ErrorMessage = "שם קטגוריה בעברית נדרש")]
    [StringLength(100, ErrorMessage = "שם הקטגוריה לא יכול להיות ארוך מ-100 תווים")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category name in English (required)
    /// </summary>
    [Required(ErrorMessage = "שם קטגוריה באנגלית נדרש")]
    [StringLength(100, ErrorMessage = "שם הקטגוריה לא יכול להיות ארוך מ-100 תווים")]
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the category
    /// </summary>
    [StringLength(500, ErrorMessage = "תיאור לא יכול להיות ארוך מ-500 תווים")]
    public string? Description { get; set; }

    /// <summary>
    /// Sort order (optional, will be auto-assigned if not provided)
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// Whether the category is active (defaults to true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
