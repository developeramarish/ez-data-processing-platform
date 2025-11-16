using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace ServiceOrchestrator.Services;

public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public int Port { get; set; }
    public string HealthEndpoint { get; set; } = string.Empty;
    public bool IsFrontend { get; set; } = false;
}

public class ProcessManager
{
    public async Task StopAllServicesAsync()
    {
        Console.WriteLine("🛑 Stopping all running services...\n");
        
        var ports = new[] { 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 3000 };
        
        foreach (var port in ports)
        {
            await StopProcessOnPortAsync(port);
        }
        
        Console.WriteLine("✅ All services stopped\n");
    }
    
    private async Task StopProcessOnPortAsync(int port)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Get-NetTCPConnection -LocalPort {port} -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | ForEach-Object {{ Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            
            if (process != null)
            {
                await process.WaitForExitAsync();
                Console.WriteLine($"  ✓ Stopped process on port {port}");
            }
        }
        catch
        {
            // Process not found or already stopped
        }
    }
    
    public Process? StartService(ServiceConfig config)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = config.ProjectPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            if (config.IsFrontend)
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c npm start";
            }
            else
            {
                startInfo.FileName = "dotnet";
                startInfo.Arguments = "run";
            }
            
            var process = Process.Start(startInfo);
            
            if (process != null)
            {
                // Capture output asynchronously to prevent blocking
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (line is null) break; // End of stream
                        if (!string.IsNullOrEmpty(line))
                        {
                            Console.WriteLine($"[{config.Name}] {line}");
                        }
                    }
                });
                
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var line = await process.StandardError.ReadLineAsync();
                        if (line is null) break; // End of stream
                        if (!string.IsNullOrEmpty(line))
                        {
                            Console.WriteLine($"[{config.Name}] ERROR: {line}");
                        }
                    }
                });
            }
            
            Console.WriteLine($"  → Started {config.Name} (port {config.Port})");
            return process;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Failed to start {config.Name}: {ex.Message}");
            return null;
        }
    }
}

public class HealthChecker
{
    private readonly HttpClient _httpClient;
    
    public HealthChecker()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    }
    
    public async Task<bool> WaitForServiceHealthyAsync(ServiceConfig config, int maxWaitSeconds = 30)
    {
        Console.Write($"  ⏳ Waiting for {config.Name} to be ready");
        
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
        {
            if (await IsServiceHealthyAsync(config))
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($" ✓ ({elapsed:F1}s)");
                return true;
            }
            
            Console.Write(".");
            await Task.Delay(1000);
        }
        
        Console.WriteLine($" ❌ Timeout after {maxWaitSeconds}s");
        return false;
    }
    
    private async Task<bool> IsServiceHealthyAsync(ServiceConfig config)
    {
        try
        {
            if (config.IsFrontend)
            {
                // For frontend, just check if port responds
                var response = await _httpClient.GetAsync($"http://localhost:{config.Port}");
                return response.StatusCode == HttpStatusCode.OK;
            }
            else
            {
                // For backend services, check /health endpoint
                var response = await _httpClient.GetAsync(config.HealthEndpoint);
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }
}

public class StartupSequencer
{
    private readonly ProcessManager _processManager;
    private readonly HealthChecker _healthChecker;
    
    public StartupSequencer()
    {
        _processManager = new ProcessManager();
        _healthChecker = new HealthChecker();
    }
    
    public async Task StartAllServicesAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("    🚀 Service Orchestrator - START MODE");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        
        // Stop any existing services first
        await _processManager.StopAllServicesAsync();
        
        var services = GetServiceConfigs();
        
        for (int i = 0; i < services.Count; i++)
        {
            var service = services[i];
            Console.WriteLine($"[{i + 1}/{services.Count}] Starting {service.Name}...");
            
            var process = _processManager.StartService(service);
            
            if (process != null)
            {
                var healthy = await _healthChecker.WaitForServiceHealthyAsync(service);
                
                if (!healthy)
                {
                    Console.WriteLine($"  ⚠️  Service may not be fully ready\n");
                }
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  ✅ All services started!");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        Console.WriteLine("📊 Dashboard: http://localhost:3000");
        Console.WriteLine("🔧 API Docs:  http://localhost:5001/swagger\n");
    }
    
    public async Task StopAllServicesAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("    🛑 Service Orchestrator - STOP MODE");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        
        await _processManager.StopAllServicesAsync();
        
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  ✅ All services stopped!");
        Console.WriteLine("═══════════════════════════════════════════════\n");
    }
    
    private List<ServiceConfig> GetServiceConfigs()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        
        return new List<ServiceConfig>
        {
            new() { Name = "DataSourceManagement", ProjectPath = Path.Combine(baseDir, "src", "Services", "DataSourceManagementService"), Port = 5001, HealthEndpoint = "http://localhost:5001/health" },
            new() { Name = "MetricsConfiguration", ProjectPath = Path.Combine(baseDir, "src", "Services", "MetricsConfigurationService"), Port = 5002, HealthEndpoint = "http://localhost:5002/health" },
            new() { Name = "Validation", ProjectPath = Path.Combine(baseDir, "src", "Services", "ValidationService"), Port = 5003, HealthEndpoint = "http://localhost:5003/health" },
            new() { Name = "Scheduling", ProjectPath = Path.Combine(baseDir, "src", "Services", "SchedulingService"), Port = 5004, HealthEndpoint = "http://localhost:5004/health" },
            new() { Name = "FilesReceiver", ProjectPath = Path.Combine(baseDir, "src", "Services", "FilesReceiverService"), Port = 5005, HealthEndpoint = "http://localhost:5005/health" },
            new() { Name = "InvalidRecords", ProjectPath = Path.Combine(baseDir, "src", "Services", "InvalidRecordsService"), Port = 5006, HealthEndpoint = "http://localhost:5006/health" },
            new() { Name = "FileDiscovery", ProjectPath = Path.Combine(baseDir, "src", "Services", "FileDiscoveryService"), Port = 5007, HealthEndpoint = "http://localhost:5007/health" },
            new() { Name = "FileProcessor", ProjectPath = Path.Combine(baseDir, "src", "Services", "FileProcessorService"), Port = 5008, HealthEndpoint = "http://localhost:5008/health" },
            new() { Name = "Frontend", ProjectPath = Path.Combine(baseDir, "src", "Frontend"), Port = 3000, HealthEndpoint = "http://localhost:3000", IsFrontend = true }
        };
    }
}
