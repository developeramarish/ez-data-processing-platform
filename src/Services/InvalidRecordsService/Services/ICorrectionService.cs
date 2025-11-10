using InvalidRecordsService.Models.Requests;
using InvalidRecordsService.Models.Responses;

namespace InvalidRecordsService.Services;

public interface ICorrectionService
{
    Task<CorrectionResult> CorrectRecordAsync(string recordId, CorrectRecordRequest request);
    Task<ReprocessResult> ReprocessRecordAsync(string recordId);
    Task<BulkOperationResult> BulkReprocessAsync(BulkOperationRequest request);
    Task<BulkOperationResult> BulkIgnoreAsync(BulkOperationRequest request);
}
