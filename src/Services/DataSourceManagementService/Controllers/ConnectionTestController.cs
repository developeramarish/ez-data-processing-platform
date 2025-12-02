// ConnectionTestController.cs - API endpoints for connection testing
// Date: December 2, 2025

using Microsoft.AspNetCore.Mvc;
using DataProcessing.DataSourceManagement.Models.ConnectionTest;
using DataProcessing.DataSourceManagement.Services.ConnectionTest;

namespace DataProcessing.DataSourceManagement.Controllers;

[ApiController]
[Route("api/v1/test-connection")]
public class ConnectionTestController : ControllerBase
{
    private readonly IConnectionTestService _connectionTestService;
    private readonly ILogger<ConnectionTestController> _logger;

    public ConnectionTestController(
        IConnectionTestService connectionTestService,
        ILogger<ConnectionTestController> logger)
    {
        _connectionTestService = connectionTestService;
        _logger = logger;
    }

    /// <summary>
    /// Test Kafka broker connectivity and topic existence
    /// </summary>
    [HttpPost("kafka")]
    public async Task<ActionResult<ConnectionTestResult>> TestKafkaConnection(
        [FromBody] KafkaConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing Kafka connection: {Broker}, topic: {Topic}",
            request.BrokerServer, request.Topic);

        var result = await _connectionTestService.TestKafkaConnectionAsync(request, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Test folder path existence, write permissions, and disk space
    /// </summary>
    [HttpPost("folder")]
    public async Task<ActionResult<ConnectionTestResult>> TestFolderConnection(
        [FromBody] FolderConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing folder connection: {Path}", request.Path);

        var result = await _connectionTestService.TestFolderConnectionAsync(request, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Test SFTP server connectivity, authentication, and directory access
    /// </summary>
    [HttpPost("sftp")]
    public async Task<ActionResult<ConnectionTestResult>> TestSftpConnection(
        [FromBody] SftpConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing SFTP connection: {Host}:{Port}", request.Host, request.Port);

        var result = await _connectionTestService.TestSftpConnectionAsync(request, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }
}
