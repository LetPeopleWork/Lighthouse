#!/usr/bin/env bash
set -euo pipefail

# sign.sh
# Minimal wrapper to sign Windows PE files using osslsigncode and a PKCS#11 token

usage() {
  cat <<'USG'
Usage: sign.sh <dir> [glob]

Arguments:
  dir    Directory to search for files (defaults to current directory)
  glob   Shell glob to filter files (default: 'Lighthouse*')

Environment variables:
  YUBIKEY_PIN       -- required pin for the PKCS#11 token (e.g., YubiKey)
  PKCS11_CERT       -- pkcs11 uri to certificate (default: pkcs11:id=%01;type=cert)
  PKCS11_KEY        -- pkcs11 uri to private key (default: pkcs11:id=%01;type=private)
  PKCS11_MODULE     -- path to the opensc PKCS#11 module (auto-detected by default)
  PROVIDER_PATH     -- path to OpenSSL pkcs11 provider (auto-detected)
  TIMESTAMP_URL     -- timestamp authority (default: http://timestamp.digicert.com)
  PRODUCT_NAME      -- product name to embed in signature (default: 'Lighthouse')
  PRODUCT_URL       -- product URL to embed (default: 'https://letpeople.work')
USG
}

if [[ "${1:-}" =~ ^-h|--help$ ]]; then
  usage
  exit 0
fi

DIR=${1:-.}
GLOB=${2:-'Lighthouse*'}

if [ -z "${YUBIKEY_PIN:-}" ]; then
  echo "Error: YUBIKEY_PIN environment variable must be set." >&2
  exit 2
fi

PKCS11_CERT=${PKCS11_CERT:-'pkcs11:id=%01;type=cert'}
PKCS11_KEY=${PKCS11_KEY:-'pkcs11:id=%01;type=private'}

PKCS11_MODULE=${PKCS11_MODULE:-$(find /usr/lib -name 'opensc-pkcs11.so' 2>/dev/null | head -n1 || true)}
PROVIDER_PATH=${PROVIDER_PATH:-$(find /usr/lib -path '*/ossl-modules/pkcs11.so' 2>/dev/null | head -n1 || true)}
ENGINE_PATH=${ENGINE_PATH:-$(find /usr/lib -path '*/engines-*/pkcs11.so' 2>/dev/null | head -n1 || true)}

TIMESTAMP_URL=${TIMESTAMP_URL:-http://timestamp.digicert.com}
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

failed=0
for f in "${FILES[@]}"; do
  echo "Signing: $f"

  # --- Build the command based on available PKCS#11 mode ---
  use_provider=false
  use_engine=false
  if [ -n "$PROVIDER_PATH" ] && [ -n "$PKCS11_MODULE" ] && [ -f "$PROVIDER_PATH" ] && [ -f "$PKCS11_MODULE" ]; then
    echo "Using OpenSSL 3 provider mode: provider=$PROVIDER_PATH pkcs11module=$PKCS11_MODULE"
    use_provider=true
    # For provider mode, embed PIN in the PKCS#11 URI
    pkcs11_key_with_pin="$PKCS11_KEY"
    if [ -n "${YUBIKEY_PIN:-}" ]; then
      pkcs11_key_with_pin="${PKCS11_KEY};pin-value=${YUBIKEY_PIN}"
    fi
    cmd=(osslsigncode sign -provider "$PROVIDER_PATH" -pkcs11module "$PKCS11_MODULE" -pkcs11cert "$PKCS11_CERT" -key "$pkcs11_key_with_pin")
  elif [ -n "$ENGINE_PATH" ] && [ -n "$PKCS11_MODULE" ] && [ -f "$ENGINE_PATH" ] && [ -f "$PKCS11_MODULE" ]; then
    echo "Using PKCS#11 engine mode: engine=$ENGINE_PATH pkcs11module=$PKCS11_MODULE"
    use_engine=true
    cmd=(osslsigncode sign -pkcs11engine "$ENGINE_PATH" -pkcs11module "$PKCS11_MODULE" -pkcs11cert "$PKCS11_CERT" -key "$PKCS11_KEY")
  else
    if [ -n "${PKCS12_FILE:-}" ] && [ -f "$PKCS12_FILE" ]; then
      echo "Provider not available but PKCS12_FILE provided; using PKCS#12: $PKCS12_FILE"
      cmd=(osslsigncode sign -pkcs12 "$PKCS12_FILE" -pass "${PKCS12_PASS:-}")
    else
      echo "No usable PKCS#11 provider/engine found and no PKCS12_FILE provided. Aborting." >&2
      exit 4
    fi
  fi
  # --- End cmd build ---

  # Common options
  cmd+=( -n "$PRODUCT_NAME" -i "$PRODUCT_URL" -t "$TIMESTAMP_URL" -in "$f" )

  # Supply PIN via engineCtrl only for engine mode (provider mode uses URI)
  if [ "$use_engine" = true ] && [ -n "${YUBIKEY_PIN:-}" ]; then
    cmd+=( -pass "${YUBIKEY_PIN}" )
  fi

  # Temporary unique output filename to avoid overwrite errors
  # mktemp creates an empty file, but osslsigncode refuses to overwrite existing files
  # So we create the unique name, then remove the file and let osslsigncode create it
  out_tmp=$(mktemp "${f}.signed.XXXXXX")
  rm -f "$out_tmp"
  # Cleanup temp file on exit/interrupt
  trap 'rm -f "$out_tmp"' EXIT INT TERM
  cmd+=( -out "$out_tmp" )

  # Run osslsigncode
  echo "+ ${cmd[*]}"
  if "${cmd[@]}"; then
    echo "Signed $f -> $out_tmp"
    # Atomically replace original file
    if mv -f -- "$out_tmp" "$f"; then
      echo "Replaced $f with signed version"
    else
      echo "WARNING: could not move signed file back to $f (signed file left at $out_tmp)" >&2
      failed=$((failed + 1))
    fi
  else
    rm -f "$out_tmp" 2>/dev/null || true
    echo "Failed to sign $f" >&2
    failed=$((failed + 1))
  fi
  # Clear trap for this iteration
  trap - EXIT INT TERM
done

if [ $failed -gt 0 ]; then
  echo "Done with $failed failure(s)." >&2
  exit 1
fi
echo "Done. All files signed successfully."
