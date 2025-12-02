// ConnectionTestModels.cs - Models for connection testing
// Date: December 2, 2025

namespace DataProcessing.DataSourceManagement.Models.ConnectionTest;

// Request Models

public class KafkaConnectionTestRequest
{
    public string BrokerServer { get; set; } = "";
    public string Topic { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public class FolderConnectionTestRequest
{
    public string Path { get; set; } = "";
    public bool CheckWritePermissions { get; set; } = true;
    public bool CheckDiskSpace { get; set; } = true;
}

public class SftpConnectionTestRequest
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public string? SshKey { get; set; }
    public string RemotePath { get; set; } = "/";
    public int TimeoutSeconds { get; set; } = 30;
}

// Response Model

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, object> Details { get; set; } = new();
    public string? ErrorDetails { get; set; }
    public int DurationMs { get; set; }

    public static ConnectionTestResult Successful(string message, Dictionary<string, object> details, int durationMs)
    {
        return new ConnectionTestResult
        {
            Success = true,
            Message = message,
            Details = details,
            DurationMs = durationMs
        };
    }

    public static ConnectionTestResult Failed(string message, string errorDetails, int durationMs)
    {
        return new ConnectionTestResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails,
            Details = new Dictionary<string, object>(),
            DurationMs = durationMs
        };
    }
}
