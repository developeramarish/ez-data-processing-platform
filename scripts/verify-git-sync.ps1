# Git Sync Verification Script
# Checks for inconsistencies between local and remote repos

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Git Sync Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check current branch
$branch = git rev-parse --abbrev-ref HEAD
Write-Host "Current branch: " -NoNewline
Write-Host $branch -ForegroundColor Yellow
Write-Host ""

# Fetch latest from remote
Write-Host "Fetching from remote..." -ForegroundColor Gray
git fetch origin --quiet
Write-Host ""

# Check if local is ahead
$ahead = git rev-list --count origin/$branch..$branch
$behind = git rev-list --count $branch..origin/$branch

Write-Host "Status:" -ForegroundColor Cyan
if ($ahead -gt 0) {
    Write-Host "  Ahead of remote: " -NoNewline -ForegroundColor Yellow
    Write-Host "$ahead commits" -ForegroundColor Red
    Write-Host "  Action needed: git push" -ForegroundColor Yellow
} else {
    Write-Host "  Ahead of remote: 0 commits" -ForegroundColor Green
}

if ($behind -gt 0) {
    Write-Host "  Behind remote: " -NoNewline -ForegroundColor Yellow
    Write-Host "$behind commits" -ForegroundColor Red
    Write-Host "  Action needed: git pull" -ForegroundColor Yellow
} else {
    Write-Host "  Behind remote: 0 commits" -ForegroundColor Green
}

Write-Host ""

# Check for uncommitted changes
$status = git status --short
if ($status) {
    Write-Host "Uncommitted changes:" -ForegroundColor Yellow
    git status --short
    Write-Host ""
    Write-Host "Action needed: git add . && git commit" -ForegroundColor Yellow
} else {
    Write-Host "Working tree: " -NoNewline
    Write-Host "clean" -ForegroundColor Green
}

Write-Host ""

# Check tags
Write-Host "Checking tags..." -ForegroundColor Cyan
$localTag = git tag -l "v0.1.0-beta"
$remoteTag = git ls-remote --tags origin v0.1.0-beta

Write-Host "Local tag v0.1.0-beta: " -NoNewline
if ($localTag) {
    $localCommit = git rev-list -n 1 v0.1.0-beta
    Write-Host "exists ($($localCommit.Substring(0,7)))" -ForegroundColor Green
} else {
    Write-Host "missing" -ForegroundColor Red
}

Write-Host "Remote tag v0.1.0-beta: " -NoNewline
if ($remoteTag) {
    $remoteCommit = $remoteTag.Split("`t")[0]
    Write-Host "exists ($($remoteCommit.Substring(0,7)))" -ForegroundColor Green
} else {
    Write-Host "missing" -ForegroundColor Red
}

Write-Host ""

# Verify specific files
Write-Host "Verifying critical files..." -ForegroundColor Cyan
$criticalFiles = @(
    "helm/ez-platform/templates/_helpers.tpl",
    "release-package/docs/docs/installation/helm-installation.md",
    "release-package/helm/ez-platform/values.yaml",
    "release-package/install.sh"
)

foreach ($file in $criticalFiles) {
    Write-Host "  $file : " -NoNewline
    if (Test-Path $file) {
        $tracked = git ls-files $file
        if ($tracked) {
            Write-Host "tracked" -ForegroundColor Green
        } else {
            Write-Host "NOT TRACKED" -ForegroundColor Red
        }
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Summary
if ($ahead -eq 0 -and $behind -eq 0 -and !$status) {
    Write-Host "✅ Repository is fully synced!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Repository needs synchronization" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Recommended actions:" -ForegroundColor Cyan
    if ($status) {
        Write-Host "  1. git add ." -ForegroundColor White
        Write-Host "  2. git commit -m 'Your message'" -ForegroundColor White
    }
    if ($ahead -gt 0) {
        Write-Host "  3. git push origin main" -ForegroundColor White
        Write-Host "  4. git push --tags --force" -ForegroundColor White
    }
    if ($behind -gt 0) {
        Write-Host "  5. git pull origin main" -ForegroundColor White
    }
}

Write-Host ""
