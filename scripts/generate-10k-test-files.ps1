# Generate 10,000 test files for stress testing (GAP-2)
# Session 34: Extended Load Testing
# Target: 10,000 files x 5 records = 50,000 total records

param(
    [int]$FileCount = 10000,
    [int]$RecordsPerFile = 5,
    [string]$OutputPath = 'C:\Users\UserC\source\repos\EZ\test-data\LoadTest-10000'
)

$categories = @('Electronics', 'Clothing', 'Food', 'Books', 'Toys')

Write-Host "═══════════════════════════════════════════════"
Write-Host "    Generating $FileCount test files"
Write-Host "    Records per file: $RecordsPerFile"
Write-Host "    Total records: $($FileCount * $RecordsPerFile)"
Write-Host "    Output: $OutputPath"
Write-Host "═══════════════════════════════════════════════"

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$startTime = Get-Date
$batchSize = 1000

for ($i = 1; $i -le $FileCount; $i++) {
    $fileName = "load-test-$($i.ToString('D5')).csv"
    $filePath = Join-Path $OutputPath $fileName

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('id,name,value,category,timestamp')

    for ($j = 1; $j -le $RecordsPerFile; $j++) {
        $id = ($i - 1) * $RecordsPerFile + $j
        $name = "Item_$($id.ToString('D7'))"
        $value = [math]::Round((Get-Random -Minimum 10 -Maximum 1000) + (Get-Random -Minimum 0 -Maximum 100) / 100, 2)
        $category = $categories[(Get-Random -Minimum 0 -Maximum $categories.Count)]
        $timestamp = (Get-Date).AddMinutes(-($FileCount - $i)).ToString('yyyy-MM-ddTHH:mm:ssZ')

        [void]$sb.AppendLine("$id,$name,$value,$category,$timestamp")
    }

    [System.IO.File]::WriteAllText($filePath, $sb.ToString())

    if ($i % $batchSize -eq 0) {
        $elapsed = (Get-Date) - $startTime
        $rate = [math]::Round($i / $elapsed.TotalSeconds, 1)
        $remaining = [math]::Round(($FileCount - $i) / $rate, 0)
        Write-Host "[$i / $FileCount] files generated (${rate}/s, ~${remaining}s remaining)"
    }
}

$totalTime = (Get-Date) - $startTime
$fileCount = (Get-ChildItem $OutputPath -Filter '*.csv').Count

Write-Host ""
Write-Host "═══════════════════════════════════════════════"
Write-Host "    Generation Complete!"
Write-Host "    Files created: $fileCount"
Write-Host "    Total time: $([math]::Round($totalTime.TotalSeconds, 1)) seconds"
Write-Host "    Rate: $([math]::Round($fileCount / $totalTime.TotalSeconds, 1)) files/second"
Write-Host "═══════════════════════════════════════════════"

# Calculate total size
$totalSize = (Get-ChildItem $OutputPath -Filter '*.csv' | Measure-Object -Sum Length).Sum
Write-Host "Total size: $([math]::Round($totalSize / 1MB, 2)) MB"
