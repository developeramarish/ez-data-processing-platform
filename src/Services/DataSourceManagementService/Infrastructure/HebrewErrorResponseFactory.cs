using System.Text.Json;
using DataProcessing.DataSourceManagement.Models.Responses;

namespace DataProcessing.DataSourceManagement.Infrastructure;

/// <summary>
/// Factory for creating Hebrew error responses with consistent UI/logging messages
/// </summary>
public static class HebrewErrorResponseFactory
{
    private static readonly Dictionary<string, object> _errorMessages = LoadErrorMessages();

    /// <summary>
    /// Creates a validation error with Hebrew UI message and English log details
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="field">Field that failed validation</param>
    /// <param name="validationKey">Key for the validation error type</param>
    /// <param name="englishDetails">English details for logging</param>
    /// <returns>Validation error response</returns>
    public static ErrorResponse CreateValidationError(string correlationId, string field, string validationKey, string englishDetails)
    {
        var hebrewMessage = GetValidationMessage(validationKey);
        var hebrewFieldName = GetFieldName(field);
        var fullHebrewMessage = $"{hebrewMessage} בשדה '{hebrewFieldName}'";

        var error = ErrorDetail.CreateValidation(
            $"VALIDATION_{validationKey.ToUpperInvariant()}",
            fullHebrewMessage,
            englishDetails,
            field
        );

        return ErrorResponse.Create(correlationId, error);
    }

    /// <summary>
    /// Creates a required field validation error
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="field">Required field that is missing</param>
    /// <returns>Required field error response</returns>
    public static ErrorResponse CreateRequiredFieldError(string correlationId, string field)
    {
        return CreateValidationError(
            correlationId, 
            field, 
            "required_field", 
            $"Required field '{field}' is missing or empty"
        );
    }

    /// <summary>
    /// Creates a duplicate name validation error
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="name">Duplicate name value</param>
    /// <returns>Duplicate name error response</returns>
    public static ErrorResponse CreateDuplicateNameError(string correlationId, string name)
    {
        return CreateValidationError(
            correlationId,
            "name",
            "duplicate_name",
            $"Data source with name '{name}' already exists"
        );
    }

    /// <summary>
    /// Creates an invalid schema validation error
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="schemaErrors">Schema validation errors</param>
    /// <returns>Schema validation error response</returns>
    public static ErrorResponse CreateInvalidSchemaError(string correlationId, string schemaErrors)
    {
        return CreateValidationError(
            correlationId,
            "json_schema",
            "invalid_schema",
            $"JSON schema validation failed: {schemaErrors}"
        );
    }

    /// <summary>
    /// Creates a not found error with Hebrew UI message
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="resourceType">Type of resource not found (e.g., "data_source")</param>
    /// <param name="resourceId">ID of the resource that wasn't found</param>
    /// <returns>Not found error response</returns>
    public static ErrorResponse CreateNotFoundError(string correlationId, string resourceType = "data_source", string? resourceId = null)
    {
        var hebrewMessage = GetNotFoundMessage(resourceType);
        var englishDetails = resourceId != null 
            ? $"{resourceType.Replace("_", " ")} with ID '{resourceId}' not found"
            : $"{resourceType.Replace("_", " ")} not found";

        var error = ErrorDetail.CreateNotFound(
            $"{resourceType.ToUpperInvariant()}_NOT_FOUND",
            hebrewMessage,
            englishDetails
        );

        return ErrorResponse.Create(correlationId, error);
    }

    /// <summary>
    /// Creates a conflict error with Hebrew UI message
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="conflictType">Type of conflict</param>
    /// <param name="englishDetails">English details for logging</param>
    /// <returns>Conflict error response</returns>
    public static ErrorResponse CreateConflictError(string correlationId, string conflictType, string englishDetails)
    {
        var hebrewMessage = GetConflictMessage(conflictType);

        var error = ErrorDetail.CreateConflict(
            $"CONFLICT_{conflictType.ToUpperInvariant()}",
            hebrewMessage,
            englishDetails
        );

        return ErrorResponse.Create(correlationId, error);
    }

    /// <summary>
    /// Creates a schema compatibility conflict error
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="compatibilityIssues">Compatibility issues description</param>
    /// <returns>Schema compatibility error response</returns>
    public static ErrorResponse CreateSchemaCompatibilityError(string correlationId, string compatibilityIssues)
    {
        return CreateConflictError(
            correlationId,
            "schema_compatibility",
            $"Schema compatibility issues: {compatibilityIssues}"
        );
    }

    /// <summary>
    /// Creates an internal server error with Hebrew UI message
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="errorType">Type of server error</param>
    /// <param name="englishDetails">English details for logging</param>
    /// <returns>Server error response</returns>
    public static ErrorResponse CreateServerError(string correlationId, string errorType = "unexpected_error", string? englishDetails = null)
    {
        var hebrewMessage = GetServerErrorMessage(errorType);
        var logDetails = englishDetails ?? $"Internal server error of type '{errorType}'";

        var error = ErrorDetail.CreateServerError(
            $"SERVER_ERROR_{errorType.ToUpperInvariant()}",
            hebrewMessage,
            logDetails
        );

        return ErrorResponse.Create(correlationId, error);
    }

    /// <summary>
    /// Creates a database error response
    /// </summary>
    /// <param name="correlationId">Request correlation ID</param>
    /// <param name="databaseError">Database error details</param>
    /// <returns>Database error response</returns>
    public static ErrorResponse CreateDatabaseError(string correlationId, string databaseError)
    {
        return CreateServerError(
            correlationId,
            "database_error",
            $"Database operation failed: {databaseError}"
        );
    }

    /// <summary>
    /// Loads error messages from the Hebrew JSON resource file
    /// </summary>
    /// <returns>Dictionary containing error messages</returns>
    private static Dictionary<string, object> LoadErrorMessages()
    {
        try
        {
            var resourcePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "ErrorMessages.he.json"
            );

            if (!File.Exists(resourcePath))
            {
                // Fallback - look in the project directory during development
                var projectPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources",
                    "ErrorMessages.he.json"
                );

                if (File.Exists(projectPath))
                {
                    resourcePath = projectPath;
                }
            }

            var json = File.ReadAllText(resourcePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, options) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            // Return empty dictionary as fallback - errors will use English messages
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Gets validation error message in Hebrew
    /// </summary>
    /// <param name="key">Validation error key</param>
    /// <returns>Hebrew validation message</returns>
    private static string GetValidationMessage(string key)
    {
        return GetNestedMessage("validation", key) ?? "שגיאת אימות";
    }

    /// <summary>
    /// Gets not found error message in Hebrew
    /// </summary>
    /// <param name="key">Resource type key</param>
    /// <returns>Hebrew not found message</returns>
    private static string GetNotFoundMessage(string key)
    {
        return GetNestedMessage("not_found", key) ?? "המשאב לא נמצא";
    }

    /// <summary>
    /// Gets conflict error message in Hebrew
    /// </summary>
    /// <param name="key">Conflict type key</param>
    /// <returns>Hebrew conflict message</returns>
    private static string GetConflictMessage(string key)
    {
        return GetNestedMessage("conflict", key) ?? "קונפליקט במשאב";
    }

    /// <summary>
    /// Gets server error message in Hebrew
    /// </summary>
    /// <param name="key">Server error type key</param>
    /// <returns>Hebrew server error message</returns>
    private static string GetServerErrorMessage(string key)
    {
        return GetNestedMessage("server_error", key) ?? "שגיאת שרת פנימית";
    }

    /// <summary>
    /// Gets field name in Hebrew
    /// </summary>
    /// <param name="field">English field name</param>
    /// <returns>Hebrew field name</returns>
    private static string GetFieldName(string field)
    {
        return GetNestedMessage("fields", field) ?? field;
    }

    /// <summary>
    /// Gets a nested message from the error messages dictionary
    /// </summary>
    /// <param name="category">Category key (e.g., "validation", "not_found")</param>
    /// <param name="key">Message key within the category</param>
    /// <returns>Hebrew message or null if not found</returns>
    private static string? GetNestedMessage(string category, string key)
    {
        try
        {
            if (_errorMessages.TryGetValue(category, out var categoryObj) && categoryObj != null)
            {
                if (categoryObj is JsonElement categoryElement && categoryElement.ValueKind == JsonValueKind.Object)
                {
                    if (categoryElement.TryGetProperty(key, out var messageElement))
                    {
                        return messageElement.GetString();
                    }
                }
            }
        }
        catch
        {
            // Return null to use fallback message
        }

        return null;
    }
}
