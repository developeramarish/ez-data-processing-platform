# Create 10 datasources for 10,000-file stress test (GAP-2)
# Each datasource points to a batch subdirectory with 1000 files

param(
    [int]$BatchCount = 10,
    [string]$BaseUrl = 'http://localhost:5001/api/v1/DataSource'
)

Write-Host "═══════════════════════════════════════════════"
Write-Host "    Creating $BatchCount datasources for 10K stress test"
Write-Host "═══════════════════════════════════════════════"

$schema = @{
    type = "object"
    properties = @{
        id = @{ type = "string" }
        name = @{ type = "string" }
        value = @{ type = "number" }
        category = @{ type = "string" }
        timestamp = @{ type = "string" }
    }
    required = @("id", "name", "value")
}

$created = 0
$failed = 0

for ($i = 0; $i -lt $BatchCount; $i++) {
    $name = "LoadTest-10K-Batch-$i"
    $filePath = "/mnt/external-test-data/LoadTest-10000/batch-$i"
    $outputPath = "/mnt/external-test-data/output/LoadTest-10000/batch-$i"

    $body = @{
        Name = $name
        SupplierName = "Stress Test 10K"
        Category = "Operations"
        Description = "10K stress test - batch $i (1000 files, 5000 records)"
        ConnectionString = $filePath
        FilePath = $filePath
        FilePattern = "*.csv"
        IsActive = $true
        JsonSchema = $schema
        Output = @{
            Destinations = @(
                @{
                    Id = "lt10k-batch$i-out"
                    Name = "LoadTest 10K Batch $i Output"
                    Type = "folder"
                    Enabled = $true
                    OutputFormat = "json"
                    FolderConfig = @{
                        Path = $outputPath
                        FileNamePattern = "{filename}-output.json"
                        CreateSubfolders = $false
                        OverwriteExisting = $true
                    }
                }
            )
            IncludeInvalidRecords = $false
            DefaultOutputFormat = "original"
        }
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri $BaseUrl -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop
        Write-Host "[OK] Created datasource: $name"
        $created++
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 409) {
            Write-Host "[SKIP] Datasource already exists: $name"
        } else {
            Write-Host "[ERROR] Failed to create $name : $_"
            $failed++
        }
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════"
Write-Host "    Results: $created created, $failed failed"
Write-Host "═══════════════════════════════════════════════"
