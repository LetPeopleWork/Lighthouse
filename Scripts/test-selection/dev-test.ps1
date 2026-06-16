# Local equivalent of the CI backend test-selection logic from slice-03b / CS-H.
# Detects which connector folders the current branch's diff touches, builds
# the matching `dotnet test --filter` string, and runs the backend test
# project. Mirrors .github/workflows/ci_backend.yml's Compute Test Backend
# filter step plus the per-connector outputs from ci_changes.yml.
#
# Usage:
#   Scripts/test-selection/dev-test.ps1            # PR-like: only run what your diff touches
#   Scripts/test-selection/dev-test.ps1 -Full      # full suite (every integration category)
#   Scripts/test-selection/dev-test.ps1 -UnitOnly  # skip every integration category
#   Scripts/test-selection/dev-test.ps1 -DryRun    # print the resolved filter and exit

[CmdletBinding()]
param(
    [switch] $Full,
    [switch] $UnitOnly,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

$JiraRegex   = '^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Jira/'
$AdoRegex    = '^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/AzureDevOps/'
$LinearRegex = '^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Linear/'
$GithubRegex = '^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/(Implementation|Interfaces)/[^/]*([Gg]it[Hh]ubService|LighthouseReleaseService)[^/]*\.cs$'
$SharedRegex = '^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/(Auth|OAuth)/|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/WorkTrackingConnectors/[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Interfaces/WorkTrackingConnectors/I[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(WorkTrackingSystemConnection|OAuth/)|^Lighthouse\.Backend/Lighthouse\.Backend/Program\.cs$|^Lighthouse\.Backend/Lighthouse\.sln$|^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/[^/]*\.csproj$'

function Test-AnyMatch {
    param([string[]] $Lines, [string] $Pattern)
    foreach ($line in $Lines) {
        if ($line -match $Pattern) { return $true }
    }
    return $false
}

# Resolve diff base; prefer origin/main when available.
$diffBase = 'origin/main'
git rev-parse --verify --quiet $diffBase *> $null
if ($LASTEXITCODE -ne 0) {
    $diffBase = 'HEAD^'
}
$LASTEXITCODE = 0

$diffLines = git diff --name-only "$diffBase...HEAD"
if (-not $diffLines) { $diffLines = @() }

$parts = @('Category!=Integration')
if ($UnitOnly) {
    # nothing to add
} elseif ($Full) {
    $parts += 'Category=Integration'
} else {
    if (Test-AnyMatch $diffLines $SharedRegex) {
        $parts += 'Category=Integration'
    } else {
        if (Test-AnyMatch $diffLines $JiraRegex)   { $parts += 'Category=JiraIntegration' }
        if (Test-AnyMatch $diffLines $AdoRegex)    { $parts += 'Category=AdoIntegration' }
        if (Test-AnyMatch $diffLines $LinearRegex) { $parts += 'Category=LinearIntegration' }
        if (Test-AnyMatch $diffLines $GithubRegex) { $parts += 'Category=GithubIntegration' }
    }
}

$filter = $parts -join '|'
$mode = if ($UnitOnly) { 'unit-only' } elseif ($Full) { 'full' } else { 'auto' }
Write-Output "dev-test mode=$mode diff_base=$diffBase"
Write-Output '  changed paths:'
$diffLines | Select-Object -First 20 | ForEach-Object { Write-Output "    $_" }
Write-Output "  resolved filter: $filter"

if ($DryRun) { exit 0 }

$repoRoot = git rev-parse --show-toplevel
$proj = Join-Path $repoRoot 'Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj'
dotnet test $proj --filter $filter
