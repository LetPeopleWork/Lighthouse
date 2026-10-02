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

readonly OLD="$MOUNT/pgdata"
readonly DOCS=https://docs.lighthouse.letpeople.work/Installation/kubernetes.html
readonly MANUAL_PATH="$DOCS#moving-data-two-or-more-majors-behind-by-hand"
readonly REFUSED_DOCS="$DOCS#when-an-upgrade-is-refused"
readonly CUT_OFF_REMOVAL_DOCS="$DOCS#when-removing-the-old-copy-was-cut-off"

say() {
  echo "lighthouse-postgres: $*"
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
}

# Kubernetes does not replace a database pod whose start keeps being refused when the chart's values
# change, so a fix made through values only takes effect once this pod is deleted.
delete_pod_hint() {
  echo "then run kubectl delete pod$(namespace_flag) $HOSTNAME so it starts again with the new values"
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
}

start_on() {
  printf '%s\n' "$1" >"$DECISION"
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
}

# The major of the one copy on the volume, when there is exactly one.
only_copy_major() {
  local copies="$1"
  if [[ "$copies" =~ ^pgdata-([0-9]+)$ ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
}

# The highest major among the pgdata-<major> copies on the volume: the one holding the newest data.
newest_copy_major() {
  local copy newest=""
  for copy in $OTHER_COPIES; do
    if [[ "$copy" =~ ^pgdata-([0-9]+)$ ]] && [[ -z "$newest" || "${BASH_REMATCH[1]}" -gt "$newest" ]]; then
      newest="${BASH_REMATCH[1]}"
    fi
  done
  echo "$newest"
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
    found="$found and $(echo "$OTHER_COPIES" | paste -sd, - | sed 's/,/, /g')"
  fi
  if [[ -e "$PARTIAL" ]]; then
    found="$found and an unfinished pgdata-$MAJOR.partial"
  fi
  echo "$found"
}

os_field() {
  sed -n "s/^$2=//p" "$1" | tr -d '"'
}

os_name() {
  echo "$(os_field "$1" ID) $(os_field "$1" VERSION_ID)"
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
}

ensure_old_programs_present() {
  local from="$1" old_bin="$2"
  if [[ ! -x "$old_bin/pg_ctl" || ! -x "$old_bin/postgres" ]]; then
    refuse "refusing to upgrade Postgres $from to $MAJOR: the upgrade-source image did not provide the Postgres $from programs; set postgresql.upgrade.image to a Postgres $from image, $(delete_pod_hint)"
  fi
}

# Data of any major but this image's and the one before it is never touched: the chart cannot carry it
# across, and the operator needs to know which two majors are involved to choose a way out. A copy newer
# than pgdata is where the database has run since an earlier upgrade, and starting pgdata again instead
# would lose everything written since, so the way back named is then that copy's major.
refuse_other_major() {
  local data="$1" where="$2" newest
  newest="$(newest_copy_major)"
  if [[ "$data" -lt "$MAJOR" && -n "$newest" ]] && [[ "$newest" -gt "$data" && "$newest" -lt "$MAJOR" ]]; then
    refuse "refusing to start Postgres $MAJOR: the database runs on Postgres $newest in pgdata-$newest, and this chart only upgrades the data in pgdata, which is Postgres $data; set postgresql.image back to postgres:$newest$(image_suffix) to start pgdata-$newest again as it was, $(delete_pod_hint), and move it by hand: $MANUAL_PATH"
  fi
  if [[ "$data" -lt "$MAJOR" ]]; then
    refuse "refusing to start Postgres $MAJOR: the data in $where is Postgres $data, and this chart only upgrades data from Postgres $PREVIOUS_MAJOR; pin postgresql.image to postgres:$data to start it again as it was, $(delete_pod_hint), and move it by hand: $MANUAL_PATH"
  fi
  refuse "refusing to start Postgres $MAJOR: the data is Postgres $data, in $where, which is newer than this image, and no Postgres $MAJOR copy of it is left to start on; set postgresql.image back to Postgres $data or remove the pin on it, $(delete_pod_hint)"
}

# Without its control file the old data can neither be opened nor compared with the note in the upgraded
# copy.
refuse_unreadable_old_data() {
  if [[ -e "$NEW" ]]; then
    refuse "refusing to start Postgres $MAJOR: pgdata holds Postgres $DATA_MAJOR data without a readable pgdata/global/pg_control, which is what a removal of the old copy cut off part-way leaves, so whether pgdata-$MAJOR is still current cannot be told; finish removing the old copy as $CUT_OFF_REMOVAL_DOCS describes"
  fi
  refuse "refusing to upgrade Postgres $DATA_MAJOR to $MAJOR: pgdata holds Postgres $DATA_MAJOR data without a readable pgdata/global/pg_control, so Postgres $DATA_MAJOR cannot open it to upgrade it; put that file back from a backup"
}

controldata() {
  local bin="$1" field="$2"
  as_postgres "$bin/pg_controldata" "$SOURCE" | sed -n "s/^$field: *//p"
}

old_psql() {
  as_postgres "$NEW_BIN/psql" -h "$SOCKET_DIR" -p "$OLD_PORT" -U "$POSTGRES_USER" -d template1 \
    -XAtq -v ON_ERROR_STOP=1 -c "$1"
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
}

kib_in() {
  du -sk "$1" | cut -f1
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
    refuse "refusing upgrade $from→$MAJOR: need $(((needed_kib + KIB_PER_MIB - 1) / KIB_PER_MIB)) MiB, $((free_kib / KIB_PER_MIB)) MiB free; grow the volume claim itself with kubectl patch pvc$(namespace_flag) data-$HOSTNAME ($REFUSED_DOCS shows how) or pin postgresql.image to postgres:$from$(image_suffix) and $(delete_pod_hint)"
  fi
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
}

# --- what the volume holds: one predicate per kind of volume, tried in the order main lists them ------

volume_is_empty() {
  [[ -z "$DATA_MAJOR" && -z "$OTHER_COPIES" && ! -e "$PARTIAL" ]] && ! has_placeholder
}

data_is_this_major() {
  [[ "$DATA_MAJOR" == "$MAJOR" && -z "$OTHER_COPIES" && ! -e "$PARTIAL" ]]
}

# The image was moved back after an upgrade: the data is this major, and the only copy beside it is a
# newer one an upgrade made from it.
pinned_back_after_upgrade() {
  [[ "$DATA_MAJOR" == "$MAJOR" && ! -e "$PARTIAL" && -n "$ONLY_COPY_MAJOR" ]] \
    && [[ "$ONLY_COPY_MAJOR" -gt "$MAJOR" ]] \
    && grep -qx "source_major=$MAJOR" "$MOUNT/pgdata-$ONLY_COPY_MAJOR/$UPGRADE_NOTE" 2>/dev/null
}

old_data_unreadable() {
  [[ "$DATA_MAJOR" == "$PREVIOUS_MAJOR" && -z "$OLD_CONTROL_HASH" ]]
}

data_one_behind_beside_its_copy() {
  [[ "$DATA_MAJOR" == "$PREVIOUS_MAJOR" && "$OTHER_COPIES" == "pgdata-$MAJOR" && -f "$NEW/$UPGRADE_NOTE" ]]
}

# The upgraded copy is current as long as the old data has not been started since the copy was made:
# any start of the old major rewrites its pg_control, and the hash noted at upgrade then differs.
already_upgraded() {
  data_one_behind_beside_its_copy && grep -qx "source_pg_control_sha256=$OLD_CONTROL_HASH" "$NEW/$UPGRADE_NOTE"
}

# Once the old major has run again it holds the newer data, so the copy made before is redone. Only
# reached with a hash that was computed and differs from the one noted in the copy.
upgraded_copy_out_of_date() {
  data_one_behind_beside_its_copy
}

data_one_behind_without_copy() {
  [[ "$DATA_MAJOR" =~ ^[0-9]+$ && "$DATA_MAJOR" -eq "$PREVIOUS_MAJOR" && -z "$OTHER_COPIES" ]]
}

# The database runs on a copy an earlier upgrade made, one major behind this image, and no copy of this
# major is on the volume yet. The copy's control file must be readable, or Postgres could not open it to
# upgrade it.
live_copy_one_behind_without_copy() {
  [[ "$LIVE_MAJOR" == "$PREVIOUS_MAJOR" && ! -e "$NEW" && -n "$(control_hash "$LIVE_COPY")" ]]
}

data_out_of_reach() {
  [[ "$DATA_MAJOR" =~ ^[0-9]+$ ]] && [[ "$DATA_MAJOR" -lt "$PREVIOUS_MAJOR" || "$DATA_MAJOR" -gt "$MAJOR" ]]
}

old_copy_removed() {
  [[ -z "$DATA_MAJOR" && "$ONLY_COPY_MAJOR" == "$MAJOR" && ! -e "$PARTIAL" ]] && has_placeholder && [[ -n "$(copy_major "pgdata-$MAJOR")" ]]
}

only_newer_copy_left() {
  [[ -z "$DATA_MAJOR" && -n "$ONLY_COPY_MAJOR" && ! -e "$PARTIAL" ]] && [[ "$ONLY_COPY_MAJOR" -gt "$MAJOR" ]]
}

main() {
  settle_set_aside_copy
  DATA_MAJOR="$(copy_major pgdata)"
  OTHER_COPIES="$(other_copies)"
  ONLY_COPY_MAJOR="$(only_copy_major "$OTHER_COPIES")"
  OLD_CONTROL_HASH="$(control_hash pgdata)"
  # Every upgrade reads the copy the database last ran on, which is pgdata until an upgrade has moved on.
  read -r LIVE_COPY LIVE_MAJOR <<<"$(live_copy)"
  SOURCE="$MOUNT/$LIVE_COPY"

  if volume_is_empty; then
    say "nothing to upgrade: the volume holds no database yet, so Postgres $MAJOR creates one in pgdata"
    start_on "$OLD"
  elif data_is_this_major; then
    say "nothing to upgrade: the data in pgdata is already Postgres $MAJOR"
    start_on "$OLD"
  elif pinned_back_after_upgrade; then
    # The newer copy is left where it is. Once this major has run, pgdata no longer matches the note in
    # that copy, so moving the image forward again redoes the upgrade from pgdata.
    say "warning: starting Postgres $MAJOR on pgdata, but a newer Postgres $ONLY_COPY_MAJOR copy of this database exists in pgdata-$ONLY_COPY_MAJOR; what was written on that copy is not in this database, and removing the pin on postgresql.image redoes the upgrade from this copy, so those writes do not come back"
    start_on "$OLD"
  elif old_data_unreadable; then
    refuse_unreadable_old_data
  elif already_upgraded; then
    say "nothing to upgrade: pgdata-$MAJOR is already the upgrade of the Postgres $DATA_MAJOR data in pgdata"
    start_on "$NEW"
  elif upgraded_copy_out_of_date || data_one_behind_without_copy || live_copy_one_behind_without_copy; then
    upgrade "$LIVE_MAJOR"
    start_on "$NEW"
  elif data_out_of_reach; then
    refuse_other_major "$DATA_MAJOR" pgdata
  elif old_copy_removed; then
    say "nothing to upgrade: the old copy was removed, and Postgres $MAJOR starts on pgdata-$MAJOR"
    start_on "$NEW"
  elif only_newer_copy_left; then
    refuse_other_major "$ONLY_COPY_MAJOR" "pgdata-$ONLY_COPY_MAJOR"
  else
    refuse "refusing to start Postgres $MAJOR: found $(describe_volume), which this chart cannot start on or upgrade by itself"
  fi
}

main
