#!/usr/bin/env bash
set -euo pipefail

# verify.sh - wrapper for osslsigncode verification

usage() {
  cat <<'USG'
Usage: verify.sh [--list-only] <file|dir> [glob ...]

Checks one or more signed Windows PE files using osslsigncode.

Arguments:
  file         Single file or shell glob to verify (e.g. ./path/Lighthouse.exe).
  dir [glob]   Directory followed by one or more globs to verify files inside that directory.

Options:
  --list-only  Don't actually verify, just list files that would be verified.
USG
}

LIST_ONLY=false
if [ "${1:-""}" = "-h" ] || [ "${1:-""}" = "--help" ]; then
  usage
  exit 0
fi

if [ "${1:-}" = "--list-only" ]; then
  LIST_ONLY=true
  shift
fi

if [ $# -eq 0 ]; then
  echo "Error: missing filename/directory to verify" >&2
  usage
  exit 2
fi

shopt -s nullglob
FILES=()

if [ -d "$1" ] && [ $# -gt 1 ]; then
  # first arg is a directory; subsequent args are patterns
  DIR=$1
  shift
  for pat in "$@"; do
    for f in "$DIR"/$pat; do
      FILES+=("$f")
    done
  done
else
  # treat each arg as a file or glob
  for pat in "$@"; do
    # if it's an existing file, add it directly
    if [ -f "$pat" ]; then
      FILES+=("$pat")
      continue
    fi
    # expand glob relative to cwd
    for f in $pat; do
      FILES+=("$f")
    done
  done
fi

if [ ${#FILES[@]} -eq 0 ]; then
  echo "No files found matching args: $*" >&2
  exit 3
fi

echo "Running osslsigncode verify on ${#FILES[@]} file(s)"

failed=0
for f in "${FILES[@]}"; do
  echo "Verifying: $f"
  if [ "$LIST_ONLY" = true ]; then
    echo "(list-only) $f"
    continue
  fi

  if osslsigncode verify -in "$f"; then
    echo "Signature verification PASSED: $f"
  else
    echo "Signature verification FAILED: $f" >&2
    failed=$((failed + 1))
  fi
done

if [ $failed -gt 0 ]; then
  echo "Finished: $failed file(s) failed verification" >&2
  exit 1
fi
echo "All files verified successfully"
exit 0
