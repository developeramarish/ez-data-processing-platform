using DataProcessing.Shared.Entities;
using DataProcessing.DataSourceManagement.Models.Schema.Requests;

namespace DataProcessing.DataSourceManagement.Services.Schema;

public interface ISchemaService
{
    Task<List<DataProcessingSchema>> GetSchemasAsync();
    Task<DataProcessingSchema?> GetSchemaByIdAsync(string id);
    Task<DataProcessingSchema> CreateSchemaAsync(CreateSchemaRequest request);
    Task<DataProcessingSchema> UpdateSchemaAsync(string id, UpdateSchemaRequest request);
    Task DeleteSchemaAsync(string id);
}
