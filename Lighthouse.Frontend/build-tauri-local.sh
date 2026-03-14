#!/bin/bash

# 1. Get the triple
TRIPLE=$(rustc -Vv | grep host: | cut -d' ' -f2)
BIN_DIR="src-tauri/binaries"
RESOURCES_DIR="src-tauri/resources"

mkdir -p "$BIN_DIR"
mkdir -p "$RESOURCES_DIR"

# 2. Build the .NET Backend (Target the .csproj directly to avoid Solution output warnings)
echo "Building .NET Backend..."
dotnet publish ../Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o ./temp_publish

# 3. Move and rename the binary for Tauri
# NOTE: Check if your binary is named 'Lighthouse.Backend' or just 'Lighthouse'
# Based on your previous lib.rs, it expects 'Lighthouse.Backend'
if [ -f "./temp_publish/Lighthouse.Backend" ]; then
    cp "./temp_publish/Lighthouse.Backend" "$BIN_DIR/Lighthouse.Backend-$TRIPLE"
else
    # Fallback if the executable is named 'Lighthouse'
    cp "./temp_publish/Lighthouse" "$BIN_DIR/Lighthouse.Backend-$TRIPLE"
fi

chmod +x "$BIN_DIR/Lighthouse.Backend-$TRIPLE"

# 4. Copy resources
echo "Copying resources..."
# Copy everything from publish to resources
cp -r ./temp_publish/* "$RESOURCES_DIR/"

# Cleanup the temp folder
rm -rf ./temp_publish

echo "✅ Sidecar ready for triple: $TRIPLE"