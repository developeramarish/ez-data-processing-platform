namespace DataProcessing.DataSourceManagement.Models.Responses;

/// <summary>
/// Generic paged result container for repository operations
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// List of items for the current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int Size { get; set; } = 25;

    /// <summary>
    /// Total number of items available across all pages
    /// </summary>
    public long TotalItems { get; set; } = 0;

    /// <summary>
    /// Total number of pages available
    /// </summary>
    public int TotalPages { get; set; } = 0;

    /// <summary>
    /// Whether there are more pages available
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are previous pages available
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a paged result with the specified items and pagination info
    /// </summary>
    /// <param name="items">Items for the current page</param>
    /// <param name="page">Current page number</param>
    /// <param name="size">Page size</param>
    /// <param name="totalItems">Total items available</param>
    public PagedResult(List<T> items, int page, int size, long totalItems)
    {
        Items = items;
        Page = page;
        Size = size;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / size);
    }

    /// <summary>
    /// Creates a paged result from pagination parameters
    /// </summary>
    /// <param name="items">Items for the current page</param>
    /// <param name="page">Current page number</param>
    /// <param name="size">Page size</param>
    /// <param name="totalItems">Total items available</param>
    /// <returns>Configured paged result</returns>
    public static PagedResult<T> Create(List<T> items, int page, int size, long totalItems)
    {
        return new PagedResult<T>(items, page, size, totalItems);
    }

    /// <summary>
    /// Creates an empty paged result for when no items are found
    /// </summary>
    /// <param name="page">Current page number</param>
    /// <param name="size">Page size</param>
    /// <returns>Empty paged result</returns>
    public static PagedResult<T> Empty(int page = 1, int size = 25)
    {
        return new PagedResult<T>(new List<T>(), page, size, 0);
    }
}
