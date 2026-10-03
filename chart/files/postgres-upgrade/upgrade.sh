#!/usr/bin/env bash
# Runs as root in the database image before every start of the database. It picks the data directory the
# server starts on and, when the copy the database runs on is one major behind this image, builds an
# upgraded copy beside it. The copy it read from stays as it was, where the previous chart release looks
# for it, so a rollback to that release still finds the database as it was before the upgrade.
#
# Anything it does not recognise is refused before a single byte on the volume is written: one line in
# the log and in the pod's termination message, then a non-zero exit so the kubelet retries by itself.
set -euo pipefail

# shellcheck source-path=SCRIPTDIR source=volume.sh
source "$(dirname "${BASH_SOURCE[0]}")/volume.sh"

readonly DOCS=https://docs.lighthouse.letpeople.work/Installation/kubernetes.html
readonly MANUAL_PATH="$DOCS#moving-data-two-or-more-majors-behind-by-hand"
readonly REFUSED_DOCS="$DOCS#when-an-upgrade-is-refused"
readonly STOPPED_CLEANUP_DOCS="$DOCS#removing-old-copies-while-the-database-is-stopped"

say() {
  echo "lighthouse-postgres: $*"
  return
}

refuse() {
  local line="lighthouse-postgres: $*. Nothing was changed"
  echo "$line" >&2
  printf '%s\n' "$line" >/dev/termination-log
  exit 1
}

# " -n <namespace>" when the pod can read its namespace, so a command built with it can be copied as it
# stands.
namespace_flag() {
  local namespace_file=/var/run/secrets/kubernetes.io/serviceaccount/namespace
  if [[ -r "$namespace_file" ]]; then
    echo " -n $(cat "$namespace_file")"
  fi
  return $?
}

# Kubernetes does not replace a database pod whose start keeps being refused when the chart's values
# change, so a fix made through values only takes effect once this pod is deleted.
delete_pod_hint() {
  echo "then run kubectl delete pod$(namespace_flag) $HOSTNAME so it starts again with the new values"
  return
}

# The programs this step runs sit where the official postgres images install them, and those images say
# which major they carry in PG_MAJOR.
if [[ ! "${PG_MAJOR:-}" =~ ^[0-9]+$ ]]; then
  refuse "refusing to start the bundled database: postgresql.image is not an official postgres image (it does not set PG_MAJOR), and the step that picks and upgrades its data needs one; set postgresql.image to an official image such as postgres:18-trixie, $(delete_pod_hint)"
fi

readonly MAJOR="$PG_MAJOR"
readonly PREVIOUS_MAJOR=$((MAJOR - 1))
readonly NEW="$MOUNT/pgdata-$MAJOR"
readonly PARTIAL="$NEW.partial"
readonly STALE="$NEW.stale"
readonly NEW_BIN="/usr/lib/postgresql/$MAJOR/bin"
readonly OLD_BINARIES=/old-binaries
readonly SOURCE_OS="$OLD_BINARIES/os-release"
readonly DECISION=/decision/pgdata
# The old server is only ever reached over a Unix socket here, never over the network.
readonly SOCKET_DIR=/tmp
readonly OLD_PORT=50431
readonly OLD_SERVER_WAIT_SECONDS=3600
readonly KIB_PER_MIB=1024
# The copy comes out no bigger than the data, which also carries WAL the copy leaves behind. The headroom
# on top covers what starting the old data writes, crash recovery after an unclean stop included, and the
# files a new, empty cluster brings.
readonly COPY_HEADROOM_PERCENT=10
readonly COPY_HEADROOM_FIXED_KIB=$((64 * KIB_PER_MIB))

as_postgres() {
  gosu postgres "$@"
  return $?
}

start_on() {
  local directory="$1"
  printf '%s\n' "$directory" >"$DECISION"
  return $?
}

# Every pgdata-* entry on the volume except an unfinished copy for this image's major, which an upgrade
# throws away before it starts.
other_copies() {
  local path
  for path in "$MOUNT"/pgdata-*; do
    if [[ -e "$path" && "$path" != "$PARTIAL" ]]; then
      echo "${path##*/}"
    fi
  done
  return $?
}

# The major of the one copy on the volume, when there is exactly one.
only_copy_major() {
  local copies="$1"
  if [[ "$copies" =~ ^pgdata-([0-9]+)$ ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
  return $?
}

# The folders of $1 joined as one phrase, and whether they take "is" or "are".
folder_list() {
  local folders="$1"
  echo "$folders" | paste -sd, - | sed 's/,/, /g'
  return $?
}

is_or_are() {
  local folders="$1"
  if [[ "$(wc -w <<<"$folders")" -gt 1 ]]; then echo are; else echo is; fi
  return
}

describe_volume() {
  local found
  if [[ -n "$DATA_MAJOR" ]]; then
    found="Postgres $DATA_MAJOR data in pgdata"
  elif has_placeholder; then
    found="no database in pgdata, only the note left when the old copy was removed"
  else
    found="no database in pgdata"
  fi
  if [[ -n "$OTHER_COPIES" ]]; then
    found="$found and $(folder_list "$OTHER_COPIES")"
  fi
  if [[ -e "$PARTIAL" ]]; then
    found="$found and an unfinished pgdata-$MAJOR.partial"
  fi
  echo "$found"
  return
}

os_field() {
  local file="$1" field="$2"
  sed -n "s/^$field=//p" "$file" | tr -d '"'
  return $?
}

os_name() {
  local file="$1"
  echo "$(os_field "$file" ID) $(os_field "$file" VERSION_ID)"
  return
}

# The suffix of an official postgres image tag built on the same system as this image: the Debian
# release name, or for Alpine, which has none, its version.
image_suffix() {
  local codename id version
  codename="$(os_field /etc/os-release VERSION_CODENAME)"
  id="$(os_field /etc/os-release ID)"
  version="$(os_field /etc/os-release VERSION_ID)"
  if [[ -n "$codename" ]]; then
    echo "-$codename"
  elif [[ -n "$id" && "$version" =~ ^([0-9]+\.[0-9]+) ]]; then
    echo "-$id${BASH_REMATCH[1]}"
  fi
  return $?
}

# The old programs run in this image, which only works when the upgrade-source image is built for the
# same operating system. Older charts did not say which system that was, so a missing note checks nothing.
ensure_old_programs_run_here() {
  local from="$1" source here
  [[ -f "$SOURCE_OS" ]] || return 0
  source="$(os_name "$SOURCE_OS")"
  here="$(os_name /etc/os-release)"
  if [[ "$source" != "$here" ]]; then
    refuse "refusing to upgrade Postgres $from to $MAJOR: the Postgres $from programs in postgresql.upgrade.image are built for $source and cannot run beside the Postgres $MAJOR image, built for $here; set postgresql.upgrade.image to postgres:$from$(image_suffix), $(delete_pod_hint)"
  fi
  return $?
}

ensure_old_programs_present() {
  local from="$1" old_bin="$2"
  if [[ ! -x "$old_bin/pg_ctl" || ! -x "$old_bin/postgres" ]]; then
    refuse "refusing to upgrade Postgres $from to $MAJOR: the upgrade-source image did not provide the Postgres $from programs; set postgresql.upgrade.image to a Postgres $from image, $(delete_pod_hint)"
  fi
  return $?
}

# Data newer than this image is never touched when no copy of this image's major is left to start on.
refuse_newer_without_copy() {
  local data="$1" where="$2"
  refuse "refusing to start Postgres $MAJOR: the data is Postgres $data, in $where, which is newer than this image, and no Postgres $MAJOR copy of it is left to start on; set postgresql.image back to Postgres $data or remove the pin on it, $(delete_pod_hint)"
}

# Each chart release that moves the image's major carries the data one major, from the copy the database runs
# on, so a copy further behind has to go through those releases first. A newer copy the older major has run
# past since it was made holds older data than the live copy, so it is never offered as a way back.
refuse_gap() {
  local out_of_date=""
  if [[ "$(is_or_are "$NEWER_THAN_LIVE")" == are ]]; then
    out_of_date="; $(folder_list "$NEWER_THAN_LIVE") are out of date because Postgres $LIVE_MAJOR ran after they were made"
  elif [[ -n "$NEWER_THAN_LIVE" ]]; then
    out_of_date="; $NEWER_THAN_LIVE is out of date because Postgres $LIVE_MAJOR ran after it was made"
  fi
  refuse "refusing to start Postgres $MAJOR: the database runs on Postgres $LIVE_MAJOR in $LIVE_COPY, $((MAJOR - LIVE_MAJOR)) majors behind Postgres $MAJOR, and the automatic upgrade moves one major per chart release, so this chart only upgrades data from Postgres $PREVIOUS_MAJOR$out_of_date; pin postgresql.image to postgres:$LIVE_MAJOR to start $LIVE_COPY again as it was, $(delete_pod_hint), then upgrade through each chart release that moved the major, one at a time, or move it by hand: $MANUAL_PATH"
}

# The walk up the copies stopped at the live copy because its control file cannot be read, so whether a newer
# copy is still current cannot be told. A removal takes a copy's PG_VERSION before anything else, so a copy that
# still counts never lost that file to one: only a backup brings it back. After a rollback the live copy may
# hold the newest data, so removing it is never offered as the way out.
refuse_unhashable_link() {
  refuse "refusing to start Postgres $MAJOR: $LIVE_COPY holds Postgres $LIVE_MAJOR data without a readable $LIVE_COPY/global/pg_control, so whether the newer $(folder_list "$NEWER_THAN_LIVE") $(is_or_are "$NEWER_THAN_LIVE") still current cannot be told; put that file back from a backup"
}

refuse_unreadable_live_copy() {
  refuse "refusing to upgrade Postgres $LIVE_MAJOR to $MAJOR: $LIVE_COPY holds Postgres $LIVE_MAJOR data without a readable $LIVE_COPY/global/pg_control, so Postgres $LIVE_MAJOR cannot open it to upgrade it; put that file back from a backup"
}

controldata() {
  local bin="$1" field="$2"
  as_postgres "$bin/pg_controldata" "$SOURCE" | sed -n "s/^$field: *//p"
  return $?
}

old_psql() {
  local sql="$1"
  as_postgres "$NEW_BIN/psql" -h "$SOCKET_DIR" -p "$OLD_PORT" -U "$POSTGRES_USER" -d template1 \
    -XAtq -v ON_ERROR_STOP=1 -c "$sql"
  return $?
}

# pg_upgrade only accepts a cleanly stopped source, and needs to be told the settings the old cluster was
# created with. Both come from one start of the old server, on a Unix socket only, and a fast stop. When
# the old server was killed rather than stopped, that start is also its crash recovery.
read_old_cluster_settings() {
  local old_bin="$1" from="$2"
  if [[ "$(controldata "$old_bin" "Database cluster state")" != "shut down" || -e "$SOURCE/postmaster.pid" ]]; then
    say "the Postgres $from data was not shut down cleanly; starting it once with Postgres $from to recover it"
  else
    say "reading the settings of the Postgres $from database"
  fi
  as_postgres "$old_bin/pg_ctl" -D "$SOURCE" -w --timeout="$OLD_SERVER_WAIT_SECONDS" \
    -o "-c listen_addresses='' -c unix_socket_directories=$SOCKET_DIR -p $OLD_PORT" start
  local template1
  SUPERUSER="$(old_psql "SELECT rolname FROM pg_authid WHERE oid = 10")"
  # The locale columns differ between majors (Postgres 16 calls datlocale daticulocale), so they are read
  # by name from the row as JSON, where a column the old major lacks reads as empty instead of failing.
  template1="$(old_psql "SELECT pg_encoding_to_char(encoding), datcollate, datctype,
                                coalesce(to_jsonb(d) ->> 'datlocprovider', 'c'),
                                coalesce(to_jsonb(d) ->> 'datlocale', to_jsonb(d) ->> 'daticulocale', ''),
                                coalesce(to_jsonb(d) ->> 'daticurules', '')
                         FROM pg_database d WHERE datname = 'template1'")"
  IFS='|' read -r ENCODING COLLATE CTYPE PROVIDER LOCALE ICU_RULES <<<"$template1"
  as_postgres "$old_bin/pg_ctl" -D "$SOURCE" -m fast -w --timeout="$OLD_SERVER_WAIT_SECONDS" stop
  say "the Postgres $from data is shut down cleanly"
  return $?
}

initdb_like_old_cluster() {
  local old_bin="$1"
  local args=(-D "$PARTIAL" -U "$SUPERUSER" --encoding="$ENCODING" --lc-collate="$COLLATE" --lc-ctype="$CTYPE")
  case "$PROVIDER" in
    i) args+=(--locale-provider=icu --icu-locale="$LOCALE") ;;
    b) args+=(--locale-provider=builtin --builtin-locale="$LOCALE") ;;
    *) args+=(--locale-provider=libc) ;;
  esac
  if [[ -n "$ICU_RULES" ]]; then
    args+=(--icu-rules="$ICU_RULES")
  fi
  # initdb turns checksums on by default from Postgres 18, which is also when it learnt to be told not to.
  if [[ "$(controldata "$old_bin" "Data page checksum version")" == "0" ]]; then
    if [[ "$MAJOR" -ge 18 ]]; then
      args+=(--no-data-checksums)
    fi
  else
    args+=(--data-checksums)
  fi
  as_postgres "$NEW_BIN/initdb" "${args[@]}"
  return $?
}

kib_in() {
  local path="$1"
  du -sk "$path" | cut -f1
  return $?
}

# Copies older than the one the upgrade reads from only serve a rollback further back, so removing them is
# one more way to make room. This step is what keeps the database pod from starting, so the removal runs from
# a pod of its own while the database is stopped.
older_copies_hint() {
  local older
  older="$(copies_older_than "$LIVE_MAJOR")"
  if [[ -n "$older" ]]; then
    echo "; or remove $(copy_names "$older"), older than $LIVE_COPY, while the database is stopped, as $STOPPED_CLEANUP_DOCS shows"
  fi
  return $?
}

# A leftover unfinished copy is thrown away before the upgrade starts, so its space counts as free. An
# out-of-date copy is only removed once its replacement is in place, so its space does not.
ensure_room_for_copy() {
  local from="$1" data_kib needed_kib free_kib
  data_kib="$(kib_in "$SOURCE")"
  needed_kib=$((data_kib * (100 + COPY_HEADROOM_PERCENT) / 100 + COPY_HEADROOM_FIXED_KIB))
  free_kib="$(df -Pk "$MOUNT" | awk 'NR == 2 { print $4 }')"
  if [[ -e "$PARTIAL" ]]; then
    free_kib=$((free_kib + $(kib_in "$PARTIAL")))
  fi
  if [[ "$free_kib" -lt "$needed_kib" ]]; then
    refuse "refusing upgrade $from→$MAJOR: need $(((needed_kib + KIB_PER_MIB - 1) / KIB_PER_MIB)) MiB, $((free_kib / KIB_PER_MIB)) MiB free; grow the volume claim itself with kubectl patch pvc$(namespace_flag) data-$HOSTNAME ($REFUSED_DOCS shows how) or pin postgresql.image to postgres:$from$(image_suffix) and $(delete_pod_hint)$(older_copies_hint)"
  fi
  return $?
}

# An upgrade that redoes an out-of-date copy sets that copy aside as pgdata-M.stale until the new one is in
# place. When a stop in between left it there, it goes back where it was unless its replacement made it,
# so the volume is never left without the copy the database last ran on.
settle_set_aside_copy() {
  [[ -e "$STALE" ]] || return 0
  if [[ -e "$NEW" ]]; then
    say "removing pgdata-$MAJOR.stale, the out-of-date copy an earlier upgrade replaced"
    rm -rf "$STALE"
  else
    say "putting pgdata-$MAJOR.stale back as pgdata-$MAJOR: an earlier upgrade set it aside and stopped before its replacement was in place"
    mv -T "$STALE" "$NEW"
  fi
  return $?
}

set_earlier_copy_aside() {
  local from="$1"
  if [[ -e "$PARTIAL" ]]; then
    say "removing pgdata-$MAJOR.partial, left by an earlier attempt that did not finish"
    rm -rf "$PARTIAL"
  fi
  if [[ -e "$NEW" ]]; then
    say "pgdata-$MAJOR is out of date: Postgres $from has run on $LIVE_COPY since that copy was made, so it is discarded and the upgrade redone from $LIVE_COPY"
    mv -T "$NEW" "$STALE"
  fi
  return $?
}

# Every later start of the old major rewrites pg_control, so this hash tells whether the copy still
# matches the data it was made from.
write_upgrade_note() {
  local from="$1" old_bin="$2"
  as_postgres tee "$PARTIAL/$UPGRADE_NOTE" >/dev/null <<EOF
source_major=$from
source_system_identifier=$(controldata "$old_bin" "Database system identifier")
source_pg_control_sha256=$(control_hash "$LIVE_COPY")
EOF
  return $?
}

upgrade() {
  local from="$1"
  local old_bin="$OLD_BINARIES/usr/lib/postgresql/$from/bin"
  ensure_old_programs_run_here "$from"
  ensure_old_programs_present "$from" "$old_bin"
  ensure_room_for_copy "$from"

  set_earlier_copy_aside "$from"
  say "upgrading the Postgres $from data in $LIVE_COPY to Postgres $MAJOR in pgdata-$MAJOR; $LIVE_COPY is kept as it is"
  mkdir "$PARTIAL"
  chown postgres:postgres "$PARTIAL"
  chmod 0700 "$PARTIAL"

  read_old_cluster_settings "$old_bin" "$from"
  initdb_like_old_cluster "$old_bin"
  (cd "$SOCKET_DIR" && as_postgres "$NEW_BIN/pg_upgrade" --copy \
    -b "$old_bin" -B "$NEW_BIN" -d "$SOURCE" -D "$PARTIAL" -U "$SUPERUSER" --socketdir="$SOCKET_DIR")
  # A cluster made by initdb alone accepts no password logins over the network, which is how Lighthouse
  # connects, so the old cluster's rules come across with the data.
  as_postgres cp "$SOURCE/pg_hba.conf" "$SOURCE/pg_ident.conf" "$PARTIAL/"

  write_upgrade_note "$from" "$old_bin"
  sync
  mv -T "$PARTIAL" "$NEW"
  sync "$MOUNT"
  rm -rf "$STALE"
  say "upgrade finished: Postgres $MAJOR starts on pgdata-$MAJOR, and the Postgres $from data stays in $LIVE_COPY"
  return $?
}

# --- what the volume holds: one predicate per kind of volume, tried in the order main lists them ------

volume_is_empty() {
  [[ -z "$DATA_MAJOR" && -z "$OTHER_COPIES" && ! -e "$PARTIAL" ]] && ! has_placeholder
  return $?
}

# The walk up the copies stopped at the live copy because its control file cannot be read, while a newer copy
# counts.
live_copy_unhashable_below_newer_copy() {
  [[ -n "$LIVE_MAJOR" && -z "$LIVE_CONTROL_HASH" && -n "$NEWER_THAN_LIVE" ]]
  return $?
}

# The database last ran on a copy of this image's major, and no copy newer than it is on the volume.
live_is_this_major_alone() {
  [[ "$LIVE_MAJOR" == "$MAJOR" && -z "$NEWER_COPY" ]]
  return $?
}

# The database last ran on a copy of this image's major, beside a newer copy that a pin or a rollback
# left out of date: this major has run since that copy was made from it.
live_is_this_major_beside_newer_copy() {
  [[ "$LIVE_MAJOR" == "$MAJOR" && -n "$NEWER_COPY" ]]
  return $?
}

# The image was moved back after an upgrade, by a pin or a rollback, and this is its first start since: the
# database last ran on a newer copy, and the copy of this major it was made from is still on the chain.
moved_back_to_a_copy_on_the_chain() {
  [[ -n "$LIVE_MAJOR" && -n "$THIS_MAJOR_ON_CHAIN" ]] && [[ "$LIVE_MAJOR" -gt "$MAJOR" ]]
  return $?
}

live_copy_one_behind_unreadable() {
  [[ "$LIVE_MAJOR" == "$PREVIOUS_MAJOR" && -z "$LIVE_CONTROL_HASH" ]]
  return $?
}

# The database last ran on a copy one major behind this image. Either no copy of this major is on the volume
# yet, or the one there is out of date: it is off the chain, so the older major has run since it was made
# and holds the newer data.
live_copy_one_behind() {
  live_copy_upgradable_to "$MAJOR"
  return $?
}

live_copy_two_or_more_behind() {
  [[ -n "$LIVE_MAJOR" ]] && [[ "$LIVE_MAJOR" -lt "$PREVIOUS_MAJOR" ]]
  return $?
}

# The database last ran on a copy newer than this image, and the chain holds no copy of this image's major
# to start on: two charts back after a second upgrade, or a pin to a major whose copy was removed.
live_newer_without_copy_of_this_major() {
  [[ -n "$LIVE_MAJOR" && -z "$THIS_MAJOR_ON_CHAIN" ]] && [[ "$LIVE_MAJOR" -gt "$MAJOR" ]]
  return $?
}

only_newer_copy_left() {
  [[ -z "$DATA_MAJOR" && -n "$ONLY_COPY_MAJOR" && ! -e "$PARTIAL" ]] && [[ "$ONLY_COPY_MAJOR" -gt "$MAJOR" ]]
  return $?
}

say_nothing_to_upgrade() {
  local source
  source="$(copy_for_major "$PREVIOUS_MAJOR")"
  if [[ "$LIVE_COPY" == pgdata ]]; then
    say "nothing to upgrade: the data in pgdata is already Postgres $MAJOR"
  elif [[ -z "$source" ]]; then
    say "nothing to upgrade: the old copy was removed, and Postgres $MAJOR starts on $LIVE_COPY"
  elif finishing_needed "$MAJOR"; then
    say "nothing to upgrade: $LIVE_COPY is already the upgrade of the Postgres $PREVIOUS_MAJOR data in $source, and Postgres $MAJOR starts on it"
  else
    say "nothing to upgrade and nothing to remove: $LIVE_COPY is already the upgrade of the Postgres $PREVIOUS_MAJOR data in $source, and Postgres $MAJOR starts on it"
  fi
  return $?
}

# Removes every copy older than the one the copy this major starts on was made from, and leaves what went in
# REMOVED. It only ever runs after the start decision is written, so a removal that fails costs room alone:
# one warning, the database starts on folder $1 all the same, and the next start tries again.
remove_older_copies() {
  local starts_on="$1" targets
  targets="$(copies_older_than "$PREVIOUS_MAJOR")"
  if ! REMOVED="$(remove_copies_older_than "$PREVIOUS_MAJOR" "$MAJOR")"; then
    say "warning: $(copy_names "$targets") could not be fully removed; Postgres $MAJOR starts on $starts_on all the same, what is left only takes room, and the next start tries again"
    return 1
  fi
  return $?
}

# Once the new copy is in place, the copy it was made from is all a rollback one chart back needs, so every
# older copy goes.
remove_copy_before_last() {
  local newest
  remove_older_copies "pgdata-$MAJOR" || return 0
  [[ -n "$REMOVED" ]] || return 0
  newest="$(awk 'NF == 2 { newest = $2 } END { print newest }' <<<"$REMOVED")"
  say "removed $(copy_names "$REMOVED"), the copy before the one this upgrade read from; a rollback to a chart on Postgres ${newest:-$((PREVIOUS_MAJOR - 1))} is no longer possible, a rollback to the chart on Postgres $PREVIOUS_MAJOR still is"
  return $?
}

# A removal a stop cut off part-way, or one that failed, is finished by a later start on the copy it left
# live. Only by that start: one on a pinned-back copy keeps every folder.
finish_earlier_removal() {
  finishing_needed "$MAJOR" || return 0
  remove_older_copies "$LIVE_COPY" || return 0
  [[ -n "$REMOVED" ]] || return 0
  say "finished removing $(copy_names "$REMOVED"), which an earlier start left behind; Postgres $MAJOR starts on $LIVE_COPY"
  return $?
}

# The newer copy is left where it is. Once this major has run, the copy it was made from no longer matches
# the note in the newer copy, so moving the image forward again redoes the upgrade from the copy started here.
warn_newer_copy_exists() {
  local folder="$1" newer="$2"
  say "warning: starting Postgres $MAJOR on $folder, but a newer Postgres ${newer#pgdata-} copy of this database exists in $newer; what was written on that copy is not in this database, and moving to Postgres ${newer#pgdata-} again redoes the upgrade from this copy, so those writes do not come back"
  return $?
}

main() {
  settle_set_aside_copy
  DATA_MAJOR="$(copy_major pgdata)"
  OTHER_COPIES="$(other_copies)"
  ONLY_COPY_MAJOR="$(only_copy_major "$OTHER_COPIES")"
  # Every upgrade reads the copy the database last ran on, which is pgdata until an upgrade has moved on.
  read -r LIVE_COPY LIVE_MAJOR <<<"$(live_copy)"
  SOURCE="$MOUNT/$LIVE_COPY"
  LIVE_CONTROL_HASH="$(control_hash "$LIVE_COPY")"
  read -r NEWER_COPY _ <<<"$(copies_newer_than "$MAJOR" | tail -n 1)"
  # Every copy newer than the live one is off the chain: its note no longer matches, or cannot be checked.
  NEWER_THAN_LIVE=""
  if [[ -n "$LIVE_MAJOR" ]]; then
    NEWER_THAN_LIVE="$(copies_newer_than "$LIVE_MAJOR" | cut -d' ' -f1)"
  fi
  THIS_MAJOR_ON_CHAIN="$(copy_for_major "$MAJOR")"

  if volume_is_empty; then
    say "nothing to upgrade: the volume holds no database yet, so Postgres $MAJOR creates one in pgdata"
    start_on "$MOUNT/pgdata"
  elif live_copy_unhashable_below_newer_copy; then
    refuse_unhashable_link
  elif live_is_this_major_alone; then
    say_nothing_to_upgrade
    start_on "$SOURCE"
    finish_earlier_removal
  elif live_is_this_major_beside_newer_copy; then
    warn_newer_copy_exists "$LIVE_COPY" "$NEWER_COPY"
    start_on "$SOURCE"
  elif moved_back_to_a_copy_on_the_chain; then
    warn_newer_copy_exists "$THIS_MAJOR_ON_CHAIN" "$LIVE_COPY"
    start_on "$MOUNT/$THIS_MAJOR_ON_CHAIN"
  elif live_copy_one_behind_unreadable; then
    refuse_unreadable_live_copy
  elif live_copy_one_behind; then
    upgrade "$LIVE_MAJOR"
    start_on "$NEW"
    remove_copy_before_last
  elif live_copy_two_or_more_behind; then
    refuse_gap
  elif live_newer_without_copy_of_this_major; then
    refuse_newer_without_copy "$LIVE_MAJOR" "$LIVE_COPY"
  elif only_newer_copy_left; then
    refuse_newer_without_copy "$ONLY_COPY_MAJOR" "pgdata-$ONLY_COPY_MAJOR"
  else
    refuse "refusing to start Postgres $MAJOR: found $(describe_volume), which this chart cannot start on or upgrade by itself"
  fi
  return $?
}

main
