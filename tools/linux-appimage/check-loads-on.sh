#!/usr/bin/env bash
# Fails when a bundled binary of the AppImage cannot resolve its libraries or glibc symbols on a distribution.
# Usage: check-loads-on.sh IMAGE APPIMAGE
# Exit 0: every ELF file resolves. Exit 1: a library or a GLIBC version was not found. Exit 2: the check could
# not look, which is never a pass. It shows the loader finds everything; it does not show the window opens.
set -uo pipefail
export LC_ALL=C

image="${1:-}"
appimage="${2:-}"

indeterminate() {
  echo "::error::loader check could not run: $1"
  echo "WHAT: $1"
  echo "WHY:  a check that looked at nothing cannot say the AppImage loads"
  echo "HOW:  $2"
  exit 2
}

if [ -z "$image" ] || [ -z "$appimage" ]; then
  indeterminate "usage is check-loads-on.sh IMAGE APPIMAGE" "pass a distribution image and the path of the AppImage"
fi
if [ ! -f "$appimage" ]; then
  indeterminate "AppImage '$appimage' is not a file" "download or build the AppImage first"
fi
if ! command -v docker >/dev/null 2>&1; then
  indeterminate "docker is not installed" "run the check where docker is available"
fi

appimage_path="$(realpath "$appimage")"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

chmod +x "$appimage_path"
if ! (cd "$work_dir" && "$appimage_path" --appimage-extract >/dev/null 2>&1); then
  indeterminate "'$appimage' could not be extracted" "check the AppImage runs with --appimage-extract"
fi
if [ ! -d "$work_dir/squashfs-root" ]; then
  indeterminate "extracting '$appimage' left no squashfs-root directory" "check the AppImage runs with --appimage-extract"
fi

# Runs inside the distribution image; its exit code is the verdict.
inner_script() {
  cat <<'INNER'
export DEBIAN_FRONTEND=noninteractive
root=/appimage/squashfs-root
if ! { apt-get update -qq && apt-get install -y -qq --no-install-recommends \
  libwebkit2gtk-4.1-0 libgtk-3-0t64 librsvg2-2 libayatana-appindicator3-1; } >/tmp/apt.log 2>&1; then
  tail -n 5 /tmp/apt.log
  echo "package install failed"
  exit 2
fi
lib_path="$(find "$root" -type f -name '*.so*' -exec dirname {} \; | sort -u | paste -sd: -)"
scanned=0
unresolved=0
while IFS= read -r file; do
  [ "$(head -c 4 "$file" | od -An -tx1 | tr -d ' \n')" = "7f454c46" ] || continue
  scanned=$((scanned + 1))
  found="$(LD_LIBRARY_PATH="$lib_path" ldd "$file" 2>&1 | grep -E 'not found' || true)"
  if [ -n "$found" ]; then
    unresolved=$((unresolved + 1))
    echo "${file#"$root"/}:"
    echo "$found"
  fi
done < <(find "$root" -type f | sort)
echo "scanned=$scanned unresolved=$unresolved"
[ "$scanned" -gt 0 ] || exit 2
[ "$unresolved" -eq 0 ] || exit 1
exit 0
INNER
}

output="$(inner_script | docker run --rm -i -v "$work_dir:/appimage:ro" "$image" bash -s 2>&1)"
status=$?
printf '%s\n' "$output"

case $status in
  0)
    echo "AppImage resolves every bundled binary on $image"
    exit 0
    ;;
  1)
    echo "::error::AppImage has a library or glibc version that $image does not provide"
    echo "WHAT: a bundled binary needs a library or GLIBC version not found on $image"
    echo "WHY:  the AppImage does not start for users of that distribution"
    echo "HOW:  bundle what is missing, or build on the distribution with the oldest glibc we support"
    exit 1
    ;;
  *)
    indeterminate "the loader check on $image ended with exit $status (no ELF file, failed package install or docker failure)" \
      "read the output above; fix the image, the package list or the AppImage"
    ;;
esac
