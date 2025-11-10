# EZ Data Processing Platform - Monitoring Stack Startup Script
# Starts OpenTelemetry Collector, Prometheus (dual), Elasticsearch, Jaeger, and Grafana

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "EZ Data Processing Platform" -ForegroundColor Cyan
Write-Host "Monitoring Stack Startup" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Navigate to project root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host ""
Write-Host "Starting monitoring infrastructure..." -ForegroundColor Yellow
Write-Host ""

# Start only monitoring services
$monitoringServices = @(
    "otel-collector",
    "prometheus-system",
    "prometheus-business",
    "elasticsearch",
    "jaeger",
    "grafana"
)

foreach ($service in $monitoringServices) {
    Write-Host "Starting $service..." -ForegroundColor Cyan
    docker-compose -f docker-compose.development.yml up -d $service
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ $service started successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to start $service" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Monitoring Stack Status" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Check service health
Write-Host ""
Write-Host "Service Status:" -ForegroundColor Yellow
docker-compose -f docker-compose.development.yml ps

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Access URLs" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OpenTelemetry Collector:" -ForegroundColor Yellow
Write-Host "  - OTLP gRPC: http://localhost:4317" -ForegroundColor White
Write-Host "  - OTLP HTTP: http://localhost:4318" -ForegroundColor White
Write-Host "  - Metrics:   http://localhost:8888/metrics" -ForegroundColor White
Write-Host "  - Health:    http://localhost:13133" -ForegroundColor White
Write-Host ""
Write-Host "System Prometheus (Infrastructure):" -ForegroundColor Yellow
Write-Host "  - UI: http://localhost:9090" -ForegroundColor White
Write-Host ""
Write-Host "Business Prometheus (KPIs):" -ForegroundColor Yellow
Write-Host "  - UI: http://localhost:9091" -ForegroundColor White
Write-Host ""
Write-Host "Elasticsearch:" -ForegroundColor Yellow
Write-Host "  - API: http://localhost:9200" -ForegroundColor White
Write-Host ""
Write-Host "Jaeger (Tracing):" -ForegroundColor Yellow
Write-Host "  - UI: http://localhost:16686" -ForegroundColor White
Write-Host ""
Write-Host "Grafana (Unified Query Layer):" -ForegroundColor Yellow
Write-Host "  - UI: http://localhost:3001" -ForegroundColor White
Write-Host "  - Login: admin / admin" -ForegroundColor White
Write-Host ""

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Configure services to send telemetry to OTel Collector (localhost:4317)" -ForegroundColor White
Write-Host "2. Access Grafana at http://localhost:3001 (admin/admin)" -ForegroundColor White
Write-Host "3. System metrics will appear in System Prometheus" -ForegroundColor White
Write-Host "4. Business metrics (business_* prefix) will appear in Business Prometheus" -ForegroundColor White
Write-Host "5. All queries should go through Grafana API" -ForegroundColor White
Write-Host ""
Write-Host "To stop the stack: docker-compose -f docker-compose.development.yml down" -ForegroundColor Yellow
Write-Host ""
Write-Host "✓ Monitoring stack is ready!" -ForegroundColor Green
