#!/usr/bin/env bash
set -euo pipefail

echo "Building codesign container (this may take a minute)..."
docker build -t lighthouse-codesign ./tools/codesign
echo "Checking osslsigncode version inside container"
docker run --rm lighthouse-codesign osslsigncode --version || true

echo "Checking osslsigncode version inside container"
docker run --rm lighthouse-codesign osslsigncode --version || true

echo "Checking sign.sh help output"
docker run --rm lighthouse-codesign /usr/local/bin/sign.sh --help || true

echo "Checking verify.sh help output"
docker run --rm lighthouse-codesign /usr/local/bin/verify.sh --help || true

echo "Smoke tests finished. If the container built and help printed, the environment is ready for interactive testing with a YubiKey attached."
