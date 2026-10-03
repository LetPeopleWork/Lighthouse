#!/usr/bin/env bash
# How the steps that read the database volume tell which folder is a copy of the database and which copy
# the database runs on, and how they remove and name the older ones. Sourced, never run: it only defines
# names, so sourcing it changes nothing.
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

# The copies of a major above $1, as "folder major" lines, oldest first.
copies_newer_than() {
  copies | awk -v major="$1" '$2 > major'
}

# Whether the copy the database last ran on can be upgraded to major $1: it is one major behind, Postgres can
# open it, and a copy of major $1 already on the volume was made by an upgrade, so it is only out of date.
live_copy_upgradable_to() {
  local target="$1" folder major
  read -r folder major <<<"$(live_copy)"
  [[ -n "$major" && "$major" == "$((target - 1))" && -n "$(control_hash "$folder")" ]] \
    && [[ ! -e "$MOUNT/pgdata-$target" || -f "$MOUNT/pgdata-$target/$UPGRADE_NOTE" ]]
}

# The folder on the chain that holds major $1, if any.
copy_for_major() {
  chain | awk -v major="$1" '$2 == major { print $1 }'
}

# Whether pgdata holds anything besides the note a removal of the old copy leaves behind.
pgdata_beyond_placeholder() {
  [[ -n "$(find "$MOUNT/pgdata" -mindepth 1 -maxdepth 1 ! -name 'UPGRADED-TO-*' -print -quit 2>/dev/null)" ]]
}

# Whether pgdata shows that a removal of the old copy had started there, for an image of major $1: it holds
# the placeholder, or it still counts one major below that image beside a copy of the image's major, which is
# what a removal written before the placeholder existed leaves when it is cut off.
removal_started_in_pgdata() {
  local image_major="$1"
  if has_placeholder; then
    return 0
  fi
  [[ "$image_major" =~ ^[0-9]+$ ]] \
    && [[ "$(copy_major pgdata)" == "$((image_major - 1))" && -e "$MOUNT/pgdata-$image_major" ]]
}

# The folders a removal with bound $1 takes, as "folder major" lines, oldest first. A pgdata-<K> goes
# whether it counts or not, since a half-removed one no longer does. pgdata goes when it counts below the
# bound, or when it no longer counts but holds the placeholder, which shows a removal had started there; its
# major is then left out, as it can no longer be told, and it goes first. A pgdata holding neither is never
# touched: nothing shows it was ever a copy.
copies_older_than() {
  local bound="$1" major path folder
  {
    major="$(copy_major pgdata)"
    if [[ -n "$major" ]]; then
      if [[ "$major" -lt "$bound" ]]; then
        echo "$major pgdata"
      fi
    elif has_placeholder && pgdata_beyond_placeholder; then
      echo "0 pgdata"
    fi
    for path in "$MOUNT"/pgdata-*; do
      folder="${path##*/}"
      if [[ -d "$path" && "$folder" =~ ^pgdata-([0-9]+)$ ]] && [[ "${BASH_REMATCH[1]}" -lt "$bound" ]]; then
        echo "${BASH_REMATCH[1]} $folder"
      fi
    done
  } | sort -n | awk '{ print $2, ($1 == 0 ? "" : $1) }'
}

# "folder (Postgres K)" for each "folder major" line of $1, or "what was left of folder" when the major can
# no longer be told, joined by commas.
copy_names() {
  local folder major names=""
  while read -r folder major; do
    [[ -n "$folder" ]] || continue
    if [[ -n "$major" ]]; then
      names="${names:+$names, }$folder (Postgres $major)"
    else
      names="${names:+$names, }what was left of $folder"
    fi
  done <<<"$1"
  echo "$names"
}

# Whether a start on a copy of major $1 has a removal to finish: pgdata holds the placeholder and more, a
# pgdata-<K> below the copy that copy was made from is on the volume whether it counts or not, or pgdata
# still counts below it. The last is what a stop between the new copy's rename and its placeholder leaves, a
# gap of milliseconds. Each is exactly what a removal with that bound would take.
finishing_needed() {
  local live="$1"
  [[ -n "$(copies_older_than "$((live - 1))")" ]]
}

# Removes every copy older than major $1 and nothing else, for a database that runs on a copy of major $2.
# A bound above that major removes nothing, so the live copy and anything newer are never touched. pgdata
# first gets the placeholder that keeps an older chart from creating an empty database in it; then, oldest
# first, each folder loses PG_VERSION before anything else, so one cut off part-way no longer counts as a
# copy. Prints one "folder major" line per folder removed, the major left out when it can no longer be told.
remove_copies_older_than() {
  local bound="$1" live="$2" targets folder major dir
  [[ "$bound" =~ ^[0-9]+$ && "$live" =~ ^[0-9]+$ ]] || return 1
  [[ "$bound" -le "$live" ]] || return 1
  targets="$(copies_older_than "$bound")"
  if grep -q '^pgdata ' <<<"$targets" && ! has_placeholder; then
    dir="$MOUNT/pgdata"
    install -m 0600 -o "$(stat -c %u "$dir")" -g "$(stat -c %g "$dir")" /dev/null \
      "$dir/UPGRADED-TO-$live-see-kubernetes-docs" || return 1
    sync || return 1
  fi
  while read -r folder major; do
    [[ -n "$folder" ]] || continue
    dir="$MOUNT/$folder"
    rm -f "$dir/PG_VERSION" || return 1
    sync || return 1
    if [[ "$folder" == pgdata ]]; then
      find "$dir" -mindepth 1 -maxdepth 1 ! -name 'UPGRADED-TO-*' -exec rm -rf {} + || return 1
    else
      rm -rf "$dir" || return 1
    fi
    echo "$folder${major:+ $major}"
  done <<<"$targets"
}
