# Generate test files with valid schema (string IDs)
# For Valid Schema Test - Output throughput measurement

param(
    [int]$FileCount = 100,
    [int]$RecordsPerFile = 10,
    [string]$OutputPath = 'C:\Users\UserC\source\repos\EZ\test-data\ValidSchemaTest'
)

$categories = @('Electronics', 'Clothing', 'Food', 'Books', 'Toys')

Write-Host "═══════════════════════════════════════════════"
Write-Host "    Generating $FileCount valid schema test files"
Write-Host "    Records per file: $RecordsPerFile"
Write-Host "    Total records: $($FileCount * $RecordsPerFile)"
Write-Host "    Output: $OutputPath"
Write-Host "═══════════════════════════════════════════════"

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$startTime = Get-Date

for ($i = 1; $i -le $FileCount; $i++) {
    $fileName = "valid-test-$($i.ToString('D4')).csv"
    $filePath = Join-Path $OutputPath $fileName

    $sb = [System.Text.StringBuilder]::new()
    # Schema: id (string), name (string), value (number), category (string), timestamp (string)
    [void]$sb.AppendLine('id,name,value,category,timestamp')

    for ($j = 1; $j -le $RecordsPerFile; $j++) {
        $recordNum = ($i - 1) * $RecordsPerFile + $j
        # Use string ID format to match schema
        $id = "ID_$($recordNum.ToString('D6'))"
        $name = "Product_$($recordNum.ToString('D6'))"
        $value = [math]::Round((Get-Random -Minimum 10 -Maximum 1000) + (Get-Random -Minimum 0 -Maximum 100) / 100, 2)
        $category = $categories[(Get-Random -Minimum 0 -Maximum $categories.Count)]
        $timestamp = (Get-Date).AddMinutes(-($FileCount - $i)).ToString('yyyy-MM-ddTHH:mm:ssZ')

        [void]$sb.AppendLine("$id,$name,$value,$category,$timestamp")
    }

    [System.IO.File]::WriteAllText($filePath, $sb.ToString())

    if ($i % 25 -eq 0) {
        Write-Host "[$i / $FileCount] files generated"
    }
}

$totalTime = (Get-Date) - $startTime
$fileCount = (Get-ChildItem $OutputPath -Filter '*.csv').Count

Write-Host ""
Write-Host "═══════════════════════════════════════════════"
Write-Host "    Generation Complete!"
Write-Host "    Files created: $fileCount"
Write-Host "    Total time: $([math]::Round($totalTime.TotalSeconds, 1)) seconds"
Write-Host "═══════════════════════════════════════════════"

# Show sample record
Write-Host ""
Write-Host "Sample record (first file):"
Get-Content (Join-Path $OutputPath "valid-test-0001.csv") | Select-Object -First 3
