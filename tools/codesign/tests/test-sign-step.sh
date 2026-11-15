#!/usr/bin/env bash
# What the sign step promises the maintainer: inside the runner image it signs the server exe, the NSIS
# installer and the MSI once each through the pkcs11 provider with the PIN on stdin, refuses at once with
# the plug-in message when no YubiKey is plugged in, never sends the PIN before a reader is listed, and
# always stops pcscd again so the key is free for the next run. Needs only Docker and bash.
# Set IMAGE to check a published image instead of building one.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd -P)"
codesign_dir="$repo_root/tools/codesign"
dockerfile="$codesign_dir/Dockerfile"
sign_script="$codesign_dir/sign-windows.sh"
sign_workflow="$repo_root/.github/workflows/ci_codesign-windows.yml"
image_workflow="$repo_root/.github/workflows/codesign-runner-image.yml"

yq_image="mikefarah/yq:4.47.1@sha256:b9285dd3b0bea3c34d0c54415dd48d767dabd9644d489bd6e253660847b58419"
softhsm_image="lighthouse-codesign-runner:oracle-sign-softhsm"
opensc_module=/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so
sign_openssl_conf=/etc/ssl/openssl-pkcs11.cnf
version=9.9.9
# A made-up PIN; finding it anywhere in what the step prints or on a command line means a leak.
sentinel_pin=oracle-sentinel-pin-31337
plug_in_message="plug in the YubiKey and re-run this job"
# The message may start a sentence, so it is matched without regard to case on both sides.
plug_in_message_lower="${plug_in_message,,}"
case_limit_seconds=90
# shellcheck disable=SC2016 # the GitHub expression is the literal text the workflow must carry
pin_secret_expression='${{ secrets.YUBI_PIN }}'

observed=0
assertions_failed=0
setup_failed=0
prefix="lh-codesign-sign-oracle-$$"
work_dir=""
image=""
created_containers=()

# shellcheck disable=SC2317 # called through the EXIT trap, which shellcheck cannot follow
cleanup() {
  local name
  for name in "${created_containers[@]+"${created_containers[@]}"}"; do docker rm -f "$name" >/dev/null 2>&1 || true; done
  if [ -n "$work_dir" ]; then rm -rf "$work_dir"; fi
}
trap cleanup EXIT

pass() {
  observed=$((observed + 1))
  echo "PASS: $1"
}

fail() {
  observed=$((observed + 1))
  assertions_failed=$((assertions_failed + 1))
  echo "FAIL: $1"
  echo "  WHAT: $2"
  echo "  WHY:  $3"
  echo "  HOW:  $4"
}

setup_failure() {
  setup_failed=$((setup_failed + 1))
  echo "SETUP FAILED: $1"
}

summary_and_exit() {
  echo "observed=$observed assertions_failed=$assertions_failed setup_failed=$setup_failed"
  if [ "$assertions_failed" -eq 0 ] && [ "$setup_failed" -eq 0 ]; then exit 0; fi
  exit 1
}

# Prints the lines of one "@@name" section of a report written by a probe inside the image.
section() {
  awk -v want="@@$1" '/^@@/ { on = ($0 == want); next } on' "$2" 2>/dev/null || true
}

values_of() {
  local key=$1 block=$2 line
  while IFS= read -r line; do
    if [[ $line == "$key="* ]]; then printf '%s\n' "${line#*=}"; fi
  done <<< "$block"
}

value_of() {
  local all
  all="$(values_of "$1" "$2")"
  printf '%s' "${all%%$'\n'*}"
}

contains_line() {
  local wanted=$1 block=$2 line
  while IFS= read -r line; do
    if [ "$line" = "$wanted" ]; then return 0; fi
  done <<< "$block"
  return 1
}

count_lines() {
  local wanted=$1 block=$2 line n=0
  while IFS= read -r line; do
    if [ "$line" = "$wanted" ]; then n=$((n + 1)); fi
  done <<< "$block"
  printf '%s' "$n"
}

# Line number of the first (or, with "last", the last) line equal to WANTED; 0 when there is none.
line_of() {
  local wanted=$1 block=$2 which=${3:-first} line n=0 found=0
  while IFS= read -r line; do
    n=$((n + 1))
    if [ "$line" = "$wanted" ]; then
      found=$n
      if [ "$which" = first ]; then break; fi
    fi
  done <<< "$block"
  printf '%s' "$found"
}

first_of() {
  local line=""
  IFS= read -r line <<< "$1" || true
  printf '%s' "$line"
}

first_line_starting_with() {
  local start=$1 block=$2 line n=0
  while IFS= read -r line; do
    n=$((n + 1))
    if [[ $line == "$start"* ]]; then printf '%s' "$n"; return 0; fi
  done <<< "$block"
  printf '0'
}

first_line_ending_with() {
  local suffix=$1 block=$2 line n=0
  while IFS= read -r line; do
    n=$((n + 1))
    if [[ $line == *"$suffix" ]]; then printf '%s' "$n"; return 0; fi
  done <<< "$block"
  printf '0'
}

# The value that follows FLAG in the argument lines (arg=...) of one recorded osslsigncode call.
arg_after() {
  local flag=$1 block=$2 line take=no
  while IFS= read -r line; do
    if [ "$take" = yes ]; then printf '%s' "${line#arg=}"; return 0; fi
    if [ "$line" = "arg=$flag" ]; then take=yes; fi
  done <<< "$block"
  return 0
}

lower() { printf '%s' "${1,,}"; }

yq_facts() {
  local file=$1 expression=$2
  docker run --rm -i --network none "$yq_image" -r "$expression" < "$file"
}

# ---------------------------------------------------------------- preparation

if ! docker info >/dev/null 2>&1; then
  setup_failure "the Docker daemon is not reachable; start Docker and re-run"
  summary_and_exit
fi
if ! docker image inspect "$yq_image" >/dev/null 2>&1 && ! docker pull -q "$yq_image" >/dev/null 2>&1; then
  setup_failure "cannot pull the YAML reader $yq_image; check the network and re-run"
  summary_and_exit
fi

work_dir="$(mktemp -d)"
chmod 755 "$work_dir"
harness_dir="$work_dir/harness"
stubs_dir="$harness_dir/stubs"
mkdir -p "$stubs_dir"

# Stand-in for sudo. It allows only the two pcscd commands the sudoers line allows and keeps the state
# of a pretend pcscd, so starting a second one fails as the real daemon does and pkill finds nothing to
# stop when none runs. Every call lands in the event log.
cat > "$stubs_dir/sudo" <<'STUB'
#!/bin/bash
rec=/tmp/oracle-record
case "$*" in
  "-n /usr/sbin/pcscd --disable-polkit")
    if [ -e "$rec/pcscd-running" ]; then
      echo "pcscd start refused: another pcscd is running" >> "$rec/events"
      echo "Another pcscd seems to be running" >&2
      exit 1
    fi
    touch "$rec/pcscd-running"
    echo "pcscd started" >> "$rec/events"
    exit 0 ;;
  "-n /usr/bin/pkill -x pcscd")
    if [ -e "$rec/pcscd-running" ]; then
      rm -f "$rec/pcscd-running"
      echo "pcscd stopped" >> "$rec/events"
      exit 0
    fi
    echo "pcscd stop found none running" >> "$rec/events"
    exit 1 ;;
  *)
    echo "sudo refused: $*" >> "$rec/events"
    echo "sudo: a password is required" >&2
    exit 1 ;;
esac
STUB

# pcscd and pkill run without sudo cannot start or stop the root daemon; the calls are only recorded.
cat > "$stubs_dir/pcscd" <<'STUB'
#!/bin/bash
echo "pcscd run without sudo: $*" >> /tmp/oracle-record/events
echo "pcscd: Permission denied" >&2
exit 1
STUB
cat > "$stubs_dir/pkill" <<'STUB'
#!/bin/bash
echo "pkill run without sudo: $*" >> /tmp/oracle-record/events
exit 1
STUB

# Stand-in for opensc-tool: lists the YubiKey reader only while the pretend pcscd runs and the key is
# plugged in; otherwise it answers as the real tool does, with no reader and exit 0.
cat > "$stubs_dir/opensc-tool" <<'STUB'
#!/bin/bash
rec=/tmp/oracle-record
listing=no
for a in "$@"; do
  case "$a" in -l|--list-readers) listing=yes ;; esac
done
if [ "$listing" = yes ] && [ -e "$rec/pcscd-running" ] && [ -e "$rec/reader-plugged" ]; then
  echo "opensc-tool $*: reader listed" >> "$rec/events"
  printf '# Detected readers (pcsc)\nNr.  Card  Features  Name\n0    Yes             Yubico YubiKey OTP+FIDO+CCID 00 00\n'
  exit 0
fi
echo "opensc-tool $*: no reader" >> "$rec/events"
echo "No smart card readers found."
exit 0
STUB

# Stand-in for osslsigncode: records each call's arguments, whether its stdin was exactly the PIN (never
# the PIN itself), the provider module setting it inherits and whether the -ac file is readable. It fails
# on the call number named in fail-call, and otherwise writes the -out file as a signed copy of -in.
cat > "$stubs_dir/osslsigncode" <<'STUB'
#!/bin/bash
rec=/tmp/oracle-record
n=$(( $(cat "$rec/osslsigncode-count" 2>/dev/null || echo 0) + 1 ))
echo "$n" > "$rec/osslsigncode-count"
echo "osslsigncode call $n" >> "$rec/events"
in="" out="" ac="" previous=""
for a in "$@"; do
  case "$previous" in
    -in) in="$a" ;;
    -out) out="$a" ;;
    -ac) ac="$a" ;;
  esac
  previous="$a"
done
stdin="$(cat; echo x)"
stdin="${stdin%x}"
pin="$(cat "$rec/expected-pin")"
{
  for a in "$@"; do echo "arg=$a"; done
  echo "module_env=${PKCS11_PROVIDER_MODULE-unset}"
  echo "openssl_conf_env=${OPENSSL_CONF-unset}"
  if [ "$stdin" = "$pin" ] || [ "$stdin" = "$pin"$'\n' ]; then echo "stdin=the PIN"; else echo "stdin=not the PIN (${#stdin} bytes)"; fi
  if [ -n "$ac" ] && [ -r "$ac" ]; then echo "ac_file=readable"; else echo "ac_file=missing"; fi
} > "$rec/osslsigncode-$n"
if [ "$(cat "$rec/fail-call" 2>/dev/null)" = "$n" ]; then
  echo "Failed" >&2
  exit 1
fi
if [ -z "$in" ] || [ -z "$out" ] || [ ! -f "$in" ]; then
  echo "Failed: no readable -in or no -out" >&2
  exit 1
fi
printf 'signed %s' "$(cat "$in")" > "$out"
echo "Succeeded"
exit 0
STUB

# Runs the checkout's sign script inside the image the way the Sign on Linux step does, with the
# stand-ins first on PATH, and prints a sectioned report on stdout. The report is written by the docker
# client, so the host can read it whatever uid the container user has.
cat > "$harness_dir/run-case.sh" <<'HARNESS'
#!/bin/bash
set -u
key=no reader=no stale=no fail_call=0
for setting in "$@"; do
  case "$setting" in
    key=*) key="${setting#key=}" ;;
    reader=*) reader="${setting#reader=}" ;;
    stale=*) stale="${setting#stale=}" ;;
    fail_call=*) fail_call="${setting#fail_call=}" ;;
  esac
done
rec=/tmp/oracle-record
work=/tmp/oracle-work
sysfs=/tmp/oracle-usb
mkdir -p "$rec" "$work/artifacts/server" "$work/artifacts/standalone" "$work/tools" "$sysfs/usb1"
: > "$rec/events"
cp -R /codesign "$work/tools/codesign"
printf 'unsigned server exe' > "$work/artifacts/server/Lighthouse.exe"
printf 'unsigned NSIS installer' > "$work/artifacts/standalone/Lighthouse-setup.exe"
printf 'unsigned MSI' > "$work/artifacts/standalone/Lighthouse.msi"
# Every real USB tree has root hubs; only a device with vendor 1050 is a YubiKey.
printf '1d6b\n' > "$sysfs/usb1/idVendor"
if [ "$key" = yes ]; then
  mkdir -p "$sysfs/1-2"
  printf '1050\n' > "$sysfs/1-2/idVendor"
  printf '0407\n' > "$sysfs/1-2/idProduct"
fi
if [ "$reader" = yes ]; then touch "$rec/reader-plugged"; fi
if [ "$stale" = yes ]; then touch "$rec/pcscd-running"; echo "a pcscd from an earlier run is running" >> "$rec/events"; fi
echo "$fail_call" > "$rec/fail-call"
printf '%s' "$YUBI_PIN" > "$rec/expected-pin"
cd "$work" || exit 1
started=$(date +%s%N)
env -u PKCS11_PROVIDER_MODULE PATH="/harness/stubs:$PATH" CODESIGN_USB_SYSFS="$sysfs" CODESIGN_READER_WAIT=2 \
  timeout 30 bash tools/codesign/sign-windows.sh 9.9.9 > "$rec/output" 2>&1 < /dev/null
rc=$?
ended=$(date +%s%N)
echo "@@exit"; echo "$rc"
echo "@@millis"; echo $(( (ended - started) / 1000000 ))
echo "@@events"; cat "$rec/events"
echo "@@pcscd_running_at_end"; if [ -e "$rec/pcscd-running" ]; then echo yes; else echo no; fi
n=1
while [ -f "$rec/osslsigncode-$n" ]; do
  echo "@@osslsigncode-$n"; cat "$rec/osslsigncode-$n"
  n=$((n + 1))
done
echo "@@artifacts"
find artifacts -type f | LC_ALL=C sort | while IFS= read -r f; do echo "$f=$(cat "$f")"; done
echo "@@output"; cat "$rec/output"
HARNESS

# Reads, without stand-ins, the tools and settings the sign step relies on in the image.
cat > "$harness_dir/probe-tools.sh" <<'PROBE'
#!/bin/bash
echo "@@help"
osslsigncode sign --help 2>&1
osslsigncode --help 2>&1
echo "@@providers"
OPENSSL_CONF=/etc/ssl/openssl-pkcs11.cnf PKCS11_PROVIDER_MODULE=/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so openssl list -providers -provider pkcs11 2>&1
echo "@@openssl_conf"
if [ -r /etc/ssl/openssl-pkcs11.cnf ]; then echo "file=present"; else echo "file=missing"; fi
if grep -qsE '^[[:space:]]*pkcs11-module-quirks[[:space:]]*=.*(^|[[:space:]=])no-deinit([[:space:]]|$)' /etc/ssl/openssl-pkcs11.cnf; then echo "no_deinit=yes"; else echo "no_deinit=no"; fi
echo "@@sudo"
probe() { local label=$1; shift; if sudo -n -l "$@" >/dev/null 2>&1; then echo "$label=allowed"; else echo "$label=refused"; fi; }
probe start /usr/sbin/pcscd --disable-polkit
probe stop /usr/bin/pkill -x pcscd
probe pcscd_other_arguments /usr/sbin/pcscd --foreground
probe pcscd_no_arguments /usr/sbin/pcscd
probe pkill_other_process /usr/bin/pkill -x sshd
probe pkill_by_pattern /usr/bin/pkill pcscd
probe shell /bin/sh
probe id /usr/bin/id
echo "@@tools"
if command -v opensc-tool >/dev/null 2>&1; then echo "opensc-tool=present"; else echo "opensc-tool=missing"; fi
if [ -r /usr/lib/x86_64-linux-gnu/opensc-pkcs11.so ]; then echo "opensc-module=present"; else echo "opensc-module=missing"; fi
if [ -x /usr/sbin/pcscd ]; then echo "pcscd=present"; else echo "pcscd=missing"; fi
if [ -x /usr/bin/pkill ]; then echo "pkill=present"; else echo "pkill=missing"; fi
if grep -rqsE '^[[:space:]]*DEVICENAME[[:space:]]+usb:1050/0407([[:space:]]|$)' /etc/reader.conf.d; then echo "reader-entry=present"; else echo "reader-entry=missing"; fi
PROBE

cat > "$harness_dir/probe-files.sh" <<'PROBE'
#!/bin/bash
echo "@@engines"
find /usr /opt /etc -path '*engines-*' -name '*pkcs11*' 2>/dev/null
echo "@@sudoers"
for f in /etc/sudoers.d/*; do
  if [ -f "$f" ] && grep -q pcscd "$f"; then
    echo "file=$f"
    echo "owner=$(stat -c '%a %U:%G' "$f")"
    grep -vE '^[[:space:]]*(#|$)' "$f" | sed 's/^/rule=/'
  fi
done
PROBE

# Signs a tiny PE file with the image's own osslsigncode and pkcs11 provider, with SoftHSM holding the
# key in place of the YubiKey, then verifies it; then signs again with the provider's module setting
# unset, which must fail rather than fall back to an engine.
cat > "$harness_dir/probe-real-signing.sh" <<'PROBE'
#!/bin/bash
set -u
le() { local v=$1 n=$2 i; for ((i = 0; i < n; i++)); do printf "\\x$(printf %02x $(((v >> (8 * i)) & 255)))"; done; }
zeros() { head -c "$1" /dev/zero; }
# A minimal 32-bit Windows executable: DOS header, PE header, one .text section holding a single ret.
tiny_pe() {
  printf 'MZ'; zeros 58; le 64 4
  printf 'PE\0\0'; le 0x14c 2; le 1 2; le 0 4; le 0 4; le 0 4; le 0xE0 2; le 0x102 2
  le 0x10b 2; le 0 2; le 0x200 4; le 0 4; le 0 4; le 0x1000 4; le 0x1000 4; le 0 4
  le 0x400000 4; le 0x1000 4; le 0x200 4; le 4 2; le 0 2; le 0 2; le 0 2; le 4 2; le 0 2; le 0 4
  le 0x2000 4; le 0x200 4; le 0 4; le 2 2; le 0 2; le 0x100000 4; le 0x1000 4; le 0x100000 4; le 0x1000 4; le 0 4; le 16 4
  zeros 128
  printf '.text\0\0\0'; le 0x200 4; le 0x1000 4; le 0x200 4; le 0x200 4; le 0 4; le 0 4; le 0 2; le 0 2; le 0x60000020 4
  zeros $((0x200 - 0x160))
  printf '\xc3'; zeros 511
}
setup_failed() { echo "@@setup"; echo "failed: $1"; exit 0; }
mkdir -p /tmp/real/tokens && cd /tmp/real || setup_failed "no work directory"
module=/usr/lib/softhsm/libsofthsm2.so
pin=4711
export SOFTHSM2_CONF=/tmp/real/softhsm2.conf
printf 'directories.tokendir = /tmp/real/tokens\nobjectstore.backend = file\n' > softhsm2.conf
tiny_pe > tiny.exe
softhsm2-util --init-token --free --label oracle --pin "$pin" --so-pin 5678 >/dev/null 2>&1 || setup_failed "SoftHSM token init"
openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:prime256v1 -nodes -keyout key.pem -out cert.pem -days 2 \
  -subj /CN=oracle-signing -addext extendedKeyUsage=codeSigning >/dev/null 2>&1 || setup_failed "test certificate"
openssl pkcs8 -topk8 -nocrypt -in key.pem -out key8.pem >/dev/null 2>&1 || setup_failed "key conversion"
softhsm2-util --import key8.pem --token oracle --label signing --id 01 --pin "$pin" >/dev/null 2>&1 || setup_failed "key import"
openssl x509 -in cert.pem -outform DER -out cert.der >/dev/null 2>&1 || setup_failed "certificate conversion"
pkcs11-tool --module "$module" --login --pin "$pin" --write-object cert.der --type cert --id 01 --label signing >/dev/null 2>&1 \
  || setup_failed "certificate import"
echo "@@setup"; echo ok
# Signing uses the image's own OpenSSL config for the sign step; only the module is SoftHSM's.
export OPENSSL_CONF=/etc/ssl/openssl-pkcs11.cnf
sign() {
  printf '%s' "$pin" | osslsigncode sign -provider pkcs11 -pkcs11module "$module" \
    -key 'pkcs11:id=%01;type=private' -pkcs11cert 'pkcs11:id=%01;type=cert' -readpass - -in tiny.exe -out "$1"
}
PKCS11_PROVIDER_MODULE="$module" sign signed.exe > sign.log 2>&1
rc=$?
echo "@@with_module"; echo "exit=$rc"; if [ -s signed.exe ]; then echo "written=yes"; else echo "written=no"; fi
osslsigncode verify -CAfile cert.pem -in signed.exe > verify.log 2>&1
rc=$?
echo "@@verify"; echo "exit=$rc"
env -u PKCS11_PROVIDER_MODULE bash -c "$(declare -f sign); pin=$pin module=$module; sign unset.exe" > unset.log 2>&1
rc=$?
echo "@@without_module"; echo "exit=$rc"; if [ -e unset.exe ]; then echo "written=yes"; else echo "written=no"; fi
echo "@@logs"; tail -n 3 sign.log verify.log unset.log
PROBE
chmod 755 "$harness_dir" "$stubs_dir" "$stubs_dir"/* "$harness_dir"/*.sh

# ---------------------------------------------------------------- the image

image_built=false
if [ -n "${IMAGE:-}" ]; then
  image="$IMAGE"
  if ! docker image inspect "$image" >/dev/null 2>&1 && ! docker pull -q "$image" >/dev/null 2>&1; then
    setup_failure "cannot pull IMAGE=$image; check the tag and the network"
    summary_and_exit
  fi
  image_built=true
elif [ ! -f "$dockerfile" ]; then
  setup_failure "tools/codesign/Dockerfile does not exist, so there is no runner image to sign in"
  summary_and_exit
else
  image="lighthouse-codesign-runner:oracle-sign"
  if docker build --platform linux/amd64 -t "$image" -f "$dockerfile" "$codesign_dir" > "$work_dir/build.log" 2>&1; then
    image_built=true
  else
    fail "the runner image builds with the signing tools" \
      "docker build failed; the last lines of its output follow" \
      "the sign step runs in this image; without it no release is signed" \
      "fix tools/codesign/Dockerfile until docker build succeeds"
    tail -n 20 "$work_dir/build.log"
  fi
fi

run_probe() {
  local label=$1 user=$2 target=$3
  shift 3
  local name="$prefix-$label"
  PROBE_REPORT="$work_dir/report-$label"
  created_containers+=("$name")
  timeout "$case_limit_seconds" docker run --name "$name" --network none --user "$user" --entrypoint bash \
    -e YUBI_PIN="$sentinel_pin" -v "$codesign_dir:/codesign:ro" -v "$harness_dir:/harness:ro" \
    "$target" "$@" > "$PROBE_REPORT" 2>&1 || true
  docker rm -f "$name" >/dev/null 2>&1 || true
}

# ---------------------------------------------------------------- the sign step with stand-ins

case_labels=(no-key no-reader signs-all fails-second stale-pcscd)
if [ "$image_built" = true ]; then
  run_probe no-key runner "$image" /harness/run-case.sh key=no reader=no
  run_probe no-reader runner "$image" /harness/run-case.sh key=yes reader=no
  run_probe signs-all runner "$image" /harness/run-case.sh key=yes reader=yes
  run_probe fails-second runner "$image" /harness/run-case.sh key=yes reader=yes fail_call=2
  run_probe stale-pcscd runner "$image" /harness/run-case.sh key=yes reader=yes stale=yes
fi

report_of() { printf '%s' "$work_dir/report-$1"; }
exit_of() { first_of "$(section exit "$(report_of "$1")")"; }
millis_of() { first_of "$(section millis "$(report_of "$1")")"; }
events_of() { section events "$(report_of "$1")"; }
output_of() { section output "$(report_of "$1")"; }
calls_of() {
  local events line n=0
  events="$(events_of "$1")"
  while IFS= read -r line; do
    if [[ $line == "osslsigncode call "* ]]; then n=$((n + 1)); fi
  done <<< "$events"
  printf '%s' "$n"
}
started_of() { count_lines "pcscd started" "$(events_of "$1")"; }
script_missing_note() {
  if [ ! -f "$sign_script" ]; then printf ' (tools/codesign/sign-windows.sh does not exist)'; fi
}

# pcscd counts as released when it was started, a stop followed the last start and none runs at the end.
pcscd_released() {
  local events last_start last_stop running
  events="$(events_of "$1")"
  last_start="$(line_of "pcscd started" "$events" last)"
  last_stop="$(line_of "pcscd stopped" "$events" last)"
  running="$(first_of "$(section pcscd_running_at_end "$(report_of "$1")")")"
  [ "$last_start" -gt 0 ] && [ "$last_stop" -gt "$last_start" ] && [ "$running" = no ]
}

released_detail() {
  local events running
  events="$(events_of "$1")"
  running="$(first_of "$(section pcscd_running_at_end "$(report_of "$1")")")"
  printf 'pcscd events: [%s]; pcscd running when the step ended: %s' "${events//$'\n'/; }" "${running:-unknown}"
}

check_released() {
  local label=$1 situation=$2
  local name="the key is released when the step ends $situation"
  if [ "$image_built" != true ]; then
    fail "$name" "there is no image to run the step in" "pcscd left running holds the YubiKey" "make the image build first"
  elif pcscd_released "$label"; then
    pass "$name"
  else
    fail "$name" "$(released_detail "$label")$(script_missing_note)" \
      "a pcscd left holding the key blocks the next release run" \
      "stop pcscd with sudo -n /usr/bin/pkill -x pcscd from a trap on EXIT, INT and TERM"
  fi
}

# No key plugged in.
name="with no YubiKey plugged in the step stops at once with the plug-in message"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "the maintainer must learn at once that the key is missing" "make the image build first"
else
  rc="$(exit_of no-key)"
  ms="$(millis_of no-key)"
  out="$(output_of no-key)"
  events="$(events_of no-key)"
  if [ "$rc" = 1 ] && [ -n "$ms" ] && [ "$ms" -le 5000 ] && [[ $(lower "$out") == *"$plug_in_message_lower"* ]] \
     && [ "$(started_of no-key)" = 0 ] && [ "$(calls_of no-key)" = 0 ]; then
    pass "$name"
  else
    fail "$name" \
      "exit ${rc:-none} (want 1) after ${ms:-?} ms (want at most 5000); plug-in message shown: $(if [[ $(lower "$out") == *"$plug_in_message_lower"* ]]; then echo yes; else echo no; fi); pcscd starts: $(started_of no-key); signing calls: $(calls_of no-key)$(script_missing_note); output: ${out:0:300}" \
      "without the key nothing can be signed, and a PIN sent to no key, or a wait, only delays the maintainer" \
      "look for a USB device with idVendor 1050 under CODESIGN_USB_SYSFS (default /sys/bus/usb/devices) first; if none, print '$plug_in_message' and exit 1 before starting pcscd"
  fi
fi

# Key on USB but no reader appears.
name="with the key on USB but no reader the step gives up within its wait and sends no PIN"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "an unbounded wait holds the release job" "make the image build first"
else
  rc="$(exit_of no-reader)"
  ms="$(millis_of no-reader)"
  out="$(lower "$(output_of no-reader)")"
  if [ "$rc" = 1 ] && [ -n "$ms" ] && [ "$ms" -le 7000 ] && [[ $out == *"$plug_in_message_lower"* ]] && [[ $out == *"check-host.sh"* ]] \
     && [ "$(started_of no-reader)" -gt 0 ] && [ "$(calls_of no-reader)" = 0 ]; then
    pass "$name"
  else
    fail "$name" \
      "exit ${rc:-none} (want 1) after ${ms:-?} ms with CODESIGN_READER_WAIT=2 (want at most 7000); plug-in message: $(if [[ $out == *"$plug_in_message_lower"* ]]; then echo yes; else echo no; fi); check-host.sh hint: $(if [[ $out == *"check-host.sh"* ]]; then echo yes; else echo no; fi); pcscd starts: $(started_of no-reader); signing calls: $(calls_of no-reader)$(script_missing_note)" \
      "a PIN sent with no reader is lost on nothing or worse, and a host process holding the key is a known cause the maintainer can check" \
      "start pcscd, poll opensc-tool --list-readers for at most CODESIGN_READER_WAIT seconds, then print the plug-in message plus a hint to run tools/codesign/check-host.sh and exit 1"
  fi
fi
check_released no-reader "after no reader appeared"

# Key and reader present: all three files signed.
expected_files="server/Lighthouse.exe standalone/Lighthouse-setup.exe standalone/Lighthouse.msi"
name="with the key plugged in the step signs the server exe, the NSIS installer and the MSI once each in place"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "a release needs all three files signed" "make the image build first"
else
  rc="$(exit_of signs-all)"
  calls="$(calls_of signs-all)"
  signed_inputs=""
  for ((i = 1; i <= calls; i++)); do
    input="$(arg_after -in "$(section "osslsigncode-$i" "$(report_of signs-all)")")"
    signed_inputs+="${input##*artifacts/} "
  done
  sorted_inputs="$(printf '%s\n' "$signed_inputs" | tr ' ' '\n' | sed '/^$/d' | LC_ALL=C sort | tr '\n' ' ')"
  artifacts="$(section artifacts "$(report_of signs-all)")"
  wanted_artifacts=$'artifacts/server/Lighthouse.exe=signed unsigned server exe\nartifacts/standalone/Lighthouse-setup.exe=signed unsigned NSIS installer\nartifacts/standalone/Lighthouse.msi=signed unsigned MSI'
  if [ "$rc" = 0 ] && [ "$calls" = 3 ] && [ "$sorted_inputs" = "$expected_files " ] && [ "$artifacts" = "$wanted_artifacts" ]; then
    pass "$name"
  else
    fail "$name" \
      "exit ${rc:-none} (want 0); signing calls: $calls (want 3); files signed: [${sorted_inputs% }] (want [$expected_files]); artifacts afterwards: [${artifacts//$'\n'/; }]$(script_missing_note)" \
      "Verify signing coverage and the upload expect each file signed under its own name; a file signed twice or missed breaks the release" \
      "sign ./artifacts/server/Lighthouse*.exe, ./artifacts/standalone/Lighthouse*.exe and ./artifacts/standalone/*.msi once each to FILE.signed, then move it over FILE"
  fi
fi

name="every signing call uses the pkcs11 provider with the PIN on stdin"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "the engine path is retired" "make the image build first"
else
  calls="$(calls_of signs-all)"
  problems=""
  for ((i = 1; i <= calls; i++)); do
    call="$(section "osslsigncode-$i" "$(report_of signs-all)")"
    [ "$(arg_after -provider "$call")" = pkcs11 ] || problems+="call $i: -provider is not pkcs11; "
    [ "$(arg_after -readpass "$call")" = - ] || problems+="call $i: -readpass is not -; "
    if contains_line "arg=-pkcs11engine" "$call"; then problems+="call $i: has -pkcs11engine; "; fi
    if contains_line "arg=-pass" "$call"; then problems+="call $i: has -pass; "; fi
    [ "$(arg_after -pkcs11module "$call")" = "$opensc_module" ] || problems+="call $i: -pkcs11module is not $opensc_module; "
    [ "$(arg_after -key "$call")" = "pkcs11:id=%01;type=private" ] || problems+="call $i: -key is not pkcs11:id=%01;type=private; "
    [ "$(arg_after -pkcs11cert "$call")" = "pkcs11:id=%01;type=cert" ] || problems+="call $i: -pkcs11cert is not pkcs11:id=%01;type=cert; "
    [[ $(arg_after -ac "$call") == *SSL_COM_CODE_SIGNING_INTERMEDIATE_CA_ECC_R2.crt ]] || problems+="call $i: -ac is not the SSL.com intermediate; "
    [ "$(value_of ac_file "$call")" = readable ] || problems+="call $i: the -ac file is not readable; "
    [ "$(arg_after -n "$call")" = "Lighthouse $version" ] || problems+="call $i: -n is not 'Lighthouse $version'; "
    [ "$(arg_after -i "$call")" = "https://letpeople.work/lighthouse" ] || problems+="call $i: -i is not https://letpeople.work/lighthouse; "
    [ "$(arg_after -t "$call")" = "http://timestamp.digicert.com" ] || problems+="call $i: -t is not http://timestamp.digicert.com; "
    [ "$(value_of stdin "$call")" = "the PIN" ] || problems+="call $i: stdin was $(value_of stdin "$call"); "
    [ "$(value_of module_env "$call")" = "$opensc_module" ] || problems+="call $i: PKCS11_PROVIDER_MODULE is $(value_of module_env "$call"); "
    [ "$(value_of openssl_conf_env "$call")" = "$sign_openssl_conf" ] || problems+="call $i: OPENSSL_CONF is $(value_of openssl_conf_env "$call"); "
  done
  if [ "$calls" -gt 0 ] && [ -z "$problems" ]; then
    pass "$name"
  else
    fail "$name" "${problems:-no signing call was made}$(script_missing_note)" \
      "the image carries no engine, the provider reads PKCS11_PROVIDER_MODULE, and a PIN on the command line is visible to every process" \
      "call osslsigncode sign -provider pkcs11 -pkcs11module $opensc_module -key 'pkcs11:id=%01;type=private' -pkcs11cert 'pkcs11:id=%01;type=cert' -ac the intermediate -readpass - with YUBI_PIN on stdin, and export PKCS11_PROVIDER_MODULE=$opensc_module and OPENSSL_CONF=$sign_openssl_conf in the script"
  fi
fi

name="no PIN is sent before a reader is listed"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "a PIN sent blind burns PIV retries" "make the image build first"
else
  events="$(events_of signs-all)"
  listed="$(first_line_ending_with ": reader listed" "$events")"
  first_call="$(line_of "osslsigncode call 1" "$events")"
  if [ "$listed" -gt 0 ] && [ "$first_call" -gt "$listed" ]; then
    pass "$name"
  else
    fail "$name" "events in order: [${events//$'\n'/; }]$(script_missing_note)" \
      "osslsigncode reads the PIN at once; with no reader it is sent to nothing and a wrong path counts down the PIV retries" \
      "sign only after opensc-tool --list-readers listed a reader"
  fi
fi
check_released signs-all "after signing all files"

# The second file fails.
name="when signing one file fails the step stops with that file named and retries nothing"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "a retried wrong PIN counts down the PIV retry counter" "make the image build first"
else
  rc="$(exit_of fails-second)"
  calls="$(calls_of fails-second)"
  first_in="$(arg_after -in "$(section osslsigncode-1 "$(report_of fails-second)")")"
  second_in="$(arg_after -in "$(section osslsigncode-2 "$(report_of fails-second)")")"
  out="$(output_of fails-second)"
  if [ -n "$rc" ] && [ "$rc" != 0 ] && [ "$calls" = 2 ] && [ -n "$second_in" ] && [ "$second_in" != "$first_in" ] \
     && [[ $out == *"$(basename "$second_in")"* ]]; then
    pass "$name"
  else
    fail "$name" \
      "exit ${rc:-none} (want non-zero); signing calls: $calls (want 2, the failed one last); first -in '$first_in', second -in '$second_in'; output names the failed file: $(if [ -n "$second_in" ] && [[ $out == *"$(basename "$second_in")"* ]]; then echo yes; else echo no; fi)$(script_missing_note)" \
      "a wrong PIN tried again locks the key, and a success with fewer than three signed files ships an unsigned binary" \
      "stop at the first failed file, print its name and exit non-zero; never call osslsigncode again for that file or later ones"
  fi
fi
check_released fails-second "after a failed signing"

# A pcscd left from an earlier killed run.
name="a pcscd left from an earlier run is stopped before the step starts its own"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "a stale pcscd keeps the new one from starting" "make the image build first"
else
  events="$(events_of stale-pcscd)"
  first_stop="$(line_of "pcscd stopped" "$events")"
  first_start="$(line_of "pcscd started" "$events")"
  rc="$(exit_of stale-pcscd)"
  if [ "$first_stop" -gt 0 ] && [ "$first_start" -gt "$first_stop" ] && [ "$rc" = 0 ] && [ "$(calls_of stale-pcscd)" = 3 ]; then
    pass "$name"
  else
    fail "$name" "events in order: [${events//$'\n'/; }]; exit ${rc:-none} (want 0); signing calls: $(calls_of stale-pcscd) (want 3)$(script_missing_note)" \
      "a container killed mid-run skips its trap, and the next run must not fail on the daemon it left behind" \
      "run sudo -n /usr/bin/pkill -x pcscd, ignoring its failure, before sudo -n /usr/sbin/pcscd --disable-polkit"
  fi
fi

name="the PIN never shows in the step's output or on any command line it runs"
if [ "$image_built" != true ]; then
  fail "$name" "there is no image to run the step in" "a PIN in the job log is public to every reader" "make the image build first"
else
  leaks=""
  # A step that never handed the PIN to a signing call cannot show whether it would leak it.
  if [ "$(calls_of signs-all)" = 0 ]; then leaks+="no case reached a signing call, so the PIN was never in use; "; fi
  for label in "${case_labels[@]}"; do
    report="$(report_of "$label")"
    if [ ! -s "$report" ]; then
      leaks+="$label: no report was written; "
    elif grep -qF -- "$sentinel_pin" "$report"; then
      leaks+="$label: the PIN appears in the output or in a recorded command line; "
    fi
  done
  if [ -z "$leaks" ]; then
    pass "$name"
  else
    fail "$name" "$leaks" "the job log and the process list are readable by more than the maintainer" \
      "pass YUBI_PIN only on stdin of osslsigncode; never echo it, never set -x, never put it in an argument"
  fi
fi

# ---------------------------------------------------------------- the image's own tools

if [ "$image_built" = true ]; then
  run_probe tools runner "$image" /harness/probe-tools.sh
  run_probe files 0 "$image" /harness/probe-files.sh
fi

name="the image's osslsigncode can sign through a provider"
help="$(section help "$(report_of tools)")"
if [ "$image_built" = true ] && [[ $help == *"-provider"* ]]; then
  pass "$name"
else
  fail "$name" "osslsigncode help does not list -provider: ${help:0:200}" \
    "osslsigncode before 2.10, such as Debian's 2.9, has no provider option and cannot use the pkcs11 provider" \
    "install Ubuntu 26.04's osslsigncode package in tools/codesign/Dockerfile"
fi

name="the pkcs11 provider loads in the image"
providers="$(section providers "$(report_of tools)")"
provider_status="$(awk '/^[[:space:]]+pkcs11[[:space:]]*$/ { inside = 1; next } /^[[:space:]]{2}[^[:space:]]/ { inside = 0 } inside && /status:/ { print $2; exit }' <<< "$providers" 2>/dev/null || true)"
if [ "$image_built" = true ] && [ "$provider_status" = active ]; then
  pass "$name"
else
  fail "$name" "openssl list -providers -provider pkcs11 shows status '${provider_status:-none}': ${providers:0:300}" \
    "osslsigncode falls back to an engine when the provider does not load, and the image has none, so no file gets signed" \
    "install pkcs11-provider in tools/codesign/Dockerfile"
fi

name="the image carries no pkcs11 engine"
engines="$(section engines "$(report_of files)")"
files_report_ok=no
if [ "$image_built" = true ] && grep -q '^@@engines$' "$(report_of files)" 2>/dev/null; then files_report_ok=yes; fi
if [ "$files_report_ok" = yes ] && [ -z "$engines" ]; then
  pass "$name"
else
  fail "$name" "pkcs11 engine files found: [${engines//$'\n'/; }]; image probed: $files_report_ok" \
    "with an engine present osslsigncode signs through it silently when the provider fails, which is the path this change retires" \
    "do not install libengine-pkcs11-openssl or anything that brings an OpenSSL pkcs11 engine"
fi

name="sudo lets the runner start and stop pcscd and nothing else"
sudo_facts="$(section sudo "$(report_of tools)")"
sudoers="$(section sudoers "$(report_of files)")"
sudo_problems=""
[ "$(value_of start "$sudo_facts")" = allowed ] || sudo_problems+="starting pcscd --disable-polkit is not allowed; "
[ "$(value_of stop "$sudo_facts")" = allowed ] || sudo_problems+="pkill -x pcscd is not allowed; "
for probe_name in pcscd_other_arguments pcscd_no_arguments pkill_other_process pkill_by_pattern shell id; do
  [ "$(value_of "$probe_name" "$sudo_facts")" = refused ] || sudo_problems+="$probe_name is not refused; "
done
sudoers_files="$(values_of file "$sudoers")"
[ -n "$sudoers_files" ] || sudo_problems+="no file under /etc/sudoers.d names pcscd; "
while IFS= read -r owner; do
  if [ -n "$owner" ] && [ "$owner" != "440 root:root" ]; then sudo_problems+="a pcscd sudoers file has mode and owner '$owner' (want 440 root:root); "; fi
done <<< "$(values_of owner "$sudoers")"
while IFS= read -r rule; do
  commands="${rule##*:}"
  if [[ $rule == *"*"* ]] || [[ $commands =~ (^|[[:space:],])ALL($|[[:space:],]) ]]; then sudo_problems+="rule '$rule' has a wildcard or an ALL command; "; fi
done <<< "$(values_of rule "$sudoers")"
if [ "$image_built" = true ] && [ -z "$sudo_problems" ]; then
  pass "$name"
else
  fail "$name" "${sudo_problems:-there is no image to probe}" \
    "the job runs code from the repository as the runner user; any wider sudo right makes it root" \
    "install tools/codesign/sudoers-pcscd as 0440 root:root with exactly: runner ALL=(root) NOPASSWD: /usr/sbin/pcscd --disable-polkit, /usr/bin/pkill -x pcscd"
fi

name="the image carries the reader entry and the tools the sign step calls"
tools="$(section tools "$(report_of tools)")"
tool_problems=""
for tool in opensc-tool opensc-module pcscd pkill reader-entry; do
  [ "$(value_of "$tool" "$tools")" = present ] || tool_problems+="$tool missing; "
done
if [ "$image_built" = true ] && [ -z "$tool_problems" ]; then
  pass "$name"
else
  fail "$name" "${tool_problems:-there is no image to probe}" \
    "the sign script from the checkout runs in whatever image the machine pulled, so the image must carry what it calls" \
    "install opensc, opensc-pkcs11, pcscd and libccid, and the reader entry for usb:1050/0407 under /etc/reader.conf.d"
fi

name="the image ships the sign step's own OpenSSL config and leaves the stock one to other jobs"
conf_facts="$(section openssl_conf "$(report_of tools)")"
image_env=""
if [ "$image_built" = true ]; then
  image_env="$(docker image inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$image" 2>/dev/null || true)"
fi
conf_problems=""
[ "$(value_of file "$conf_facts")" = present ] || conf_problems+="$sign_openssl_conf is missing; "
[ "$(value_of no_deinit "$conf_facts")" = yes ] || conf_problems+="$sign_openssl_conf sets no pkcs11-module-quirks with no-deinit; "
if [ -n "$(values_of OPENSSL_CONF "$image_env")" ]; then conf_problems+="the image ENV sets OPENSSL_CONF=$(value_of OPENSSL_CONF "$image_env"); "; fi
if [ "$image_built" = true ] && [ -z "$conf_problems" ]; then
  pass "$name"
else
  fail "$name" "${conf_problems:-there is no image to probe}" \
    "the provider crashes osslsigncode at exit unless it skips finalizing the module, and other jobs on this runner must keep OpenSSL's stock config" \
    "ship tools/codesign/openssl-pkcs11.cnf as $sign_openssl_conf, including the system config and declaring the pkcs11 provider with pkcs11-module-quirks = no-deinit; set OPENSSL_CONF only inside sign-windows.sh, never as image ENV"
fi

# ---------------------------------------------------------------- a real signature with SoftHSM

real_checks=("the image's osslsigncode makes a signature through the pkcs11 provider that verifies against the signing certificate"
  "without the provider's module setting signing fails and writes no file")
softhsm_ready=false
if [ "$image_built" = true ]; then
  # SoftHSM stands in for the YubiKey. Its p11-kit registration is removed so that, with the provider's
  # module setting unset, nothing else hands it the token.
  if docker build --build-arg "BASE=$image" -t "$softhsm_image" - > "$work_dir/softhsm-build.log" 2>&1 <<'DOCKERFILE'
ARG BASE
FROM ${BASE}
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends softhsm2 opensc \
 && rm -rf /var/lib/apt/lists/* \
 && rm -f /usr/share/p11-kit/modules/softhsm2.module
USER runner
DOCKERFILE
  then
    softhsm_ready=true
  else
    setup_failure "cannot build the SoftHSM test image from the runner image; the last lines follow"
    tail -n 20 "$work_dir/softhsm-build.log"
  fi
fi

if [ "$softhsm_ready" = true ]; then
  run_probe real runner "$softhsm_image" /harness/probe-real-signing.sh
  real="$(report_of real)"
  real_setup="$(first_of "$(section setup "$real")")"
  if [ "$real_setup" != ok ]; then
    setup_failure "the SoftHSM token could not be prepared: ${real_setup:-no report}"
  else
    with_module="$(section with_module "$real")"
    verify="$(section verify "$real")"
    without_module="$(section without_module "$real")"
    logs="$(section logs "$real")"
    if [ "$(value_of exit "$with_module")" = 0 ] && [ "$(value_of written "$with_module")" = yes ] && [ "$(value_of exit "$verify")" = 0 ]; then
      pass "${real_checks[0]}"
    else
      fail "${real_checks[0]}" \
        "sign exit $(value_of exit "$with_module"), signed file written: $(value_of written "$with_module"), verify exit $(value_of exit "$verify"); log tail: ${logs//$'\n'/ | }" \
        "the release is signed by this binary and this provider; argv checks alone cannot show they produce a valid Authenticode signature" \
        "install Ubuntu 26.04's osslsigncode and pkcs11-provider in tools/codesign/Dockerfile and ship $sign_openssl_conf with pkcs11-module-quirks = no-deinit"
    fi
    if [ "$(value_of exit "$with_module")" = 0 ] && [ "$(value_of exit "$without_module")" != 0 ] && [ "$(value_of written "$without_module")" = no ]; then
      pass "${real_checks[1]}"
    else
      fail "${real_checks[1]}" \
        "with the module setting: sign exit $(value_of exit "$with_module"); without it: sign exit $(value_of exit "$without_module"), file written: $(value_of written "$without_module")" \
        "osslsigncode falls back to an engine when the provider has no module; only a failure here shows that no engine signs instead of the provider" \
        "keep every OpenSSL pkcs11 engine out of the image so signing works only through the provider"
    fi
  fi
else
  for check in "${real_checks[@]}"; do
    fail "$check" "there is no image to derive the SoftHSM test image from" \
      "a real signature through the provider is the only proof short of the YubiKey" "make the image build first"
  done
fi

# ---------------------------------------------------------------- the workflows

name="the release job's Linux sign step runs the sign script with the PIN only in its environment"
if [ ! -f "$sign_workflow" ]; then
  fail "$name" ".github/workflows/ci_codesign-windows.yml does not exist" "the release signs through it" "restore the workflow"
else
  sign_facts="$(yq_facts "$sign_workflow" '
    "step=" + ((.jobs["codesign-windows"].steps // [])[] | (.name // "")),
    ((.jobs["codesign-windows"].steps // [])[] | select(.name == "Sign on Linux") | (
      "linux_if=" + (.["if"] // ""),
      "linux_calls_script=" + ((.run // "") | test("tools/codesign/sign-windows\\.sh[^\n]*inputs\\.version") | tostring),
      "linux_inline_signing=" + ((.run // "") | test("osslsigncode|pkcs11engine|-pass[[:space:]]|YUBI_PIN") | tostring),
      "linux_pin_env=" + (.env.YUBI_PIN // "")))' 2>&1 || true)"
  steps="$(values_of step "$sign_facts")"
  linux_line="$(line_of "Sign on Linux" "$steps")"
  coverage_line="$(line_of "Verify signing coverage" "$steps")"
  if [ "$(value_of linux_if "$sign_facts")" = "runner.os == 'Linux'" ] && [ "$(value_of linux_calls_script "$sign_facts")" = true ] \
     && [ "$(value_of linux_inline_signing "$sign_facts")" = false ] && [ "$(value_of linux_pin_env "$sign_facts")" = "$pin_secret_expression" ] \
     && [ "$linux_line" -gt 0 ] && [ "$coverage_line" -gt "$linux_line" ]; then
    pass "$name"
  else
    fail "$name" \
      "Sign on Linux if '$(value_of linux_if "$sign_facts")', runs tools/codesign/sign-windows.sh with inputs.version: $(value_of linux_calls_script "$sign_facts"), still signs inline or names YUBI_PIN in its run: $(value_of linux_inline_signing "$sign_facts"), YUBI_PIN env '$(value_of linux_pin_env "$sign_facts")'; Verify signing coverage after it: $(if [ "$coverage_line" -gt "$linux_line" ] && [ "$linux_line" -gt 0 ]; then echo yes; else echo no; fi)" \
      "the inline loop signs through the engine with the PIN on the command line; the coverage check must still judge the signed files" \
      "replace the run of Sign on Linux with: bash tools/codesign/sign-windows.sh \"\${{ inputs.version }}\", keep env YUBI_PIN: \${{ secrets.YUBI_PIN }}, and keep Verify signing coverage after it"
  fi
fi

name="the image workflow checks the sign step in the image before it is published and before latest moves"
if [ ! -f "$image_workflow" ]; then
  fail "$name" ".github/workflows/codesign-runner-image.yml does not exist" "nothing checks the image" "create the workflow"
else
  image_facts="$(yq_facts "$image_workflow" '
    "verify_runs_sign_oracle=" + ((.jobs.verify.steps // []) | to_json(0) | test("tools/codesign/tests/test-sign-step\\.sh") | tostring),
    "publish_step=" + ((.jobs.publish.steps // [])[] | (to_json(0) | test("tools/codesign/tests/test-sign-step\\.sh") | tostring) + "," + (to_json(0) | test("(?i)latest") | tostring))' 2>&1 || true)"
  publish_steps="$(values_of publish_step "$image_facts")"
  sign_oracle_line="$(first_line_starting_with "true," "$publish_steps")"
  latest_line="$(first_line_ending_with ",true" "$publish_steps")"
  if [ "$(value_of verify_runs_sign_oracle "$image_facts")" = true ] && [ "$sign_oracle_line" -gt 0 ] && [ "$latest_line" -gt "$sign_oracle_line" ]; then
    pass "$name"
  else
    fail "$name" \
      "verify runs tools/codesign/tests/test-sign-step.sh: $(value_of verify_runs_sign_oracle "$image_facts"); publish runs it on the pushed image at step $sign_oracle_line, latest moves at step $latest_line" \
      "the machine pulls latest and runs the sign script from the checkout in it; an image that cannot run it must never become latest" \
      "run bash tools/codesign/tests/test-sign-step.sh in verify on the built image and in publish on the pushed commit-tag image before latest moves"
  fi
fi

summary_and_exit
