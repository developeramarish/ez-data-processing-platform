using MongoDB.Entities;

namespace DataProcessing.Shared.Entities;

/// <summary>
/// Entity representing a datasource category for admin management
/// </summary>
[Collection("DataSourceCategories")]
public class DataSourceCategory : Entity
{
    /// <summary>
    /// Category name in Hebrew (primary display name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category name in English
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the category
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order for displaying categories (lower numbers appear first)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this category is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the category was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the category was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this category (optional)
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified this category (optional)
    /// </summary>
    public string? ModifiedBy { get; set; }
}
