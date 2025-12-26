<#
.SYNOPSIS
Restore database files from DB_Backup into the project directory (overwrites existing files).
#>

$projectDir = Join-Path $PSScriptRoot 'Lighthouse.Backend'
$backupDir = Join-Path $projectDir 'DB_Backup'
$targetDir = $projectDir

if (-not (Test-Path $backupDir)) {
    Write-Error "Backup directory '$backupDir' does not exist."
    exit 1
}

$files = Get-ChildItem -Path (Join-Path $backupDir '*.db*') -File -ErrorAction SilentlyContinue
if (-not $files -or $files.Count -eq 0) {
    Write-Warning "No DB files found in '$backupDir'. Nothing to restore."
    exit 0
}

$answer = Read-Host "This will copy and OVERWRITE DB files from '$backupDir' to '$targetDir'. Proceed? (y/N)"
if ($answer -ne 'y' -and $answer -ne 'Y') {
    Write-Host 'Cancelled by user.'
    exit 0
}

foreach ($f in $files) {
    try {
        Copy-Item -Path $f.FullName -Destination $targetDir -Force -ErrorAction Stop
        Write-Host "Restored: $($f.Name)"
    }
    catch {
        Write-Warning "Failed to restore $($f.Name): $_"
    }
}

Write-Host 'Restore complete.'
