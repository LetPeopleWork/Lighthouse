#!/usr/bin/env bash
# Fails when any ELF file under DIR needs a newer glibc than the written minimum.
# Usage: check-glibc-floor.sh DIR [FLOOR]
# Exit 0: every file is within the floor. Exit 1: a file needs a newer glibc. Exit 2: the check could not
# read what it needs, which is never a pass.
set -uo pipefail
export LC_ALL=C

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
floor_file="$script_dir/glibc-floor"

scan_dir="${1:-}"
floor="${2:-}"

indeterminate() {
  echo "::error::floor check could not run: $1"
  echo "WHAT: $1"
  echo "WHY:  a check that read nothing cannot say the AppImage is within the glibc floor"
  echo "HOW:  $2"
  exit 2
}

if [ -z "$scan_dir" ] || [ ! -d "$scan_dir" ]; then
  indeterminate "directory '${scan_dir}' does not exist or is not a directory" \
    "extract the AppImage first and pass the extracted directory"
fi
if ! command -v readelf >/dev/null 2>&1; then
  indeterminate "readelf is not installed, so no GLIBC version can be read" "install binutils"
fi
if [ -z "$floor" ]; then
  floor="$(sed -n 's/^FLOOR=\([0-9][0-9.]*\)[[:space:]]*$/\1/p' "$floor_file" 2>/dev/null | head -n 1)"
  if [ -z "$floor" ]; then
    indeterminate "no FLOOR=<version> line in $floor_file" "write the minimum glibc as FLOOR=2.39 in $floor_file"
  fi
fi

# Numeric, field by field: 2.9 is older than 2.39 and 2.100 is newer than 2.39.
version_greater() {
  local -a left right
  local i a b
  IFS=. read -r -a left <<< "$1"
  IFS=. read -r -a right <<< "$2"
  for ((i = 0; i < ${#left[@]} || i < ${#right[@]}; i++)); do
    a=$((10#${left[i]:-0}))
    b=$((10#${right[i]:-0}))
    if [ "$a" -gt "$b" ]; then return 0; fi
    if [ "$a" -lt "$b" ]; then return 1; fi
  done
  return 1
}

is_elf() {
  [ "$(head -c 4 "$1" 2>/dev/null | od -An -tx1 | tr -d ' \n')" = "7f454c46" ]
}

# The GLIBC versions the file requires (its version-needs section only, not versions it defines).
needed_glibc_versions() {
  readelf -V "$1" 2>/dev/null | awk '
    /^Version needs section/ { in_needs = 1; next }
    /^Version (symbols|definition) section/ { in_needs = 0 }
    in_needs && match($0, /GLIBC_[0-9][0-9.]*/) { print substr($0, RSTART + 6, RLENGTH - 6) }
  '
}

links_libc() {
  readelf -d "$1" 2>/dev/null | grep -q -F '[libc.so.6]'
}

scanned=0
over_floor=0
unreadable=0
highest=""

while IFS= read -r -d '' file; do
  is_elf "$file" || continue
  scanned=$((scanned + 1))

  file_highest=""
  while IFS= read -r version; do
    if [ -z "$file_highest" ] || version_greater "$version" "$file_highest"; then file_highest="$version"; fi
  done < <(needed_glibc_versions "$file")

  if [ -z "$file_highest" ]; then
    if links_libc "$file"; then
      unreadable=$((unreadable + 1))
      echo "::error::no GLIBC version could be read from $file"
      echo "WHAT: $file links libc.so.6 but readelf -V gave no GLIBC versions"
      echo "WHY:  a changed readelf output format must not read as a file that needs nothing"
      echo "HOW:  run readelf -V on the file and update the parsing in $script_dir/check-glibc-floor.sh"
    fi
    continue
  fi

  if [ -z "$highest" ] || version_greater "$file_highest" "$highest"; then highest="$file_highest"; fi
  if version_greater "$file_highest" "$floor"; then
    over_floor=$((over_floor + 1))
    echo "::error::$file needs GLIBC_$file_highest, above the floor $floor"
    echo "WHAT: $file needs GLIBC_$file_highest"
    echo "WHY:  the written floor is glibc $floor ($floor_file); a newer requirement stops the AppImage starting on the floor distribution"
    echo "HOW:  build in the pinned Ubuntu 24.04 container, or raise the floor on purpose by a new decision in $floor_file"
  fi
done < <(find "$scan_dir" -type f -print0)

echo "scanned=$scanned highest=GLIBC_${highest:-none} over_floor=$over_floor"

if [ "$scanned" -eq 0 ]; then
  indeterminate "no ELF file found under $scan_dir" "check that the AppImage extracted into this directory"
fi
if [ "$over_floor" -gt 0 ]; then exit 1; fi
if [ "$unreadable" -gt 0 ]; then exit 2; fi
echo "Every ELF file under $scan_dir needs at most GLIBC_$floor"
exit 0
