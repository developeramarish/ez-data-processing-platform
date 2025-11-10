# Check schema patterns in MongoDB
Write-Host "🔍 Checking schema patterns in MongoDB..." -ForegroundColor Cyan

# Get first schema from API
$response = Invoke-RestMethod -Uri "http://localhost:5010/api/v1/schema" -Method Get
$firstSchema = $response.items[0]

Write-Host "`n📋 Schema: $($firstSchema.name)" -ForegroundColor Yellow
Write-Host "Display Name: $($firstSchema.displayName)" -ForegroundColor Gray

# Parse JSON schema content
$jsonSchema = $firstSchema.jsonSchemaContent | ConvertFrom-Json

Write-Host "`n🔍 Checking for pattern properties..." -ForegroundColor Cyan

# Function to check patterns recursively
function Check-Patterns {
    param($obj, $path = "root")
    
    if ($obj -is [PSCustomObject]) {
        $obj.PSObject.Properties | ForEach-Object {
            $propName = $_.Name
            $propValue = $_.Value
            
            if ($propName -eq "pattern") {
                Write-Host "  ✓ Found pattern at $path" -ForegroundColor Green
                Write-Host "    Pattern: '$propValue'" -ForegroundColor White
                
                # Check if pattern looks reversed
                if ($propValue -match '^\$\{' -or $propValue -match '\}\$$' -or $propValue -match '\]\^' -or $propValue -match '^\$') {
                    Write-Host "    ⚠️  WARNING: Pattern looks REVERSED!" -ForegroundColor Red
                } else {
                    Write-Host "    ✅ Pattern looks correct (LTR)" -ForegroundColor Green
                }
            }
            
            if ($propValue -is [PSCustomObject] -or $propValue -is [Array]) {
                Check-Patterns -obj $propValue -path "$path.$propName"
            }
        }
    } elseif ($obj -is [Array]) {
        for ($i = 0; $i -lt $obj.Count; $i++) {
            Check-Patterns -obj $obj[$i] -path "$path[$i]"
        }
    }
}

Check-Patterns -obj $jsonSchema

Write-Host "`n✅ Pattern check complete!" -ForegroundColor Green
