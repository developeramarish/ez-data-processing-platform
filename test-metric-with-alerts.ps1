# Test creating a metric with alert rules
$uri = "http://localhost:5002/api/v1/metrics"

$body = @{
    name = "test_metric_with_alerts"
    displayName = "Test Metric With Alerts"
    description = "Testing alert persistence"
    category = "business"
    scope = "global"
    dataSourceId = $null
    dataSourceName = $null
    formula = "sum"
    fieldPath = "$.amount"
    prometheusType = "gauge"
    labelNames = "status,region"
    labelsExpression = '{status="$status", region="$region"}'
    labels = @("status", "region")
    alertRules = @(
        @{
            name = "high_amount_alert"
            description = "Alert for high amount"
            expression = "test_metric_with_alerts > 1000"
            for = "5m"
            severity = "warning"
            isEnabled = $true
            labels = @{
                team = "finance"
            }
            annotations = @{
                summary = "Amount is too high"
                description = "The metric value exceeded 1000"
            }
        }
    )
    retention = "30d"
    status = 1
    createdBy = "TestScript"
} | ConvertTo-Json -Depth 10

Write-Host "Creating metric with alert rules..." -ForegroundColor Cyan
Write-Host "Payload:" -ForegroundColor Yellow
Write-Host $body -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $body -ContentType "application/json"
    Write-Host "`nSuccess! Metric created:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    
    # Get the created metric ID
    $metricId = $response.data.id
    Write-Host "`nMetric ID: $metricId" -ForegroundColor Cyan
    
    # Retrieve the metric to verify alert rules persisted
    Write-Host "`nRetrieving metric to verify alert rules..." -ForegroundColor Cyan
    $getResponse = Invoke-RestMethod -Uri "$uri/$metricId" -Method Get
    
    Write-Host "`nRetrieved metric:" -ForegroundColor Green
    Write-Host ($getResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    
    if ($getResponse.data.alertRules -and $getResponse.data.alertRules.Count -gt 0) {
        Write-Host "`n✅ SUCCESS! Alert rules were persisted correctly!" -ForegroundColor Green
        Write-Host "   Found $($getResponse.data.alertRules.Count) alert rule(s)" -ForegroundColor Green
    } else {
        Write-Host "`n❌ FAILURE! Alert rules were NOT persisted!" -ForegroundColor Red
    }
} catch {
    Write-Host "`nError:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
