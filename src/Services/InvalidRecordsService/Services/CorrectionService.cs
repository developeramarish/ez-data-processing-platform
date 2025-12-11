using DataProcessing.Shared.Entities;
using DataProcessing.Shared.Messages;
using InvalidRecordsService.Models.Requests;
using InvalidRecordsService.Models.Responses;
using InvalidRecordsService.Repositories;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;

namespace InvalidRecordsService.Services;

public class CorrectionService : ICorrectionService
{
    private readonly IInvalidRecordRepository _repository;
    private readonly ILogger<CorrectionService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public CorrectionService(
        IInvalidRecordRepository repository,
        ILogger<CorrectionService> logger,
        IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<CorrectionResult> CorrectRecordAsync(string recordId, CorrectRecordRequest request)
    {
        _logger.LogInformation(
            "Correcting invalid record {RecordId} by {User}",
            recordId, request.CorrectedBy);

        try
        {
            // 1. Get the invalid record
            var record = await _repository.GetByIdAsync(recordId);
            if (record == null)
            {
                return new CorrectionResult
                {
                    Success = false,
                    Message = "Record not found"
                };
            }

            // 2. Convert corrected data to BsonDocument for storage
            var correctedDataJson = System.Text.Json.JsonSerializer.Serialize(request.CorrectedData);
            var correctedDataBson = BsonDocument.Parse(correctedDataJson);

            // 3. Update record using repository (marks as reviewed/corrected)
            await _repository.UpdateStatusAsync(recordId, request.CorrectedBy, "Data corrected", ignore: false);

            _logger.LogInformation(
                "Record {RecordId} corrected successfully by {User}",
                recordId, request.CorrectedBy);

            // 4. If AutoReprocess is enabled, publish ValidationRequestEvent for actual revalidation
            if (request.AutoReprocess)
            {
                await PublishRevalidationRequest(record, correctedDataBson);

                _logger.LogInformation(
                    "Published ValidationRequestEvent for reprocessing record {RecordId}",
                    recordId);
            }

            return new CorrectionResult
            {
                Success = true,
                Message = "Record corrected successfully",
                ValidationResultId = null  // Would be set if reprocessed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to correct record {RecordId}", recordId);
            return new CorrectionResult
            {
                Success = false,
                Message = $"Correction failed: {ex.Message}"
            };
        }
    }

    public async Task<ReprocessResult> ReprocessRecordAsync(string recordId)
    {
        _logger.LogInformation("Reprocessing invalid record {RecordId}", recordId);

        try
        {
            // 1. Get the invalid record
            var record = await _repository.GetByIdAsync(recordId);
            if (record == null)
            {
                return new ReprocessResult
                {
                    Success = false,
                    IsValid = false,
                    Message = "Record not found",
                    ValidationErrors = new List<string> { "Record not found" }
                };
            }

            // 2. For MVP: Mark as reviewed/reprocessed using repository
            // Full implementation would send to ValidationService for actual revalidation
            await _repository.UpdateStatusAsync(recordId, "System", "Reprocessed", ignore: false);

            _logger.LogInformation(
                "Record {RecordId} marked for reprocessing - actual revalidation requires ValidationService HTTP client (future enhancement)",
                recordId);

            // For MVP, return success without actual validation
            // Real implementation would POST to ValidationService API
            return new ReprocessResult
            {
                Success = true,
                IsValid = false,  // Unknown until actual revalidation
                Message = "Record marked for reprocessing (ValidationService integration pending)",
                ValidationErrors = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reprocess record {RecordId}", recordId);
            return new ReprocessResult
            {
                Success = false,
                IsValid = false,
                Message = $"Reprocess failed: {ex.Message}",
                ValidationErrors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BulkOperationResult> BulkReprocessAsync(BulkOperationRequest request)
    {
        _logger.LogInformation(
            "Bulk reprocessing {Count} records by {User}",
            request.RecordIds.Count, request.RequestedBy);

        var result = new BulkOperationResult
        {
            TotalRequested = request.RecordIds.Count
        };

        foreach (var recordId in request.RecordIds)
        {
            try
            {
                await _repository.UpdateStatusAsync(recordId, request.RequestedBy, "Bulk reprocessed", ignore: false);
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
                
                _logger.LogWarning(ex, "Failed to reprocess record {RecordId}", recordId);
            }
        }

        _logger.LogInformation(
            "Bulk reprocess completed: {Successful}/{Total} successful",
            result.Successful, result.TotalRequested);

        return result;
    }

    public async Task<BulkOperationResult> BulkIgnoreAsync(BulkOperationRequest request)
    {
        _logger.LogInformation(
            "Bulk ignoring {Count} records by {User}",
            request.RecordIds.Count, request.RequestedBy);

        var result = new BulkOperationResult
        {
            TotalRequested = request.RecordIds.Count
        };

        foreach (var recordId in request.RecordIds)
        {
            try
            {
                await _repository.UpdateStatusAsync(recordId, request.RequestedBy, "Bulk ignored", ignore: true);
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
                
                _logger.LogWarning(ex, "Failed to ignore record {RecordId}", recordId);
            }
        }

        _logger.LogInformation(
            "Bulk ignore completed: {Successful}/{Total} successful",
            result.Successful, result.TotalRequested);

        return result;
    }

    private async Task PublishRevalidationRequest(
        DataProcessingInvalidRecord record,
        BsonDocument correctedData)
    {
        _logger.LogInformation(
            "Publishing revalidation request for invalid record {RecordId}",
            record.ID);

        // Convert corrected data to JSON bytes
        var jsonContent = correctedData.ToJson();
        var fileContent = Encoding.UTF8.GetBytes(jsonContent);

        // Create ValidationRequestEvent for reprocessing
        var validationRequest = new ValidationRequestEvent
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            PublishedBy = "InvalidRecordsService-Reprocess",
            DataSourceId = record.DataSourceId,
            FileName = $"{record.FileName}_CORRECTED",
            FileContent = fileContent,
            FileContentType = "application/json",
            FileSizeBytes = fileContent.Length,
            HazelcastKey = string.Empty,  // No Hazelcast needed for single record
            OriginalFormat = "JSON",
            FormatMetadata = new Dictionary<string, object>(),
            IsReprocess = true,
            OriginalInvalidRecordId = record.ID
        };

        // Publish to RabbitMQ for ValidationService to consume
        await _publishEndpoint.Publish(validationRequest);

        _logger.LogInformation(
            "ValidationRequestEvent published successfully for record {RecordId}, CorrelationId: {CorrelationId}",
            record.ID, validationRequest.CorrelationId);
    }
}
