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