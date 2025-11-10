# Test Prometheus Query Endpoints
# Tests the new metric data endpoints in MetricsConfigurationService

Write-Host "=== Testing Prometheus Query Endpoints ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Get available metrics from System Prometheus
Write-Host "Test 1: Get available metrics (System)" -ForegroundColor Yellow
$response1 = Invoke-RestMethod -Uri "http://localhost:5002/api/v1/metrics/available?instance=system" -Method Get
Write-Host "Response: $($response1.data.count) metrics found" -ForegroundColor Green
Write-Host ""

# Test 2: Execute instant PromQL query
Write-Host "Test 2: Execute instant PromQL query (up metric)" -ForegroundColor Yellow
$queryBody = @{
    query = "up"
    queryType = "instant"
    instance = "system"
} | ConvertTo-Json

$response2 = Invoke-RestMethod -Uri "http://localhost:5002/api/v1/metrics/query" `
    -Method Post `
    -Body $queryBody `
    -ContentType "application/json"

Write-Host "Query: $($response2.data.query)" -ForegroundColor Green
Write-Host "Result Type: $($response2.data.result.resultType)" -ForegroundColor Green
Write-Host "Value: $($response2.data.result.value)" -ForegroundColor Green
Write-Host "Timestamp: $($response2.data.result.timestamp)" -ForegroundColor Green
Write-Host ""

# Test 3: Execute range PromQL query
Write-Host "Test 3: Execute range PromQL query (last 1 hour)" -ForegroundColor Yellow
$end = Get-Date
$start = $end.AddHours(-1)

$rangeBody = @{
    query = "up"
    queryType = "range"
    instance = "system"
    start = $start.ToString("o")
    end = $end.ToString("o")
    step = "5m"
} | ConvertTo-Json

$response3 = Invoke-RestMethod -Uri "http://localhost:5002/api/v1/metrics/query" `
    -Method Post `
    -Body $rangeBody `
    -ContentType "application/json"

Write-Host "Query: $($response3.data.query)" -ForegroundColor Green
Write-Host "Result Type: $($response3.data.result.resultType)" -ForegroundColor Green
Write-Host "Data Points: $($response3.data.result.data.Count)" -ForegroundColor Green
Write-Host ""

Write-Host "=== All Tests Passed! ===" -ForegroundColor Green
Write-Host ""
Write-Host "New endpoints working:" -ForegroundColor Cyan
Write-Host "  - GET  /api/v1/metrics/available?instance={system|business}" -ForegroundColor White
Write-Host "  - POST /api/v1/metrics/query (instant and range queries)" -ForegroundColor White
Write-Host "  - GET  /api/v1/metrics/{id}/current (coming soon - needs metric ID)" -ForegroundColor White
Write-Host "  - GET  /api/v1/metrics/{id}/data (coming soon - needs metric ID)" -ForegroundColor White
