using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Responses;

/// <summary>
/// Standard API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Response data (null if error occurred)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error details (null if successful)
    /// </summary>
    public ErrorDetail? Error { get; set; }

    /// <summary>
    /// Indicates whether the request was successful
    /// </summary>
    public bool IsSuccess => Error == null;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="data">Data to return</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse<T> Success(T data, string correlationId)
    {
        return new ApiResponse<T>
        {
            CorrelationId = correlationId,
            Data = data,
            Error = null
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="error">Error details</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Error API response</returns>
    public static ApiResponse<T> Failure(ErrorDetail error, string correlationId)
    {
        return new ApiResponse<T>
        {
            CorrelationId = correlationId,
            Data = default(T),
            Error = error
        };
    }
}

/// <summary>
/// Paginated API response for list endpoints
/// </summary>
/// <typeparam name="T">Type of items in the list</typeparam>
public class PagedApiResponse<T> : ApiResponse<List<T>>
{
    /// <summary>
    /// Pagination information
    /// </summary>
    [Required]
    public PagingInfo Paging { get; set; } = new();

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    /// <param name="data">List of items</param>
    /// <param name="paging">Pagination information</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Successful paginated API response</returns>
    public static PagedApiResponse<T> Success(List<T> data, PagingInfo paging, string correlationId)
    {
        return new PagedApiResponse<T>
        {
            CorrelationId = correlationId,
            Data = data,
            Paging = paging,
            Error = null
        };
    }

    /// <summary>
    /// Creates an error paginated response
    /// </summary>
    /// <param name="error">Error details</param>
    /// <param name="correlationId">Request correlation ID</param>
    /// <returns>Error paginated API response</returns>
    public new static PagedApiResponse<T> Failure(ErrorDetail error, string correlationId)
    {
        return new PagedApiResponse<T>
        {
            CorrelationId = correlationId,
            Data = new List<T>(),
            Paging = new PagingInfo(),
            Error = error
        };
    }
}

/// <summary>
/// Pagination information for paginated responses
/// </summary>
public class PagingInfo
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    [Required]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Required]
    public int Size { get; set; } = 25;

    /// <summary>
    /// Total number of items available
    /// </summary>
    [Required]
    public long TotalItems { get; set; } = 0;

    /// <summary>
    /// Total number of pages available
    /// </summary>
    [Required]
    public int TotalPages { get; set; } = 0;

    /// <summary>
    /// Whether there are more items available
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Whether there are previous items available
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Creates pagination info from query parameters
    /// </summary>
    /// <param name="page">Current page</param>
    /// <param name="size">Page size</param>
    /// <param name="totalItems">Total items count</param>
    /// <returns>Populated paging info</returns>
    public static PagingInfo Create(int page, int size, long totalItems)
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / size);
        
        return new PagingInfo
        {
            Page = page,
            Size = size,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }
}
