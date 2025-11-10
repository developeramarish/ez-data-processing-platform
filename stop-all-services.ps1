# EZ Platform - Stop All Services Script
# Stops all .NET backend service processes

Write-Host "=== Stopping All EZ Platform Services ===" -ForegroundColor Red
Write-Host ""

# Service executable names
$serviceProcesses = @(
    "DataProcessing.DataSource",
    "MetricsConfigurationService",
    "DataProcessing.Validation",
    "DataProcessing.InvalidRecords",
    "DataProcessing.Scheduling",
    "DataProcessing.FilesReceiver",
    "DataProcessing.Chat"
)

$stopped = 0

foreach ($processName in $serviceProcesses) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            try {
                Stop-Process -Id $proc.Id -Force
                Write-Host "✓ Stopped $processName (PID: $($proc.Id))" -ForegroundColor Green
                $stopped++
            } catch {
                Write-Host "✗ Failed to stop $processName (PID: $($proc.Id))" -ForegroundColor Red
            }
        }
    }
}

Write-Host ""
if ($stopped -gt 0) {
    Write-Host "Stopped $stopped service process(es)" -ForegroundColor Green
} else {
    Write-Host "No running services found" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "All PowerShell windows with services will close automatically." -ForegroundColor Gray
