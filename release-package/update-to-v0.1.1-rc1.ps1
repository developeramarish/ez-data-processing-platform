# EZ Platform - Update Release Package to v0.1.1-rc1
# This script updates all configuration files, YAML manifests, and documentation
# for the v0.1.1-rc1 release

param(
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host "EZ Platform Release Package Updater"
Write-Host "Version: v0.1.0-beta → v0.1.1-rc1"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$updates = @()

function Update-FileContent {
    param(
        [string]$FilePath,
        [string]$OldText,
        [string]$NewText,
        [string]$Description
    )

    if (Test-Path $FilePath) {
        $content = Get-Content $FilePath -Raw
        if ($content -match [regex]::Escape($OldText)) {
            if (-not $WhatIf) {
                $content = $content -replace [regex]::Escape($OldText), $NewText
                Set-Content -Path $FilePath -Value $content -NoNewline
                Write-Host "[✓] Updated: $Description" -ForegroundColor Green
                Write-Host "    File: $FilePath" -ForegroundColor Gray
            } else {
                Write-Host "[WHATIF] Would update: $Description" -ForegroundColor Yellow
                Write-Host "    File: $FilePath" -ForegroundColor Gray
            }
            $script:updates += $FilePath
        } else {
            Write-Host "[SKIP] No match found: $Description" -ForegroundColor DarkGray
            Write-Host "    File: $FilePath" -ForegroundColor Gray
        }
    } else {
        Write-Host "[WARN] File not found: $FilePath" -ForegroundColor Yellow
    }
}

Write-Host "Phase 1: Root Files" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Update README.md
Update-FileContent `
    -FilePath "README.md" `
    -OldText "# EZ Platform v0.1.0-beta - Installation Package" `
    -NewText "# EZ Platform v0.1.1-rc1 - Installation Package" `
    -Description "README.md - Title"

Update-FileContent `
    -FilePath "README.md" `
    -OldText "**Version:** v0.1.0-beta" `
    -NewText "**Version:** v0.1.1-rc1" `
    -Description "README.md - Version"

Update-FileContent `
    -FilePath "README.md" `
    -OldText "**Release Date:** December 29, 2025" `
    -NewText "**Release Date:** January 1, 2026" `
    -Description "README.md - Release Date"

Update-FileContent `
    -FilePath "README.md" `
    -OldText "- ✅ **21 Docker Images** (3.96GB)" `
    -NewText "- ✅ **21 Docker Images** (4.1GB)" `
    -Description "README.md - Package Size"

# Update IMAGE-MANIFEST.txt
Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "# EZ Platform v0.1.0-beta - Complete Image Manifest" `
    -NewText "# EZ Platform v0.1.1-rc1 - Complete Image Manifest" `
    -Description "IMAGE-MANIFEST.txt - Title"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "datasource-management:v0.1.0-beta" `
    -NewText "datasource-management:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - datasource-management image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "filediscovery:v0.1.0-beta" `
    -NewText "filediscovery:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - filediscovery image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "fileprocessor:v0.1.0-beta" `
    -NewText "fileprocessor:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - fileprocessor image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "validation:v0.1.0-beta" `
    -NewText "validation:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - validation image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "output:v0.1.0-beta" `
    -NewText "output:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - output image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "invalidrecords:v0.1.0-beta" `
    -NewText "invalidrecords:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - invalidrecords image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "scheduling:v0.1.0-beta" `
    -NewText "scheduling:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - scheduling image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "metrics-configuration:v0.1.0-beta" `
    -NewText "metrics-configuration:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - metrics-configuration image tag"

Update-FileContent `
    -FilePath "IMAGE-MANIFEST.txt" `
    -OldText "frontend:v0.1.0-beta" `
    -NewText "frontend:v0.1.1-rc1" `
    -Description "IMAGE-MANIFEST.txt - frontend image tag"

# Update install.sh
Update-FileContent `
    -FilePath "install.sh" `
    -OldText "# Version: 0.1.0-beta" `
    -NewText "# Version: 0.1.1-rc1" `
    -Description "install.sh - Version"

Update-FileContent `
    -FilePath "install.sh" `
    -OldText "# Date: December 30, 2025" `
    -NewText "# Date: January 1, 2026" `
    -Description "install.sh - Date"

Update-FileContent `
    -FilePath "install.sh" `
    -OldText 'echo "  EZ Platform v0.1.0-beta Installation"' `
    -NewText 'echo "  EZ Platform v0.1.1-rc1 Installation"' `
    -Description "install.sh - Header"

# Update uninstall.sh
Update-FileContent `
    -FilePath "uninstall.sh" `
    -OldText "# Version: 0.1.0-beta" `
    -NewText "# Version: 0.1.1-rc1" `
    -Description "uninstall.sh - Version"

Update-FileContent `
    -FilePath "uninstall.sh" `
    -OldText "# Date: December 30, 2025" `
    -NewText "# Date: January 1, 2026" `
    -Description "uninstall.sh - Date"

Write-Host ""
Write-Host "Phase 2: Kubernetes ConfigMaps" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Update k8s/configmaps/services-config.yaml
$configMapPath = "k8s/configmaps/services-config.yaml"
if (Test-Path $configMapPath) {
    $content = Get-Content $configMapPath -Raw
    if ($content -notmatch 'database-name:') {
        if (-not $WhatIf) {
            $content = $content -replace '(mongodb-connection: "mongodb")', "`$1`r`n  database-name: `"ezplatform`""
            Set-Content -Path $configMapPath -Value $content -NoNewline
            Write-Host "[✓] Updated: k8s ConfigMap - Added database-name" -ForegroundColor Green
        } else {
            Write-Host "[WHATIF] Would update: k8s ConfigMap - Add database-name" -ForegroundColor Yellow
        }
        $script:updates += $configMapPath
    } else {
        Write-Host "[SKIP] database-name already exists in: $configMapPath" -ForegroundColor DarkGray
    }
}

# Update helm ConfigMap
$helmConfigMapPath = "helm/ez-platform/templates/configmaps/services-config.yaml"
if (Test-Path $helmConfigMapPath) {
    $content = Get-Content $helmConfigMapPath -Raw
    if ($content -notmatch 'database-name:') {
        if (-not $WhatIf) {
            $content = $content -replace '(mongodb-connection: "mongodb")', "`$1`r`n  database-name: `"ezplatform`""
            Set-Content -Path $helmConfigMapPath -Value $content -NoNewline
            Write-Host "[✓] Updated: Helm ConfigMap - Added database-name" -ForegroundColor Green
        } else {
            Write-Host "[WHATIF] Would update: Helm ConfigMap - Add database-name" -ForegroundColor Yellow
        }
        $script:updates += $helmConfigMapPath
    } else {
        Write-Host "[SKIP] database-name already exists in: $helmConfigMapPath" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Phase 3: Deployment YAMLs - Image Tags" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Update deployment image tags
$deployments = @(
    "datasource-management",
    "filediscovery",
    "fileprocessor",
    "validation",
    "output",
    "invalidrecords",
    "scheduling",
    "metrics-configuration",
    "frontend"
)

foreach ($deployment in $deployments) {
    $deploymentFile = "k8s/deployments/$deployment-deployment.yaml"
    if (Test-Path $deploymentFile) {
        Update-FileContent `
            -FilePath $deploymentFile `
            -OldText "image: ez-platform/${deployment}:v0.1.0-beta" `
            -NewText "image: ez-platform/${deployment}:v0.1.1-rc1" `
            -Description "k8s/$deployment - Image tag"
    }
}

Write-Host ""
Write-Host "Phase 4: Metrics-Configuration Probes" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Update metrics-configuration deployment probes
$metricsDeployment = "k8s/deployments/metrics-configuration-deployment.yaml"
if (Test-Path $metricsDeployment) {
    if (-not $WhatIf) {
        $content = Get-Content $metricsDeployment -Raw

        # Update imagePullPolicy
        $content = $content -replace 'imagePullPolicy: Always', 'imagePullPolicy: Never'

        # Update liveness probe
        $content = $content -replace 'initialDelaySeconds: 30\s+periodSeconds: 10', 'initialDelaySeconds: 60
          periodSeconds: 30
          timeoutSeconds: 5
          failureThreshold: 5'

        # Update readiness probe
        $content = $content -replace '(readinessProbe:.*?initialDelaySeconds:) 15\s+(periodSeconds:) 5', '$1 30
          $2 10
          timeoutSeconds: 5
          failureThreshold: 3'

        Set-Content -Path $metricsDeployment -Value $content -NoNewline
        Write-Host "[✓] Updated: metrics-configuration deployment - Probes and imagePullPolicy" -ForegroundColor Green
    } else {
        Write-Host "[WHATIF] Would update: metrics-configuration deployment probes" -ForegroundColor Yellow
    }
    $script:updates += $metricsDeployment
}

Write-Host ""
Write-Host "Phase 5: Helm Chart" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Update Helm Chart.yaml
Update-FileContent `
    -FilePath "helm/ez-platform/Chart.yaml" `
    -OldText "version: 1.0.0" `
    -NewText "version: 1.1.0" `
    -Description "Helm Chart.yaml - Chart version"

Update-FileContent `
    -FilePath "helm/ez-platform/Chart.yaml" `
    -OldText 'appVersion: "0.1.0-beta"' `
    -NewText 'appVersion: "0.1.1-rc1"' `
    -Description "Helm Chart.yaml - App version"

# Update Helm values.yaml - imagePullPolicy
Update-FileContent `
    -FilePath "helm/ez-platform/values.yaml" `
    -OldText "imagePullPolicy: Always" `
    -NewText "imagePullPolicy: Never" `
    -Description "Helm values.yaml - Global imagePullPolicy"

# Update all service image tags in values.yaml
foreach ($deployment in $deployments) {
    $repoName = $deployment
    if ($deployment -eq "datasource-management") {
        $repoName = "datasource-management"
    }

    Update-FileContent `
        -FilePath "helm/ez-platform/values.yaml" `
        -OldText "repository: ez-platform/$repoName`n      tag: latest" `
        -NewText "repository: ez-platform/$repoName`n      tag: v0.1.1-rc1" `
        -Description "Helm values.yaml - $deployment image tag"
}

Write-Host ""
Write-Host "Phase 6: Scripts" -ForegroundColor Cyan
Write-Host "================" -ForegroundColor Cyan
Write-Host ""

# Update load-images.sh
Update-FileContent `
    -FilePath "scripts/load-images.sh" `
    -OldText 'docker images | grep -E "v0.1.0-beta' `
    -NewText 'docker images | grep -E "v0.1.1-rc1|v0.1.0-beta' `
    -Description "load-images.sh - Image verification grep"

Write-Host ""
Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host "Update Summary"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Total files updated: $($script:updates.Count)" -ForegroundColor Green
Write-Host ""

if ($WhatIf) {
    Write-Host "This was a WHATIF run. No files were actually modified." -ForegroundColor Yellow
    Write-Host "Run without -WhatIf to apply changes." -ForegroundColor Yellow
} else {
    Write-Host "All updates completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Build Docker images with v0.1.1-rc1 tag" -ForegroundColor White
    Write-Host "2. Export images to images/ folder" -ForegroundColor White
    Write-Host "3. Update RELEASE-PACKAGE-MANIFEST.md manually" -ForegroundColor White
    Write-Host "4. Test deployment" -ForegroundColor White
}
