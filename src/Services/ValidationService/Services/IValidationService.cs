using DataProcessing.Validation.Models;

namespace DataProcessing.Validation.Services;

/// <summary>
/// Service for validating file content using JSON Schema
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validate file content against JSON Schema for the specified data source
    /// </summary>
    /// <param name="dataSourceId">ID of the data source</param>
    /// <param name="fileName">Name of the file being validated</param>
    /// <param name="fileContent">Content of the file as bytes</param>
    /// <param name="contentType">MIME type of the file content</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>Validation result with counts and stored result ID</returns>
    Task<ValidationResult> ValidateFileContentAsync(
        string dataSourceId, 
        string fileName, 
        byte[] fileContent, 
        string contentType, 
        string correlationId);
}
