# Create ValidSchemaTest datasource
param(
    [string]$BaseUrl = 'http://localhost:5001/api/v1/DataSource'
)

$schema = @{
    type = 'object'
    properties = @{
        id = @{ type = 'string' }
        name = @{ type = 'string' }
        value = @{ type = 'number' }
        category = @{ type = 'string' }
        timestamp = @{ type = 'string' }
    }
    required = @('id', 'name', 'value')
}

$body = @{
    Name = 'ValidSchemaTest'
    SupplierName = 'Schema Validation Test'
    Category = 'Testing'
    Description = 'Test with valid schema - all records should pass validation'
    ConnectionString = '/mnt/external-test-data/ValidSchemaTest'
    FilePath = '/mnt/external-test-data/ValidSchemaTest'
    FilePattern = '*.csv'
    IsActive = $true
    JsonSchema = $schema
    Output = @{
        Destinations = @(
            @{
                Id = 'valid-schema-out'
                Name = 'Valid Schema Output'
                Type = 'folder'
                Enabled = $true
                OutputFormat = 'json'
                FolderConfig = @{
                    Path = '/mnt/external-test-data/output/ValidSchemaTest'
                    FileNamePattern = '{filename}-output.json'
                    CreateSubfolders = $false
                    OverwriteExisting = $true
                }
            }
        )
        IncludeInvalidRecords = $false
        DefaultOutputFormat = 'original'
    }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri $BaseUrl -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop
    Write-Host "[OK] Datasource created: $($response.name)"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 409) {
        Write-Host "[SKIP] Datasource already exists"
    } else {
        Write-Host "[ERROR] $_"
    }
}
