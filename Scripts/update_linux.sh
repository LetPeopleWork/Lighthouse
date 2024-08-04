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