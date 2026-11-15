#!/usr/bin/env bash
# Signs the Windows release binaries with the YubiKey, inside the codesign runner image.
# Called by the Sign on Linux step of .github/workflows/ci_codesign-windows.yml from the directory that
# holds ./artifacts/server and ./artifacts/standalone, with the PIN in YUBI_PIN:
#   bash tools/codesign/sign-windows.sh VERSION
# Never add set -x here: it would print the PIN.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
version="${1:-}"
usb_sysfs="${CODESIGN_USB_SYSFS:-/sys/bus/usb/devices}"
reader_wait="${CODESIGN_READER_WAIT:-10}"
yubico_vendor=1050
opensc_module=/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so
intermediate_cert="$script_dir/SSL_COM_CODE_SIGNING_INTERMEDIATE_CA_ECC_R2.crt"
plug_in_message="Plug in the YubiKey and re-run this job."

# The pkcs11 provider reads its module from this variable; the sign config adds the provider settings.
# Both are set here, not in the image, so other jobs on this runner keep OpenSSL's stock behaviour.
export PKCS11_PROVIDER_MODULE="$opensc_module"
export OPENSSL_CONF=/etc/ssl/openssl-pkcs11.cnf

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

check_arguments() {
  if [ -z "$version" ]; then fail "no version given. Call: bash tools/codesign/sign-windows.sh VERSION"; fi
  # An empty PIN sent to the key counts as a wrong one and uses up one of its few PIN retries.
  if [ -z "${YUBI_PIN:-}" ]; then fail "YUBI_PIN is empty. Set it from the YUBI_PIN secret in the step env."; fi
  if ! [[ $reader_wait =~ ^[0-9]+$ ]]; then fail "CODESIGN_READER_WAIT must be a whole number of seconds, not '$reader_wait'."; fi
}

check_key() {
  local vendor_file vendor
  for vendor_file in "$usb_sysfs"/*/idVendor; do
    [ -r "$vendor_file" ] || continue
    vendor="$(< "$vendor_file")"
    if [ "$vendor" = "$yubico_vendor" ]; then return 0; fi
  done
  fail "no YubiKey is on USB. $plug_in_message"
}

# The files to sign, in order: the server exe, the NSIS installer, the MSI. Each group must have one,
# so a missing download fails here instead of shipping an unsigned binary.
files_to_sign=()
collect_files() {
  local group file found
  for group in "./artifacts/server/Lighthouse*.exe" "./artifacts/standalone/Lighthouse*.exe" "./artifacts/standalone/*.msi"; do
    found=0
    for file in $group; do
      if [ -f "$file" ] && [[ $file != *.msi.zip ]]; then
        files_to_sign+=("$file")
        found=$((found + 1))
      fi
    done
    if [ "$found" -eq 0 ]; then fail "no file matches $group in $(pwd). Download the release artifacts before signing."; fi
  done
}

# shellcheck disable=SC2317 # called through the EXIT trap, which shellcheck cannot follow
stop_pcscd() {
  sudo -n /usr/bin/pkill -x pcscd >/dev/null 2>&1 || true
}

start_pcscd() {
  trap stop_pcscd EXIT
  # The EXIT trap stops pcscd; INT and TERM (a cancelled job) only have to end the script to reach it.
  trap 'exit 130' INT
  trap 'exit 143' TERM
  # A container killed during an earlier run skipped its trap and may have left a pcscd behind.
  stop_pcscd
  if ! sudo -n /usr/sbin/pcscd --disable-polkit; then fail "pcscd did not start; see its message above."; fi
}

reader_listed() {
  local listing line
  listing="$(timeout 3 opensc-tool --list-readers 2>/dev/null || true)"
  while IFS= read -r line; do
    if [[ $line =~ ^[0-9]+[[:space:]] ]]; then return 0; fi
  done <<< "$listing"
  return 1
}

wait_for_reader() {
  local deadline=$((SECONDS + reader_wait))
  while ! reader_listed; do
    if [ "$SECONDS" -ge "$deadline" ]; then
      fail "the YubiKey is on USB but no reader appeared within ${reader_wait} s. $plug_in_message If it is plugged in, a host process may hold it: run tools/codesign/check-host.sh on the signing machine."
    fi
    sleep 0.5
  done
}

sign_file() {
  local file=$1
  echo "Signing $file"
  # The PIN goes on stdin only: an argument is visible to every process on the machine.
  if ! printf '%s' "$YUBI_PIN" | osslsigncode sign \
      -provider pkcs11 \
      -pkcs11module "$opensc_module" \
      -key "pkcs11:id=%01;type=private" \
      -pkcs11cert "pkcs11:id=%01;type=cert" \
      -ac "$intermediate_cert" \
      -readpass - \
      -n "Lighthouse $version" \
      -i "https://letpeople.work/lighthouse" \
      -t http://timestamp.digicert.com \
      -in "$file" \
      -out "$file.signed"; then
    rm -f "$file.signed"
    # No retry: if the PIN was wrong, each further try uses up one of the key's PIN retries.
    fail "signing $file failed; nothing more was signed. Check the osslsigncode message above, then re-run this job."
  fi
  mv "$file.signed" "$file"
}

sign_files() {
  local file
  for file in "${files_to_sign[@]}"; do sign_file "$file"; done
  echo "Signed ${#files_to_sign[@]} files."
}

check_arguments
check_key
collect_files
start_pcscd
wait_for_reader
sign_files
