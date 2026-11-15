#!/usr/bin/env bash
# What the Linux packaging run promises the maintainer: the AppImage is built in a container image we
# chose, on a runner label GitHub does not retire, with the same glibc floor as before, and the finished
# AppImage still loads on Ubuntu 24.04 and Debian 13. The workflow, renovate.json and the written decision
# are read as files. The package install of the workflow runs in a fresh container of the digest the
# workflow names. With APPIMAGE set to a real AppImage, the loader check runs on both distributions the
# workflow lists. Needs Docker, bash, jq and network.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd -P)"
tools_dir="$repo_root/tools/linux-appimage"
floor_file="$tools_dir/glibc-floor"
loader_script="$tools_dir/check-loads-on.sh"
workflow_rel=".github/workflows/ci_package-linux-standalone.yml"
workflow_file="$repo_root/$workflow_rel"
renovate_file="$repo_root/renovate.json"
adr_file="$repo_root/docs/product/architecture/adr-284-linux-appimage-builds-in-a-pinned-ubuntu-24-04-container-and-the-glibc-floor-is-2-39.md"

floor_image="ubuntu:24.04@sha256:2198e4c65076e9cb0b3d695219df667c0e3b0072da64ca3c2648aca17dcecaf5"
newer_image="ubuntu:26.04@sha256:065aa8626841f16057b78b027b464cd59c4fac36c204c72dcbf48d66f5fa8a96"

observed=0
assertions_failed=0
setup_failed=0
prefix="lh-build-env-oracle-$$"
work_dir=""
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

line_number_of() {
  local wanted=$1 file=$2 found
  found="$(grep -n -m1 -F -- "$wanted" "$file" 2>/dev/null)" || true
  echo "${found%%:*}"
}

shown_output() {
  printf '%s' "$check_output" | tail -n 12
}

# --- reading the workflow ----------------------------------------------------------------------

workflow_text=""
if [ -f "$workflow_file" ]; then workflow_text="$(cat "$workflow_file")"; fi

job_block() {
  awk -v job="$1" '
    $0 ~ "^  " job ":[[:space:]]*$" { inside = 1; print; next }
    inside && /^  [A-Za-z0-9_-]+:/ { exit }
    inside { print }' "$workflow_file" 2>/dev/null
}

# Prints the first step of the job block on stdin whose text contains the fixed string.
step_containing() {
  awk -v want="$1" '
    function flush() { if (buf != "" && index(buf, want)) { printf "%s", buf; found = 1; exit } }
    /^      - / { flush(); buf = "" }
    { buf = buf $0 "\n" }
    END { if (!found) flush() }'
}

# Prints the script of a step as bash would receive it.
run_script_of() {
  awk '
    !grab && /^[[:space:]]+(- )?run:[[:space:]]*\|/ { grab = 1; next }
    grab {
      match($0, /^[[:space:]]*/)
      if (indent == "") indent = RLENGTH
      if ($0 ~ /[^[:space:]]/ && RLENGTH < indent) exit
      print substr($0, indent + 1)
    }'
}

package_job="$(job_block package-linux-standalone)"
loader_job="$(job_block verify-appimage-loads)"

container_image_of_package_job() {
  printf '%s\n' "$package_job" \
    | sed -n -E 's/^[[:space:]]+(image|container):[[:space:]]*([^[:space:]]+)[[:space:]]*$/\2/p' \
    | head -n 1 | tr -d "\"'"
}

# The glibc of the Ubuntu release the floor documents; an unknown floor has no release.
release_for_floor() {
  case $1 in
    2.39) echo "24.04" ;;
    2.43) echo "26.04" ;;
    *) echo "" ;;
  esac
}

# Accepts only an Ubuntu image named by digest whose release is the one the floor documents.
container_release_verdict() {
  local image=$1 floor=$2 expected
  expected="$(release_for_floor "$floor")"
  [ -n "$expected" ] || return 1
  [[ $image =~ ^ubuntu:([0-9]+\.[0-9]+)@sha256:[0-9a-f]{64}$ ]] || return 1
  [ "${BASH_REMATCH[1]}" = "$expected" ]
}

floor_value="$(sed -n 's/^FLOOR=\([^[:space:]]*\)[[:space:]]*$/\1/p' "$floor_file" 2>/dev/null | head -n 1)"
workflow_image="$(container_image_of_package_job)"

# --- no runner image that GitHub will retire -----------------------------------------------------

if has_text "runs-on: ubuntu-latest" "$package_job"; then
  pass "the Linux packaging job runs on the ubuntu-latest label"
else
  fail "the Linux packaging job runs on the ubuntu-latest label" \
    "the packaging job does not name runs-on: ubuntu-latest" \
    "a label GitHub moves forward cannot be retired under the release" \
    "set runs-on: ubuntu-latest on the packaging job and take the glibc from the job container"
fi

versioned_runner="$(grep -n -E 'runs-on:[[:space:]]*.*ubuntu-[0-9]+\.[0-9]+' "$workflow_file" 2>/dev/null | head -n 3)"
if [ -z "$versioned_runner" ] && [ -f "$workflow_file" ]; then
  pass "no job in the workflow names a versioned Ubuntu runner"
else
  fail "no job in the workflow names a versioned Ubuntu runner" \
    "versioned runner label found: ${versioned_runner:-the workflow file is missing}" \
    "GitHub retires versioned runner images and the release stops that day" \
    "name ubuntu-latest and let the job container fix the glibc"
fi

# A container image is judged as the written decision demands: by digest, of the release whose glibc is the floor.
# Cases: tag only, a digest of another release, a digest of the floor's release, a floor moved without the image.
digest_24="${floor_image#*@}"
digest_26="${newer_image#*@}"
for row in \
  "tag only without digest|ubuntu:24.04|2.39|1" \
  "digest with release 26.04|ubuntu:26.04@$digest_26|2.39|1" \
  "digest with release 24.04|ubuntu:24.04@$digest_24|2.39|0" \
  "digest with release 24.04 after the floor moved to 2.43|ubuntu:24.04@$digest_24|2.43|1"; do
  IFS='|' read -r label image floor_in expected <<< "$row"
  if container_release_verdict "$image" "$floor_in"; then got=0; else got=1; fi
  if [ "$got" = "$expected" ]; then
    pass "container judgement: $label"
  else
    fail "container judgement: $label" \
      "the judgement of '$image' against floor $floor_in was $got, wanted $expected" \
      "the container image and the floor must be changed together on purpose" \
      "judge an image accepted only when it is by digest and its release has the floor's glibc"
  fi
done

if container_release_verdict "$workflow_image" "$floor_value"; then
  pass "the packaging job container is an Ubuntu image by digest whose release matches the floor $floor_value"
else
  fail "the packaging job container is an Ubuntu image by digest whose release matches the floor $floor_value" \
    "the container image of the packaging job is '${workflow_image:-none}'; wanted ubuntu:$(release_for_floor "$floor_value")@sha256:<digest>" \
    "a tag moves under the build and the glibc, and with it the floor, changes without a decision" \
    "set the job container to the Ubuntu release of the floor, by sha256 digest"
fi

if [ -f "$adr_file" ] && [ -n "$(release_for_floor "$floor_value")" ]; then
  pass "the written decision exists and the floor file holds a floor with a known Ubuntu release"
else
  fail "the written decision exists and the floor file holds a floor with a known Ubuntu release" \
    "decision record present: $([ -f "$adr_file" ] && echo yes || echo no); floor '${floor_value:-none}' maps to Ubuntu '$(release_for_floor "$floor_value")'" \
    "the floor and the container release are one written decision" \
    "write the decision record and keep FLOOR=<version> in the floor file"
fi

# --- what the packaging job does -----------------------------------------------------------------

if grep -q -F -- "actions/setup-dotnet" "$workflow_file" 2>/dev/null; then
  fail "the packaging job has no .NET setup step" \
    "the workflow still uses actions/setup-dotnet" \
    "no step runs dotnet and the bare container has no ICU library for it" \
    "remove the setup-dotnet step"
else
  pass "the packaging job has no .NET setup step"
fi

pin_lines="$(grep -n -i -E '(webkit|javascriptcore)[^[:space:]]*=[0-9]' "$workflow_file" 2>/dev/null | head -n 3)"
if [ -z "$pin_lines" ] && [ -f "$workflow_file" ]; then
  pass "no WebKitGTK or JavaScriptCore package carries an exact version pin"
else
  fail "no WebKitGTK or JavaScriptCore package carries an exact version pin" \
    "pinned package lines: ${pin_lines:-the workflow file is missing}" \
    "an exact pin names a version that leaves the archive and blocks the release" \
    "install the packages without =version and print the installed version"
fi

install_step="$(printf '%s\n' "$package_job" | step_containing "apt-get install")"
install_apt_line="$(printf '%s\n' "$install_step" | grep -n -m1 'apt-get install' | cut -d: -f1)"
print_line="$(printf '%s\n' "$install_step" | grep -n -E 'libwebkit2gtk-4\.1.*(dpkg|apt-cache|apt list|apt policy)|(dpkg|apt-cache|apt list|apt policy).*libwebkit2gtk-4\.1' | tail -n 1 | cut -d: -f1)"
if [ -n "$install_apt_line" ] && [ -n "$print_line" ] && [ "$print_line" -gt "$install_apt_line" ]; then
  pass "the install step prints the installed WebKitGTK version after the install"
else
  fail "the install step prints the installed WebKitGTK version after the install" \
    "the step that runs apt-get install has no later line asking the package database for the libwebkit2gtk-4.1 version" \
    "the maintainer needs to see which WebKitGTK the AppImage was built with, now that it is not pinned" \
    "add a command such as dpkg-query -W libwebkit2gtk-4.1-0 after the install"
fi

build_step="$(printf '%s\n' "$package_job" | step_containing "tauri-apps/tauri-action")"
if printf '%s\n' "$build_step" | grep -q -E 'APPIMAGE_EXTRACT_AND_RUN:[[:space:]]*["'"'"']?1'; then
  pass "the AppImage build step runs the bundler tools without FUSE"
else
  fail "the AppImage build step runs the bundler tools without FUSE" \
    "the Tauri build step does not set APPIMAGE_EXTRACT_AND_RUN to 1" \
    "a job container has no FUSE and linuxdeploy cannot start without it" \
    "set APPIMAGE_EXTRACT_AND_RUN: 1 in the env of the Tauri build step"
fi

install_line="$(line_number_of "apt-get install" "$workflow_file")"
checkout_line="$(line_number_of "actions/checkout" "$workflow_file")"
if [ -n "$install_line" ] && [ -n "$checkout_line" ] && [ "$install_line" -lt "$checkout_line" ] \
  && ! printf '%s\n' "$package_job" | grep -q -w sudo; then
  pass "the build packages are installed as root before the checkout, without sudo"
else
  fail "the build packages are installed as root before the checkout, without sudo" \
    "package install at line '${install_line:-none}', checkout at line '${checkout_line:-none}', sudo used: $(printf '%s\n' "$package_job" | grep -c -w sudo)" \
    "the bare container has no sudo, and the checkout needs git, which the first step installs" \
    "make the package install the first step of the job and drop sudo"
fi

job_line_of() {
  local found
  found="$(printf '%s\n' "$package_job" | grep -n -m1 -F -- "$1")" || true
  echo "${found%%:*}"
}
build_line="$(job_line_of "tauri-apps/tauri-action")"
libssl_line="$(job_line_of "Reject an AppImage that bundles libssl")"
floor_line="$(job_line_of "check-glibc-floor.sh")"
upload_line="$(job_line_of "actions/upload-artifact")"
if [ -n "$build_line" ] && [ -n "$libssl_line" ] && [ -n "$upload_line" ] \
  && [ "$build_line" -lt "$libssl_line" ] && [ "$libssl_line" -lt "$upload_line" ]; then
  pass "the libssl rejection runs in the packaging job after the AppImage build and before the upload"
else
  fail "the libssl rejection runs in the packaging job after the AppImage build and before the upload" \
    "line numbers in the packaging job: build '${build_line:-none}', libssl rejection '${libssl_line:-none}', upload '${upload_line:-none}'" \
    "the rejection reads the finished AppImage and must stop an AppImage that bundles libssl before it is uploaded" \
    "keep the libssl rejection step between the build step and the upload step"
fi
if [ -n "$build_line" ] && [ -n "$floor_line" ] && [ -n "$upload_line" ] \
  && [ "$build_line" -lt "$floor_line" ] && [ "$floor_line" -lt "$upload_line" ]; then
  pass "the glibc floor check runs in the packaging job after the AppImage build and before the upload"
else
  fail "the glibc floor check runs in the packaging job after the AppImage build and before the upload" \
    "line numbers in the packaging job: build '${build_line:-none}', floor check '${floor_line:-none}', upload '${upload_line:-none}'" \
    "an AppImage that needs a newer glibc than the floor must never be uploaded" \
    "run check-glibc-floor.sh on the extracted AppImage between the build step and the upload step"
fi

# --- Renovate keeps the digest current and never moves the Ubuntu release -------------------------

frozen_runner_rules="$(jq --arg wf "$workflow_rel" '
  [.packageRules[]
    | select((.matchFileNames // []) | index($wf))
    | select((.matchDepNames // []) | index("ubuntu"))
    | select(.enabled == false)
    | select((.matchUpdateTypes // []) | length == 0)] | length' "$renovate_file" 2>/dev/null)"
if [ "${frozen_runner_rules:-x}" = "0" ]; then
  pass "Renovate no longer freezes the runner of the Linux packaging workflow"
else
  fail "Renovate no longer freezes the runner of the Linux packaging workflow" \
    "$frozen_runner_rules rule(s) disable every Ubuntu update of the workflow, which would also stop digest updates of the container (jq output empty means renovate.json could not be read)" \
    "the runner is no longer a decision, and the container digest must keep moving" \
    "remove the rule that disables every Ubuntu update for the workflow"
fi

container_rules="$(jq --arg wf "$workflow_rel" '
  [.packageRules[]
    | select((.matchFileNames // []) | index($wf))
    | select((.matchDepNames // []) | index("ubuntu"))
    | select(.enabled == false)
    | select(((.matchUpdateTypes // []) | index("major")) and ((.matchUpdateTypes // []) | index("minor")))
    | select(((.matchUpdateTypes // []) | index("digest")) | not)] | length' "$renovate_file" 2>/dev/null)"
if [ "${container_rules:-0}" -ge 1 ] 2>/dev/null; then
  pass "Renovate may move the container digest but not its Ubuntu release"
else
  fail "Renovate may move the container digest but not its Ubuntu release" \
    "no rule for the workflow disables major and minor updates of ubuntu while leaving digest updates on" \
    "a new Ubuntu release raises the glibc floor and must be a written decision" \
    "add a rule with matchFileNames for the workflow, matchDepNames ubuntu, matchUpdateTypes major and minor, enabled false"
fi

# --- the loader job ------------------------------------------------------------------------------

loader_images="$(printf '%s\n' "$loader_job" | grep -o -E '(ubuntu|debian):[0-9.]+@sha256:[0-9a-f]{64}' | LC_ALL=C sort -u)"
ubuntu_loader_image="$(printf '%s\n' "$loader_images" | grep '^ubuntu:24\.04@' | head -n 1)"
debian_loader_image="$(printf '%s\n' "$loader_images" | grep '^debian:13@' | head -n 1)"

if [ -n "$loader_job" ] && printf '%s\n' "$loader_job" | grep -q -E 'needs:[[:space:]]*(\[[^]]*)?package-linux-standalone'; then
  pass "the loader job waits for the packaging job"
else
  fail "the loader job waits for the packaging job" \
    "the workflow has no verify-appimage-loads job that needs package-linux-standalone" \
    "a loader job without needs would check an artifact that does not exist yet" \
    "add the job verify-appimage-loads with needs: package-linux-standalone"
fi

if [ -n "$ubuntu_loader_image" ] && [ -n "$debian_loader_image" ] \
  && has_text "tools/linux-appimage/check-loads-on.sh" "$loader_job"; then
  pass "the loader job runs the loader check on Ubuntu 24.04 and Debian 13 by digest"
else
  fail "the loader job runs the loader check on Ubuntu 24.04 and Debian 13 by digest" \
    "loader job images by digest: ${loader_images:-none}; runs check-loads-on.sh: $(has_text "tools/linux-appimage/check-loads-on.sh" "$loader_job" && echo yes || echo no)" \
    "the AppImage must be shown to load on the oldest tested systems before the release" \
    "run bash tools/linux-appimage/check-loads-on.sh for ubuntu:24.04@sha256:<digest> and debian:13@sha256:<digest>"
fi

# --- docker part -----------------------------------------------------------------------------------

work_dir="$(mktemp -d)"
mkdir -p "$work_dir/fx" "$work_dir/appimages"

cat > "$work_dir/build-fixtures.sh" <<'BUILD'
set -eu
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends gcc libc6-dev
cd /work/fx
printf 'int main(void){return 0;}\n' > plain.c
printf '#define _GNU_SOURCE\n#include <pthread.h>\nint main(void){return (int)pthread_gettid_np(pthread_self());}\n' > offender.c
printf 'int lh_missing_symbol(void){return 0;}\n' > missing_lib.c
printf 'int lh_missing_symbol(void);\nint main(void){return lh_missing_symbol();}\n' > missing_main.c
gcc plain.c -o plain
gcc offender.c -o offender
mkdir -p lib
gcc -shared -fPIC -Wl,-soname,liblh_missing.so.1 missing_lib.c -o lib/liblh_missing.so.1
ln -s liblh_missing.so.1 lib/liblh_missing.so
gcc missing_main.c -o missing -Llib -llh_missing
rm -rf lib
chown -R "$HOST_UID:$HOST_GID" /work/fx
BUILD

# A stand-in AppImage: extracting it puts the given ELF file into squashfs-root of the working directory.
make_fake_appimage() {
  local name=$1 elf=$2 dir="$work_dir/appimages/$1"
  mkdir -p "$dir/payload"
  if [ -n "$elf" ]; then cp "$work_dir/fx/$elf" "$dir/payload/lighthouse-test-binary"; fi
  cat > "$dir/Fake.AppImage" <<FAKE
#!/usr/bin/env bash
if [ "\${1:-}" = "--appimage-extract" ]; then
  rm -rf squashfs-root
  mkdir -p squashfs-root/usr/bin squashfs-root/usr/lib
  cp -a "$dir/payload/." squashfs-root/usr/bin/
fi
exit 0
FAKE
  chmod +x "$dir/Fake.AppImage"
}

run_in_fresh_dir() {
  local run_dir
  run_dir="$(mktemp -d -p "$work_dir")"
  check_output="$(cd "$run_dir" && "$@" 2>&1)"
  check_exit=$?
}

run_loader() {
  run_in_fresh_dir bash "$loader_script" "$1" "$2"
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

# Package install of the workflow, in a fresh container of the digest the workflow names.
probe_image="$workflow_image"
probe_note=""
if ! container_release_verdict "$workflow_image" "$floor_value"; then
  probe_image="$floor_image"
  probe_note=" (the workflow names no valid digest image, so the same Ubuntu 24.04 digest the floor documents is used)"
fi

printf '%s\n' "$install_step" | run_script_of > "$work_dir/install-step.sh"
install_container="$prefix-install"
created_containers+=("$install_container")
install_exit=1
install_output=""
installed_version=""
floor_exit=1
floor_output=""

if ! docker run -d --name "$install_container" -v "$work_dir:/work:ro" -v "$tools_dir:/tools:ro" \
  "$probe_image" sleep infinity >/dev/null 2>&1; then
  setup_failure "could not start $probe_image"
elif [ ! -s "$work_dir/install-step.sh" ]; then
  install_output="the workflow has no step with a run script that installs packages"
else
  install_output="$(docker exec "$install_container" bash -e /work/install-step.sh 2>&1)"
  install_exit=$?
  if [ "$install_exit" -eq 0 ]; then
    installed_version="$(docker exec "$install_container" dpkg-query -W -f='${Version}' libwebkit2gtk-4.1-0 2>/dev/null)"
    floor_output="$(docker exec "$install_container" bash -c '
      mkdir -p /layouts/webkit
      find /usr/lib -type f \( -name "libwebkit2gtk-4.1.so.0*" -o -name "libjavascriptcoregtk-4.1.so.0*" \) -exec cp {} /layouts/webkit/ \;
      bash /tools/check-glibc-floor.sh /layouts/webkit' 2>&1)"
    floor_exit=$?
  fi
fi

if [ "$setup_failed" -eq 0 ]; then
  if [ "$install_exit" -eq 0 ]; then
    pass "the package install of the workflow succeeds in a fresh container$probe_note"
  else
    check_output="$install_output"
    fail "the package install of the workflow succeeds in a fresh container$probe_note" \
      "the install step failed with exit $install_exit. Output: $(shown_output)" \
      "a missing package or tool stops the release build before the AppImage exists" \
      "make the install step run as root in the bare Ubuntu container and list only packages the archive has"
  fi

  if [ "$install_exit" -eq 0 ] && [ -n "$installed_version" ] && has_text "$installed_version" "$install_output"; then
    pass "the install step prints the installed WebKitGTK version $installed_version"
  else
    check_output="$install_output"
    fail "the install step prints the installed WebKitGTK version" \
      "the install step output does not contain the installed libwebkit2gtk-4.1-0 version '${installed_version:-unknown}'. Output: $(shown_output)" \
      "the maintainer needs to see which WebKitGTK the AppImage was built with" \
      "print the version, for example with dpkg-query -W libwebkit2gtk-4.1-0, in the install step"
  fi

  if [ "$install_exit" -eq 0 ] && [ "$floor_exit" -eq 0 ]; then
    pass "the installed libwebkit2gtk and libjavascriptcoregtk pass the glibc floor"
  else
    check_output="$floor_output"
    fail "the installed libwebkit2gtk and libjavascriptcoregtk pass the glibc floor" \
      "the floor check exit was $floor_exit (install exit $install_exit). Output: $(shown_output)" \
      "an unpinned WebKitGTK from the archive must still load on the floor distribution" \
      "keep the package list installable and the libraries within the floor, or raise the floor by a new decision"
  fi
fi

# --- the loader check on a distribution image ------------------------------------------------------

loader_container="$prefix-fixtures"
created_containers+=("$loader_container")
fixtures_ready=0
loader_script_present=0
if [ -f "$loader_script" ]; then
  loader_script_present=1
  pass "the loader check script tools/linux-appimage/check-loads-on.sh exists"
else
  fail "the loader check script tools/linux-appimage/check-loads-on.sh exists" \
    "tools/linux-appimage/check-loads-on.sh is not a regular file, so no loader case can run" \
    "without the script the loader cases would fail like a broken fixture and say nothing about the AppImage" \
    "create tools/linux-appimage/check-loads-on.sh taking IMAGE and APPIMAGE"
fi

if [ "$loader_script_present" -eq 1 ] && docker run -d --name "$loader_container" -v "$work_dir:/work" -e "HOST_UID=$(id -u)" -e "HOST_GID=$(id -g)" \
  "$newer_image" sleep infinity >/dev/null 2>&1; then
  if docker exec "$loader_container" bash /work/build-fixtures.sh >"$work_dir/build.log" 2>&1; then
    fixtures_ready=1
  else
    setup_failure "could not build the test ELF files in $newer_image: $(tail -n 5 "$work_dir/build.log")"
  fi
elif [ "$loader_script_present" -eq 1 ]; then
  setup_failure "could not start $newer_image"
fi

if [ "$fixtures_ready" -eq 1 ]; then
  make_fake_appimage plain plain
  make_fake_appimage offender offender
  make_fake_appimage missing missing
  make_fake_appimage empty ""

  run_loader "$floor_image" "$work_dir/appimages/does-not-exist.AppImage"
  expect_exit "a missing AppImage is not a pass" 2 \
    "a check that looked at nothing cannot say the AppImage loads" \
    "exit 2 when the AppImage file does not exist"

  run_loader "$floor_image" "$work_dir/appimages/empty/Fake.AppImage"
  expect_exit "an AppImage with no ELF file is not a pass" 2 \
    "a check that looked at nothing cannot say the AppImage loads" \
    "exit 2 when the extracted AppImage holds no ELF file"

  run_loader "$floor_image" "$work_dir/appimages/missing/Fake.AppImage"
  expect_exit "a bundled binary with a library no distribution has is refused" 1 \
    "an AppImage that cannot resolve a library does not start" \
    "exit 1 when ldd reports a library as not found"
  expect_output_has "a bundled binary with a library no distribution has is refused" "liblh_missing.so.1" \
    "the maintainer must see which library was not found" \
    "print the ldd line that says not found"

  run_loader "$floor_image" "$work_dir/appimages/offender/Fake.AppImage"
  expect_exit "a bundled binary needing a newer glibc than the distribution has is refused" 1 \
    "an AppImage needing a glibc the system lacks does not start" \
    "exit 1 when ldd reports a GLIBC version as not found"
  expect_output_has "a bundled binary needing a newer glibc than the distribution has is refused" "GLIBC_2.42" \
    "the maintainer must see which glibc version the system lacks" \
    "print the ldd line that says the version was not found"

  run_loader "$floor_image" "$work_dir/appimages/plain/Fake.AppImage"
  expect_exit "a bundled binary that resolves everything is accepted" 0 \
    "a loader check that refuses every AppImage would block every release" \
    "exit 0 when every bundled binary resolves its libraries and glibc symbols"
fi

if [ "$loader_script_present" -eq 0 ]; then
  echo "NOTE: loader cases skipped because the loader check script is missing"
elif [ -n "${APPIMAGE:-}" ]; then
  if [ ! -f "$APPIMAGE" ]; then
    setup_failure "APPIMAGE is set to '$APPIMAGE', which is not a file"
  else
    appimage_path="$(realpath "$APPIMAGE")"
    for image in "$ubuntu_loader_image" "$debian_loader_image"; do
      if [ -z "$image" ]; then
        fail "the finished AppImage resolves every bundled binary on a distribution the workflow lists" \
          "the loader job names no Ubuntu 24.04 or Debian 13 digest image to run on (found: ${loader_images:-none})" \
          "the oracle runs the loader check on the digests the release job will use" \
          "list ubuntu:24.04@sha256:<digest> and debian:13@sha256:<digest> in the loader job"
        continue
      fi
      run_loader "$image" "$appimage_path"
      expect_exit "the finished AppImage resolves every bundled binary on ${image%%@*}" 0 \
        "an AppImage that does not load on Ubuntu 24.04 or Debian 13 does not start for those users" \
        "bundle only what resolves there, or fix the distribution runtime package list of the loader check"
    done
  fi
else
  echo "NOTE: APPIMAGE is not set, so no real AppImage was loaded on Ubuntu 24.04 and Debian 13"
fi

summary_and_exit
