# Expectation to run this script
# Lighthouse binaries are in the same folder as this script
# Lighthouse is not running at the moment
# PowerShell 5.1 or later is required (Windows comes with it by default)

# Define the GitHub repository and asset keyword
$GITHUB_USER = "letpeoplework"
$REPO_NAME = "lighthouse"
$ASSET_KEYWORD = "win"

# Fetch the latest release information
Write-Host "Fetching the latest release information..."
$response = Invoke-RestMethod -Uri "https://api.github.com/repos/$GITHUB_USER/$REPO_NAME/releases/latest"

# Find the asset URL containing the keyword
$asset = $response.assets | Where-Object { $_.name -like "*$ASSET_KEYWORD*" } | Select-Object -First 1

if (-not $asset) {
    Write-Host "No asset found with the keyword '$ASSET_KEYWORD'"
    exit 1
}

$assetUrl = $asset.browser_download_url
$assetName = [System.IO.Path]::GetFileName($assetUrl)

# Download the asset
Write-Host "Downloading $assetName..."
Invoke-WebRequest -Uri $assetUrl -OutFile $assetName

# Extract the zip file
Write-Host "Extracting $assetName..."
Expand-Archive -Force -Path $assetName -DestinationPath .

# Make the Lighthouse executable (if applicable)
$executable = ".\Lighthouse"
if (Test-Path $executable) {
    Write-Host "Setting executable permissions for $executable"
    icacls $executable /grant Everyone:(RX)
} else {
    Write-Host "Lighthouse executable not found."
}

# Cleanup
Write-Host "Cleaning up..."
Remove-Item -Path $assetName

Write-Host "Done!"
