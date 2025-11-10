using SchemaManagementService.Models.Responses;
using SchemaManagementService.Models.Requests;

namespace SchemaManagementService.Services;

/// <summary>
/// Interface for schema validation service
/// </summary>
public interface ISchemaValidationService
{
    /// <summary>
    /// Validate JSON Schema syntax
    /// </summary>
    Task<ValidationResult> ValidateJsonSchemaAsync(string jsonSchemaContent);

    /// <summary>
    /// Validate data against schema
    /// </summary>
    Task<DataValidationResult> ValidateDataAgainstSchemaAsync(string schemaId, object data);

    /// <summary>
    /// Validate data against JSON Schema content directly
    /// </summary>
    Task<DataValidationResult> ValidateDataAgainstJsonSchemaAsync(string jsonSchemaContent, object data);

    /// <summary>
    /// Test regex pattern against test strings
    /// </summary>
    Task<RegexTestResult> TestRegexPatternAsync(string pattern, List<string> testStrings);

    /// <summary>
    /// Test regex pattern against test strings with options
    /// </summary>
    Task<RegexTestResult> TestRegexPatternAsync(string pattern, List<string> testStrings, RegexTestOptions options);
}
