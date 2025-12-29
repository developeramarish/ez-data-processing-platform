# Split 10,000 files into 10 subdirectories of 1000 each
# Workaround for 9p mount limitation with large directories

$basePath = 'C:\Users\UserC\source\repos\EZ\test-data\LoadTest-10000'
$files = Get-ChildItem $basePath -Filter '*.csv' | Sort-Object Name
$batchSize = 1000

Write-Host "Splitting $($files.Count) files into subdirectories..."

for ($batch = 0; $batch -lt 10; $batch++) {
    $subDir = Join-Path $basePath "batch-$batch"
    if (!(Test-Path $subDir)) {
        New-Item -ItemType Directory -Path $subDir | Out-Null
    }

    $start = $batch * $batchSize
    $end = [Math]::Min($start + $batchSize - 1, $files.Count - 1)

    for ($i = $start; $i -le $end; $i++) {
        Move-Item -Path $files[$i].FullName -Destination $subDir -Force
    }
    Write-Host "Batch $batch : Moved $($end - $start + 1) files"
}

Write-Host "Done! Files organized into 10 subdirectories"
