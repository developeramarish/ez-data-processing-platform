# Generate 20 test files with valid schema (string IDs) for ValidSchemaTest3
$categories = @('Electronics', 'Clothing', 'Food', 'Books', 'Toys')
$OutputPath = 'C:\Users\UserC\source\repos\EZ\test-data\ValidSchemaTest3'
$FileCount = 20
$RecordsPerFile = 10

Write-Host "Generating $FileCount files in ValidSchemaTest3"

if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

for ($i = 1; $i -le $FileCount; $i++) {
    $fileName = "valid3-test-$($i.ToString('D4')).csv"
    $filePath = Join-Path $OutputPath $fileName

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('id,name,value,category,timestamp')

    for ($j = 1; $j -le $RecordsPerFile; $j++) {
        $recordNum = ($i - 1) * $RecordsPerFile + $j
        $id = "ID_$($recordNum.ToString('D6'))"
        $name = "Product_$($recordNum.ToString('D6'))"
        $value = [math]::Round((Get-Random -Minimum 10 -Maximum 1000) + (Get-Random -Minimum 0 -Maximum 100) / 100, 2)
        $category = $categories[(Get-Random -Minimum 0 -Maximum $categories.Count)]
        $timestamp = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssZ')

        [void]$sb.AppendLine("$id,$name,$value,$category,$timestamp")
    }

    [System.IO.File]::WriteAllText($filePath, $sb.ToString())
}

Write-Host "Generated $FileCount files with $RecordsPerFile records each"
Write-Host "Total records: $($FileCount * $RecordsPerFile)"
Get-Content (Join-Path $OutputPath "valid3-test-0001.csv") | Select-Object -First 3
