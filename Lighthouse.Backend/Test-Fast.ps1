<#
.SYNOPSIS
Run only the fast backend tests.

.DESCRIPTION
The quick answer while working: the tests that start no web host and no container, so the run needs
neither Docker nor a token. It leaves out the Integration category and the five live-connector categories,
which means it cannot reach a real Jira, Linear, Azure DevOps, ServiceNow or GitHub instance. The filter is
fixed on purpose, so this script cannot be pointed at those tests.

This is not a gate: a change is done when the full dotnet test run in CLAUDE.md passes.

Verified under pwsh on Linux only. Windows is unverified.

.PARAMETER NoBuild
Skip the build and run the tests of the last Debug build, as a plain dotnet build leaves it.

.EXAMPLE
./Test-Fast.ps1
Build, then run the fast tests.

.EXAMPLE
./Test-Fast.ps1 -NoBuild
Run the fast tests against the last Debug build.
#>
[CmdletBinding()]
param(
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

$fastFilter = 'TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration'
$testProject = Join-Path $PSScriptRoot 'Lighthouse.Backend.Tests'

$dotnetTestArguments = @('--filter', $fastFilter)
if ($NoBuild) {
    $dotnetTestArguments += '--no-build'
}

dotnet test $testProject @dotnetTestArguments
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet test failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}
