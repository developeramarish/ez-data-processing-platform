$header = 'TransactionId,CustomerName,Amount,Date,Status,Category,PaymentMethod'
$names = @('Alice Williams', 'Bob Johnson', 'Charlie Brown', 'Diana Prince', 'Eve Anderson', 'Frank Miller', 'Grace Lee', 'Henry Wilson', 'John Smith', 'Jane Doe')
$statuses = @('Approved', 'Pending', 'Rejected')
$categories = @('Retail', 'Wholesale', 'Services')
$methods = @('Credit Card', 'Bank Transfer', 'Cash')

$lines = @($header)
$random = New-Object System.Random
for ($i = 1; $i -le 10000; $i++) {
    $txn = 'TXN-' + $i.ToString('D8')
    $name = $names[$random.Next($names.Length)]
    $amount = [math]::Round($random.NextDouble() * 1000 + 10, 2)
    $date = '2025-12-15'
    $status = $statuses[$random.Next($statuses.Length)]
    $category = $categories[$random.Next($categories.Length)]
    $method = $methods[$random.Next($methods.Length)]
    $lines += "$txn,$name,$amount,$date,$status,$category,$method"
}
$lines -join "`n" | Out-File -FilePath 'C:\Users\UserC\source\repos\EZ\test-data\E2E-004\corvus-load-test-10k.csv' -Encoding UTF8
Write-Host "Created 10,000 record load test file"
(Get-Content 'C:\Users\UserC\source\repos\EZ\test-data\E2E-004\corvus-load-test-10k.csv' | Measure-Object -Line).Lines
