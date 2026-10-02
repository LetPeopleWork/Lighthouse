#!/usr/bin/env bash
# How the steps that read the database volume tell which folder is a copy of the database and which copy
# the database runs on. Sourced, never run: it only defines names, so sourcing it changes nothing.
#
# Every copy but the first was made from the one just below it by an upgrade, which leaves a note in the
# new copy naming the major it came from and the hash of that copy's pg_control. Following those notes
# up from the first copy leads to the copy the database last ran on: any start of an older copy rewrites
# its pg_control, so its note no longer matches and the chain ends there.

readonly MOUNT=/var/lib/postgresql/data
readonly UPGRADE_NOTE=.lighthouse-upgrade

# The major folder $1 holds when it counts as a copy, else nothing. A removal deletes PG_VERSION first, so
# a half-removed copy no longer counts; a pgdata-<K> only counts when it holds the major its name says,
# because only an upgrade into K ever makes one.
copy_major() {
  local folder="$1" version=""
  if [[ -r "$MOUNT/$folder/PG_VERSION" ]]; then
    version="$(cat "$MOUNT/$folder/PG_VERSION")"
  fi
  if [[ "$version" =~ ^[0-9]+$ ]] && [[ "$folder" == pgdata || "$folder" == "pgdata-$version" ]]; then
    echo "$version"
  fi
}

# One "folder major" line per copy, oldest major first. An unfinished or set-aside folder never counts.
copies() {
  local path folder major
  for path in "$MOUNT/pgdata" "$MOUNT"/pgdata-*; do
    folder="${path##*/}"
    if [[ -d "$path" ]] && [[ "$folder" == pgdata || "$folder" =~ ^pgdata-[0-9]+$ ]]; then
      major="$(copy_major "$folder")"
      if [[ -n "$major" ]]; then
        echo "$folder $major"
      fi
    fi
  done | sort -k2,2n
}

# Whether pgdata holds the note a removal of the old copy leaves behind.
has_placeholder() {
  local path
  for path in "$MOUNT"/pgdata/UPGRADED-TO-*; do
    if [[ -e "$path" ]]; then
      return 0
    fi
  done
  return 1
}

# Empty when the control file of folder $1 is missing, empty or unreadable. Only a hash actually computed
# may tie a newer copy to the one it was made from: an empty one matches no note.
control_hash() {
  local control="$MOUNT/$1/global/pg_control" hash=""
  if [[ -n "$1" && -f "$control" && -s "$control" && -r "$control" ]]; then
    hash="$(sha256sum "$control" 2>/dev/null | cut -d' ' -f1)" || hash=""
  fi
  echo "$hash"
}

# The first copy of the chain, as "folder major": pgdata when it counts, otherwise, once a removal has
# left its placeholder there, the oldest copy that does. Nothing when neither holds.
base() {
  local major
  major="$(copy_major pgdata)"
  if [[ -n "$major" ]]; then
    echo "pgdata $major"
  elif has_placeholder; then
    copies | awk 'NR == 1'
  fi
}

# The copy made from folder $1 (major $2), when it is still current: it counts, and its note names this
# major and the hash this copy's pg_control has now.
next_link() {
  local folder="$1" major="$2" next hash
  next="pgdata-$((major + 1))"
  hash="$(control_hash "$folder")"
  if [[ -n "$hash" && -n "$(copy_major "$next")" ]] \
    && grep -qx "source_major=$major" "$MOUNT/$next/$UPGRADE_NOTE" 2>/dev/null \
    && grep -qx "source_pg_control_sha256=$hash" "$MOUNT/$next/$UPGRADE_NOTE" 2>/dev/null; then
    echo "$next"
  fi
}

# Every copy on the chain, as "folder major" lines from the base up.
chain() {
  local folder major next
  read -r folder major <<<"$(base)"
  if [[ -z "$folder" ]]; then
    return 0
  fi
  echo "$folder $major"
  while next="$(next_link "$folder" "$major")" && [[ -n "$next" ]]; do
    folder="$next"
    major=$((major + 1))
    echo "$folder $major"
  done
}

# The copy the database last ran on, as "folder major"; nothing when the volume holds no chain.
live_copy() {
  chain | tail -n 1
}

# The folder on the chain that holds major $1, if any.
copy_for_major() {
  chain | awk -v major="$1" '$2 == major { print $1 }'
}
