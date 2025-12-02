// ConnectionTestService.cs - Implementation of connection testing
// Date: December 2, 2025

using System.Diagnostics;
using Confluent.Kafka;
using DataProcessing.DataSourceManagement.Models.ConnectionTest;
using Renci.SshNet;

namespace DataProcessing.DataSourceManagement.Services.ConnectionTest;

public class ConnectionTestService : IConnectionTestService
{
    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(ILogger<ConnectionTestService> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestKafkaConnectionAsync(
        KafkaConnectionTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Testing Kafka connection to {Broker}, topic {Topic}",
                request.BrokerServer, request.Topic);

            var config = new AdminClientConfig
            {
                BootstrapServers = request.BrokerServer,
                SocketTimeoutMs = request.TimeoutSeconds * 1000,
                ApiVersionRequestTimeoutMs = request.TimeoutSeconds * 1000
            };

            // Add SASL authentication if provided
            if (!string.IsNullOrEmpty(request.Username))
            {
                config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                config.SaslMechanism = SaslMechanism.Plain;
                config.SaslUsername = request.Username;
                config.SaslPassword = request.Password;
            }

            using var adminClient = new AdminClientBuilder(config).Build();

            // Test broker connectivity
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(request.TimeoutSeconds));

            if (metadata.Brokers.Count == 0)
            {
                sw.Stop();
                return ConnectionTestResult.Failed(
                    "Kafka connection failed",
                    "No brokers found. Check broker server address.",
                    (int)sw.ElapsedMilliseconds
                );
            }

            // Check if topic exists
            var topicExists = metadata.Topics.Any(t => t.Topic == request.Topic);

            sw.Stop();

            var details = new Dictionary<string, object>
            {
                ["brokerReachable"] = true,
                ["brokerCount"] = metadata.Brokers.Count,
                ["topicExists"] = topicExists,
                ["topicName"] = request.Topic,
                ["authenticationSuccessful"] = !string.IsNullOrEmpty(request.Username),
                ["latencyMs"] = sw.ElapsedMilliseconds
            };

            if (!topicExists)
            {
                details["warning"] = $"Topic '{request.Topic}' does not exist. It will be created automatically when first message is sent.";
            }

            return ConnectionTestResult.Successful(
                "Kafka connection successful",
                details,
                (int)sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Kafka connection test failed for {Broker}", request.BrokerServer);

            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" | {ex.InnerException.Message}";
            }

            return ConnectionTestResult.Failed(
                "Kafka connection failed",
                errorMessage,
                (int)sw.ElapsedMilliseconds
            );
        }
    }

    public async Task<ConnectionTestResult> TestFolderConnectionAsync(
        FolderConnectionTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Testing folder connection to {Path}", request.Path);

            var details = new Dictionary<string, object>();

            // Check if folder exists
            if (!Directory.Exists(request.Path))
            {
                sw.Stop();
                return ConnectionTestResult.Failed(
                    "Folder validation failed",
                    $"Folder does not exist: {request.Path}",
                    (int)sw.ElapsedMilliseconds
                );
            }

            details["folderExists"] = true;
            details["path"] = request.Path;

            // Check write permissions
            if (request.CheckWritePermissions)
            {
                var testFileName = Path.Combine(request.Path, $".test-{Guid.NewGuid()}.tmp");
                try
                {
                    await File.WriteAllTextAsync(testFileName, "test", cancellationToken);
                    File.Delete(testFileName);
                    details["writable"] = true;
                }
                catch (UnauthorizedAccessException)
                {
                    sw.Stop();
                    return ConnectionTestResult.Failed(
                        "Folder validation failed",
                        $"No write permissions for folder: {request.Path}",
                        (int)sw.ElapsedMilliseconds
                    );
                }
            }

            // Check disk space
            if (request.CheckDiskSpace)
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(request.Path)!);
                var freeSpaceGB = Math.Round((double)driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024), 2);
                details["diskSpaceGB"] = freeSpaceGB;

                if (freeSpaceGB < 1.0)
                {
                    details["warning"] = "Low disk space (< 1GB available)";
                }
            }

            sw.Stop();

            return ConnectionTestResult.Successful(
                "Folder validation successful",
                details,
                (int)sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Folder validation failed for {Path}", request.Path);

            return ConnectionTestResult.Failed(
                "Folder validation failed",
                ex.Message,
                (int)sw.ElapsedMilliseconds
            );
        }
    }

    public async Task<ConnectionTestResult> TestSftpConnectionAsync(
        SftpConnectionTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Testing SFTP connection to {Host}:{Port}", request.Host, request.Port);

            AuthenticationMethod authMethod;

            // Determine authentication method
            if (!string.IsNullOrEmpty(request.SshKey))
            {
                // SSH Key authentication
                using var keyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(request.SshKey));
                var keyFile = new PrivateKeyFile(keyStream);
                authMethod = new PrivateKeyAuthenticationMethod(request.Username, keyFile);
            }
            else
            {
                // Password authentication
                authMethod = new PasswordAuthenticationMethod(request.Username, request.Password ?? "");
            }

            var connectionInfo = new Renci.SshNet.ConnectionInfo(
                request.Host,
                request.Port,
                request.Username,
                authMethod
            );

            connectionInfo.Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds);

            using var client = new SftpClient(connectionInfo);

            // Connect to SFTP server
            await Task.Run(() => client.Connect(), cancellationToken);

            if (!client.IsConnected)
            {
                sw.Stop();
                return ConnectionTestResult.Failed(
                    "SFTP connection failed",
                    "Could not establish connection to SFTP server",
                    (int)sw.ElapsedMilliseconds
                );
            }

            var details = new Dictionary<string, object>
            {
                ["serverReachable"] = true,
                ["authenticationSuccessful"] = true,
                ["connected"] = client.IsConnected,
                ["serverVersion"] = client.ConnectionInfo.ServerVersion ?? "Unknown"
            };

            // Test directory access
            try
            {
                var exists = client.Exists(request.RemotePath);
                details["directoryAccessible"] = exists;

                if (!exists)
                {
                    details["warning"] = $"Remote path '{request.RemotePath}' does not exist";
                }
                else
                {
                    // Try to list directory contents to verify read permissions
                    var files = client.ListDirectory(request.RemotePath).Take(1).ToList();
                    details["readable"] = true;

                    // Try to create a test file to verify write permissions
                    var testFileName = $"{request.RemotePath.TrimEnd('/')}/. test-{Guid.NewGuid()}.tmp";
                    try
                    {
                        using var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));
                        client.UploadFile(testStream, testFileName);
                        client.DeleteFile(testFileName);
                        details["writable"] = true;
                    }
                    catch
                    {
                        details["writable"] = false;
                        details["warning"] = "Directory is readable but not writable";
                    }
                }
            }
            catch (Exception dirEx)
            {
                details["directoryAccessible"] = false;
                details["directoryError"] = dirEx.Message;
            }

            client.Disconnect();
            sw.Stop();

            return ConnectionTestResult.Successful(
                "SFTP connection successful",
                details,
                (int)sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SFTP connection test failed for {Host}:{Port}", request.Host, request.Port);

            var errorMessage = ex.Message;
            if (ex is Renci.SshNet.Common.SshAuthenticationException)
            {
                errorMessage = "Authentication failed. Check username and password/SSH key.";
            }
            else if (ex is System.Net.Sockets.SocketException)
            {
                errorMessage = $"Cannot reach SFTP server at {request.Host}:{request.Port}. Check host and port.";
            }

            return ConnectionTestResult.Failed(
                "SFTP connection failed",
                errorMessage,
                (int)sw.ElapsedMilliseconds
            );
        }
    }
}
