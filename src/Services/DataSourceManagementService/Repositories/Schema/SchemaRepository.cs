using DataProcessing.Shared.Entities;
using MongoDB.Entities;

namespace DataProcessing.DataSourceManagement.Repositories.Schema;

public interface ISchemaRepository
{
    Task<List<DataProcessingSchema>> GetAllAsync();
    Task<DataProcessingSchema?> GetByIdAsync(string id);
    Task<DataProcessingSchema> CreateAsync(DataProcessingSchema schema);
    Task<DataProcessingSchema> UpdateAsync(DataProcessingSchema schema);
    Task DeleteAsync(string id);
}

public class SchemaRepository : ISchemaRepository
{
    public async Task<List<DataProcessingSchema>> GetAllAsync()
    {
        return await DB.Find<DataProcessingSchema>()
            .Match(s => !s.IsDeleted)
            .ExecuteAsync();
    }

    public async Task<DataProcessingSchema?> GetByIdAsync(string id)
    {
        return await DB.Find<DataProcessingSchema>()
            .Match(s => s.ID == id && !s.IsDeleted)
            .ExecuteFirstAsync();
    }

    public async Task<DataProcessingSchema> CreateAsync(DataProcessingSchema schema)
    {
        await schema.SaveAsync();
        return schema;
    }

    public async Task<DataProcessingSchema> UpdateAsync(DataProcessingSchema schema)
    {
        await schema.SaveAsync();
        return schema;
    }

    public async Task DeleteAsync(string id)
    {
        await DB.Update<DataProcessingSchema>()
            .Match(s => s.ID == id)
            .Modify(s => s.IsDeleted, true)
            .Modify(s => s.UpdatedAt, DateTime.UtcNow)
            .ExecuteAsync();
    }
}
