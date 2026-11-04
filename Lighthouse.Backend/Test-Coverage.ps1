<#
.SYNOPSIS
Run the backend tests with coverage collected locally and write Cobertura reports.

.DESCRIPTION
CI runs the backend tests without a coverage collector. This script is the way to get a coverage report
when you want one. It never runs the live-connector categories by default, because those talk to real
Jira, Linear, Azure DevOps and ServiceNow instances.

.PARAMETER Filter
The dotnet test filter. Defaults to excluding the live-connector categories. Narrow it to run a
single class, for example "FullyQualifiedName~MyTestClass".

.EXAMPLE
./Test-Coverage.ps1
Run everything except the live-connector tests and collect coverage.

.EXAMPLE
./Test-Coverage.ps1 -Filter "FullyQualifiedName~MyTestClass"
Collect coverage for one test class.
#>
[CmdletBinding()]
param(
    [string] $Filter = 'TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration'
)

$ErrorActionPreference = 'Stop'

$backendRoot = $PSScriptRoot

dotnet test $backendRoot --filter $Filter --collect:'XPlat Code Coverage'
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet test failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Get-ChildItem -Path $backendRoot -Recurse -Filter 'coverage.cobertura.xml' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 |
    ForEach-Object { Write-Host "Coverage report: $($_.FullName)" -ForegroundColor Green }
