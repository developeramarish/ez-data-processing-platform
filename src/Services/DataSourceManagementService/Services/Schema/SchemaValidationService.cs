using DataProcessing.DataSourceManagement.Models.Schema.Responses;
using DataProcessing.DataSourceManagement.Repositories.Schema;
using DataProcessing.Shared.Entities;
using MongoDB.Entities;
using NJsonSchema;
using System.Text.Json;

namespace DataProcessing.DataSourceManagement.Services.Schema;

public class SchemaValidationService : ISchemaValidationService
{
    private readonly ISchemaRepository _schemaRepository;

    public SchemaValidationService(ISchemaRepository schemaRepository)
    {
        _schemaRepository = schemaRepository;
    }

    public async Task<ValidationResult> ValidateJsonSchemaAsync(string jsonSchemaContent)
    {
        var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

        try
        {
            // Parse JSON first
            var jsonDoc = JsonDocument.Parse(jsonSchemaContent);
            
            // Validate using NJsonSchema
            var schema = await JsonSchema.FromJsonAsync(jsonSchemaContent);
            
            // Schema parsed successfully
            return result;
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid JSON: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid JSON Schema: {ex.Message}");
            return result;
        }
    }

    public async Task<DataValidationResult> ValidateDataAgainstSchemaAsync(string schemaId, object data)
    {
        var result = new DataValidationResult { IsValid = true, Errors = new List<FieldValidationError>() };

        try
        {
            var schemaEntity = await _schemaRepository.GetByIdAsync(schemaId);
            if (schemaEntity == null)
            {
                result.IsValid = false;
                result.Errors.Add(new FieldValidationError 
                { 
                    Field = "schema",
                    Message = "Schema not found",
                    MessageHebrew = "Schema לא נמצא"
                });
                return result;
            }

            // Parse schema
            var schema = await JsonSchema.FromJsonAsync(schemaEntity.JsonSchemaContent);
            
            // Serialize data to JSON string
            var dataJson = JsonSerializer.Serialize(data);
            
            // Validate data against schema
            var errors = schema.Validate(dataJson);
            
            if (errors.Count > 0)
            {
                result.IsValid = false;
                foreach (var error in errors)
                {
                    result.Errors.Add(new FieldValidationError
                    {
                        Field = error.Path ?? "root",
                        Message = error.ToString(),
                        MessageHebrew = TranslateValidationError(error)
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(new FieldValidationError 
            { 
                Field = "general",
                Message = ex.Message,
                MessageHebrew = "שגיאה באימות"
            });
            return result;
        }
    }

    private string TranslateValidationError(NJsonSchema.Validation.ValidationError error)
    {
        var kind = error.Kind.ToString();
        
        return kind switch
        {
            "Required" => $"שדה חובה: {error.Property}",
            "Type" => "סוג לא תקין",
            "Format" => "פורמט לא תקין",
            "Pattern" => "תבנית לא תקינה",
            "MinLength" => "אורך מינימלי לא מתקיים",
            "MaxLength" => "אורך מקסימלי חורג",
            "Minimum" => "ערך מינימלי לא מתקיים",
            "Maximum" => "ערך מקסימלי חורג",
            "MinItems" => "מספר פריטים מינימלי לא מתקיים",
            "MaxItems" => "מספר פריטים מקסימלי חורג",
            "UniqueItems" => "פריטים חייבים להיות ייחודיים",
            "Enum" => "ערך חייב להיות אחד מהרשימה",
            "AdditionalProperties" => "מאפיין נוסף לא מורשה",
            _ => error.ToString()
        };
    }
}
