# Emergency Push Script - EZ Platform
# Pushes commits in small, reliable batches

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Emergency GitHub Push - Batch Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configure git for large repos
Write-Host "Configuring git..." -ForegroundColor Yellow
git config http.postBuffer 524288000
git config pack.windowMemory "100m"
git config pack.packSizeLimit "100m"
git config pack.threads "1"

Write-Host "✓ Git configured for large pushes" -ForegroundColor Green
Write-Host ""

# Get commit count
$ahead = git rev-list --count origin/main..HEAD
Write-Host "Commits to push: $ahead" -ForegroundColor Yellow
Write-Host ""

if ($ahead -eq 0) {
    Write-Host "✓ Already synced!" -ForegroundColor Green
    exit 0
}

# Push in batches of 3
$batchSize = 3
$batches = [math]::Ceiling($ahead / $batchSize)

Write-Host "Push strategy: $batches batches of ~$batchSize commits each" -ForegroundColor Cyan
Write-Host ""

for ($i = $batches; $i -gt 0; $i--) {
    $skip = ($i - 1) * $batchSize
    $commitRef = "HEAD~$skip"

    Write-Host "Batch $($batches - $i + 1)/$batches : Pushing to $commitRef" -ForegroundColor Yellow

    $result = git push origin "${commitRef}:refs/heads/main" 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Batch pushed successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Batch failed: $result" -ForegroundColor Red
        Write-Host ""
        Write-Host "Retry with: git push origin ${commitRef}:refs/heads/main" -ForegroundColor Yellow
        exit 1
    }

    if ($i -gt 1) {
        Write-Host "  Waiting 10 seconds..." -ForegroundColor Gray
        Start-Sleep -Seconds 10
    }
    Write-Host ""
}

# Final push to HEAD
Write-Host "Final push: HEAD to main" -ForegroundColor Yellow
$result = git push origin main 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All commits pushed successfully!" -ForegroundColor Green
} else {
    Write-Host "✗ Final push failed: $result" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify
git fetch origin --quiet
$newAhead = git rev-list --count origin/main..HEAD

if ($newAhead -eq 0) {
    Write-Host "✅ SUCCESS: Repository fully synced!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now push tags:" -ForegroundColor Yellow
    Write-Host "  git push --tags --force" -ForegroundColor White
} else {
    Write-Host "⚠️  Still $newAhead commits ahead" -ForegroundColor Yellow
    Write-Host "Run script again or push manually" -ForegroundColor Yellow
}

Write-Host ""
