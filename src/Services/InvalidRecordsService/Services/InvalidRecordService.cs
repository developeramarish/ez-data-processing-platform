using DataProcessing.Shared.Entities;
using InvalidRecordsService.Models.Requests;
using InvalidRecordsService.Models.Responses;
using InvalidRecordsService.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace InvalidRecordsService.Services;

public class InvalidRecordService : IInvalidRecordService
{
    private readonly IInvalidRecordRepository _repository;
    private readonly ILogger<InvalidRecordService> _logger;

    public InvalidRecordService(
        IInvalidRecordRepository repository,
        ILogger<InvalidRecordService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<InvalidRecordListResponse> GetListAsync(InvalidRecordListRequest request)
    {
        var (records, totalCount) = await _repository.GetPagedAsync(request);
        
        // Map to DTOs with data source names
        var dtos = await MapToDtosAsync(records);

        return new InvalidRecordListResponse
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<InvalidRecordDto?> GetByIdAsync(string id)
    {
        var record = await _repository.GetByIdAsync(id);
        if (record == null)
        {
            return null;
        }

        var dtos = await MapToDtosAsync(new List<DataProcessingInvalidRecord> { record });
        return dtos.FirstOrDefault();
    }

    public async Task<StatisticsDto> GetStatisticsAsync()
    {
        return await _repository.GetStatisticsAsync();
    }

    public async Task UpdateStatusAsync(string id, UpdateStatusRequest request)
    {
        var ignore = request.Status.Equals("ignored", StringComparison.OrdinalIgnoreCase);
        await _repository.UpdateStatusAsync(id, request.UpdatedBy, request.Notes, ignore);
        
        _logger.LogInformation("Updated status for invalid record {RecordId} to {Status} by {User}",
            id, request.Status, request.UpdatedBy);
    }

    public async Task<BulkOperationResult> BulkDeleteAsync(BulkOperationRequest request)
    {
        var result = new BulkOperationResult
        {
            TotalRequested = request.RecordIds.Count
        };

        foreach (var recordId in request.RecordIds)
        {
            try
            {
                await _repository.DeleteAsync(recordId);
                result.Successful++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkOperationError
                {
                    RecordId = recordId,
                    Error = ex.Message
                });
                
                _logger.LogWarning(ex, "Failed to delete invalid record {RecordId}", recordId);
            }
        }

        _logger.LogInformation("Bulk delete completed: {Successful}/{Total} successful, RequestedBy: {User}",
            result.Successful, result.TotalRequested, request.RequestedBy);

        return result;
    }

    /// <summary>
    /// Maps entity models to DTOs with data source name resolution
    /// </summary>
    private async Task<List<InvalidRecordDto>> MapToDtosAsync(List<DataProcessingInvalidRecord> records)
    {
        if (!records.Any())
        {
            return new List<InvalidRecordDto>();
        }

        // Get unique data source IDs
        var dataSourceIds = records.Select(r => r.DataSourceId).Distinct().ToList();

        // Batch fetch data sources for efficiency
        var dataSources = await DB.Find<DataProcessingDataSource>()
            .Match(ds => dataSourceIds.Contains(ds.ID))
            .ExecuteAsync();

        var dataSourceMap = dataSources.ToDictionary(ds => ds.ID, ds => ds.Name);

        // Map records to DTOs
        return records.Select(r => MapToDto(r, dataSourceMap)).ToList();
    }

    /// <summary>
    /// Maps a single entity to DTO
    /// </summary>
    private InvalidRecordDto MapToDto(
        DataProcessingInvalidRecord record,
        Dictionary<string, string> dataSourceMap)
    {
        return new InvalidRecordDto
        {
            Id = record.ID,
            DataSourceId = record.DataSourceId,
            DataSourceName = dataSourceMap.GetValueOrDefault(record.DataSourceId, "Unknown"),
            FileName = record.FileName,
            LineNumber = record.LineNumber,
            CreatedAt = record.CreatedAt,
            Errors = MapValidationErrors(record),
            OriginalData = BsonDocumentToObject(record.OriginalRecord),
            ErrorType = record.ErrorType,
            Severity = record.Severity,
            IsReviewed = record.IsReviewed,
            ReviewedBy = record.ReviewedBy,
            ReviewedAt = record.ReviewedAt,
            ReviewNotes = record.ReviewNotes,
            IsIgnored = record.IsIgnored
        };
    }

    /// <summary>
    /// Maps validation errors to DTOs
    /// </summary>
    private List<ValidationErrorDto> MapValidationErrors(DataProcessingInvalidRecord record)
    {
        return record.ValidationErrors.Select(error => new ValidationErrorDto
        {
            Field = record.FieldName ?? "Unknown",
            Message = error,
            ErrorType = record.ErrorType,
            ExpectedValue = record.ExpectedValue,
            ActualValue = record.ActualValue
        }).ToList();
    }

    /// <summary>
    /// Converts BsonDocument to object for JSON serialization
    /// </summary>
    private object? BsonDocumentToObject(BsonDocument? bsonDoc)
    {
        if (bsonDoc == null)
        {
            return null;
        }

        try
        {
            var json = bsonDoc.ToJson();
            return BsonSerializer.Deserialize<object>(bsonDoc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize BsonDocument");
            return new { error = "Failed to parse original data" };
        }
    }

    /// <summary>
    /// Export invalid records to CSV format
    /// </summary>
    public async Task<byte[]> ExportToCsvAsync(ExportRequest request)
    {
        _logger.LogInformation("Exporting invalid records to CSV with filters");

        // Get filtered records (without pagination for export)
        var exportFilters = request.Filters;
        exportFilters.Page = 1;
        exportFilters.PageSize = int.MaxValue;  // Get all matching records

        var (records, _) = await _repository.GetPagedAsync(exportFilters);
        var dtos = await MapToDtosAsync(records);

        // Build CSV
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("ID,DataSource,FileName,LineNumber,CreatedAt,ErrorType,Severity,Errors,IsReviewed,ReviewedBy,IsIgnored");

        // Data rows
        foreach (var record in dtos)
        {
            var errorsText = string.Join("; ", record.Errors.Select(e => $"{e.Field}: {e.Message}"));
            errorsText = errorsText.Replace("\"", "\"\"");  // Escape quotes for CSV

            csv.AppendLine($"\"{record.Id}\",\"{record.DataSourceName}\",\"{record.FileName}\"," +
                $"{record.LineNumber ?? 0},\"{record.CreatedAt:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"{record.ErrorType}\",\"{record.Severity}\"," +
                $"\"{errorsText}\"," +
                $"{record.IsReviewed},\"{record.ReviewedBy ?? ""}\",{record.IsIgnored}");
        }

        _logger.LogInformation("Exported {Count} invalid records to CSV", dtos.Count);

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }
}
