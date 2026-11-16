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

expect_failure_block() {
  local label=$1 line label_name found=0
  for label_name in WHAT WHY HOW; do
    found=0
    while IFS= read -r line; do
      if [[ $line == "$label_name:"* ]]; then found=1; fi
    done <<< "$check_output"
    if [ "$found" -eq 1 ]; then
      pass "$label: carries a $label_name line"
    else
      fail "$label" "the output has no line starting with $label_name:. Output: $(shown_output)" \
        "a refusal without WHAT, WHY and HOW does not tell the maintainer what to do" \
        "write the failure as a WHAT, a WHY and a HOW line"
    fi
  done
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
# A healthy program whose ELF header points at a missing string table: readelf warns, yet still lists the needs.
cp plain warns-but-readable
printf '\377\377' | dd of=warns-but-readable bs=1 seek=62 conv=notrunc 2>/dev/null
printf '\211PNG\r\n\032\nnot really a picture' > picture.png
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
mkdir -p /layouts/warns /layouts/mixed
cp "$fx/warns-but-readable" /layouts/warns/warns-but-readable
printf '#!/bin/sh\necho hello\n' > /layouts/mixed/launcher.sh
cp "$fx/picture.png" /layouts/mixed/picture.png
cp "$fx/plain" /layouts/mixed/plain
# The highest GLIBC version the staged real libraries need, read straight from readelf -V.
for lib in /layouts/real/*; do readelf -V "$lib" 2>/dev/null; done \
  | awk '/^Version needs section/ { n = 1; next } /^Version (symbols|definition) section/ { n = 0 } n && match($0, /Name: GLIBC_[0-9][0-9.]*/) { print substr($0, RSTART + 12, RLENGTH - 12) }' \
  | sort -V | tail -n 1 > /layouts/real-highest
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

# The cases rely on what each fixture needs from glibc, so read it back with readelf -V before trusting them.
fixture_glibc_needs() {
  docker exec "$check_container" bash -c 'readelf -V "$1" 2>/dev/null | grep -o "Name: GLIBC_[0-9][0-9.]*" | sort -u | tr "\n" " "' _ "$1"
}
self_check_fixture() {
  local name=$1 path=$2 wanted=$3 seen
  seen="$(fixture_glibc_needs "$path")"
  if [ "$seen" != "$wanted" ]; then
    setup_failure "fixture $name should need '$wanted' by readelf -V but needs '$seen'"
  fi
}
if [ "$setup_failed" -eq 0 ]; then
  for tag in 2_9 2_39 2_40 2_100; do
    self_check_fixture "needs_$tag" "/work/fx/needs_$tag" "Name: GLIBC_${tag//_/.} "
  done
  if ! docker exec "$check_container" bash -c 'readelf -V /work/fx/offender | grep -q "Name: GLIBC_2.42"'; then
    setup_failure "fixture offender does not need GLIBC_2.42 by readelf -V"
  fi
  self_check_fixture "no-version-needs" /work/fx/no-version-needs ""
  if ! docker exec "$check_container" bash -c 'readelf -d /work/fx/no-version-needs | grep -q "libc.so.6"'; then
    setup_failure "fixture no-version-needs does not link libc.so.6 by readelf -d"
  fi
fi

if [ "$setup_failed" -ne 0 ]; then summary_and_exit; fi

# --- the packaging run on today's build -------------------------------------------------------

run_check /layouts/real
expect_exit "today's libraries pass the written floor" 0 \
  "the release must keep uploading an AppImage whose libraries the floor distribution can load" \
  "make the floor check exit 0 when every file needs at most the floor"
real_highest="$(docker exec "$check_container" cat /layouts/real-highest 2>/dev/null)"
if [ -z "$real_highest" ]; then
  setup_failure "readelf -V found no GLIBC version in the staged libraries, so there is nothing to compare the check against"
fi
expect_output_has "today's libraries pass the written floor" "highest=GLIBC_$real_highest " \
  "an exit 0 without the parsed highest version cannot be told from a check that read nothing" \
  "print the highest GLIBC version found, as readelf -V lists it, in the summary line"

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

# The boundary is taken from the highest version readelf reports for today's libraries, one minor below it and equal to it.
below_highest=""
if [[ $real_highest =~ ^([0-9]+)\.([0-9]+)$ ]] && [ "${BASH_REMATCH[2]}" -gt 0 ]; then
  below_highest="${BASH_REMATCH[1]}.$((BASH_REMATCH[2] - 1))"
else
  setup_failure "the highest GLIBC version '$real_highest' is not a major.minor pair above minor 0, so no floor one minor below it exists"
fi
run_check /layouts/real "$below_highest"
expect_exit "a floor of $below_highest refuses today's libraries" 1 \
  "today's libraries need GLIBC_$real_highest, which is newer than $below_highest" \
  "use the floor argument in place of the floor file"
expect_output_has "a floor of $below_highest refuses today's libraries" "GLIBC_$real_highest" \
  "the maintainer must see which version crossed the lowered floor" \
  "name the highest GLIBC version the file needs"
run_check /layouts/real "$real_highest"
expect_exit "a floor of $real_highest accepts today's libraries" 0 \
  "a file needing exactly the floor still starts on the floor distribution" \
  "fail only on versions strictly above the floor"

run_check /layouts/real 2.39.1
expect_exit "a floor of 2.39.1 accepts today's libraries" 0 \
  "a floor may carry a third number, and today's libraries are older than it" \
  "accept a floor of dotted numbers of any length"
run_check /layouts/needs-2_40 2.39.1
expect_exit "a floor of 2.39.1 refuses a binary needing GLIBC_2.40" 1 \
  "2.40 is newer than 2.39.1" \
  "compare every number of the dotted versions"

# --- files that are not a problem ----------------------------------------------------------------

run_check /layouts/warns
expect_exit "a healthy binary that makes readelf warn still passes" 0 \
  "the needs were read, so a warning about something else must not stop a good build" \
  "refuse only when no GLIBC version was parsed from the file"
expect_output_has "a healthy binary that makes readelf warn still passes" "scanned=1 " \
  "the file must have been scanned, not skipped" \
  "count every ELF file that was read"
run_check /layouts/mixed
expect_exit "a text file and a picture beside a clean binary are skipped" 0 \
  "only ELF files can need glibc, so other files must not fail the check" \
  "skip files that do not start with the ELF magic"
expect_output_has "a text file and a picture beside a clean binary are skipped" "scanned=1 " \
  "only the binary is an ELF file, so exactly one file was scanned" \
  "count only ELF files"

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
expect_output_has "a directory with no ELF file is not a pass" "/layouts/no-elf" \
  "the maintainer must see which directory held nothing to check" \
  "name the directory in the message"
expect_failure_block "a directory with no ELF file is not a pass"
run_check /layouts/does-not-exist
expect_exit "a missing directory is not a pass" 2 \
  "an extraction that failed must not read as a clean AppImage" \
  "exit 2 and name the directory that could not be read"
expect_output_has "a missing directory is not a pass" "/layouts/does-not-exist" \
  "the maintainer must see which directory could not be read" \
  "name the directory in the message"
expect_failure_block "a missing directory is not a pass"
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
  what_line="$(grep -m1 '^WHAT' <<< "$check_output")"
  if has_text "'$bad_floor'" "$what_line"; then
    pass "a floor of '$bad_floor' is not a version: the WHAT line names it"
  else
    fail "a floor of '$bad_floor' is not a version" \
      "the WHAT line is '${what_line:-none}', it does not name '$bad_floor'. Output: $(shown_output)" \
      "the maintainer must see which floor value was refused" \
      "name the floor value in the WHAT line"
  fi
  if [ "$(count_lines_with "WHAT" "$check_output")" -eq 1 ] && ! has_text "scanned=" "$check_output"; then
    pass "a floor of '$bad_floor' is refused before any file is judged"
  else
    fail "a floor of '$bad_floor' is refused before any file is judged" \
      "the output holds more than the floor refusal, or a scan summary. Output: $(shown_output)" \
      "a bad floor must not be reported as an offending file" \
      "refuse the floor value and stop before scanning"
  fi
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

# The package job of the workflow, as the lines between its key and the next job.
package_job="$(awk '
  /^  package-linux-standalone:[[:space:]]*$/ { inside = 1; print; next }
  inside && /^  [A-Za-z0-9_-]+:/ { exit }
  inside { print }' "$workflow_file" 2>/dev/null)"

# One line about the package job's steps, in order: the first step that builds the AppImage, rejects
# libssl, runs the floor check and uploads, the number of steps that run the floor check, whether one of
# them may fail without failing the job, and the first step whose apt-get install list holds binutils.
# A step is read by its keys, so a name, a comment or another package that mentions a word counts for nothing.
package_steps_report() {
  awk '
    function note(text,   i, count, words) {
      if (text ~ /^[[:space:]]*#/) return
      if (index(text, "check-glibc-floor.sh")) {
        floor_steps[step] = 1
        argument = substr(text, index(text, "check-glibc-floor.sh") + 20)
        sub(/[;&|].*/, "", argument)
        if (argument ~ /(^|[^A-Za-z0-9_.-])squashfs-root($|[^A-Za-z0-9_.-])/) { scans_extracted[step] = 1 }
        continued = (text ~ /\\[[:space:]]*$/)
      } else if (continued) {
        if (text ~ /(^|[^A-Za-z0-9_.-])squashfs-root($|[^A-Za-z0-9_.-])/) { scans_extracted[step] = 1 }
        continued = (text ~ /\\[[:space:]]*$/)
      }
      if (index(text, "libssl.so") && !ssl) ssl = step
      if (collecting || index(text, "apt-get install")) {
        if (!collecting) sub(/.*apt-get install/, "", text)
        count = split(text, words, /[[:space:]]+/)
        for (i = 1; i <= count; i++) if (words[i] == "binutils") has_binutils[step] = 1
        collecting = (text ~ /\\[[:space:]]*$/)
      }
    }
    /^      - / { step++; collecting = 0; in_run = 0; continued = 0; line = "        " substr($0, 9) }
    /^    continue-on-error:/ { value = $0; sub(/^ +continue-on-error:[[:space:]]*/, "", value); if (value !~ /^false/) job_may_fail = 1 }
    step == 0 { next }
    !/^      - / { line = $0 }
    line ~ /^        [A-Za-z_-]+:/ {
      key = line; sub(/^ +/, "", key); sub(/:.*/, "", key)
      value = line; sub(/^ +[A-Za-z_-]+:[[:space:]]*/, "", value)
      in_run = (key == "run"); collecting = 0
      if (key == "uses" && index(value, "tauri-apps/tauri-action") && !build) build = step
      if (key == "uses" && index(value, "actions/upload-artifact") && !upload) upload = step
      if (key == "continue-on-error" && value !~ /^false/) { may_fail[step] = 1 }
      if (key == "if") { conditional[step] = 1 }
      if (in_run && value !~ /^[|>]/) note(value)
      next
    }
    in_run && line ~ /[^[:space:]]/ { note(line) }
    END {
      for (s in floor_steps) { floor_count++; if (!floor || s < floor) floor = s; if (s in may_fail) floor_may_fail = 1 }
      for (s in has_binutils) if (!install || s < install) install = s
      floor_conditional = (floor in conditional) ? 1 : 0
      floor_scans_extracted = (floor in scans_extracted) ? 1 : 0
      printf "build=%d ssl=%d floor=%d upload=%d floor_count=%d floor_may_fail=%d install=%d job_may_fail=%d floor_conditional=%d floor_scans_extracted=%d\n", build, ssl, floor, upload, floor_count, floor_may_fail, install, job_may_fail + 0, floor_conditional, floor_scans_extracted
    }' <<< "$package_job"
}

report="$(package_steps_report)"
read -r -a report_words <<< "$report"
step_of() {
  local word
  for word in "${report_words[@]}"; do
    if [[ $word == "$1="* ]]; then echo "${word#*=}"; return; fi
  done
  echo 0
}
build_step="$(step_of build)"
ssl_step="$(step_of ssl)"
floor_step="$(step_of floor)"
upload_step="$(step_of upload)"
floor_steps_count="$(step_of floor_count)"
floor_step_may_fail="$(step_of floor_may_fail)"
install_step="$(step_of install)"
job_may_fail="$(step_of job_may_fail)"
floor_conditional="$(step_of floor_conditional)"
floor_scans_extracted="$(step_of floor_scans_extracted)"

if [ "$build_step" -gt 0 ] && [ "$build_step" -lt "$ssl_step" ] && [ "$ssl_step" -lt "$floor_step" ] \
  && [ "$floor_step" -lt "$upload_step" ] && [ "$floor_steps_count" -eq 1 ]; then
  pass "the packaging run checks the floor once, after the build and the libssl rejection and before the upload"
else
  fail "the packaging run checks the floor once, after the build and the libssl rejection and before the upload" \
    "step positions in the package job (0 means not found): build $build_step, libssl rejection $ssl_step, floor check $floor_step, upload $upload_step, steps that run the floor check $floor_steps_count" \
    "a build that needs a newer glibc must stop before it is uploaded" \
    "add one step that runs the floor check on the extracted AppImage between the libssl rejection and the upload"
fi

if [ "$floor_step" -gt 0 ] && [ "$floor_step_may_fail" -eq 0 ] && [ "$job_may_fail" -eq 0 ]; then
  pass "a failing floor check fails the packaging run"
else
  fail "a failing floor check fails the packaging run" \
    "the floor check step is $([ "$floor_step" -gt 0 ] && echo "present (step marked continue-on-error: $floor_step_may_fail, whole job marked continue-on-error: $job_may_fail)" || echo "missing")" \
    "a floor check that cannot fail the job lets a too-new AppImage be uploaded" \
    "remove continue-on-error from the floor check step and from the package job"
fi

if [ "$floor_step" -gt 0 ] && [ "$floor_conditional" -eq 0 ]; then
  pass "the floor check runs whenever the packaging job runs"
else
  fail "the floor check runs whenever the packaging job runs" \
    "the floor check step is $([ "$floor_step" -gt 0 ] && echo "guarded by an if condition" || echo "missing")" \
    "a floor check that can be skipped lets a too-new AppImage be uploaded" \
    "remove the if condition from the floor check step"
fi

if [ "$floor_step" -gt 0 ] && [ "$floor_scans_extracted" -eq 1 ]; then
  pass "the floor check scans the extracted AppImage"
else
  fail "the floor check scans the extracted AppImage" \
    "the floor check step does not name the extracted AppImage folder squashfs-root as an argument" \
    "a floor check over another folder passes while the AppImage still needs a newer glibc" \
    "run the floor check on the squashfs-root folder of the extracted AppImage"
fi

if [ "$install_step" -gt 0 ] && [ "$floor_step" -gt 0 ] && [ "$install_step" -lt "$floor_step" ]; then
  pass "the packaging run installs the tool that reads glibc versions before the floor check"
else
  fail "the packaging run installs the tool that reads glibc versions before the floor check" \
    "the first step whose apt-get install list holds binutils is step $install_step (0 means none), the floor check is step $floor_step" \
    "without readelf the floor check can only report that it could not read anything" \
    "add binutils to the apt-get install list of a step before the floor check"
fi

summary_and_exit
