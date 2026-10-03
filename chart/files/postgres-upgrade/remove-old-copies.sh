#!/usr/bin/env bash
# The cleanup the Kubernetes docs give the operator: removes every copy of the database older than the one
# it runs on, the copy that one was made from included. The upgrade step removes old copies through the same
# function but always keeps that copy, because a rollback one chart back starts on it; this cleanup takes it
# too, so it only ever runs when the operator asks for it. It decides from reads alone and refuses before it
# removes anything when which copy is current cannot be told. The one exception is a pgdata whose control
# file is gone where a removal had visibly started: it holds the placeholder, or it is one major behind this
# image beside a copy of this image's major, which the upgrade step also reads as a removal cut off part-way.
set -euo pipefail

# shellcheck source-path=SCRIPTDIR source=volume.sh
source "$(dirname "${BASH_SOURCE[0]}")/volume.sh"

say() {
  echo "lighthouse-postgres: $*"
}

refuse() {
  echo "lighthouse-postgres: refusing to remove old copies: $*. Nothing was removed" >&2
  exit 1
}

newer_copies_than() {
  copies | awk -v major="$1" '$2 > major'
}

removal_started_in_pgdata() {
  local major="$1"
  has_placeholder \
    || { [[ "${PG_MAJOR:-}" =~ ^[0-9]+$ && "$major" == "$((PG_MAJOR - 1))" && -e "$MOUNT/pgdata-$PG_MAJOR" ]]; }
}

names_of() {
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

read -r live_folder live_major <<<"$(live_copy)"
if [[ -n "$live_major" && -z "$(control_hash "$live_folder")" && -n "$(newer_copies_than "$live_major")" ]]; then
  if [[ "$live_folder" != pgdata ]] || ! removal_started_in_pgdata "$live_major"; then
    refuse "$live_folder/global/pg_control is unreadable, so which copy is current cannot be told; put that file back from a backup before removing anything"
  fi
  read -r live_folder live_major <<<"$(newer_copies_than "$live_major" | tail -n 1)"
fi

if [[ -z "$live_folder" ]]; then
  refuse "the volume holds no copy of the database"
fi
if [[ "$live_folder" == pgdata ]]; then
  refuse "the database runs on pgdata, the copy the volume was first set up with, so no copy is older than it"
fi

targets="$(copies_older_than "$live_major" | sort -n | awk '{ print $2, ($1 == 0 ? "" : $1) }')"
while read -r folder _; do
  if [[ -n "$folder" && -e "$MOUNT/$folder/postmaster.pid" ]]; then
    refuse "$folder holds a postmaster.pid, so a database may be running on it; stop that database first"
  fi
done <<<"$targets"

if [[ -z "$targets" ]]; then
  say "nothing to remove: no copy older than $live_folder (Postgres $live_major), the copy the database runs on, is left"
  exit 0
fi
if ! removed="$(remove_copies_older_than "$live_major" "$live_major")"; then
  say "$(names_of "$targets") could not be fully removed; run this again to finish removing them" >&2
  exit 1
fi
say "removed $(names_of "$removed"); the database runs on $live_folder (Postgres $live_major), and a rollback to a chart on an older Postgres is no longer possible"
