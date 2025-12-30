# Export all EZ Platform Docker images as .tar files for offline installation
# Run from repository root

$ErrorActionPreference = "Stop"
$OUTPUT_DIR = "release-package/images"

Write-Host "Exporting EZ Platform Docker Images" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Output Directory: $OUTPUT_DIR" -ForegroundColor Gray
Write-Host ""

# Create output directory
if (!(Test-Path $OUTPUT_DIR)) {
    New-Item -ItemType Directory -Path $OUTPUT_DIR | Out-Null
    Write-Host "Created output directory: $OUTPUT_DIR" -ForegroundColor Green
}

# Application images
$appImages = @(
    @{Name="datasource-management"; Tag="v0.1.0-beta"},
    @{Name="filediscovery"; Tag="v0.1.0-beta"},
    @{Name="fileprocessor"; Tag="v0.1.0-beta"},
    @{Name="validation"; Tag="v0.1.0-beta"},
    @{Name="output"; Tag="v0.1.0-beta"},
    @{Name="invalidrecords"; Tag="v0.1.0-beta"},
    @{Name="scheduling"; Tag="v0.1.0-beta"},
    @{Name="metrics-configuration"; Tag="v0.1.0-beta"},
    @{Name="frontend"; Tag="v0.1.0-beta"},
    @{Name="ezplatform-docs"; Tag="v0.1.0-beta"}
)

# Infrastructure images
$infraImages = @(
    @{Name="mongo"; Tag="8.0"},
    @{Name="rabbitmq"; Tag="3-management-alpine"},
    @{Name="confluentinc/cp-kafka"; Tag="7.5.0"},
    @{Name="confluentinc/cp-zookeeper"; Tag="7.5.0"},
    @{Name="hazelcast/hazelcast"; Tag="5.6"},
    @{Name="docker.elastic.co/elasticsearch/elasticsearch"; Tag="8.17.0"},
    @{Name="prom/prometheus"; Tag="latest"},
    @{Name="grafana/grafana"; Tag="latest"},
    @{Name="jaegertracing/all-in-one"; Tag="latest"},
    @{Name="otel/opentelemetry-collector-contrib"; Tag="latest"},
    @{Name="fluent/fluent-bit"; Tag="latest"}
)

$exportCount = 0
$totalImages = $appImages.Count + $infraImages.Count

Write-Host "Exporting Application Images..." -ForegroundColor Yellow
Write-Host ""

foreach ($img in $appImages) {
    $exportCount++
    $imageFull = "$($img.Name):$($img.Tag)"
    $fileName = "$($img.Name)-$($img.Tag).tar" -replace "/", "-"
    $outputPath = "$OUTPUT_DIR/$fileName"

    Write-Host "[$exportCount/$totalImages] Exporting $imageFull..." -ForegroundColor Gray

    docker save -o $outputPath $imageFull

    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $outputPath).Length / 1MB
        Write-Host "  SUCCESS: $fileName ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "  FAILED: $imageFull" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Exporting Infrastructure Images..." -ForegroundColor Yellow
Write-Host ""

foreach ($img in $infraImages) {
    $exportCount++
    $imageFull = "$($img.Name):$($img.Tag)"
    $fileName = "$($img.Name)-$($img.Tag).tar" -replace "/", "-" -replace ":", "-"
    $outputPath = "$OUTPUT_DIR/$fileName"

    Write-Host "[$exportCount/$totalImages] Exporting $imageFull..." -ForegroundColor Gray

    docker save -o $outputPath $imageFull

    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $outputPath).Length / 1MB
        Write-Host "  SUCCESS: $fileName ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "  FAILED: $imageFull" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Export Complete!" -ForegroundColor Green
Write-Host "Total Images Exported: $exportCount" -ForegroundColor Cyan
Write-Host "Output Directory: $OUTPUT_DIR" -ForegroundColor Cyan

# Calculate total size
$totalSize = (Get-ChildItem $OUTPUT_DIR -File | Measure-Object -Property Length -Sum).Sum / 1GB
Write-Host "Total Package Size: $([math]::Round($totalSize, 2)) GB" -ForegroundColor Cyan
