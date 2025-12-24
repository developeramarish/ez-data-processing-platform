$basePath = 'C:\Users\UserC\source\repos\EZ\test-data\LoadTest-100'
$transactionTypes = @('Deposit', 'Withdrawal', 'Transfer', 'Payment', 'Refund')
$statuses = @('Completed', 'Pending', 'Processing', 'Failed')
$currencies = @('USD', 'EUR', 'GBP', 'ILS')

Write-Host "Generating 100 test files in $basePath..."

for ($i = 1; $i -le 100; $i++) {
    $fileName = "load-test-file-$($i.ToString('D3')).csv"
    $filePath = Join-Path $basePath $fileName

    $lines = @()
    $lines += 'TransactionId,CustomerId,CustomerName,TransactionDate,Amount,Currency,TransactionType,Status,Description'

    # Generate 50-100 records per file
    $recordCount = Get-Random -Minimum 50 -Maximum 101

    for ($j = 1; $j -le $recordCount; $j++) {
        $txnId = "TXN-LT$i-$($j.ToString('D5'))"
        $custId = "CUST-$(Get-Random -Minimum 1000 -Maximum 9999)"
        $custName = "Customer_$($i)_$($j)"
        $date = (Get-Date).AddDays(-(Get-Random -Minimum 1 -Maximum 30)).ToString('yyyy-MM-dd HH:mm:ss')
        $amount = [math]::Round((Get-Random -Minimum 100 -Maximum 10000) + (Get-Random -Minimum 0 -Maximum 100) / 100, 2)
        $currency = $currencies | Get-Random
        $txnType = $transactionTypes | Get-Random
        $status = $statuses | Get-Random
        $desc = "Load test transaction $i-$j for stress testing"

        $lines += "$txnId,$custId,$custName,$date,$amount,$currency,$txnType,$status,$desc"
    }

    $lines | Out-File -FilePath $filePath -Encoding UTF8

    if ($i % 10 -eq 0) {
        Write-Host "Generated $i files..."
    }
}

$fileCount = (Get-ChildItem $basePath -Filter '*.csv').Count
Write-Host "Completed! Generated $fileCount test files"

# Calculate total records
$totalRecords = 0
Get-ChildItem $basePath -Filter '*.csv' | ForEach-Object {
    $totalRecords += (Get-Content $_.FullName | Measure-Object -Line).Lines - 1
}
Write-Host "Total records across all files: $totalRecords"
