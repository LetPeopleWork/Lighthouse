#!/usr/bin/env bash
set -euo pipefail

# sign.sh
# Minimal wrapper to sign Windows PE files using osslsigncode and a PKCS#11 token
# Designed for use inside a container, with PIN provided via YUBIKEY_PIN env var.

usage() {
  cat <<'USG'
Usage: sign.sh <dir> [glob]

Arguments:
  dir    Directory to search for files (defaults to current directory)
  glob   Shell glob to filter files (default: 'Lighthouse*')

Environment variables:
  YUBIKEY_PIN       -- required pin for the PKCS#11 token (e.g., YubiKey)
  PKCS11_CERT       -- pkcs11 uri to certificate (default: pkcs11:object=9a;type=cert)
  PKCS11_KEY        -- pkcs11 uri to private key (default: pkcs11:object=9a;type=private)
  PKCS11_MODULE     -- path to the opensc PKCS#11 module (auto-detected by default)
  PROVIDER_PATH     -- path to OpenSSL pkcs11 provider (auto-detected)
  TIMESTAMP_URL     -- timestamp authority (defaults to $TIMESTAMP_URL)
  PRODUCT_NAME      -- product name to embed in signature (default: 'Lighthouse')
  PRODUCT_URL       -- product URL to embed (default: 'https://letpeople.work')

Example:
  YUBIKEY_PIN=123456 ./sign.sh ./publish 'Lighthouse.*'
USG
}

if [ "${1:-""}" = "-h" ] || [ "${1:-""}" = "--help" ]; then
  usage
  exit 0
fi

DIR=${1:-.}
GLOB=${2:-'Lighthouse*'}

if [ -z "${YUBIKEY_PIN:-}" ]; then
  echo "Error: YUBIKEY_PIN environment variable must be set (PIN for the token)." >&2
  exit 2
fi

PKCS11_CERT=${PKCS11_CERT:-'pkcs11:object=9a;type=cert'}
PKCS11_KEY=${PKCS11_KEY:-'pkcs11:object=9a;type=private'}

# Try to detect common module locations for opensc-pkcs11 and provider
PKCS11_MODULE=${PKCS11_MODULE:-$(ls /usr/lib*/opensc-pkcs11*.so 2>/dev/null | head -n1 || true)}
PROVIDER_PATH=${PROVIDER_PATH:-$(ls /usr/lib*/ossl-modules/pkcs11prov*.so 2>/dev/null | head -n1 || true)}
ENGINE_PATH=${ENGINE_PATH:-$(ls /usr/lib*/engines-1.1/pkcs11*.so 2>/dev/null | head -n1 || true)}

TIMESTAMP_URL=${TIMESTAMP_URL:-${TIMESTAMP_URL:-http://timestamp.digicert.com}}
PRODUCT_NAME=${PRODUCT_NAME:-'Lighthouse'}
PRODUCT_URL=${PRODUCT_URL:-'https://letpeople.work'}

if [ ! -d "$DIR" ]; then
  echo "Error: directory does not exist: $DIR" >&2
  exit 2
fi

shopt -s nullglob
FILES=("$DIR"/$GLOB)
if [ ${#FILES[@]} -eq 0 ]; then
  echo "No files found matching $DIR/$GLOB" >&2
  exit 3
fi

echo "Found ${#FILES[@]} file(s) to sign"

for f in "${FILES[@]}"; do
  # prepare output filename - keep original extension
  out="${f%.*}-signed.${f##*.}"
  echo "Signing: $f -> $out"

  # Build base osslsigncode command
  if [ -n "$PROVIDER_PATH" ] && [ -n "$PKCS11_MODULE" ] && [ -f "$PROVIDER_PATH" ] && [ -f "$PKCS11_MODULE" ]; then
    echo "Using OpenSSL 3 provider mode: provider=$PROVIDER_PATH pkcs11module=$PKCS11_MODULE"
    cmd=(osslsigncode sign -provider "$PROVIDER_PATH" -pkcs11module "$PKCS11_MODULE" -pkcs11cert "$PKCS11_CERT" -key "$PKCS11_KEY")
  elif [ -n "$ENGINE_PATH" ] && [ -n "$PKCS11_MODULE" ] && [ -f "$ENGINE_PATH" ] && [ -f "$PKCS11_MODULE" ]; then
    echo "Using PKCS#11 engine mode: engine=$ENGINE_PATH pkcs11module=$PKCS11_MODULE"
    cmd=(osslsigncode sign -engine "$ENGINE_PATH" -pkcs11module "$PKCS11_MODULE" -pkcs11cert "$PKCS11_CERT" -key "$PKCS11_KEY")
  else
    # Fall back to using a PKCS#12 file if present in the workspace (developer convenience)
    if [ -n "${PKCS12_FILE:-}" ] && [ -f "$PKCS12_FILE" ]; then
      echo "Provider not available but PKCS12_FILE provided; using PKCS#12: $PKCS12_FILE"
      cmd=(osslsigncode sign -pkcs12 "$PKCS12_FILE" -pass "${PKCS12_PASS:-}" )
    else
      echo "No usable PKCS#11 provider/engine found and no PKCS12_FILE provided. Aborting." >&2
      exit 4
    fi
  fi

  # Common options
  cmd+=( -n "$PRODUCT_NAME" -i "$PRODUCT_URL" -t "$TIMESTAMP_URL" -in "$f" -out "$out" )

  # Supply PIN using engineCtrl (most osslsigncode builds accept engineCtrl for token interaction)
  if [ -n "${YUBIKEY_PIN:-}" ]; then
    cmd+=( -engineCtrl "PIN:$YUBIKEY_PIN" )
  fi

  # Run the command and capture output
  echo "+ ${cmd[*]}"
  if "${cmd[@]}"; then
    echo "Signed $f -> $out"
  else
    echo "Failed to sign $f" >&2
  fi
done

echo "Done."
