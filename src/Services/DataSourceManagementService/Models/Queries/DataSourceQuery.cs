using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Queries;

/// <summary>
/// Query parameters for filtering and paginating data sources
/// </summary>
public class DataSourceQuery
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
    public int Size { get; set; } = 25;

    /// <summary>
    /// Filter by data source name (partial match)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by supplier name (partial match)
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by creation date range - start date
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by creation date range - end date
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter by last update date range - start date
    /// </summary>
    public DateTime? UpdatedFrom { get; set; }

    /// <summary>
    /// Filter by last update date range - end date
    /// </summary>
    public DateTime? UpdatedTo { get; set; }

    /// <summary>
    /// Search text for full-text search across name, supplier name, and description
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Sort field options
    /// </summary>
    public DataSourceSortField SortBy { get; set; } = DataSourceSortField.CreatedAt;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Include soft-deleted entities in results
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Available fields for sorting data sources
/// </summary>
public enum DataSourceSortField
{
    /// <summary>
    /// Sort by ID
    /// </summary>
    ID,

    /// <summary>
    /// Sort by name
    /// </summary>
    Name,

    /// <summary>
    /// Sort by supplier name
    /// </summary>
    SupplierName,

    /// <summary>
    /// Sort by creation date
    /// </summary>
    CreatedAt,

    /// <summary>
    /// Sort by last update date
    /// </summary>
    UpdatedAt,

    /// <summary>
    /// Sort by category
    /// </summary>
    Category,

    /// <summary>
    /// Sort by last processed date
    /// </summary>
    LastProcessedAt,

    /// <summary>
    /// Sort by total files processed
    /// </summary>
    TotalFilesProcessed,

    /// <summary>
    /// Sort by total error records
    /// </summary>
    TotalErrorRecords
}

/// <summary>
/// Sort direction options
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending order (A-Z, 1-9, oldest first)
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order (Z-A, 9-1, newest first)
    /// </summary>
    Descending
}
