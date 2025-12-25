# ------------------------------
# start-runner-windows.ps1 (verbose)
# ------------------------------

param(
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Usage: .\start-runner-windows.ps1

Optional environment variables (defaults provided):
  GITHUB_PAT      : GitHub Personal Access Token with 'repo' or 'admin:org' scope (required)
  GITHUB_OWNER    : Repository owner (default: LetPeopleWork)
  GITHUB_REPO     : Repository name (default: Lighthouse)
  RUNNER_NAME     : Name for this runner (default: codesign-runner-windows)

Example:
  $env:GITHUB_PAT="ghp_12345..."
  .\start-runner-windows.ps1
"@
    exit 0
}

# ------------------------------
# Validate required environment variables
# ------------------------------
if (-not $env:GITHUB_PAT) {
    Write-Error "Missing required environment variable: GITHUB_PAT"
    exit 1
}

# ------------------------------
# Set variables with defaults
# ------------------------------
$Owner = $env:GITHUB_OWNER ?? "LetPeopleWork"
$Repo  = $env:GITHUB_REPO   ?? "Lighthouse"
$RunnerName = $env:RUNNER_NAME ?? "codesign-runner-windows"
$RunnerDir = "$PSScriptRoot\actions-runner"
$WorkDir = "$RunnerDir\_work"

Write-Host "=== Configuration ==="
Write-Host "Owner       : $Owner"
Write-Host "Repo        : $Repo"
Write-Host "RunnerName  : $RunnerName"
Write-Host "RunnerDir   : $RunnerDir"
Write-Host "WorkDir     : $WorkDir"
Write-Host "====================="

# ------------------------------
# Download latest runner if not exists
# ------------------------------
if (-not (Test-Path $RunnerDir)) {
    Write-Host "Creating directory $RunnerDir"
    New-Item -ItemType Directory -Path $RunnerDir | Out-Null
}

Set-Location $RunnerDir

if (-not (Test-Path "$RunnerDir\config.cmd")) {
    Write-Host "Downloading latest GitHub Actions runner..."
    try {
        $latestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/actions/runner/releases/latest"
        $version = $latestRelease.tag_name.TrimStart("v")
        $zipUrl = "https://github.com/actions/runner/releases/download/$($latestRelease.tag_name)/actions-runner-win-x64-$version.zip"
        Write-Host "Downloading $zipUrl ..."
        Invoke-WebRequest -Uri $zipUrl -OutFile "actions-runner.zip"
        Expand-Archive -LiteralPath "actions-runner.zip" -DestinationPath "." -Force
    } catch {
        Write-Error "Failed to download or extract runner: $_"
        exit 1
    }
}

# ------------------------------
# Get registration token
# ------------------------------
Write-Host "Requesting registration token..."

$Headers = @{ Authorization = "token $($env:GITHUB_PAT)"; "User-Agent" = "PowerShell" }
Write-Host "Url: https://api.github.com/repos/$Owner/$Repo/actions/runners/registration-token"

try {

    $tokenResp = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$Owner/$Repo/actions/runners/registration-token" -Headers $Headers -ErrorAction Stop
    if (-not $tokenResp.token) {
        Write-Error "No token received. Response: $($tokenResp | ConvertTo-Json -Depth 3)"
        exit 1
    }
    $RunnerToken = $tokenResp.token
    Write-Host "Token retrieved successfully."
} catch {
    Write-Error "Failed to get registration token."
    Write-Host "HTTP request failed: $_"
    Write-Host "Check that:"
    Write-Host "  - PAT has 'repo' and admin permissions"
    Write-Host "  - Repository '$Owner/$Repo' exists and is accessible"
    exit 1
}

# ------------------------------
# Configure runner
# ------------------------------
Write-Host "Configuring runner..."
try {
    & .\config.cmd --unattended `
        --url "https://github.com/$Owner/$Repo" `
        --token $RunnerToken `
        --name $RunnerName `
        --labels "windows,codesign" `
        --work $WorkDir `
        --replace
} catch {
    Write-Error "Failed to configure runner: $_"
    exit 1
}

# ------------------------------
# Start runner
# ------------------------------
Write-Host "Starting runner. Press Ctrl+C to stop."
try {
    Start-Process -FilePath ".\run.cmd" -NoNewWindow -Wait
} catch {
    Write-Error "Failed to start runner: $_"
    exit 1
}
