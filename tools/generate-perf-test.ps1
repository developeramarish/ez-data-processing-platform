param([int]$RecordCount = 10000, [string]$OutputName = "perf-test")

$timestamp = Get-Date -Format "HHmmss"
$outputFile = "C:\Users\UserC\source\repos\EZ\test-data\E2E-004\$OutputName-$timestamp.csv"

$names = @('Alice Williams', 'Bob Johnson', 'Charlie Brown', 'Diana Prince', 'Eve Anderson', 'Frank Miller', 'Grace Lee', 'Henry Wilson', 'John Smith', 'Jane Doe')
$statuses = @('Approved', 'Pending', 'Rejected')
$categories = @('Retail', 'Wholesale', 'Services')
$methods = @('Credit Card', 'Bank Transfer', 'Cash')

$random = New-Object System.Random
$sw = New-Object System.IO.StreamWriter($outputFile, $false, [System.Text.Encoding]::UTF8)

try {
    # Write header
    $sw.WriteLine('TransactionId,CustomerName,Amount,Date,Status,Category,PaymentMethod')

    # Write records with progress
    $progressInterval = [math]::Max(1, $RecordCount / 100)
    for ($i = 1; $i -le $RecordCount; $i++) {
        $txn = "TXN-$timestamp" + $i.ToString('D8')
        $name = $names[$random.Next($names.Length)]
        $amount = [math]::Round($random.NextDouble() * 1000 + 10, 2)
        $status = $statuses[$random.Next($statuses.Length)]
        $category = $categories[$random.Next($categories.Length)]
        $method = $methods[$random.Next($methods.Length)]

        $sw.WriteLine("$txn,$name,$amount,2025-12-15,$status,$category,$method")

        if ($i % $progressInterval -eq 0) {
            $pct = [math]::Round(($i / $RecordCount) * 100)
            Write-Host "`rGenerating: $pct% ($i / $RecordCount)" -NoNewline
        }
    }
    Write-Host ""
}
finally {
    $sw.Close()
}

Write-Host "Created $RecordCount record test file: $outputFile"
