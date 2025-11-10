# EZ Platform - Start All Services Script
# Starts all .NET backend services in separate PowerShell windows

Write-Host "=== EZ Platform Service Startup ===" -ForegroundColor Cyan
Write-Host ""

# Define all services
$services = @(
    @{Name = "Frontend (React)"; Path = "src\Frontend"; Port = "3000"; Command = "npm start"},
    @{Name = "DataSourceManagementService"; Path = "src\Services\DataSourceManagementService"; Port = "5000"; Command = "dotnet run"},
    @{Name = "MetricsConfigurationService"; Path = "src\Services\MetricsConfigurationService"; Port = "5002"; Command = "dotnet run"},
    @{Name = "ValidationService"; Path = "src\Services\ValidationService"; Port = "5003"; Command = "dotnet run"},
    @{Name = "InvalidRecordsService"; Path = "src\Services\InvalidRecordsService"; Port = "5004"; Command = "dotnet run"},
    @{Name = "SchedulingService"; Path = "src\Services\SchedulingService"; Port = "5005"; Command = "dotnet run"},
    @{Name = "FilesReceiverService"; Path = "src\Services\FilesReceiverService"; Port = "5006"; Command = "dotnet run"},
    @{Name = "DataSourceChatService"; Path = "src\Services\DataSourceChatService"; Port = "5007"; Command = "dotnet run"}
)

Write-Host "Starting $($services.Count) backend services..." -ForegroundColor Cyan
Write-Host ""

# Start each service in a new window
foreach ($service in $services) {
    Write-Host "Starting $($service.Name) on port $($service.Port)..." -ForegroundColor Green
    
    $cmd = "Set-Location '$($service.Path)'; $($service.Command)"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $cmd
    
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "All services started in separate windows!" -ForegroundColor Green
Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "  - $($service.Name): http://localhost:$($service.Port)" -ForegroundColor White
}
Write-Host ""
Write-Host "Use stop-all-services.ps1 to stop all services" -ForegroundColor Gray
