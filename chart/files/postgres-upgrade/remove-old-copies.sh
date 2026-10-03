#!/usr/bin/env bash
# The cleanup the Kubernetes docs give the operator: removes every copy of the database older than the one
# it runs on, the copy that one was made from included. The upgrade step removes old copies through the same
# function but always keeps that copy, because a rollback one chart back starts on it; this cleanup takes it
# too, so it only ever runs when the operator asks for it. It decides from reads alone and refuses before it
# removes anything when which copy is current cannot be told, as when the control file of the copy the walk up
# the copies stopped at is unreadable while a newer copy counts: the older copy may hold the newest data.
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

read -r live_folder live_major <<<"$(live_copy)"
if [[ -n "$live_major" && -z "$(control_hash "$live_folder")" && -n "$(copies_newer_than "$live_major")" ]]; then
  refuse "$live_folder/global/pg_control is unreadable, so which copy is current cannot be told; put that file back from a backup before removing anything"
fi

if [[ -z "$live_folder" ]]; then
  refuse "the volume holds no copy of the database"
fi
if [[ "$live_folder" == pgdata ]]; then
  refuse "the database runs on pgdata, the copy the volume was first set up with, so no copy is older than it"
fi

targets="$(copies_older_than "$live_major")"
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
  say "$(copy_names "$targets") could not be fully removed; run this again to finish removing them" >&2
  exit 1
fi
say "removed $(copy_names "$removed"); the database runs on $live_folder (Postgres $live_major), and a rollback to a chart on an older Postgres is no longer possible"
