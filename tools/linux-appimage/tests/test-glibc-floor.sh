#!/usr/bin/env bash
# What the Linux packaging run promises the maintainer: it fails, naming the binary and the GLIBC version,
# when the AppImage needs a newer glibc than the written minimum, and it passes on today's build. The
# floor check runs inside a digest-pinned Ubuntu 24.04 container over real libraries from its archive and
# over small ELF files built in Ubuntu 26.04. Needs Docker, bash and network.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd -P)"
tools_dir="$repo_root/tools/linux-appimage"
floor_file="$tools_dir/glibc-floor"
workflow_file="$repo_root/.github/workflows/ci_package-linux-standalone.yml"
docs_file="$repo_root/docs/Installation/standalone.md"
adr_file="$repo_root/docs/product/architecture/adr-284-linux-appimage-builds-in-a-pinned-ubuntu-24-04-container-and-the-glibc-floor-is-2-39.md"

floor_image="ubuntu:24.04@sha256:2198e4c65076e9cb0b3d695219df667c0e3b0072da64ca3c2648aca17dcecaf5"
newer_image="ubuntu:26.04@sha256:065aa8626841f16057b78b027b464cd59c4fac36c204c72dcbf48d66f5fa8a96"
written_floor="2.39"

observed=0
assertions_failed=0
setup_failed=0
prefix="lh-glibc-floor-oracle-$$"
work_dir=""
check_container=""
check_output=""
check_exit=0
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

has_text() {
  [[ $2 == *"$1"* ]]
}

count_lines_with() {
  local wanted=$1 block=$2 line count=0
  while IFS= read -r line; do
    if [[ $line == *"$wanted"* ]]; then count=$((count + 1)); fi
  done <<< "$block"
  echo "$count"
}

line_number_of() {
  local wanted=$1 file=$2 found
  found="$(grep -n -m1 -F -- "$wanted" "$file" 2>/dev/null)" || true
  echo "${found%%:*}"
}

# Runs the floor check inside the pinned container; sets check_exit and check_output (both streams).
run_check() {
  check_output="$(docker exec "$check_container" bash /tools/check-glibc-floor.sh "$@" 2>&1)"
  check_exit=$?
}

shown_output() {
  printf '%s' "$check_output" | head -n 12
}

expect_exit() {
  local label=$1 expected=$2 why=$3 how=$4
  if [ "$check_exit" -eq "$expected" ]; then
    pass "$label: exit $expected"
  else
    fail "$label" "exit code was $check_exit, wanted $expected. Output: $(shown_output)" "$why" "$how"
  fi
}

expect_output_has() {
  local label=$1 wanted=$2 why=$3 how=$4
  if has_text "$wanted" "$check_output"; then
    pass "$label: names $wanted"
  else
    fail "$label" "output does not contain '$wanted'. Output: $(shown_output)" "$why" "$how"
  fi
}

expect_output_lacks() {
  local label=$1 unwanted=$2 why=$3 how=$4
  if ! has_text "WHAT" "$check_output"; then
    fail "$label" "the check wrote no failure block to inspect. Output: $(shown_output)" "$why" "$how"
  elif has_text "$unwanted" "$check_output"; then
    fail "$label" "output names '$unwanted' although it is not an offender. Output: $(shown_output)" "$why" "$how"
  else
    pass "$label: does not name $unwanted"
  fi
}

# --- fixtures ---------------------------------------------------------------------------------

work_dir="$(mktemp -d)"
mkdir -p "$work_dir/fx"

cat > "$work_dir/build-fixtures.sh" <<'BUILD'
set -eu
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends gcc libc6-dev binutils
cd /work/fx
printf 'int main(void){return 0;}\n' > plain.c
printf '#define _GNU_SOURCE\n#include <pthread.h>\nint main(void){return (int)pthread_gettid_np(pthread_self());}\n' > offender.c
gcc plain.c -o plain
gcc offender.c -o offender
objcopy --remove-section .gnu.version_r plain no-version-needs
# One program per glibc version: a stand-in libc.so.6 defines exactly one symbol version, so the
# program needs exactly that version and nothing else.
for version in 2.9 2.39 2.40 2.100; do
  tag="${version//./_}"
  printf 'GLIBC_%s { global: sym_%s; };\n' "$version" "$tag" > "version_$tag.map"
  printf 'int sym_%s(void){return 0;}\n' "$tag" > "stub_$tag.c"
  mkdir -p "stub_$tag"
  gcc -shared -fPIC -nostdlib -Wl,-soname,libc.so.6 -Wl,--version-script="version_$tag.map" "stub_$tag.c" -o "stub_$tag/libc.so.6"
  printf 'int sym_%s(void);\nvoid _start(void){ sym_%s(); for(;;){} }\n' "$tag" "$tag" > "needs_$tag.c"
  gcc -nostdlib -no-pie -o "needs_$tag" "needs_$tag.c" -L"stub_$tag" -l:libc.so.6
done
chown -R "$HOST_UID:$HOST_GID" /work/fx
BUILD

cat > "$work_dir/stage-layouts.sh" <<'STAGE'
set -eu
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends binutils libwebkit2gtk-4.1-0
fx=/work/fx
mkdir -p /layouts/real /layouts/empty /layouts/no-elf /layouts/offender /layouts/nested/lib/deep /layouts/stripped
cp /usr/bin/bash /layouts/real/bash
find /usr/lib -type f \( -name 'libwebkit2gtk-4.1.so.0*' -o -name 'libjavascriptcoregtk-4.1.so.0*' -o -name WebKitWebProcess \) -exec cp {} /layouts/real/ \;
printf '#!/bin/sh\necho hello\n' > /layouts/no-elf/launcher.sh
cp "$fx/offender" /layouts/offender/offender
cp "$fx/no-version-needs" /layouts/stripped/no-version-needs
cp "$fx/offender" /layouts/nested/lib/deep/offender
ln -s deep/offender /layouts/nested/lib/offender-link
cp "$fx/plain" /layouts/nested/lib/plain-neighbour
cp /usr/bin/bash /layouts/nested/bash-neighbour
for tag in 2_9 2_39 2_40 2_100; do
  mkdir -p "/layouts/needs-$tag"
  cp "$fx/needs_$tag" "/layouts/needs-$tag/needs"
done
mkdir -p /layouts/truncated /layouts/unreadable
head -c 2000 /usr/bin/bash > /layouts/truncated/cut-short
cp "$fx/plain" /layouts/unreadable/readable-neighbour
cp "$fx/plain" /layouts/unreadable/hidden
chmod 000 /layouts/unreadable/hidden
# A PATH with every tool except readelf.
mkdir -p /shadow/bin
for tool in /usr/bin/*; do
  if [ "$(basename "$tool")" != readelf ]; then ln -s "$tool" "/shadow/bin/$(basename "$tool")"; fi
done
STAGE

build_container="$prefix-build"
created_containers+=("$build_container")
if ! docker run -d --name "$build_container" -v "$work_dir:/work" -e "HOST_UID=$(id -u)" -e "HOST_GID=$(id -g)" \
  "$newer_image" sleep infinity >/dev/null 2>&1; then
  setup_failure "could not start $newer_image"
elif ! docker exec "$build_container" bash /work/build-fixtures.sh >"$work_dir/build.log" 2>&1; then
  setup_failure "could not build the test ELF files in $newer_image: $(tail -n 5 "$work_dir/build.log")"
fi

check_container="$prefix-check"
created_containers+=("$check_container")
if [ "$setup_failed" -eq 0 ]; then
  if ! docker run -d --name "$check_container" -v "$work_dir:/work:ro" -v "$tools_dir:/tools:ro" \
    "$floor_image" sleep infinity >/dev/null 2>&1; then
    setup_failure "could not start $floor_image"
  elif ! docker exec "$check_container" bash /work/stage-layouts.sh >"$work_dir/stage.log" 2>&1; then
    setup_failure "could not stage libraries in $floor_image: $(tail -n 5 "$work_dir/stage.log")"
  fi
fi

if [ "$setup_failed" -ne 0 ]; then summary_and_exit; fi

# --- the packaging run on today's build -------------------------------------------------------

run_check /layouts/real
expect_exit "today's libraries pass the written floor" 0 \
  "the release must keep uploading an AppImage whose libraries the floor distribution can load" \
  "make the floor check exit 0 when every file needs at most the floor"
expect_output_has "today's libraries pass the written floor" "highest=GLIBC_2.38" \
  "an exit 0 without the parsed highest version cannot be told from a check that read nothing" \
  "print the highest GLIBC version found in the summary line"

# --- refusal: a file needs a newer glibc than the floor ---------------------------------------

run_check /layouts/offender
expect_exit "a binary needing a newer glibc is refused" 1 \
  "an AppImage needing a newer glibc than the floor stops starting on the floor distribution" \
  "exit 1 when any ELF file needs a GLIBC version above the floor"
expect_output_has "a binary needing a newer glibc is refused" "/layouts/offender/offender" \
  "the maintainer must see which binary raised the requirement" \
  "name the file path in the failure block"
expect_output_has "a binary needing a newer glibc is refused" "GLIBC_2.42" \
  "the maintainer must see which version the binary needs" \
  "name the highest GLIBC version the file needs"
expect_output_has "a binary needing a newer glibc is refused" "$written_floor" \
  "the maintainer must see the floor the file was measured against" \
  "state the floor value in the failure block"
expect_output_has "a binary needing a newer glibc is refused" "/glibc-floor" \
  "the maintainer must see where the floor is written to change it on purpose" \
  "name the floor file in the failure block"

# --- versions compare as numbers, against the floor written in the floor file ------------------

for pair in "2_9:2.9:0" "2_39:2.39:0" "2_40:2.40:1" "2_100:2.100:1"; do
  tag="${pair%%:*}"
  rest="${pair#*:}"
  version="${rest%%:*}"
  expected="${rest#*:}"
  run_check "/layouts/needs-$tag"
  expect_exit "a binary needing GLIBC_$version against the written floor $written_floor" "$expected" \
    "glibc versions are dotted numbers: 2.9 is older than 2.39 and 2.100 is newer than 2.39" \
    "compare the dotted versions numerically, never as text"
done

run_check /layouts/offender 2.9
expect_exit "a floor of 2.9 refuses a binary needing GLIBC_2.42" 1 \
  "2.42 is newer than 2.9 although it sorts before it as text" \
  "compare the dotted versions numerically"
run_check /layouts/offender 2.100
expect_exit "a floor of 2.100 accepts a binary needing GLIBC_2.42" 0 \
  "2.42 is older than 2.100 although it sorts after it as text" \
  "compare the dotted versions numerically"

# --- the floor argument moves the line ---------------------------------------------------------

run_check /layouts/real 2.37
expect_exit "a floor of 2.37 refuses today's libraries" 1 \
  "today's libraries need GLIBC_2.38, which is newer than 2.37" \
  "use the floor argument in place of the floor file"
expect_output_has "a floor of 2.37 refuses today's libraries" "GLIBC_2.38" \
  "the maintainer must see which version crossed the lowered floor" \
  "name the highest GLIBC version the file needs"
run_check /layouts/real 2.38
expect_exit "a floor of 2.38 accepts today's libraries" 0 \
  "a file needing exactly the floor still starts on the floor distribution" \
  "fail only on versions strictly above the floor"

# --- every file at any depth, each offender once ----------------------------------------------

run_check /layouts/nested
expect_exit "an offender in a nested directory beside passing files is refused" 1 \
  "a library hidden in a subdirectory still loads in the AppImage" \
  "scan every ELF file under the directory at any depth"
expect_output_has "an offender in a nested directory beside passing files is refused" "/layouts/nested/lib/deep/offender" \
  "the maintainer must see the real path of the offender" \
  "name the file by its path under the directory"
expect_output_lacks "an offender beside passing files does not blame them" "plain-neighbour" \
  "naming files that pass hides the real offender" \
  "report only files above the floor"
expect_output_lacks "an offender beside passing files does not blame them" "bash-neighbour" \
  "naming files that pass hides the real offender" \
  "report only files above the floor"
expect_output_lacks "a symlink to the offender is not reported as a second file" "offender-link" \
  "one library reported twice makes the failure look like two problems" \
  "do not follow symbolic links when scanning"
if [ "$(count_lines_with "WHAT" "$check_output")" -eq 1 ]; then
  pass "the offender and its symlink are reported as one failure"
else
  fail "the offender and its symlink are reported as one failure" \
    "the output holds $(count_lines_with "WHAT" "$check_output") failure blocks, wanted 1. Output: $(shown_output)" \
    "one library reported twice makes the failure look like two problems" \
    "scan each real file once and write one WHAT/WHY/HOW block per offending file"
fi

# --- never a silent pass -----------------------------------------------------------------------

run_check /layouts/empty
expect_exit "an empty directory is not a pass" 2 \
  "a check that looked at nothing cannot say the AppImage is within the floor" \
  "exit 2 when no ELF file was scanned"
run_check /layouts/no-elf
expect_exit "a directory with no ELF file is not a pass" 2 \
  "a check that looked at nothing cannot say the AppImage is within the floor" \
  "exit 2 when no ELF file was scanned"
run_check /layouts/does-not-exist
expect_exit "a missing directory is not a pass" 2 \
  "an extraction that failed must not read as a clean AppImage" \
  "exit 2 and name the directory that could not be read"
check_output="$(docker exec "$check_container" env PATH=/shadow/bin /usr/bin/bash /tools/check-glibc-floor.sh /layouts/real 2>&1)"
check_exit=$?
expect_exit "a runner without readelf is not a pass" 2 \
  "without readelf no version is read, and no version read must not mean no version needed" \
  "exit 2 and name readelf when it is missing"
expect_output_has "a runner without readelf is not a pass" "readelf" \
  "the maintainer must see which tool is missing" \
  "name readelf in the message"
run_check /layouts/stripped
expect_exit "a libc-linked file whose versions cannot be read is not a pass" 2 \
  "a changed readelf format must not read as a file that needs nothing" \
  "exit 2 when an ELF needs libc.so.6 and no GLIBC version was parsed from it"
expect_output_has "a libc-linked file whose versions cannot be read is not a pass" "/layouts/stripped/no-version-needs" \
  "the maintainer must see which file could not be read" \
  "name the file in the message"
expect_output_has "a libc-linked file whose versions cannot be read is not a pass" "readelf -V" \
  "the maintainer must see which reading came back empty" \
  "say that readelf -V gave no GLIBC versions"

run_check /layouts/truncated
expect_exit "a cut-off ELF file is not a pass" 2 \
  "a file whose body cannot be read says nothing about what glibc it needs" \
  "exit 2 when a file with ELF magic cannot be parsed by readelf"
expect_output_has "a cut-off ELF file is not a pass" "/layouts/truncated/cut-short" \
  "the maintainer must see which file could not be read" \
  "name the file in the message"

check_output="$(docker exec -u nobody "$check_container" bash /tools/check-glibc-floor.sh /layouts/unreadable 2>&1)"
check_exit=$?
expect_exit "a file the scanning user cannot read is not a pass" 2 \
  "a file skipped for its permissions may be the one that needs a newer glibc" \
  "exit 2 when a file under the directory cannot be opened"
expect_output_has "a file the scanning user cannot read is not a pass" "/layouts/unreadable/hidden" \
  "the maintainer must see which file could not be read" \
  "name the file in the message"

for bad_floor in abc 2.; do
  run_check /layouts/real "$bad_floor"
  expect_exit "a floor of '$bad_floor' is not a version" 2 \
    "a floor that is not a dotted number cannot be compared, and must not read as every file passing" \
    "exit 2 when the floor is not a dotted number"
  expect_output_has "a floor of '$bad_floor' is not a version" "WHAT" \
    "the maintainer must be told what is wrong, why it matters and how to fix it" \
    "write a WHAT/WHY/HOW block naming the floor value"
  expect_output_has "a floor of '$bad_floor' is not a version" "'$bad_floor'" \
    "the maintainer must see which floor value was refused" \
    "name the floor value in the message"
done

# --- the minimum is written down and the workflow runs the check -------------------------------

if [ -f "$floor_file" ] && [ "$(tr -d '[:space:]' < "$floor_file")" = "FLOOR=$written_floor" ]; then
  pass "the floor file holds the one written floor"
else
  fail "the floor file holds the one written floor" \
    "the floor file is missing or does not hold the single line FLOOR=$written_floor" \
    "the minimum glibc is a written decision that the check reads from one place" \
    "write FLOOR=$written_floor as the only line of the floor file"
fi

linux_section=""
if [ -f "$docs_file" ]; then linux_section="$(sed -n '/^## Linux/,/^## Where your data lives/p' "$docs_file")"; fi
if has_text "$written_floor" "$linux_section" && has_text "Ubuntu 24.04" "$linux_section"; then
  pass "the Linux install page states the minimum"
else
  fail "the Linux install page states the minimum" \
    "the Linux section does not name Ubuntu 24.04 and glibc $written_floor" \
    "users must be able to read which Linux the AppImage supports" \
    "state the minimum in the Linux section of the Standalone installation page"
fi

if [ -f "$adr_file" ] && grep -q -F -- "glibc $written_floor" "$adr_file"; then
  pass "the written decision names the same floor"
else
  fail "the written decision names the same floor" \
    "the decision record is missing or does not name glibc $written_floor" \
    "the floor file, the install page and the decision record must agree" \
    "keep the three numbers equal and change them only by a new decision"
fi

libssl_line="$(line_number_of "Reject an AppImage that bundles libssl" "$workflow_file")"
floor_line="$(line_number_of "check-glibc-floor.sh" "$workflow_file")"
upload_line="$(line_number_of "Upload Linux standalone artifact" "$workflow_file")"
if [ -n "$libssl_line" ] && [ -n "$floor_line" ] && [ -n "$upload_line" ] \
  && [ "$libssl_line" -lt "$floor_line" ] && [ "$floor_line" -lt "$upload_line" ]; then
  pass "the packaging run checks the floor after the libssl rejection and before the upload"
else
  fail "the packaging run checks the floor after the libssl rejection and before the upload" \
    "line numbers in the workflow: libssl rejection '${libssl_line:-none}', floor check '${floor_line:-none}', upload '${upload_line:-none}'" \
    "a build that needs a newer glibc must stop before it is uploaded" \
    "add a step that runs the floor check on the extracted AppImage between those two steps"
fi

if grep -q -F -- "binutils" "$workflow_file" 2>/dev/null; then
  pass "the packaging run installs the tool that reads glibc versions"
else
  fail "the packaging run installs the tool that reads glibc versions" \
    "the workflow does not install binutils" \
    "without readelf the floor check can only report that it could not read anything" \
    "install binutils before the floor check runs"
fi

summary_and_exit
