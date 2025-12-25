#!/usr/bin/env pwsh
param([switch]$Help)

if ($Help) {
    Write-Host "Usage: ./start-runner-linux.ps1"
    exit 0
}

# ------------------------------
# 1. Pre-Flight YubiKey Checks
# ------------------------------
Write-Host "Checking for YubiKey and Code Signing Key (ID 01)..."

# Check if pcscd is running
if ((Get-Process pcscd -ErrorAction SilentlyContinue) -eq $null) {
    Write-Error "pcscd service is not running. Run 'sudo systemctl start pcscd' first."
    exit 1
}

# Check for the specific Key ID 01 via OpenSC
$pkcs11Module = "/usr/lib/opensc-pkcs11.so"
if (-not (Test-Path $pkcs11Module)) {
    Write-Error "OpenSC PKCS11 module not found at $pkcs11Module"
    exit 1
}

$keyCheck = pkcs11-tool --module $pkcs11Module --list-objects | Select-String "ID:.*01"
if (-not $keyCheck) {
    Write-Error "YubiKey detected, but Code Signing Key (ID 01) was not found."
    Write-Host "Ensure your YubiKey is plugged in and the PIV application is initialized."
    exit 1
}

Write-Host "Success: YubiKey with ID 01 detected."

# ------------------------------
# 2. Validate Environment
# ------------------------------
if (-not $env:GITHUB_PAT) {
    Write-Error "Missing required environment variable: GITHUB_PAT"
    exit 1
}

$Owner = $env:GITHUB_OWNER ?? "LetPeopleWork"
$Repo  = $env:GITHUB_REPO   ?? "Lighthouse"
$RunnerName = $env:RUNNER_NAME ?? "codesign-runner-linux"
$RunnerDir = "$PSScriptRoot/actions-runner"

# ------------------------------
# 3. Setup Directory & Download
# ------------------------------
if (-not (Test-Path $RunnerDir)) {
    New-Item -ItemType Directory -Path $RunnerDir | Out-Null
}
Set-Location $RunnerDir

if (-not (Test-Path "$RunnerDir/config.sh")) {
    Write-Host "Fetching latest runner..."
    $latestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/actions/runner/releases/latest"
    $tarUrl = "https://github.com/actions/runner/releases/download/$($latestRelease.tag_name)/actions-runner-linux-x64-$($latestRelease.tag_name.TrimStart('v')).tar.gz"
    curl -L $tarUrl -o runner.tar.gz
    tar xzf runner.tar.gz
    Remove-Item runner.tar.gz
}

# ------------------------------
# 4. Registration & Run
# ------------------------------
Write-Host "Requesting registration token..."
$Headers = @{ 
    Authorization = "token $($env:GITHUB_PAT)"
    "Accept" = "application/vnd.github+json"
}

try {
    $tokenResp = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$Owner/$Repo/actions/runners/registration-token" -Headers $Headers
    $RunnerToken = $tokenResp.token
} catch {
    Write-Error "Failed to get token: $_"
    exit 1
}

Write-Host "Configuring runner..."
/bin/bash ./config.sh --unattended `
    --url "https://github.com/$Owner/$Repo" `
    --token $RunnerToken `
    --name $RunnerName `
    --labels "linux,codesign" `
    --replace

Write-Host "Starting runner..."
/bin/bash ./run.sh