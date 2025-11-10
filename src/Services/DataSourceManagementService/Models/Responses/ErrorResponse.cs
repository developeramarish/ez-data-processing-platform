using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Responses;

/// <summary>
/// Standard error response for API operations
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Error details
    /// </summary>
    [Required]
    public ErrorDetail Error { get; set; } = new();

    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="error">Error details</param>
    /// <returns>Error response</returns>
    public static ErrorResponse Create(string correlationId, ErrorDetail error)
    {
        return new ErrorResponse
        {
            CorrelationId = correlationId,
            Error = error
        };
    }
}

/// <summary>
/// Detailed error information with Hebrew UI messages and English log details
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Hebrew error message for UI display (RTL supported)
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// English error details for logging and debugging
    /// </summary>
    [Required]
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Field name that caused the validation error (if applicable)
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// HTTP status code associated with this error
    /// </summary>
    [Required]
    public int StatusCode { get; set; } = 400;

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a validation error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <param name="field">Field that caused the error</param>
    /// <returns>Validation error detail</returns>
    public static ErrorDetail CreateValidation(string code, string hebrewMessage, string englishDetails, string? field = null)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            Field = field,
            StatusCode = 400,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a not found error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <returns>Not found error detail</returns>
    public static ErrorDetail CreateNotFound(string code, string hebrewMessage, string englishDetails)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            StatusCode = 404,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <returns>Conflict error detail</returns>
    public static ErrorDetail CreateConflict(string code, string hebrewMessage, string englishDetails)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            StatusCode = 409,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an internal server error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <returns>Internal server error detail</returns>
    public static ErrorDetail CreateServerError(string code, string hebrewMessage, string englishDetails)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            StatusCode = 500,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <returns>Unauthorized error detail</returns>
    public static ErrorDetail CreateUnauthorized(string code, string hebrewMessage, string englishDetails)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            StatusCode = 401,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="hebrewMessage">Hebrew message for UI</param>
    /// <param name="englishDetails">English details for logs</param>
    /// <returns>Forbidden error detail</returns>
    public static ErrorDetail CreateForbidden(string code, string hebrewMessage, string englishDetails)
    {
        return new ErrorDetail
        {
            Code = code,
            Message = hebrewMessage,
            Details = englishDetails,
            StatusCode = 403,
            Timestamp = DateTime.UtcNow
        };
    }
}
