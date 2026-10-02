#!/usr/bin/env bash
# Runs as root in the database image before every start of the database. It picks the data directory the
# server starts on and, when the data is one major behind this image, builds an upgraded copy beside it.
# The original data stays in pgdata/, where the previous chart release looks for it, so a rollback to
# that release still finds the database as it was before the upgrade.
#
# Anything it does not recognise is refused before a single byte on the volume is written: one line in
# the log and in the pod's termination message, then a non-zero exit so the kubelet retries by itself.
set -euo pipefail

readonly MOUNT=/var/lib/postgresql/data
readonly OLD="$MOUNT/pgdata"
readonly MAJOR="$PG_MAJOR"
readonly NEW="$MOUNT/pgdata-$MAJOR"
readonly PARTIAL="$NEW.partial"
readonly NEW_BIN="/usr/lib/postgresql/$MAJOR/bin"
readonly OLD_BINARIES=/old-binaries
readonly SOURCE_OS="$OLD_BINARIES/os-release"
readonly MANUAL_PATH=https://docs.lighthouse.letpeople.work/Installation/kubernetes.html#moving-data-two-or-more-majors-behind-by-hand
readonly DECISION=/decision/pgdata
# The old server is only ever reached over a Unix socket here, never over the network.
readonly SOCKET_DIR=/tmp
readonly OLD_PORT=50431

say() {
  echo "lighthouse-postgres: $*"
}

refuse() {
  local line="lighthouse-postgres: $*"
  echo "$line" >&2
  printf '%s\n' "$line" >/dev/termination-log
  exit 1
}

# Kubernetes does not replace a database pod whose start keeps being refused when the chart's values
# change, so a fix made through values only takes effect once this pod is deleted. The command names this
# pod, and its namespace when the pod can read it, so it can be copied as it stands.
delete_pod_hint() {
  local namespace_file=/var/run/secrets/kubernetes.io/serviceaccount/namespace namespace=""
  if [[ -r "$namespace_file" ]]; then
    namespace=" -n $(cat "$namespace_file")"
  fi
  echo "then run kubectl delete pod$namespace $HOSTNAME so it starts again with the new values"
}

as_postgres() {
  gosu postgres "$@"
}

start_on() {
  printf '%s\n' "$1" >"$DECISION"
}

data_major() {
  if [[ -s "$OLD/PG_VERSION" ]]; then
    cat "$OLD/PG_VERSION"
  fi
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

has_placeholder() {
  local path
  for path in "$OLD"/UPGRADED-TO-*; do
    if [[ -e "$path" ]]; then
      return 0
    fi
  done
  return 1
}

describe_volume() {
  local data="$1" copies="$2" found
  if [[ -n "$data" ]]; then
    found="Postgres $data data in pgdata"
  elif has_placeholder; then
    found="no database in pgdata, only the note left when the old copy was removed"
  else
    found="no database in pgdata"
  fi
  if [[ -n "$copies" ]]; then
    found="$found and $(echo "$copies" | paste -sd, - | sed 's/,/, /g')"
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

# The old programs run in this image, which only works when the upgrade-source image is built for the
# same operating system. Older charts did not say which system that was, so a missing note checks nothing.
ensure_old_programs_run_here() {
  local from="$1" source here
  [[ -f "$SOURCE_OS" ]] || return 0
  source="$(os_name "$SOURCE_OS")"
  here="$(os_name /etc/os-release)"
  if [[ "$source" != "$here" ]]; then
    refuse "refusing to upgrade Postgres $from to $MAJOR: the Postgres $from programs in postgresql.upgrade.image are built for $source and cannot run beside the Postgres $MAJOR image, built for $here; set postgresql.upgrade.image to postgres:$from-$(os_field /etc/os-release VERSION_CODENAME), $(delete_pod_hint). Nothing was changed"
  fi
}

# Data of any major but this image's and the one before it is never touched: the chart cannot carry it
# across, and the operator needs to know which two majors are involved to choose a way out.
refuse_other_major() {
  local data="$1" where="$2"
  if [[ "$data" -lt "$MAJOR" ]]; then
    refuse "refusing to start Postgres $MAJOR: the data in $where is Postgres $data, and this chart only upgrades data from Postgres $((MAJOR - 1)); pin postgresql.image to postgres:$data to start it again as it was, $(delete_pod_hint), and move it by hand: $MANUAL_PATH. Nothing was changed"
  fi
  refuse "refusing to start Postgres $MAJOR: the data is Postgres $data, in $where, which is newer than this image, and no Postgres $MAJOR copy of it is left to start on; set postgresql.image back to Postgres $data or remove the pin on it, $(delete_pod_hint). Nothing was changed"
}

controldata() {
  local bin="$1" field="$2"
  as_postgres "$bin/pg_controldata" "$OLD" | sed -n "s/^$field: *//p"
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
  if [[ "$(controldata "$old_bin" "Database cluster state")" != "shut down" || -e "$OLD/postmaster.pid" ]]; then
    say "the Postgres $from data was not shut down cleanly; starting it once with Postgres $from to recover it"
  else
    say "reading the settings of the Postgres $from database"
  fi
  as_postgres "$old_bin/pg_ctl" -D "$OLD" -w --timeout=3600 \
    -o "-c listen_addresses='' -c unix_socket_directories=$SOCKET_DIR -p $OLD_PORT" start
  local template1
  SUPERUSER="$(old_psql "SELECT rolname FROM pg_authid WHERE oid = 10")"
  template1="$(old_psql "SELECT pg_encoding_to_char(encoding), datcollate, datctype, datlocprovider,
                                coalesce(datlocale, ''), coalesce(daticurules, '')
                         FROM pg_database WHERE datname = 'template1'")"
  IFS='|' read -r ENCODING COLLATE CTYPE PROVIDER LOCALE ICU_RULES <<<"$template1"
  as_postgres "$old_bin/pg_ctl" -D "$OLD" -m fast -w --timeout=3600 stop
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
  if [[ "$(controldata "$old_bin" "Data page checksum version")" == "0" ]]; then
    args+=(--no-data-checksums)
  else
    args+=(--data-checksums)
  fi
  as_postgres "$NEW_BIN/initdb" "${args[@]}"
}

kib_in() {
  du -sk "$1" | cut -f1
}

# The copy is about the size of the data. The margin covers what starting the old data writes, which
# includes crash recovery after an unclean stop, and the files a new, empty cluster brings. A leftover copy
# for this major is thrown away before the upgrade starts, so its space counts as free.
ensure_room_for_copy() {
  local from="$1" needed_kib free_kib path
  needed_kib=$(($(kib_in "$OLD") * 11 / 10 + 64 * 1024))
  free_kib="$(df -Pk "$MOUNT" | awk 'NR == 2 { print $4 }')"
  for path in "$NEW" "$PARTIAL"; do
    if [[ -e "$path" ]]; then
      free_kib=$((free_kib + $(kib_in "$path")))
    fi
  done
  if [[ "$free_kib" -lt "$needed_kib" ]]; then
    refuse "refusing upgrade $from→$MAJOR: need $(((needed_kib + 1023) / 1024)) MiB, $((free_kib / 1024)) MiB free; grow the volume (postgresql.persistence.size) or pin postgresql.image to postgres:$from-trixie and $(delete_pod_hint). Nothing was changed"
  fi
}

upgrade() {
  local from="$1"
  local old_bin="$OLD_BINARIES/usr/lib/postgresql/$from/bin"
  ensure_old_programs_run_here "$from"
  if [[ ! -x "$old_bin/pg_ctl" || ! -x "$old_bin/postgres" ]]; then
    refuse "refusing to upgrade Postgres $from to $MAJOR: the upgrade-source image did not provide the Postgres $from programs; set postgresql.upgrade.image to a Postgres $from image, $(delete_pod_hint). Nothing was changed"
  fi
  ensure_room_for_copy "$from"

  if [[ -e "$NEW" ]]; then
    say "pgdata-$MAJOR is out of date: Postgres $from has run on pgdata since that copy was made, so it is discarded and the upgrade redone from pgdata"
    rm -rf "$PARTIAL"
    # Renamed before it is removed, so a stop part-way through leaves only an unfinished copy, which the
    # next start throws away like any other.
    mv -T "$NEW" "$PARTIAL"
    rm -rf "$PARTIAL"
  elif [[ -e "$PARTIAL" ]]; then
    say "removing pgdata-$MAJOR.partial, left by an earlier attempt that did not finish"
    rm -rf "$PARTIAL"
  fi
  say "upgrading the Postgres $from data in pgdata to Postgres $MAJOR in pgdata-$MAJOR; pgdata is kept as it is"
  mkdir "$PARTIAL"
  chown postgres:postgres "$PARTIAL"
  chmod 0700 "$PARTIAL"

  read_old_cluster_settings "$old_bin" "$from"
  initdb_like_old_cluster "$old_bin"
  (cd "$SOCKET_DIR" && as_postgres "$NEW_BIN/pg_upgrade" --copy \
    -b "$old_bin" -B "$NEW_BIN" -d "$OLD" -D "$PARTIAL" -U "$SUPERUSER" --socketdir="$SOCKET_DIR")
  # A cluster made by initdb alone accepts no password logins over the network, which is how Lighthouse
  # connects, so the old cluster's rules come across with the data.
  as_postgres cp "$OLD/pg_hba.conf" "$OLD/pg_ident.conf" "$PARTIAL/"

  # Every later start of the old major rewrites pg_control, so this hash tells whether the copy still
  # matches the data it was made from.
  as_postgres tee "$PARTIAL/.lighthouse-upgrade" >/dev/null <<EOF
source_major=$from
source_system_identifier=$(controldata "$old_bin" "Database system identifier")
source_pg_control_sha256=$(sha256sum "$OLD/global/pg_control" | cut -d' ' -f1)
EOF
  sync
  mv -T "$PARTIAL" "$NEW"
  sync "$MOUNT"
  say "upgrade finished: Postgres $MAJOR starts on pgdata-$MAJOR, and the Postgres $from data stays in pgdata"
}

# The upgraded copy is current as long as the old data has not been started since the copy was made:
# any start of the old major rewrites its pg_control, and the hash noted at upgrade then differs.
upgraded_copy_is_current() {
  local note="$NEW/.lighthouse-upgrade" hash
  [[ -f "$note" ]] || return 1
  hash="$(sha256sum "$OLD/global/pg_control" | cut -d' ' -f1)"
  grep -qx "source_pg_control_sha256=$hash" "$note"
}

# The major of the copy an upgrade made from this image's major, when that copy is the only one on the
# volume: the image was moved back after an upgrade.
newer_copy_major() {
  local copies="$1"
  if [[ "$copies" =~ ^pgdata-([0-9]+)$ && "${BASH_REMATCH[1]}" -gt "$MAJOR" ]] \
    && grep -qx "source_major=$MAJOR" "$MOUNT/$copies/.lighthouse-upgrade" 2>/dev/null; then
    echo "${BASH_REMATCH[1]}"
  fi
}

# The major of the one copy on the volume, when there is exactly one.
only_copy_major() {
  local copies="$1"
  if [[ "$copies" =~ ^pgdata-([0-9]+)$ ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
}

copy_holds_major() {
  [[ "$(cat "$MOUNT/pgdata-$1/PG_VERSION" 2>/dev/null)" == "$1" ]]
}

main() {
  local data copies newer only
  data="$(data_major)"
  copies="$(other_copies)"
  only="$(only_copy_major "$copies")"

  if [[ -z "$data" && -z "$copies" && ! -e "$PARTIAL" ]] && ! has_placeholder; then
    say "nothing to upgrade: the volume holds no database yet, so Postgres $MAJOR creates one in pgdata"
    start_on "$OLD"
  elif [[ "$data" == "$MAJOR" && -z "$copies" && ! -e "$PARTIAL" ]]; then
    say "nothing to upgrade: the data in pgdata is already Postgres $MAJOR"
    start_on "$OLD"
  elif [[ "$data" == "$((MAJOR - 1))" && "$copies" == "pgdata-$MAJOR" ]] && upgraded_copy_is_current; then
    say "nothing to upgrade: pgdata-$MAJOR is already the upgrade of the Postgres $data data in pgdata"
    start_on "$NEW"
  elif [[ "$data" == "$((MAJOR - 1))" && "$copies" == "pgdata-$MAJOR" && -f "$NEW/.lighthouse-upgrade" ]]; then
    # Once the old major has run again it holds the newer data, so the copy made before is redone.
    upgrade "$data"
    start_on "$NEW"
  elif [[ "$data" =~ ^[0-9]+$ && "$data" -eq $((MAJOR - 1)) && -z "$copies" ]]; then
    upgrade "$data"
    start_on "$NEW"
  elif [[ "$data" == "$MAJOR" && ! -e "$PARTIAL" ]] && newer="$(newer_copy_major "$copies")" && [[ -n "$newer" ]]; then
    # The newer copy is left where it is. Once this major has run, pgdata no longer matches the note in
    # that copy, so moving the image forward again redoes the upgrade from pgdata.
    say "warning: starting Postgres $MAJOR on pgdata, but a newer Postgres $newer copy of this database exists in pgdata-$newer; what was written on that copy is not in this database, and removing the pin on postgresql.image redoes the upgrade from this copy, so those writes do not come back"
    start_on "$OLD"
  elif [[ -z "$data" && "$only" == "$MAJOR" && ! -e "$PARTIAL" ]] && has_placeholder && copy_holds_major "$MAJOR"; then
    say "nothing to upgrade: the old copy was removed, and Postgres $MAJOR starts on pgdata-$MAJOR"
    start_on "$NEW"
  elif [[ "$data" =~ ^[0-9]+$ ]] && [[ "$data" -lt $((MAJOR - 1)) || "$data" -gt "$MAJOR" ]]; then
    refuse_other_major "$data" pgdata
  elif [[ -z "$data" && -n "$only" && ! -e "$PARTIAL" ]] && [[ "$only" -gt "$MAJOR" ]]; then
    refuse_other_major "$only" "pgdata-$only"
  else
    refuse "refusing to start Postgres $MAJOR: found $(describe_volume "$data" "$copies"), which this chart cannot start on or upgrade by itself. Nothing was changed"
  fi
}

main
