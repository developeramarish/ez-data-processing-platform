# EZ Platform - Update to v0.1.1-rc1
# Simple version updater

$ErrorActionPreference = "Stop"

Write-Host "Updating EZ Platform to v0.1.1-rc1..." -ForegroundColor Cyan

# Update README.md
(Get-Content "README.md") -replace 'v0.1.0-beta', 'v0.1.1-rc1' | Set-Content "README.md"
(Get-Content "README.md") -replace 'December 29, 2025', 'January 1, 2026' | Set-Content "README.md"
(Get-Content "README.md") -replace '3.96GB', '4.1GB' | Set-Content "README.md"
Write-Host "[OK] Updated README.md" -ForegroundColor Green

# Update IMAGE-MANIFEST.txt
(Get-Content "IMAGE-MANIFEST.txt") -replace 'v0.1.0-beta', 'v0.1.1-rc1' | Set-Content "IMAGE-MANIFEST.txt"
Write-Host "[OK] Updated IMAGE-MANIFEST.txt" -ForegroundColor Green

# Update install.sh
(Get-Content "install.sh") -replace '0.1.0-beta', '0.1.1-rc1' | Set-Content "install.sh"
(Get-Content "install.sh") -replace 'December 30, 2025', 'January 1, 2026' | Set-Content "install.sh"
Write-Host "[OK] Updated install.sh" -ForegroundColor Green

# Update uninstall.sh
(Get-Content "uninstall.sh") -replace '0.1.0-beta', '0.1.1-rc1' | Set-Content "uninstall.sh"
(Get-Content "uninstall.sh") -replace 'December 30, 2025', 'January 1, 2026' | Set-Content "uninstall.sh"
Write-Host "[OK] Updated uninstall.sh" -ForegroundColor Green

# Update Helm Chart.yaml
(Get-Content "helm/ez-platform/Chart.yaml") -replace 'version: 1.0.0', 'version: 1.1.0' | Set-Content "helm/ez-platform/Chart.yaml"
(Get-Content "helm/ez-platform/Chart.yaml") -replace 'appVersion: "0.1.0-beta"', 'appVersion: "0.1.1-rc1"' | Set-Content "helm/ez-platform/Chart.yaml"
Write-Host "[OK] Updated Helm Chart.yaml" -ForegroundColor Green

# Update Helm values.yaml
(Get-Content "helm/ez-platform/values.yaml") -replace 'imagePullPolicy: Always', 'imagePullPolicy: Never' | Set-Content "helm/ez-platform/values.yaml"
(Get-Content "helm/ez-platform/values.yaml") -replace 'tag: latest', 'tag: v0.1.1-rc1' | Set-Content "helm/ez-platform/values.yaml"
Write-Host "[OK] Updated Helm values.yaml" -ForegroundColor Green

# Update k8s deployment YAMLs
Get-ChildItem "k8s/deployments/*-deployment.yaml" | ForEach-Object {
    (Get-Content $_) -replace 'v0.1.0-beta', 'v0.1.1-rc1' | Set-Content $_
    Write-Host "[OK] Updated $($_.Name)" -ForegroundColor Green
}

# Update mkdocs.yml in docs
(Get-Content "docs/mkdocs.yml") -replace 'v0.1.0-beta', 'v0.1.1-rc1' | Set-Content "docs/mkdocs.yml"
(Get-Content "docs/mkdocs.yml") -replace '0.1.0-beta', '0.1.1-rc1' | Set-Content "docs/mkdocs.yml"
Write-Host "[OK] Updated docs/mkdocs.yml" -ForegroundColor Green

Write-Host ""
Write-Host "All configuration files updated successfully!" -ForegroundColor Green
