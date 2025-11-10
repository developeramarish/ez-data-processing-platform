# Test updating an existing metric to add alert rules
$uri = "http://localhost:5002/api/v1/metrics"

# First, get the first metric from the list
Write-Host "Fetching existing metrics..." -ForegroundColor Cyan
$metricsResponse = Invoke-RestMethod -Uri $uri -Method Get
$firstMetric = $metricsResponse.data[0]

if (-not $firstMetric) {
    Write-Host "No metrics found to update!" -ForegroundColor Red
    exit 1
}

Write-Host "`nFirst metric found:" -ForegroundColor Green
Write-Host "  ID: $($firstMetric.id)" -ForegroundColor Gray
Write-Host "  Name: $($firstMetric.name)" -ForegroundColor Gray
Write-Host "  DisplayName: $($firstMetric.displayName)" -ForegroundColor Gray
Write-Host "  Current Alert Rules: $($firstMetric.alertRules.Count)" -ForegroundColor Gray

# Prepare update payload with a new alert rule
$updateBody = @{
    displayName = $firstMetric.displayName
    description = $firstMetric.description
    category = $firstMetric.category
    scope = $firstMetric.scope
    dataSourceId = $firstMetric.dataSourceId
    dataSourceName = $firstMetric.dataSourceName
    formula = $firstMetric.formula
    fieldPath = $firstMetric.fieldPath
    prometheusType = $firstMetric.prometheusType
    labelNames = $firstMetric.labelNames
    labelsExpression = $firstMetric.labelsExpression
    labels = $firstMetric.labels
    alertRules = @(
        @{
            name = "test_update_alert"
            description = "Alert added via update test"
            expression = "$($firstMetric.name) > 500"
            for = "3m"
            severity = "critical"
            isEnabled = $true
            labels = @{
                test = "update"
            }
            annotations = @{
                summary = "Test update alert"
                description = "This alert was added via update operation"
            }
        }
    )
    retention = $firstMetric.retention
    status = $firstMetric.status
    updatedBy = "UpdateTestScript"
} | ConvertTo-Json -Depth 10

Write-Host "`nUpdating metric with alert rule..." -ForegroundColor Cyan
Write-Host "Payload:" -ForegroundColor Yellow
Write-Host $updateBody -ForegroundColor Gray

try {
    $updateResponse = Invoke-RestMethod -Uri "$uri/$($firstMetric.id)" -Method Put -Body $updateBody -ContentType "application/json"
    Write-Host "`nUpdate Success!" -ForegroundColor Green
    Write-Host ($updateResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    
    # Retrieve the metric to verify alert rules persisted
    Write-Host "`nRetrieving updated metric to verify alert rules..." -ForegroundColor Cyan
    $getResponse = Invoke-RestMethod -Uri "$uri/$($firstMetric.id)" -Method Get
    
    Write-Host "`nRetrieved metric:" -ForegroundColor Green
    Write-Host ($getResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    
    if ($getResponse.data.alertRules -and $getResponse.data.alertRules.Count -gt 0) {
        Write-Host "`n✅ SUCCESS! Alert rules were persisted correctly after UPDATE!" -ForegroundColor Green
        Write-Host "   Found $($getResponse.data.alertRules.Count) alert rule(s)" -ForegroundColor Green
    } else {
        Write-Host "`n❌ FAILURE! Alert rules were NOT persisted after UPDATE!" -ForegroundColor Red
    }
} catch {
    Write-Host "`nError:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
