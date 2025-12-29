using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Requests;

/// <summary>
/// Request model for reordering categories
/// </summary>
public class ReorderCategoriesRequest
{
    /// <summary>
    /// Ordered list of category IDs
    /// </summary>
    [Required(ErrorMessage = "רשימת מזהי קטגוריות נדרשת")]
    [MinLength(1, ErrorMessage = "חייבת להיות לפחות קטגוריה אחת")]
    public List<string> CategoryIds { get; set; } = new();
}
