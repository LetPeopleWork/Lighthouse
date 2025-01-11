---
title: Standard Installation
layout: home
parent: Installation
nav_order: 1
---

If you can't or don't want to use [Docker](./docker.html), you can also run Lighthouse on your system directly.

- TOC
{:toc}

## Prerequisites
The packages provided by Lighthouse have everything included you need to run it, so there are no prerequisites.

Lighthouse runs on Windows, MacOs, and Linux based systems.

## Download Lighthouse
Download the latest version of Lighthouse for your operating system from the [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest).
Download the zip file, and extract it to the location you want to run the application from.

## Updating Lighthouse
If you want to update Lighthouse, you can simply replace the files in the directory. As the published packages do not include the database, you will keep your data. Lighthouse will in normal circumstances always support migrations to newer versions, so you will not lose any data.

{: .note }
You must make sure to stop Lighthouse from running before updating.

## Installation and Update Scripts
If you don't want to manually download it, you can also use the following scripts, which will look for the latest released version and will download and extract it from the directory you run the script from.

### Windows
```powershell
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
Expand-Archive -Path $assetName -DestinationPath .

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
```

### Linux
```shell
#!/bin/bash

# Expectation to run this script
# Lighthouse binaries are in the same folder as this script
# Lighthouse is not running at the moment
# unzip must be installed (sudo apt-get install unzip)
echo "Fetching the latest release information..."
RELEASE_INFO=$(wget -qO- "https://api.github.com/repos/letpeoplework/lighthouse/releases/latest")

# Parse the download URL of the asset that contains the keyword
ASSET_URL=$(echo "$RELEASE_INFO" | grep "browser_download_url" | grep "linux" | head -n 1 | sed -E 's/.*"([^"]+)".*/\1/')

if [ -z "$ASSET_URL" ]; then
  echo "No asset found with the keyword \"linux\""
  exit 1
fi

# Download the asset
ASSET_NAME=$(basename $ASSET_URL)
echo "Downloading $ASSET_NAME..."
wget -O $ASSET_NAME $ASSET_URL

# Extract the zip file
echo "Extracting $ASSET_NAME..."
unzip -o $ASSET_NAME -d .

chmod +x Lighthouse

# Cleanup
echo "Cleaning up..."
rm $ASSET_NAME

echo "Done!"
```

### MacOS
```shell
#!/bin/bash

# Expectation to run this script
# Lighthouse binaries are in the same folder as this script
# Lighthouse is not running at the moment
# unzip must be installed (usually pre-installed on macOS)
echo "Fetching the latest release information..."
RELEASE_INFO=$(curl -s "https://api.github.com/repos/letpeoplework/lighthouse/releases/latest")

# Parse the download URL of the asset that contains the keyword
ASSET_URL=$(echo "$RELEASE_INFO" | grep "browser_download_url" | grep "osx" | head -n 1 | sed -E 's/.*"([^"]+)".*/\1/')

if [ -z "$ASSET_URL" ]; then
  echo "No asset found with the keyword \"osx\""
  exit 1
fi

# Download the asset
ASSET_NAME=$(basename $ASSET_URL)
echo "Downloading $ASSET_NAME..."
curl -L -o $ASSET_NAME $ASSET_URL

# Extract the zip file
echo "Extracting $ASSET_NAME..."
unzip -o $ASSET_NAME -d .

chmod +x Lighthouse

# Cleanup
echo "Cleaning up..."
rm $ASSET_NAME

echo "Done!"
```

### Prerequisites
- **Windows**: PowerShell 5.1 or later
- **Linux**: `unzip` installed (`sudo apt-get install unzip`)
- **MacOS**: `unzip` installed (usually pre-installed)

### How to Execute the Script
1. Download the appropriate script for your operating system.
2. Place the script in the same directory as the Lighthouse binaries.
3. Ensure Lighthouse is not running.
4. Run the script:
   - **Windows**: Open PowerShell and execute `.\update_windows.ps1`
   - **Linux**: Open a terminal and execute `./update_linux.sh`
   - **MacOS**: Open a terminal and execute `./update_mac.sh`

### Installation/Update Scripts
In [Scripts](https://github.com/LetPeopleWork/Lighthouse/tree/main/Scripts) you can find 3 scripts to download the latest version of Lighthouse for [Linux](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_linux.sh), [Mac](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_mac.sh) and [Windows](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_windows.ps1).

The scripts will download the latest version in the folder they are executed from and will replace all existing files in the folder.
**Important:** The database will not be replaced and the new version will work against the same database.

## Run as Service Scripts

### Start Lighthouse
Once extracted, you can run the the `Lighthouse` application (for example: `Lighthouse.exe` on Windows). A terminal will open and you should see a window similar to this:

![Starting Lighthouse](https://github.com/user-attachments/assets/9bd034a9-0b5d-48fe-897f-3cc749402b24)

By default, Lighthouse will start running on the system on port 5001. If everything worked as expected, you can open the app now in your browser via [https://localhost:5001](https://localhost:5001).
You should see the (empty) landing page:
![Landing Page](https://github.com/user-attachments/assets/06cf29cd-d9a8-4a93-84aa-2335747d8699)



## Prerequisites
- Windows, MacOS, or Linux operating system
- Administrative access for initial setup
- 500MB free disk space

## Installation Steps

1. **Download Package**
   - Get latest version from [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest)
   - Choose package matching your operating system
   - Extract to desired location

2. **Using Installation Scripts**

   Choose the appropriate script from [Scripts](https://github.com/LetPeopleWork/Lighthouse/tree/main/Scripts):
   
   Windows:
   ```powershell
   .\update_windows.ps1
   ```
   
   Linux:
   ```bash
   ./update_linux.sh
   ```
   
   MacOS:
   ```bash
   ./update_mac.sh
   ```

3. **Launch Application**
   - Run `Lighthouse` executable
   - Access web interface at [https://localhost:5001](https://localhost:5001)

## Updating

Use the same scripts mentioned above to update to the latest version. Your database and settings will be preserved.

See [Configuration](../configuration.md) for detailed configuration options.
