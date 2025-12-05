#!/usr/bin/env bash
set -euo pipefail

# verify.sh - wrapper for osslsigncode verification

usage() {
  cat <<'USG'
Usage: verify.sh <file>

Checks a signed Windows PE file using osslsigncode.
USG
}

if [ "${1:-""}" = "-h" ] || [ "${1:-""}" = "--help" ]; then
  usage
  exit 0
fi

f=${1:-}
if [ -z "$f" ]; then
  echo "Error: missing filename to verify" >&2
  usage
  exit 2
fi

if [ ! -f "$f" ]; then
  echo "Error: file not found: $f" >&2
  exit 3
fi

echo "Running osslsigncode verify on $f"
if osslsigncode verify -in "$f"; then
  echo "Signature verification PASSED: $f"
  exit 0
else
  echo "Signature verification FAILED: $f" >&2
  exit 1
fi
