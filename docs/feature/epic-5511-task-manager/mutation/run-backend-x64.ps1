# Runs the backend mutation gate on a Windows ARM64 machine.
#
# Stryker hands VSTest <TargetPlatform>X64</TargetPlatform>, and on win-arm64 an x64 test host launched
# by the arm64 VSTest discovers nothing at all - Stryker then reports "Number of tests found: 0" and
# blames the NUnit adapter, which is not the problem. Running the whole chain as x64 fixes it.
#
# Needs an x64 SDK once (no admin, does not touch the arm64 install or PATH):
#   dotnet-install.ps1 -Architecture x64 -Version <same as arm64 SDK> -InstallDir $env:USERPROFILE\.dotnet-x64
#
# On an x64 machine none of this is needed - call dotnet-stryker directly.
#
# Usage: .\run-backend-x64.ps1 -Config ..\..\..\..\docs\feature\<feature>\mutation\stryker.<id>.backend.json

param(
    [Parameter(Mandatory = $true)][string]$Config,
    [string]$StrykerVersion = '4.16.0',
    # Stryker resolves `solution` in the config relative to the directory it runs from, so this is not
    # cosmetic - run it anywhere else and the whole solution simply does not exist.
    [string]$TestProject = "$PSScriptRoot\..\..\..\..\Lighthouse.Backend\Lighthouse.Backend.Tests"
)

$ErrorActionPreference = 'Stop'

$root = Join-Path $env:USERPROFILE '.dotnet-x64'
if (-not (Test-Path (Join-Path $root 'dotnet.exe'))) {
    throw "No x64 SDK at $root. Install one with dotnet-install.ps1 -Architecture x64 -InstallDir $root"
}

$cli = Join-Path $env:USERPROFILE ".dotnet\tools\.store\dotnet-stryker\$StrykerVersion\dotnet-stryker\$StrykerVersion\tools\net8.0\any\Stryker.CLI.dll"
if (-not (Test-Path $cli)) {
    throw "Stryker $StrykerVersion is not installed. Run: dotnet tool install -g dotnet-stryker --version $StrykerVersion"
}

$env:DOTNET_ROOT = $root
$env:PATH = "$root;$env:PATH"
# Stryker's CLI targets net8.0 and the x64 SDK ships only its own major, so it has to roll forward.
$env:DOTNET_ROLL_FORWARD = 'Major'

$configPath = (Resolve-Path $Config).Path
Set-Location (Resolve-Path $TestProject).Path
Remove-Item -Recurse -Force 'StrykerOutput' -ErrorAction SilentlyContinue

& (Join-Path $root 'dotnet.exe') $cli --config-file $configPath
exit $LASTEXITCODE
