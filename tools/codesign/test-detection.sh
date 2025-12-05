#!/usr/bin/env bash
set -euo pipefail

echo "Building codesign container for detection test..."
docker build -t lighthouse-codesign ./tools/codesign

echo "Running detection test inside container (no files will be signed)"
# create an empty temp directory in a mounted volume
tmpdir=$(mktemp -d)
trap 'rm -rf "$tmpdir"' EXIT

docker run --rm -e YUBIKEY_PIN=0000 -v "$tmpdir":/workspace lighthouse-codesign /usr/local/bin/sign.sh /workspace 'NoMatchingFiles*' | tee /tmp/sign-detect.out || true

grep -E "Using (OpenSSL|PKCS)" /tmp/sign-detect.out && echo "DETECTION_OK" || (echo "DETECTION_FAILED" && exit 1)

echo "Detection test passed. sign.sh auto-detected provider/engine successfully."
