<#
.SYNOPSIS
Start the Lighthouse backend for local development, with a key store that cannot collide with the one the test suite uses.

.DESCRIPTION
Lighthouse refuses to start when it finds two key rings that are not the same key, because picking the
wrong one leaves every stored secret unreadable. The dev profile used to keep its ring in the one
directory the app reads as a legacy store to carry over and compare against, so a dev run and a test run
in the same project directory produced two rings and broke both at once. The profile names dev-keys now,
which no comparison ever looks at.

This script additionally keeps the dev key ring outside the repository, where git clean and a fresh
worktree cannot throw it away. Losing a ring is what leaves stored credentials unreadable.

.PARAMETER KeyStorePath
Where to keep the dev key ring. Defaults to ~/.config/Lighthouse/dev-keys. Anywhere outside the
repository works; anywhere inside it re-creates the collision this script exists to prevent.

.PARAMETER Fresh
Delete the dev database first and start on an empty one. The key ring is kept, so this is safe.

.PARAMETER SkipBuild
Skip the migration-project build check. Only use it when both migration assemblies are known to be current.

.EXAMPLE
./Start-DevServer.ps1
Start on http://localhost:5169 with the existing database.

.EXAMPLE
./Start-DevServer.ps1 -Fresh
Throw the dev database away and start on an empty one.
#>
[CmdletBinding()]
param(
    [string] $KeyStorePath = (Join-Path $HOME '.config/Lighthouse/dev-keys'),
    [switch] $Fresh,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

$backendRoot = $PSScriptRoot
$projectDir = Join-Path $backendRoot 'Lighthouse.Backend'

if (-not (Test-Path $projectDir)) {
    Write-Error "Project directory '$projectDir' does not exist."
    exit 1
}

# A key ring inside the project directory is what breaks both the dev server and the test suite, so say
# so here rather than letting the app fail later with an error about keys nobody put there on purpose.
$colliding = @('data-protection-keys', 'keys') |
    ForEach-Object { Join-Path $projectDir $_ } |
    Where-Object { Test-Path (Join-Path $_ 'encryption-keyring.protected') }

if ($colliding) {
    $quarantine = Join-Path $HOME "lighthouse-keystore-quarantine-$(Get-Date -Format yyyyMMdd)"
    Write-Warning 'A key ring exists inside the project directory:'
    $colliding | ForEach-Object { Write-Warning "  $_" }
    Write-Warning 'The backend test suite mints one there; a dev run must not. Move it aside and start again.'
    Write-Warning 'Move it, do not delete it: it is what any secret stored under it was encrypted with.'
    $colliding | ForEach-Object { Write-Warning "  Move-Item '$_' '$quarantine'" }
    exit 1
}

if ($Fresh) {
    $dbFiles = Get-ChildItem -Path (Join-Path $projectDir 'LighthouseAppContext.db*') -File -ErrorAction SilentlyContinue
    if ($dbFiles) {
        Write-Host 'Removing the dev database:' -ForegroundColor Yellow
        $dbFiles | ForEach-Object { Write-Host "  $($_.Name)"; Remove-Item $_.FullName -Force }
    }
}

if (-not $SkipBuild) {
    # The migration projects are referenced by path, so the app cannot load them until they are built.
    foreach ($project in @('Lighthouse.Migrations.Sqlite', 'Lighthouse.Migrations.Postgres')) {
        $assembly = Join-Path $backendRoot "$project/bin/Debug/net10.0/$project.dll"
        if (-not (Test-Path $assembly)) {
            Write-Host "Building $project ..." -ForegroundColor Cyan
            dotnet build (Join-Path $backendRoot $project) | Out-Null
            if ($LASTEXITCODE -ne 0) { Write-Error "Building $project failed."; exit 1 }
        }
    }
}

New-Item -ItemType Directory -Force -Path $KeyStorePath | Out-Null

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Encryption__KeyStorePath = $KeyStorePath

Write-Host ''
Write-Host "Key store : $KeyStorePath" -ForegroundColor Green
Write-Host "Database  : $(Join-Path $projectDir 'LighthouseAppContext.db')" -ForegroundColor Green
Write-Host 'Listening : http://localhost:5169 (authentication disabled)' -ForegroundColor Green
Write-Host ''

Push-Location $projectDir
try {
    dotnet run --launch-profile http
}
finally {
    Pop-Location
}
