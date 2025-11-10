using DataProcessing.Shared.Entities;
using DataProcessing.DataSourceManagement.Models.Schema.Requests;
using DataProcessing.DataSourceManagement.Repositories.Schema;

namespace DataProcessing.DataSourceManagement.Services.Schema;

public class SchemaService : ISchemaService
{
    private readonly ISchemaRepository _repository;

    public SchemaService(ISchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DataProcessingSchema>> GetSchemasAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DataProcessingSchema?> GetSchemaByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DataProcessingSchema> CreateSchemaAsync(CreateSchemaRequest request)
    {
        var schema = new DataProcessingSchema
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            DataSourceId = request.DataSourceId,
            JsonSchemaContent = request.JsonSchemaContent,
            Tags = request.Tags,
            Status = request.Status,
            Version = request.Version,
            CreatedBy = request.CreatedBy,
            CorrelationId = Guid.NewGuid().ToString()
        };

        return await _repository.CreateAsync(schema);
    }

    public async Task<DataProcessingSchema> UpdateSchemaAsync(string id, UpdateSchemaRequest request)
    {
        var schema = await _repository.GetByIdAsync(id);
        if (schema == null)
            throw new ArgumentException($"Schema with ID {id} not found");

        schema.DisplayName = request.DisplayName;
        schema.Description = request.Description;
        schema.DataSourceId = request.DataSourceId;
        schema.JsonSchemaContent = request.JsonSchemaContent;
        schema.Tags = request.Tags;
        schema.Status = request.Status;
        schema.UpdatedBy = request.UpdatedBy;
        schema.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(schema);
    }

    public async Task DeleteSchemaAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }
}
