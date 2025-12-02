// IConnectionTestService.cs - Interface for connection testing
// Date: December 2, 2025

using DataProcessing.DataSourceManagement.Models.ConnectionTest;

namespace DataProcessing.DataSourceManagement.Services.ConnectionTest;

public interface IConnectionTestService
{
    Task<ConnectionTestResult> TestKafkaConnectionAsync(KafkaConnectionTestRequest request, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestFolderConnectionAsync(FolderConnectionTestRequest request, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestSftpConnectionAsync(SftpConnectionTestRequest request, CancellationToken cancellationToken = default);
}
