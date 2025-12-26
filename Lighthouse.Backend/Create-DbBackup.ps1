<#
.SYNOPSIS
Copy current DB files from project directory into DB_Backup (overwrites existing backups).
#>

$projectDir = Join-Path $PSScriptRoot 'Lighthouse.Backend'
$backupDir = Join-Path $projectDir 'DB_Backup'
$sourceDir = $projectDir

if (-not (Test-Path $backupDir)) {
    Write-Host "Backup directory '$backupDir' does not exist. Creating..."
    try { New-Item -ItemType Directory -Path $backupDir -ErrorAction Stop > $null } catch { Write-Error "Failed to create backup directory: $_"; exit 1 }
}

# Find DB files in the project root but exclude files that are already in DB_Backup
$files = Get-ChildItem -Path (Join-Path $sourceDir '*.db*') -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notlike (Join-Path $backupDir '*') }

if (-not $files -or $files.Count -eq 0) {
    Write-Warning "No DB files found in '$sourceDir' to back up."
    exit 0
}

$answer = Read-Host "This will copy and OVERWRITE DB files from '$sourceDir' to '$backupDir'. Proceed? (y/N)"
if ($answer -ne 'y' -and $answer -ne 'Y') {
    Write-Host 'Cancelled by user.'
    exit 0
}

foreach ($f in $files) {
    try {
        Copy-Item -Path $f.FullName -Destination $backupDir -Force -ErrorAction Stop
        Write-Host "Backed up: $($f.Name)"
    }
    catch {
        Write-Warning "Failed to back up $($f.Name): $_"
    }
}

Write-Host 'Backup complete.'
