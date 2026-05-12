<#
.SYNOPSIS
Delete the LighthouseAppContext SQLite database files (.db, .db-shm, .db-wal) from the project directory.
#>
$projectDir = Join-Path $PSScriptRoot 'Lighthouse.Backend'
$targetDir = $projectDir
if (-not (Test-Path $targetDir)) {
    Write-Error "Project directory '$targetDir' does not exist."
    exit 1
}
$files = Get-ChildItem -Path (Join-Path $targetDir 'LighthouseAppContext.db*') -File -ErrorAction SilentlyContinue
if (-not $files -or $files.Count -eq 0) {
    Write-Warning "No LighthouseAppContext DB files found in '$targetDir'. Nothing to delete."
    exit 0
}
Write-Host 'The following files will be deleted:'
foreach ($f in $files) {
    Write-Host "  $($f.FullName)"
}
$answer = Read-Host "This will permanently DELETE the files listed above. Proceed? (y/N)"
if ($answer -ne 'y' -and $answer -ne 'Y') {
    Write-Host 'Cancelled by user.'
    exit 0
}
foreach ($f in $files) {
    try {
        Remove-Item -Path $f.FullName -Force -ErrorAction Stop
        Write-Host "Deleted: $($f.Name)"
    }
    catch {
        Write-Warning "Failed to delete $($f.Name): $_"
    }
}
Write-Host 'Delete complete.'
