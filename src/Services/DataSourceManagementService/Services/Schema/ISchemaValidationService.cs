using DataProcessing.DataSourceManagement.Models.Schema.Responses;

namespace DataProcessing.DataSourceManagement.Services.Schema;

public interface ISchemaValidationService
{
    Task<ValidationResult> ValidateJsonSchemaAsync(string jsonSchemaContent);
    Task<DataValidationResult> ValidateDataAgainstSchemaAsync(string schemaId, object data);
}
