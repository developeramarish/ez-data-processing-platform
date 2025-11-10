using SchemaManagementService.Entities;
using SchemaManagementService.Models.Requests;

namespace SchemaManagementService.Services;

/// <summary>
/// Interface for schema management service
/// </summary>
public interface ISchemaService
{
    /// <summary>
    /// Get all schemas
    /// </summary>
    Task<List<DataProcessingSchema>> GetSchemasAsync();

    /// <summary>
    /// Get schema by ID
    /// </summary>
    Task<DataProcessingSchema?> GetSchemaByIdAsync(string id);

    /// <summary>
    /// Create new schema
    /// </summary>
    Task<DataProcessingSchema> CreateSchemaAsync(CreateSchemaRequest request);

    /// <summary>
    /// Update existing schema
    /// </summary>
    Task<DataProcessingSchema> UpdateSchemaAsync(string id, UpdateSchemaRequest request);

    /// <summary>
    /// Delete schema (soft delete)
    /// </summary>
    Task DeleteSchemaAsync(string id);

    /// <summary>
    /// Check if schema name exists
    /// </summary>
    Task<bool> SchemaNameExistsAsync(string name, string? excludeId = null);

    /// <summary>
    /// Get schemas by data source ID
    /// </summary>
    Task<List<DataProcessingSchema>> GetSchemasByDataSourceAsync(string dataSourceId);

    /// <summary>
    /// Update schema usage count
    /// </summary>
    Task UpdateUsageCountAsync(string schemaId, int increment = 1);
}
