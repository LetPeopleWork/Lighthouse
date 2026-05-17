[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments = $true)][string[]] $Arguments)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$summarise = Join-Path $scriptDir 'summarise.py'

if (-not (Test-Path -LiteralPath $summarise)) {
    Write-Error "summarise.py not found at $summarise"
    exit 1
}

$python = Get-Command python3 -ErrorAction SilentlyContinue
if ($null -eq $python) {
    $python = Get-Command python -ErrorAction SilentlyContinue
}
if ($null -eq $python) {
    Write-Error 'Neither python3 nor python is available on PATH.'
    exit 1
}

& $python.Source $summarise @Arguments
exit $LASTEXITCODE
