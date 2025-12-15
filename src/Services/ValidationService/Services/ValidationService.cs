using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Monitoring;
using DataProcessing.Validation.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Corvus.Json;
using Corvus.Json.Validator;
using System.Diagnostics;
using System.Text;
using JsonElement = System.Text.Json.JsonElement;
using JsonDocument = System.Text.Json.JsonDocument;

namespace DataProcessing.Validation.Services;

/// <summary>
/// Service for validating file content using JSON Schema validation
/// Validates normalized JSON records while preserving original file format structure
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly DataProcessingMetrics _metrics;
    private readonly ActivitySource _activitySource;

    public ValidationService(
        ILogger<ValidationService> logger,
        DataProcessingMetrics metrics,
        ActivitySource activitySource)
    {
        _logger = logger;
        _metrics = metrics;
        _activitySource = activitySource;
    }

    public async Task<Models.ValidationResult> ValidateFileContentAsync(
        string dataSourceId,
        string fileName,
        byte[] fileContent,
        string contentType,
        string correlationId)
    {
        using var activity = _activitySource.StartDataProcessingActivity(
            "ValidationService.ValidateFileContent",
            correlationId,
            dataSourceId);

        _logger.LogInformation("Starting validation for file: {FileName}, DataSource: {DataSourceId}, CorrelationId: {CorrelationId}",
            fileName, dataSourceId, correlationId);

        try
        {
            // Get data source configuration and schema
            var dataSource = await DB.Find<DataProcessingDataSource>()
                .Match(ds => ds.ID == dataSourceId && ds.IsActive && !ds.IsDeleted)
                .ExecuteFirstAsync();

            if (dataSource == null)
            {
                throw new InvalidOperationException($"Data source not found or inactive: {dataSourceId}");
            }

            // Parse file content as JSON (already normalized by Files Receiver Service)
            var jsonContent = Encoding.UTF8.GetString(fileContent);
            var jsonData = JToken.Parse(jsonContent);

            // Get JSON Schema for validation
            var schema = await GetValidationSchemaAsync(dataSource, correlationId);

            // Extract individual records for validation
            var records = ExtractRecordsFromJson(jsonData);
            
            _logger.LogInformation("Extracted {RecordCount} records from file: {FileName}", records.Count, fileName);

            // Validate each record against the schema
            var validationResults = await ValidateRecordsAsync(records, schema, correlationId);

            // Store validation results
            var validationResultId = await StoreValidationResultsAsync(
                dataSourceId,
                fileName,
                jsonContent,
                validationResults,
                correlationId);

            var totalRecords = validationResults.Count;
            var validRecords = validationResults.Count(r => r.IsValid);
            var invalidRecords = totalRecords - validRecords;

            activity?.SetValidationContext(totalRecords, validRecords, invalidRecords);

            _logger.LogInformation("Validation completed for file: {FileName}, Total: {Total}, Valid: {Valid}, Invalid: {Invalid}",
                fileName, totalRecords, validRecords, invalidRecords);

            // Extract valid records data for Hazelcast caching
            var validRecordsData = records
                .Where((r, i) => i < validationResults.Count && validationResults[i].IsValid)
                .ToList();

            return new Models.ValidationResult
            {
                ValidationResultId = validationResultId,
                TotalRecords = totalRecords,
                ValidRecords = validRecords,
                InvalidRecords = invalidRecords,
                ValidRecordsData = validRecordsData.Count > 0 ? validRecordsData : null,
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for file: {FileName}, DataSource: {DataSourceId}",
                fileName, dataSourceId);
            activity?.SetError(ex);
            throw;
        }
    }

    private Task<Corvus.Json.Validator.JsonSchema> GetValidationSchemaAsync(DataProcessingDataSource dataSource, string correlationId)
    {
        var schemaJson = dataSource.JsonSchema?.ToString();

        _logger.LogInformation("Loading JSON Schema for data source: {DataSourceId}", dataSource.ID);

        // For now, use a default schema if none is specified
        var defaultSchema = """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "type": "object",
          "properties": {},
          "additionalProperties": true
        }
        """;

        var effectiveSchema = !string.IsNullOrEmpty(schemaJson)
            ? schemaJson
            : defaultSchema;

        // Parse schema using Corvus.Json.Validator - dynamically generated validators are cached automatically
        // Provide a canonical URI for the schema (required by Corvus.Json.Validator)
        var canonicalUri = $"urn:datasource:{dataSource.ID}:schema";
        var schema = Corvus.Json.Validator.JsonSchema.FromText(effectiveSchema, canonicalUri);
        return Task.FromResult(schema);
    }

    private List<JObject> ExtractRecordsFromJson(JToken jsonData)
    {
        var records = new List<JObject>();

        switch (jsonData.Type)
        {
            case JTokenType.Array:
                // Array of records - each element is a record
                foreach (var item in jsonData.Children<JObject>())
                {
                    records.Add(item);
                }
                break;

            case JTokenType.Object:
                var obj = (JObject)jsonData;
                
                // Look for arrays within the object that represent records
                var arrayFound = false;
                foreach (var property in obj.Properties())
                {
                    if (property.Value.Type == JTokenType.Array)
                    {
                        foreach (var item in property.Value.Children<JObject>())
                        {
                            records.Add(item);
                        }
                        arrayFound = true;
                        break; // Use first array found
                    }
                }
                
                // If no arrays found, treat the entire object as a single record
                if (!arrayFound)
                {
                    records.Add(obj);
                }
                break;

            default:
                // Single value - wrap in object
                records.Add(new JObject { ["value"] = jsonData });
                break;
        }

        return records;
    }

    private Task<List<RecordValidationResult>> ValidateRecordsAsync(
        List<JObject> records,
        Corvus.Json.Validator.JsonSchema schema,
        string correlationId)
    {
        return Task.Run(() =>
        {
            var results = new List<RecordValidationResult>();

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                try
                {
                    // Convert JObject to System.Text.Json.JsonElement for Corvus validation
                    var recordJson = record.ToString(Formatting.None);
                    using var jsonDoc = JsonDocument.Parse(recordJson);
                    var jsonElement = jsonDoc.RootElement;

                    // Validate using Corvus.Json.Validator (dynamic code gen with Roslyn, cached)
                    var validationContext = schema.Validate(jsonElement, ValidationLevel.Detailed);
                    var isValid = validationContext.IsValid;

                    // Extract error messages from validation context
                    // Results contains validation errors when IsValid is false
                    var errors = new List<string>();
                    if (!isValid)
                    {
                        foreach (var validationError in validationContext.Results)
                        {
                            // Each result in Results is already an error - use ToString()
                            errors.Add(validationError.ToString());
                        }
                    }

                    results.Add(new RecordValidationResult
                    {
                        RecordIndex = i,
                        RecordData = recordJson,
                        IsValid = isValid,
                        ValidationErrors = errors
                    });

                    if (!isValid)
                    {
                        _logger.LogDebug("Record {RecordIndex} validation failed: {Errors}",
                            i, string.Join(", ", errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error validating record {RecordIndex}: {Error}", i, ex.Message);

                    results.Add(new RecordValidationResult
                    {
                        RecordIndex = i,
                        RecordData = record.ToString(Formatting.None),
                        IsValid = false,
                        ValidationErrors = new List<string> { $"Validation error: {ex.Message}" }
                    });
                }
            }

            return results;
        });
    }

    private async Task<string> StoreValidationResultsAsync(
        string dataSourceId,
        string fileName,
        string originalContent,
        List<RecordValidationResult> validationResults,
        string correlationId)
    {
        var validationResult = new DataProcessingValidationResult
        {
            DataSourceId = dataSourceId,
            FileName = fileName,
            TotalRecords = validationResults.Count,
            ValidRecords = validationResults.Count(r => r.IsValid),
            InvalidRecords = validationResults.Count(r => !r.IsValid),
            Status = validationResults.All(r => r.IsValid) ? "Success" : "PartialFailure",
            CorrelationId = correlationId,
            CreatedBy = "ValidationService"
        };

        await validationResult.SaveAsync();

        // Store invalid records separately for detailed analysis
        var invalidRecords = validationResults
            .Where(r => !r.IsValid)
            .Select(r => new DataProcessingInvalidRecord
            {
                DataSourceId = dataSourceId,
                FileName = fileName,
                ValidationResultId = validationResult.ID,
                OriginalRecord = MongoDB.Bson.BsonDocument.Parse(r.RecordData),
                ValidationErrors = r.ValidationErrors,
                ErrorType = "SchemaValidation",
                LineNumber = r.RecordIndex,
                CorrelationId = correlationId,
                CreatedBy = "ValidationService"
            })
            .ToList();

        if (invalidRecords.Any())
        {
            await invalidRecords.SaveAsync();
            
            _logger.LogInformation("Stored {InvalidCount} invalid records for validation result: {ValidationResultId}",
                invalidRecords.Count, validationResult.ID);
        }

        _logger.LogInformation("Stored validation result: {ValidationResultId} for file: {FileName}",
            validationResult.ID, fileName);

        return validationResult.ID;
    }
}

/// <summary>
/// Result of validating a single record
/// </summary>
internal class RecordValidationResult
{
    public int RecordIndex { get; set; }
    public string RecordData { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}
