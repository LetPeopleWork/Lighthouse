#!/usr/bin/env bash
# Kind harness for chart/tests/acceptance/upgrade-bundled-postgres.feature. One function per @real-io
# scenario, named after it.
#
# The same script runs locally and in CI, so the cluster is an argument rather than an assumption. Every
# scenario gets a namespace of its own, so scenarios sharing a cluster never see each other's volume.
#
# Usage: run.sh CLUSTER GROUP [SCENARIO...]
#   CLUSTER   name of an existing kind cluster (kind create cluster --name CLUSTER)
#   GROUP     happy | refusals | chain | chain-refusals | chain-cleanup — the legs of the CI matrix, each on
#             its own cluster
#   SCENARIO  optional subset of the group's scenario functions, run in the order given
#
# The "before" chart is the committed package, so no network access to the Helm repo is needed.
set -euo pipefail

readonly BEFORE_CHART="docs/charts/lighthouse-0.1.17.tgz"
readonly NEW_CHART="./chart"

readonly RELEASE="l8e"
readonly POSTGRES_STATEFULSET="$RELEASE-lighthouse-postgres"
readonly POSTGRES_POD="$POSTGRES_STATEFULSET-0"
readonly API="$RELEASE-lighthouse-api"
readonly DB_SECRET="$RELEASE-lighthouse-db"
readonly DB_USER="lighthouse"
readonly MOUNT="/var/lib/postgresql/data"
readonly WAIT_SECONDS=600
readonly CRASH_LOOP_RESTARTS=3
readonly NOTHING_TO_UPGRADE="nothing to upgrade"
readonly UPGRADING_17_TO_18="upgrading the Postgres 17 data in pgdata-17 to Postgres 18 in pgdata-18"

# The install and every upgrade take exactly these values, so an upgrade never carries a value the
# install did not have.
ENCRYPTION_KEY="$(head -c 32 /dev/urandom | base64)"
readonly ENCRYPTION_KEY
readonly VALUES=(
  --set ingress.enabled=false
  --set postgresql.auth.password=upgrade-path
  --set encryption.key="$ENCRYPTION_KEY"
)
MARKER="upgrade-path-$(date +%s)-$RANDOM"
readonly MARKER

fail() {
  echo "✗ upgrade-path: $*" >&2
  exit 1
}

# --- shared steps ------------------------------------------------------------------------------------

# Prints the one line of $2 that carries text $1, and fails when there is none or more than one.
the_one_line_with() {
  local text="$1" input="$2" lines
  lines="$(grep -F -- "$text" <<<"$input" || true)"
  [[ -n "$lines" && "$(wc -l <<<"$lines")" == "1" ]] || return 1
  echo "$lines"
  return
}

chart_app_version() {
  local chart="$1"
  helm show chart "$chart" | awk '/^appVersion:/ { gsub(/"/, "", $2); print $2; exit }'
  return $?
}

# Pulls each image group $2 starts once into the local Docker and loads it into the kind node, so the node
# never pulls from Docker Hub itself: its anonymous pull limit is shared by every job on a CI runner's
# address. A group loads only its own images, because every image loaded costs its leg time.
preload_images() {
  local cluster="$1" group="$2" image
  local images=(
    "ghcr.io/letpeoplework/lighthouse:$(chart_app_version "$BEFORE_CHART")"
    "ghcr.io/letpeoplework/lighthouse:$(chart_app_version "$NEW_CHART")"
  )
  case "$group" in
    happy | refusals)
      images+=(postgres:16 postgres:17 postgres:17-trixie postgres:17-alpine postgres:18-trixie busybox:1.37) ;;
    chain | chain-cleanup)
      images+=(postgres:16-trixie postgres:17-trixie postgres:18-trixie) ;;
    chain-refusals)
      images+=(postgres:15-bookworm postgres:16-bookworm postgres:16-trixie postgres:17-trixie postgres:18-trixie) ;;
    *) ;;
  esac
  # `kind load docker-image` exports every platform of a multi-platform image and fails on the ones
  # Docker never pulled, so only the node's own platform is exported.
  local platform node
  platform="$(docker version --format '{{.Server.Os}}/{{.Server.Arch}}')"
  for image in "${images[@]}"; do
    if ! docker image inspect "$image" >/dev/null 2>&1; then
      docker pull --quiet --platform "$platform" "$image" >/dev/null \
        || { echo "  (could not pull $image; the node pulls it)"; continue; }
    fi
    for node in $(kind get nodes --name "$cluster"); do
      docker save --platform "$platform" "$image" \
        | docker exec -i "$node" ctr --namespace=k8s.io images import --platform "$platform" --digests --snapshotter=overlayfs - >/dev/null \
        || echo "  (could not preload $image; the node pulls it)"
    done
  done
  return $?
}

fresh_namespace() {
  local ns="$1"
  kubectl delete namespace "$ns" --ignore-not-found --wait >/dev/null
  kubectl create namespace "$ns" >/dev/null
  return $?
}

dump_diagnostics() {
  local ns="$1"
  echo "--- diagnostics for namespace $ns ---" >&2
  kubectl -n "$ns" get pods -o wide >&2 || true
  kubectl -n "$ns" describe pod "$POSTGRES_POD" >&2 || true
  kubectl -n "$ns" logs "$POSTGRES_POD" --all-containers --tail=80 >&2 || true
  kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres --previous --tail=40 >&2 || true
  return $?
}

install_before_chart() {
  local ns="$1"
  fresh_namespace "$ns"
  helm install "$RELEASE" "$BEFORE_CHART" -n "$ns" "${VALUES[@]}" "${@:2}" --wait --timeout 10m >/dev/null \
    || { dump_diagnostics "$ns"; fail "chart 0.1.17 did not install in $ns"; }
  return $?
}

install_new_chart() {
  local ns="$1"
  fresh_namespace "$ns"
  helm install "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" >/dev/null
  return $?
}

# No --wait: a database that crash-loops would hold Helm for its whole timeout, and the wait below
# notices a crash loop and reports its log at once.
upgrade_to_new_chart() {
  local ns="$1"
  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" "${@:2}" >/dev/null
  return $?
}

highest_restart_count() {
  local ns="$1"
  # While the StatefulSet replaces the pod there is briefly none to read, which counts as no restarts
  # rather than ending the run.
  { kubectl -n "$ns" get pod "$POSTGRES_POD" \
    -o jsonpath='{range .status.initContainerStatuses[*]}{.restartCount}{"\n"}{end}{range .status.containerStatuses[*]}{.restartCount}{"\n"}{end}' \
    2>/dev/null || true; } | sort -n | tail -1
  return $?
}

# Fails with the pod's diagnostics once the database pod in $1 has crash-looped, or once deadline $2 has
# passed.
fail_if_crash_looping_or_late() {
  local ns="$1" deadline="$2" crash_looping="$3" late="$4" restarts
  restarts="$(highest_restart_count "$ns")"
  if [[ "${restarts:-0}" -ge "$CRASH_LOOP_RESTARTS" ]]; then
    dump_diagnostics "$ns"
    fail "$crash_looping"
  fi
  if [[ $SECONDS -ge $deadline ]]; then
    dump_diagnostics "$ns"
    fail "$late"
  fi
  return $?
}

wait_postgres_ready() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS))
  until kubectl -n "$ns" rollout status "statefulset/$POSTGRES_STATEFULSET" --timeout=10s >/dev/null 2>&1; do
    fail_if_crash_looping_or_late "$ns" "$deadline" \
      "the database pod in $ns is crash-looping" \
      "the database in $ns did not become Ready within ${WAIT_SECONDS}s"
  done
  kubectl -n "$ns" wait --for=condition=Ready "pod/$POSTGRES_POD" --timeout="${WAIT_SECONDS}s" >/dev/null
  return $?
}

wait_api_ready() {
  local ns="$1"
  kubectl -n "$ns" rollout status "deploy/$API" --timeout="${WAIT_SECONDS}s" >/dev/null \
    || { dump_diagnostics "$ns"; fail "the API in $ns did not become Ready"; }
  local deadline=$((SECONDS + 120))
  until kubectl get --raw "/api/v1/namespaces/$ns/services/$API:80/proxy/health/ready" >/dev/null 2>&1; do
    [[ $SECONDS -lt $deadline ]] || fail "the API in $ns does not serve /health/ready"
    sleep 3
  done
  return $?
}

in_postgres() {
  local ns="$1"
  kubectl -n "$ns" exec "$POSTGRES_POD" -c postgres -- "${@:2}"
  return $?
}

psql_on() {
  local ns="$1" pod="$2" sql="$3"
  kubectl -n "$ns" exec "$pod" -c postgres -- \
    psql -U "$DB_USER" -d lighthouse -XAtq -v ON_ERROR_STOP=1 -c "$sql"
  return $?
}

psql_in() {
  local ns="$1" sql="$2"
  psql_on "$ns" "$POSTGRES_POD" "$sql"
  return $?
}

seed_marker_row() {
  local ns="$1"
  psql_in "$ns" "CREATE TABLE upgrade_path_marker (marker text PRIMARY KEY);
                 INSERT INTO upgrade_path_marker VALUES ('$MARKER');
                 CREATE TABLE upgrade_path_bulk AS SELECT g AS id, md5(g::text) AS payload FROM generate_series(1, 20000) g;" >/dev/null
  return $?
}

# One "table|rows" line per table of the public schema, sorted, so two recordings compare as text.
public_row_counts() {
  local ns="$1" pod="${2:-$POSTGRES_POD}"
  psql_on "$ns" "$pod" "SELECT table_name || '|' || (xpath('/row/c/text()',
                   query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name), false, true, '')))[1]::text
                 FROM information_schema.tables
                 WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                 ORDER BY table_name"
  return $?
}

assert_row_counts() {
  local ns="$1" before="$2" after
  after="$(public_row_counts "$ns")"
  [[ "$after" == "$before" ]] || fail "row counts in $ns changed: $(diff <(echo "$before") <(echo "$after") || true)"
  return $?
}

# A table of its own holding this run's marker, so whether a write made at one point survives can be
# checked later by name.
write_marker_table() {
  local ns="$1" table="$2"
  psql_in "$ns" "CREATE TABLE $table (marker text);
                 INSERT INTO $table VALUES ('$MARKER')" >/dev/null
  return $?
}

marker_rows_in() {
  local ns="$1" table="$2" pod="${3:-$POSTGRES_POD}"
  psql_on "$ns" "$pod" "SELECT count(*) FROM $table WHERE marker = '$MARKER'"
  return $?
}

assert_marker_row() {
  local ns="$1" pod="${2:-$POSTGRES_POD}"
  [[ "$(marker_rows_in "$ns" upgrade_path_marker "$pod")" == "1" ]] \
    || fail "the marker row is missing in $ns"
  return $?
}

assert_server_major() {
  local ns="$1" major="$2" version
  version="$(psql_in "$ns" "SHOW server_version_num")"
  [[ "$version" == "$major"* ]] || fail "the database in $ns runs $version, expected Postgres $major"
  return $?
}

secret_fingerprint() {
  local ns="$1"
  kubectl -n "$ns" get secret "$DB_SECRET" \
    -o jsonpath='{.data}' | sha256sum
  return $?
}

volume_file() {
  local ns="$1" path="$2"
  in_postgres "$ns" cat "$MOUNT/$path"
  return $?
}

volume_entries() {
  local ns="$1"
  in_postgres "$ns" ls -1A "$MOUNT"
  return $?
}

upgrade_log() {
  local ns="$1"
  kubectl -n "$ns" logs "$POSTGRES_POD" -c pg-upgrade
  return $?
}

assert_upgrade_log_says() {
  local ns="$1" text="$2" log
  # Read whole before searching: grep -q stops at the first match, and under pipefail the writer it
  # cut off would fail the check.
  log="$(upgrade_log "$ns")"
  grep -qF -- "$text" <<<"$log" || { dump_diagnostics "$ns"; fail "the upgrade log in $ns does not say: $text"; }
  return $?
}

# Prints the one line of the upgrade log in $1 that carries text $2, and fails with the pod's diagnostics,
# saying the log does not carry exactly one line $3, when there is none or more than one.
the_one_upgrade_log_line() {
  local ns="$1" text="$2" what="$3"
  the_one_line_with "$text" "$(upgrade_log "$ns")" \
    || { dump_diagnostics "$ns"; fail "the upgrade log in $ns does not carry exactly one line $what"; }
  return $?
}

# Runs a shell command in a throwaway pod that mounts the database claim read-only at /volume, so the
# volume can be read while the database pod is not running or not Ready.
on_volume() {
  local ns="$1" script="$2" overrides
  overrides="$(jq -cn --arg script "$script" --arg claim "data-$POSTGRES_POD" '{spec: {
    containers: [{name: "on-volume", image: "postgres:18-trixie", command: ["bash", "-c", $script],
      volumeMounts: [{name: "data", mountPath: "/volume", readOnly: true}]}],
    volumes: [{name: "data", persistentVolumeClaim: {claimName: $claim}}]}}')"
  # Read from the finished pod's log rather than attached: a short-lived pod can finish before the attach,
  # and the log stream kubectl falls back to then may end early.
  kubectl -n "$ns" run on-volume --restart=Never --image=postgres:18-trixie --overrides="$overrides" >/dev/null
  kubectl -n "$ns" wait --for=jsonpath='{.status.phase}'=Succeeded pod/on-volume --timeout=300s >/dev/null \
    || { kubectl -n "$ns" logs on-volume >&2 || true; fail "reading the database volume in $ns failed"; }
  kubectl -n "$ns" logs on-volume
  kubectl -n "$ns" delete pod on-volume --wait >/dev/null
  return $?
}

# The type, mode and owner of every entry on the database volume, then a sorted sha256sum of every file,
# so a refused attempt can be shown to have changed nothing: no byte, and no folder made or handed over.
volume_fingerprint() {
  local ns="$1"
  on_volume "$ns" 'cd /volume && find . -printf "%y %m %u %g %p\n" | sort && find . -type f -print0 | sort -z | xargs -0 -r sha256sum'
  return $?
}

# Stops the Postgres 17 server process from the kind node, so it can neither shut down cleanly nor be
# restarted in place. The upgrade that follows replaces the pod; the kubelet's SIGTERM never gets an
# answer and the grace period ends in SIGKILL, which is how a busy 0.1.17 pod usually goes down.
freeze_postgres_server() {
  local ns="$1" pid
  pid="$(container_pid "$ns" containerStatuses postgres)"
  docker exec "$(pod_field "$ns" '{.spec.nodeName}')" kill -STOP "$pid"
  return $?
}

# The process id, on the kind node, of the first process of container $3 in the database pod; $2 is
# containerStatuses or initContainerStatuses.
container_pid() {
  local ns="$1" statuses="$2" name="$3" container pid
  container="$(pod_field "$ns" "{.status.${statuses}[?(@.name==\"$name\")].containerID}")"
  pid="$(docker exec "$(pod_field "$ns" '{.spec.nodeName}')" \
    crictl inspect --output go-template --template '{{.info.pid}}' "${container#containerd://}")"
  [[ -n "$pid" ]] || fail "could not find the $name process of $POSTGRES_POD"
  echo "$pid"
  return
}

# A shell command for the kind node that freezes the whole container of process $1 through its cgroup: one
# write stops every process in it at once, children included. Stopping them one by one leaves the children
# of a step running while the others are still being found, long enough for a removal to finish.
freeze_container_of() {
  local pid="$1"
  echo "echo 1 > \"/sys/fs/cgroup\$(sed -n 's/^0:://p' /proc/$pid/cgroup)/cgroup.freeze\""
  return
}

# Stops every process of the upgrade step from the kind node, pg_upgrade included, so the copy it is
# making cannot reach its rename however long the pod takes to go.
freeze_upgrade_step() {
  local ns="$1" pid
  pid="$(container_pid "$ns" initContainerStatuses pg-upgrade)"
  docker exec "$(pod_field "$ns" '{.spec.nodeName}')" sh -c "$(freeze_container_of "$pid")"
  return $?
}

install_with_data() {
  local ns="$1"
  install_before_chart "$ns" "${@:2}"
  wait_api_ready "$ns"
  seed_marker_row "$ns"
  return $?
}

assert_carried_across() {
  local ns="$1" counts="$2"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  return $?
}

readonly UPGRADED_NS="plain-upgrade"
readonly FRESH_NS="fresh-install"

# What a scenario leaves behind for the ones after it: the row counts recorded before each namespace was
# upgraded, the Helm revision of the upgrade, and whether the fresh install exists. A scenario run on its
# own finds none of them and makes its Given itself. The new chart may still carry the old chart's version
# number, so the upgrade's revision is recorded rather than looked up by chart name.
#
# A Given runs its scenario inside an if, never on the right of ||: bash ignores set -e in a function called
# there, so every check in that scenario that relies on it would pass whatever happened.
declare -A RECORDED_COUNTS=()
BEFORE_REVISION=""
UPGRADE_REVISION=""
ROLLED_BACK=""
FRESH_INSTALLED=""

given_upgraded() {
  if [[ -z "${RECORDED_COUNTS[$UPGRADED_NS]:-}" ]]; then upgrade_plain_helm_upgrade_keeps_every_row; fi
  return $?
}

given_rolled_back() {
  if [[ -z "$ROLLED_BACK" ]]; then rollback_starts_17_on_pre_upgrade_data; fi
  return $?
}

given_fresh_install() {
  if [[ -z "$FRESH_INSTALLED" ]]; then fresh_install_starts_18_without_upgrading; fi
  return $?
}

# The field at jsonpath $2 of the database pod, empty while there is no pod or the field is not set. Any
# other failure to read it fails, so a check that wants the field empty cannot pass because kubectl failed.
pod_field() {
  local ns="$1" path="$2" value
  value="$(kubectl -n "$ns" get pod "$POSTGRES_POD" --ignore-not-found -o jsonpath="$path")" \
    || fail "could not read $path of $POSTGRES_POD in $ns"
  echo "$value"
  return
}

pod_uid() {
  local ns="$1"
  pod_field "$ns" '{.metadata.uid}'
  return $?
}

assert_not_ready() {
  local ns="$1" message="$2" ready
  ready="$(pod_field "$ns" '{.status.conditions[?(@.type=="Ready")].status}')" || fail "$message: its state could not be read"
  [[ "$ready" != "True" ]] || fail "$message"
  return $?
}

# Fails when Postgres has started in the database pod of $1 while the upgrade step should still be copying.
assert_postgres_not_started() {
  local ns="$1" started
  started="$(postgres_started_at "$ns")" || fail "whether Postgres started in $ns could not be read"
  [[ -z "$started" ]] || fail "Postgres started in $ns while the upgrade step was still copying"
  return $?
}

stop_database() {
  local ns="$1"
  kubectl -n "$ns" scale statefulset "$POSTGRES_STATEFULSET" --replicas=0 >/dev/null
  kubectl -n "$ns" wait --for=delete "pod/$POSTGRES_POD" --timeout=120s >/dev/null 2>&1 || true
  return $?
}

start_database() {
  local ns="$1"
  kubectl -n "$ns" scale statefulset "$POSTGRES_STATEFULSET" --replicas=1 >/dev/null
  return $?
}

postgres_started_at() {
  local ns="$1"
  pod_field "$ns" '{.status.containerStatuses[?(@.name=="postgres")].state.running.startedAt}'
  return $?
}

# Deletes the database pod and waits until the StatefulSet's replacement has started its postgres
# container. Readiness is left to the caller.
restart_database() {
  local ns="$1" old_uid deadline=$((SECONDS + WAIT_SECONDS))
  old_uid="$(pod_uid "$ns")"
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --wait >/dev/null
  until [[ "$(pod_uid "$ns")" != "$old_uid" && -n "$(postgres_started_at "$ns")" ]]; do
    fail_if_crash_looping_or_late "$ns" "$deadline" \
      "the restarted database pod in $ns is crash-looping" \
      "the restarted database in $ns did not start within ${WAIT_SECONDS}s"
    sleep 1
  done
  return $?
}

# From the pod being created to its postgres container starting: the time the init containers took.
# Both timestamps are whole seconds.
start_delay_ms() {
  local ns="$1" created started
  created="$(date -d "$(pod_field "$ns" '{.metadata.creationTimestamp}')" +%s)"
  started="$(date -d "$(postgres_started_at "$ns")" +%s)"
  echo $(((started - created) * 1000))
  return
}

assert_data_directory() {
  local ns="$1" directory="$2" actual
  actual="$(psql_in "$ns" "SHOW data_directory")"
  [[ "$actual" == "$MOUNT/$directory" ]] || fail "the database in $ns runs on $actual, expected $MOUNT/$directory"
  return $?
}

assert_upgrade_note() {
  local ns="$1"
  volume_file "$ns" pgdata-18/.lighthouse-upgrade | grep -qx "source_major=17" \
    || fail "pgdata-18 in $ns carries no note saying it was upgraded from Postgres 17"
  return $?
}

current_revision() {
  local ns="$1"
  helm history "$RELEASE" -n "$ns" -o json | jq -r '.[-1].revision'
  return $?
}

apply_rendered() {
  local ns="$1" chart="$2"
  helm template "$RELEASE" "$chart" -n "$ns" "${VALUES[@]}" | kubectl -n "$ns" apply -f - >/dev/null
  return $?
}

# Starts Postgres on a copy of a kept folder taken from the volume, mounted read-only, so opening the kept
# data cannot change a byte of it: any start of Postgres on the volume itself rewrites pg_control. The
# image defaults to Postgres 17 and the folder to pgdata, the copy a first upgrade from 17 keeps.
open_kept_copy() {
  local ns="$1" image="${2:-postgres:17-trixie}" folder="${3:-pgdata}" script overrides deadline=$((SECONDS + 300))
  script="cp -a /volume/$folder /copy/kept && chown -R postgres:postgres /copy/kept && chmod 0700 /copy/kept && exec gosu postgres postgres -D /copy/kept"
  overrides="$(jq -cn --arg script "$script" --arg claim "data-$POSTGRES_POD" --arg image "$image" '{spec: {
    containers: [{name: "postgres", image: $image, command: ["bash", "-c", $script],
      volumeMounts: [{name: "data", mountPath: "/volume", readOnly: true}, {name: "copy", mountPath: "/copy"}]}],
    volumes: [{name: "data", persistentVolumeClaim: {claimName: $claim, readOnly: true}}, {name: "copy", emptyDir: {}}]}}')"
  kubectl -n "$ns" run kept-copy --restart=Never --image="$image" --overrides="$overrides" >/dev/null
  until kubectl -n "$ns" exec kept-copy -c postgres -- pg_isready -q >/dev/null 2>&1; do
    if [[ $SECONDS -ge $deadline || "$(kubectl -n "$ns" get pod kept-copy -o jsonpath='{.status.phase}')" == "Failed" ]]; then
      kubectl -n "$ns" logs kept-copy >&2 || true
      fail "$image did not open the kept copy $folder in $ns"
    fi
    sleep 2
  done
  return $?
}

close_kept_copy() {
  local ns="$1"
  kubectl -n "$ns" delete pod kept-copy --wait >/dev/null
  return $?
}

# --- group: happy, in the order the scenarios chain ---------------------------------------------------

fresh_install_starts_18_without_upgrading() {
  local ns="$FRESH_NS"
  install_new_chart "$ns"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  wait_api_ready "$ns"
  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  [[ "$(volume_entries "$ns")" == "pgdata" ]] \
    || fail "the volume in $ns holds more than one copy: $(volume_entries "$ns" | tr '\n' ' ')"
  FRESH_INSTALLED=1
  return $?
}

upgrade_plain_helm_upgrade_keeps_every_row() {
  local ns="$UPGRADED_NS" counts secret
  install_with_data "$ns"
  BEFORE_REVISION="$(current_revision "$ns")"
  counts="$(public_row_counts "$ns")"
  secret="$(secret_fingerprint "$ns")"

  upgrade_to_new_chart "$ns"
  UPGRADE_REVISION="$(current_revision "$ns")"

  assert_carried_across "$ns" "$counts"
  [[ "$(secret_fingerprint "$ns")" == "$secret" ]] || fail "the database Secret in $ns changed"
  [[ "$(volume_file "$ns" pgdata/PG_VERSION)" == "17" ]] || fail "pgdata/PG_VERSION in $ns no longer reads 17"
  RECORDED_COUNTS[$ns]="$counts"
  return $?
}

# The baseline is a restart of the fresh install rather than its first start, so both measurements
# start on an existing volume and neither includes provisioning it.
restart_after_upgrade_does_not_upgrade_again() {
  local ns="$UPGRADED_NS" baseline delay
  given_fresh_install
  given_upgraded
  restart_database "$FRESH_NS"
  baseline="$(start_delay_ms "$FRESH_NS")"

  restart_database "$ns"

  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  delay="$(start_delay_ms "$ns")"
  echo "  start delay: ${delay} ms after the upgrade, ${baseline} ms on a fresh install"
  [[ $((delay - baseline)) -lt 5000 ]] \
    || fail "the database in $ns started ${delay} ms after its pod was created, ${baseline} ms on a fresh install"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_row_counts "$ns" "${RECORDED_COUNTS[$ns]}"
  return $?
}

rollback_between_new_chart_revisions_changes_nothing() {
  local ns="$UPGRADED_NS"
  given_upgraded
  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" --set shutdownTimeoutSeconds=45 >/dev/null
  wait_api_ready "$ns"
  write_marker_table "$ns" upgrade_path_written_on_18

  helm rollback "$RELEASE" "$UPGRADE_REVISION" -n "$ns" >/dev/null
  # The changed setting reaches only the API, so the rollback leaves the database running. Restarting it
  # is what makes the database start under the rolled-back revision.
  restart_database "$ns"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_18)" == "1" ]] \
    || fail "the row written on Postgres 18 in $ns is gone after the rollback"
  wait_api_ready "$ns"
  return $?
}

# Comparing rows, never file hashes: pg_upgrade rewrites pg_control of the data it upgrades.
kept_copy_holds_pre_upgrade_rows() {
  local ns="$UPGRADED_NS" counts
  given_upgraded
  stop_database "$ns"

  open_kept_copy "$ns"

  counts="$(public_row_counts "$ns" kept-copy)"
  [[ "$counts" == "${RECORDED_COUNTS[$ns]}" ]] \
    || fail "the kept copy in $ns differs from before the upgrade: $(diff <(echo "${RECORDED_COUNTS[$ns]}") <(echo "$counts") || true)"
  assert_marker_row "$ns" kept-copy

  close_kept_copy "$ns"
  start_database "$ns"
  wait_postgres_ready "$ns"
  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  assert_server_major "$ns" 18
  return $?
}

table_count() {
  local ns="$1" table="$2"
  psql_in "$ns" "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '$table'"
  return $?
}

rollback_starts_17_on_pre_upgrade_data() {
  local ns="$UPGRADED_NS"
  given_upgraded
  write_marker_table "$ns" upgrade_path_before_rollback

  helm rollback "$RELEASE" "$BEFORE_REVISION" -n "$ns" >/dev/null

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  wait_api_ready "$ns"
  assert_marker_row "$ns"
  [[ "$(table_count "$ns" upgrade_path_before_rollback)" == "0" ]] \
    || fail "the row written on Postgres 18 in $ns is still there after the rollback to Postgres 17"
  ROLLED_BACK=1
  return $?
}

upgrade_again_after_rollback_starts_afresh() {
  local ns="$UPGRADED_NS"
  given_rolled_back
  write_marker_table "$ns" upgrade_path_after_rollback

  upgrade_to_new_chart "$ns"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_upgrade_log_says "$ns" "out of date"
  [[ "$(marker_rows_in "$ns" upgrade_path_after_rollback)" == "1" ]] \
    || fail "the row written on Postgres 17 after the rollback in $ns did not reach Postgres 18"
  [[ "$(table_count "$ns" upgrade_path_before_rollback)" == "0" ]] \
    || fail "the row written on Postgres 18 before the rollback in $ns came back"
  assert_marker_row "$ns"
  wait_api_ready "$ns"
  return $?
}

pin_back_after_upgrade_starts_on_kept_copy_and_warns() {
  local ns="$UPGRADED_NS" log warning
  given_upgraded
  write_marker_table "$ns" upgrade_path_before_pin

  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" --set postgresql.image=postgres:17-trixie >/dev/null
  echo "  pinned to Postgres 17 at revision $(current_revision "$ns")"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata
  assert_marker_row "$ns"
  [[ "$(table_count "$ns" upgrade_path_before_pin)" == "0" ]] \
    || fail "the row written on Postgres 18 in $ns is there after pinning to Postgres 17"
  log="$(kubectl -n "$ns" logs "$POSTGRES_POD" --all-containers)"
  warning="$(the_one_line_with "newer Postgres 18 copy" "$log")" \
    || { dump_diagnostics "$ns"; fail "the database log in $ns does not carry exactly one warning about the newer copy"; }
  echo "  $warning"
  if ! grep -qF "not in this database" <<<"$warning" || ! grep -qF "redoes the upgrade from this copy" <<<"$warning"; then
    fail "the warning in $ns does not say what is missing and what moving to Postgres 18 again does: $warning"
  fi
  wait_api_ready "$ns"
  write_marker_table "$ns" upgrade_path_during_pin

  upgrade_to_new_chart "$ns"
  echo "  pin removed at revision $(current_revision "$ns")"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_upgrade_log_says "$ns" "out of date"
  [[ "$(marker_rows_in "$ns" upgrade_path_during_pin)" == "1" ]] \
    || fail "the row written on Postgres 17 during the pin in $ns did not reach Postgres 18"
  [[ "$(table_count "$ns" upgrade_path_before_pin)" == "0" ]] \
    || fail "the row written on Postgres 18 before the pin in $ns came back"
  assert_marker_row "$ns"
  wait_api_ready "$ns"
  return $?
}

# --reuse-values keeps the 0.1.17 image value, so the database stays on 17 and only the notes speak up.
reuse_values_stays_on_17_and_says_so() {
  local ns="reuse-values" output line
  install_with_data "$ns"

  output="$(helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" --reuse-values)"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  wait_api_ready "$ns"
  assert_marker_row "$ns"
  line="$(the_one_line_with "behind" "$output")" \
    || fail "the upgrade output in $ns does not carry exactly one line saying the database is behind"
  echo "  $line"
  grep -qF -- "--reset-then-reuse-values" <<<"$line" \
    || fail "the behind line in $ns does not say how to move the database: $line"
  return $?
}

upgrade_with_reset_then_reuse_values_keeps_every_row() {
  local ns="reset-then-reuse" counts
  install_with_data "$ns"
  counts="$(public_row_counts "$ns")"

  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" --reset-then-reuse-values >/dev/null

  assert_carried_across "$ns" "$counts"
  assert_upgrade_note "$ns"
  return $?
}

upgrade_by_rendered_manifests_keeps_every_row() {
  local ns="rendered-manifests" counts
  fresh_namespace "$ns"
  apply_rendered "$ns" "$BEFORE_CHART"
  wait_postgres_ready "$ns"
  wait_api_ready "$ns"
  seed_marker_row "$ns"
  counts="$(public_row_counts "$ns")"

  apply_rendered "$ns" "$NEW_CHART"

  assert_carried_across "$ns" "$counts"
  assert_upgrade_note "$ns"
  return $?
}

# --- group: refusals ------------------------------------------------------------------------------------

upgrade_after_unclean_stop_keeps_every_row() {
  local ns="unclean-stop" counts
  install_with_data "$ns"
  counts="$(public_row_counts "$ns")"

  freeze_postgres_server "$ns"
  upgrade_to_new_chart "$ns"

  assert_carried_across "$ns" "$counts"
  assert_upgrade_log_says "$ns" "was not shut down cleanly"
  assert_upgrade_log_says "$ns" "automatic recovery in progress"
  assert_upgrade_log_says "$ns" "Upgrade Complete"
  return $?
}

# A volume whose size is enforced and can be changed in place: a tmpfs on the kind node, offered through a
# PV that is bound to the StatefulSet's claim before the claim exists. The claim asks for more than the
# tmpfs holds; a hostPath PV's capacity is only a label, so the tmpfs size is what the database sees.
readonly SMALL_NS="small-volume"
readonly SMALL_CLASS="lh-small"
readonly SMALL_VALUES=(--set "postgresql.persistence.storageClass=$SMALL_CLASS")
SMALL_COUNTS=""
SMALL_GROWN_MIB=""

kind_node() {
  kubectl get nodes -o jsonpath='{.items[0].metadata.name}'
  return $?
}

# The tmpfs of namespace $1's volume on the kind node; each namespace has its own, so two scenarios on
# small volumes can share a cluster.
small_dir() {
  local ns="$1"
  echo "/mnt/lh-small/$ns"
  return
}

provide_small_volume() {
  local ns="$1" node dir
  node="$(kind_node)"
  dir="$(small_dir "$ns")"
  kubectl delete namespace "$ns" --ignore-not-found --wait >/dev/null
  kubectl delete pv "lh-small-$ns" --ignore-not-found --wait >/dev/null
  docker exec "$node" sh -c "if mountpoint -q '$dir'; then umount '$dir'; fi; mkdir -p '$dir' && mount -t tmpfs -o size=1024m tmpfs '$dir'"
  kubectl apply -f - >/dev/null <<EOF
apiVersion: v1
kind: PersistentVolume
metadata:
  name: lh-small-$ns
spec:
  capacity:
    storage: 8Gi
  accessModes: [ReadWriteOnce]
  persistentVolumeReclaimPolicy: Retain
  storageClassName: $SMALL_CLASS
  claimRef:
    namespace: $ns
    name: data-$POSTGRES_POD
  hostPath:
    path: $dir
    type: Directory
EOF
  return $?
}

resize_small_volume() {
  local ns="$1" mib="$2"
  docker exec "$(kind_node)" mount -o "remount,size=${mib}m" "$(small_dir "$ns")"
  return $?
}

mount_size_mib() {
  local ns="$1"
  in_postgres "$ns" df -Pm "$MOUNT" | awk 'NR == 2 { print $2 }'
  return $?
}

volume_used_mib() {
  local ns="$1" used_kib
  used_kib="$(in_postgres "$ns" df -Pk "$MOUNT" | awk 'NR == 2 { print $3 }')"
  echo $(((used_kib + 1023) / 1024))
  return
}

# The room the upgrade step asks for to copy folder $2, by its own measure, in MiB.
copy_needs_mib() {
  local ns="$1" folder="$2" data_kib
  data_kib="$(in_postgres "$ns" du -sk "$MOUNT/$folder" | cut -f1)"
  echo $((data_kib * 11 / 10 / 1024 + 64))
  return
}

# Sizes the volume by the same measure the upgrade step uses: room for the data and about half a second
# copy, and after growing, room for two second copies.
shrink_to_one_copy() {
  local ns="$1" used_mib needed_mib small_mib
  used_mib="$(volume_used_mib "$ns")"
  needed_mib="$(copy_needs_mib "$ns" pgdata)"
  small_mib=$((used_mib + needed_mib / 2))
  SMALL_GROWN_MIB=$((used_mib + needed_mib * 2 + 64))
  resize_small_volume "$ns" "$small_mib"
  [[ "$(mount_size_mib "$ns")" == "$small_mib" ]] \
    || fail "the database in $ns sees a ${MOUNT} of $(mount_size_mib "$ns") MiB, expected the ${small_mib} MiB tmpfs"
  echo "  volume: ${used_mib} MiB used, a second copy needs about ${needed_mib} MiB; sized to ${small_mib} MiB, grows to ${SMALL_GROWN_MIB} MiB"
  return
}

upgrade_restarts() {
  local ns="$1"
  pod_field "$ns" '{.status.initContainerStatuses[?(@.name=="pg-upgrade")].restartCount}'
  return $?
}

wait_upgrade_refused_twice() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS))
  local restarts=""
  until [[ "${restarts:-0}" -ge 2 ]]; do
    if [[ $SECONDS -ge $deadline ]]; then
      dump_diagnostics "$ns"
      fail "the upgrade step in $ns did not fail and retry within ${WAIT_SECONDS}s"
    fi
    sleep 2
    restarts="$(upgrade_restarts "$ns")"
  done
  return $?
}

# Waits until the upgrade step has been refused and retried, checks the database is not Ready, and prints
# the one refusal line the step logged.
refusal_line() {
  local ns="$1" log line
  wait_upgrade_refused_twice "$ns"
  assert_not_ready "$ns" "the database in $ns became Ready although its upgrade was refused"
  log="$(upgrade_log "$ns")"
  line="$(the_one_line_with "lighthouse-postgres: refusing" "$log")" \
    || { dump_diagnostics "$ns"; fail "the upgrade log in $ns does not carry exactly one refusal line"; }
  echo "$line"
  return
}

assert_line_says() {
  local ns="$1" line="$2" part
  for part in "${@:3}"; do
    grep -qF -- "$part" <<<"$line" || fail "the refusal line in $ns does not say \"$part\": $line"
  done
  return $?
}

assert_volume_unchanged() {
  local ns="$1" before="$2" after
  after="$(volume_fingerprint "$ns")"
  [[ "$after" == "$before" ]] \
    || fail "the refused attempt changed the volume in $ns: $(diff <(echo "$before") <(echo "$after") | head -20 || true)"
  echo "  volume fingerprint unchanged: $(sha256sum <<<"$after" | cut -c1-16)"
  return
}

FINGERPRINT=""

# Stops the database, fingerprints the volume, then applies the new chart with the given values and starts
# the database again, so the fingerprint is taken while nothing writes to the volume.
fingerprint_then_upgrade() {
  local ns="$1"
  stop_database "$ns"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  upgrade_to_new_chart "$ns" "${@:2}"
  start_database "$ns"
  return $?
}

too_little_room_refuses_and_touches_nothing() {
  local ns="$SMALL_NS" counts line message
  provide_small_volume "$ns"
  install_with_data "$ns" "${SMALL_VALUES[@]}"
  counts="$(public_row_counts "$ns")"
  shrink_to_one_copy "$ns"

  fingerprint_then_upgrade "$ns" "${SMALL_VALUES[@]}"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "MiB free" "need " "kubectl patch pvc -n $ns data-$POSTGRES_POD" \
    "#when-an-upgrade-is-refused" "postgresql.image"
  message="$(kubectl -n "$ns" describe pod "$POSTGRES_POD")"
  grep -qF -- "$line" <<<"$message" || { dump_diagnostics "$ns"; fail "describing the database pod in $ns does not show the refusal line"; }
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  SMALL_COUNTS="$counts"
  return $?
}

given_refused_for_room() {
  if [[ -z "$SMALL_COUNTS" ]]; then too_little_room_refuses_and_touches_nothing; fi
  return $?
}

grown_volume_lets_refused_upgrade_proceed() {
  local ns="$SMALL_NS"
  given_refused_for_room

  resize_small_volume "$ns" "$SMALL_GROWN_MIB"

  kubectl -n "$ns" wait --for=condition=Ready "pod/$POSTGRES_POD" --timeout="${WAIT_SECONDS}s" >/dev/null \
    || { dump_diagnostics "$ns"; fail "the database in $ns did not become Ready after the volume grew"; }
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$SMALL_COUNTS"
  assert_marker_row "$ns"
  return $?
}

readonly DOCS_PAGE="https://docs.lighthouse.letpeople.work/Installation/kubernetes.html"
readonly MANUAL_PATH="$DOCS_PAGE#moving-data-two-or-more-majors-behind-by-hand"
readonly KUBERNETES_DOCS="docs/Installation/kubernetes.md"
readonly DATA16_NS="data-16"
readonly CLEANED_NS="old-copy-removed"
DATA16_REFUSED=""
CLEANED_COUNTS=""

# Waits until the StatefulSet has replaced the database pod with uid $2.
wait_pod_replaced() {
  local ns="$1" old_uid="$2" deadline=$((SECONDS + WAIT_SECONDS))
  until [[ "$(pod_uid "$ns")" != "$old_uid" && -n "$(pod_uid "$ns")" ]]; do
    if [[ $SECONDS -ge $deadline ]]; then
      dump_diagnostics "$ns"
      fail "the database pod in $ns was not replaced within ${WAIT_SECONDS}s"
    fi
    sleep 2
  done
  return $?
}

# What the docs and the refusal line tell the operator to do after changing values: Kubernetes does not
# replace a database pod stuck on a refused start. The StatefulSet must have taken the new values first, or
# the replacement would start with the old ones.
delete_stuck_pod() {
  local ns="$1" deadline=$((SECONDS + 120))
  until [[ "$(kubectl -n "$ns" get statefulset "$POSTGRES_STATEFULSET" -o jsonpath='{.status.updateRevision}')" \
           != "$(pod_field "$ns" '{.metadata.labels.controller-revision-hash}')" ]]; do
    [[ $SECONDS -lt $deadline ]] || fail "the StatefulSet in $ns did not take the new values"
    sleep 1
  done
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --wait=false >/dev/null
  return $?
}

two_majors_behind_refuses_and_touches_nothing() {
  local ns="$DATA16_NS" line
  install_with_data "$ns" --set postgresql.image=postgres:16

  fingerprint_then_upgrade "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "Postgres 16" "Postgres 18" "$MANUAL_PATH" "kubectl delete pod -n $ns $POSTGRES_POD"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  DATA16_REFUSED=1
  return $?
}

pinning_to_data_major_starts_without_other_step() {
  local ns="$DATA16_NS" old_uid
  if [[ -z "$DATA16_REFUSED" ]]; then two_majors_behind_refuses_and_touches_nothing; fi
  old_uid="$(pod_uid "$ns")"

  upgrade_to_new_chart "$ns" --set postgresql.image=postgres:16
  delete_stuck_pod "$ns"

  wait_pod_replaced "$ns" "$old_uid"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 16
  assert_upgrade_log_says "$ns" "$NOTHING_TO_UPGRADE"
  assert_marker_row "$ns"
  return $?
}

# The first sh block under heading $1 of the Kubernetes docs.
documented_block() {
  local heading="$1"
  awk -v heading="$heading" '$0 == heading { section = 1; next }
                             section && /^```sh$/ { block = 1; next }
                             block && /^```$/ { exit }
                             block { print }' "$KUBERNETES_DOCS"
  return $?
}

# Runs commands exactly as the Kubernetes docs print them, stopping at the first that fails, and returns
# its code. The docs leave the namespace to the reader's context, so they run with a copy of the kubeconfig
# whose context points at $ns.
try_as_documented() {
  local ns="$1" commands="$2" kubeconfig status=0
  kubeconfig="$(mktemp)"
  kubectl config view --raw >"$kubeconfig"
  KUBECONFIG="$kubeconfig" kubectl config set-context --current --namespace="$ns" >/dev/null
  KUBECONFIG="$kubeconfig" bash -ec "$commands" 2>&1 || status=$?
  rm -f "$kubeconfig"
  return "$status"
}

run_as_documented() {
  local ns="$1" status=0
  try_as_documented "$@" || status=$?
  [[ $status -eq 0 ]] || fail "the documented commands failed in $ns with code $status"
  return $?
}

remove_old_copy_as_documented() {
  local ns="$1" command
  command="$(documented_block "### Removing the old copy")"
  [[ "$command" == "kubectl exec "* ]] || fail "no cleanup command found under \"Removing the old copy\" in $KUBERNETES_DOCS"
  run_as_documented "$ns" "$command"
  return $?
}

# The docs remove old copies from a pod of its own, with the same cleanup command, when the database pod
# cannot run it: its start is refused, so its container never runs.
stopped_database_cleanup_as_documented() {
  local removal commands
  removal="$(documented_block "### Removing the old copy")"
  commands="$(documented_block "### Removing old copies while the database is stopped")"
  [[ -n "$commands" ]] || fail "no commands found under \"Removing old copies while the database is stopped\" in $KUBERNETES_DOCS"
  [[ "$commands" == *"-- ${removal#* -- }"* ]] \
    || fail "the docs remove old copies while the database is stopped with another command than the one that removes the old copy"
  echo "$commands"
  return
}

given_old_copy_removed() {
  if [[ -z "$CLEANED_COUNTS" ]]; then cleanup_then_rollback_refuses_empty_database; fi
  return $?
}

postgres_restarts() {
  local ns="$1"
  pod_field "$ns" '{.status.containerStatuses[?(@.name=="postgres")].restartCount}'
  return $?
}

# After $3, a rollback to chart 0.1.17, its Postgres $2 finds pgdata holding only the placeholder and fails
# on it again and again rather than creating an empty database there. Waits for that, prints the line of
# its log that says so, and checks pgdata still holds the placeholder alone.
assert_rolled_back_postgres_fails_on_placeholder() {
  local ns="$1" major="$2" rollback="$3" deadline log=""
  deadline=$((SECONDS + WAIT_SECONDS))
  until [[ "$(postgres_restarts "$ns")" -ge 1 ]] 2>/dev/null; do
    [[ $SECONDS -lt $deadline ]] || { dump_diagnostics "$ns"; fail "Postgres $major in $ns neither failed nor restarted after $rollback"; }
    sleep 2
  done
  assert_not_ready "$ns" "the database in $ns became Ready after $rollback"
  # The container restarts every few seconds, so its last run's log is read from whichever of the current
  # and previous container still has it.
  deadline=$((SECONDS + 120))
  until grep -qF "exists but is not empty" <<<"$log"; do
    [[ $SECONDS -lt $deadline ]] \
      || { dump_diagnostics "$ns"; fail "the rolled-back database log in $ns does not say the data folder is not empty"; }
    log="$(kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres 2>/dev/null || true; kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres --previous 2>/dev/null || true)"
    sleep 1
  done
  echo "  $(grep -F "exists but is not empty" <<<"$log" | head -1)"
  [[ "$(on_volume "$ns" 'ls -1A /volume/pgdata')" == "UPGRADED-TO-18-see-kubernetes-docs" ]] \
    || fail "$rollback in $ns created something in pgdata"
  return $?
}

cleanup_then_rollback_refuses_empty_database() {
  local ns="$CLEANED_NS" counts before_revision old_uid
  install_with_data "$ns"
  before_revision="$(current_revision "$ns")"
  counts="$(public_row_counts "$ns")"
  upgrade_to_new_chart "$ns"
  assert_carried_across "$ns" "$counts"

  remove_old_copy_as_documented "$ns"
  [[ "$(in_postgres "$ns" ls -1A "$MOUNT/pgdata")" == "UPGRADED-TO-18-see-kubernetes-docs" ]] \
    || fail "pgdata in $ns holds more than the placeholder after the documented cleanup"
  restart_database "$ns"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  echo "  a restart after the cleanup starts Postgres 18 on pgdata-18"

  old_uid="$(pod_uid "$ns")"
  helm rollback "$RELEASE" "$before_revision" -n "$ns" >/dev/null

  wait_pod_replaced "$ns" "$old_uid"
  assert_rolled_back_postgres_fails_on_placeholder "$ns" 17 "the rollback"

  old_uid="$(pod_uid "$ns")"
  upgrade_to_new_chart "$ns"
  delete_stuck_pod "$ns"

  wait_pod_replaced "$ns" "$old_uid"
  assert_carried_across "$ns" "$counts"
  CLEANED_COUNTS="$counts"
  return $?
}

newer_data_without_kept_copy_refuses() {
  local ns="$CLEANED_NS" line
  given_old_copy_removed

  fingerprint_then_upgrade "$ns" --set postgresql.image=postgres:17-trixie

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 17" "the data is Postgres 18"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

readonly BALLAST_ROWS=1000000
readonly COPY_STARTED="Restoring database schemas in the new cluster"
readonly DISCARDED_PARTIAL="removing pgdata-18.partial, left by an earlier attempt that did not finish"

# About 300 MB in a table of the harness's own, so copying it and syncing the copy to disk take long
# enough for the pod to be deleted before the finished copy is renamed into place.
seed_bulk_data() {
  local ns="$1"
  psql_in "$ns" "CREATE TABLE upgrade_path_ballast AS
                 SELECT g AS id, repeat(md5(g::text), 8) AS payload FROM generate_series(1, $BALLAST_ROWS) g" >/dev/null
  return $?
}

wait_upgrade_step_running() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS))
  until [[ -n "$(pod_field "$ns" '{.status.initContainerStatuses[?(@.name=="pg-upgrade")].state.running.startedAt}')" ]]; do
    [[ $SECONDS -lt $deadline ]] || { dump_diagnostics "$ns"; fail "the upgrade step in $ns did not start"; }
    sleep 1
  done
  return $?
}

# Follows the upgrade step's log until pg_upgrade starts copying the data files. It prints a step's name
# only together with its result, so the copy's own line appears once the copy is over; the step before it
# is the last line printed before the copy begins.
wait_copy_started() {
  local ns="$1"
  wait_upgrade_step_running "$ns"
  grep -m1 -F -- "$COPY_STARTED" < <(kubectl -n "$ns" logs -f "$POSTGRES_POD" -c pg-upgrade) \
    || { dump_diagnostics "$ns"; fail "the upgrade step in $ns ended without saying it was copying the data"; }
  return $?
}

interrupted_upgrade_is_redone_from_start() {
  local ns="interrupted-upgrade" counts old_uid log copying
  install_with_data "$ns"
  seed_bulk_data "$ns"
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns"
  copying="$(wait_copy_started "$ns")"
  echo "  mid-copy: $copying"
  assert_postgres_not_started "$ns"
  old_uid="$(pod_uid "$ns")"
  # Frozen first, so the copy stays unfinished however long the delete takes; the short grace period then
  # ends the frozen step at once, as a node going down would.
  freeze_upgrade_step "$ns"
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --grace-period=1 --wait >/dev/null

  wait_pod_replaced "$ns" "$old_uid"
  wait_postgres_ready "$ns"
  log="$(upgrade_log "$ns")"
  echo "  next start: $(grep -F -- "$DISCARDED_PARTIAL" <<<"$log" || true)"
  grep -qF -- "$DISCARDED_PARTIAL" <<<"$log" \
    || { dump_diagnostics "$ns"; fail "the start after the delete in $ns did not find a partial copy, so the delete did not land mid-copy"; }
  grep -qF -- "upgrade finished" <<<"$log" || { dump_diagnostics "$ns"; fail "the upgrade in $ns was not redone to the end"; }
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  [[ "$(volume_entries "$ns" | tr '\n' ' ')" == "pgdata pgdata-18 " ]] \
    || fail "the volume in $ns holds $(volume_entries "$ns" | tr '\n' ' ')after the redone upgrade"
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"

  stop_database "$ns"
  open_kept_copy "$ns"
  [[ "$(public_row_counts "$ns" kept-copy)" == "$counts" ]] || fail "the kept Postgres 17 copy in $ns differs from before the upgrade"
  assert_marker_row "$ns" kept-copy
  close_kept_copy "$ns"
  return $?
}

foreign_upgrade_source_refuses_and_touches_nothing() {
  local ns="foreign-source" line
  install_with_data "$ns"

  fingerprint_then_upgrade "$ns" --set postgresql.upgrade.image=postgres:17-alpine

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "the Postgres 17 programs" "cannot run beside the Postgres 18 image"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# Moves the control file of folder $2 off the database volume of $1, to a place on the kind node it can be put
# back from.
set_control_file_aside() {
  local ns="$1" folder="$2"
  on_node mv "$(node_volume_dir "$ns")/$folder/global/pg_control" "/tmp/$ns-$folder-pg_control"
  return $?
}

put_control_file_back() {
  local ns="$1" folder="$2"
  on_node mv "/tmp/$ns-$folder-pg_control" "$(node_volume_dir "$ns")/$folder/global/pg_control"
  return $?
}

# The start was refused because the control file of $3, a Postgres $4 copy, cannot be read below the newer
# copy $5: the line names both and says to put the file back from a backup, never to finish a removal.
assert_unreadable_control_refusal() {
  local ns="$1" line="$2" folder="$3" major="$4" newer="$5"
  assert_line_says "$ns" "$line" "$folder holds Postgres $major data without a readable $folder/global/pg_control" \
    "$newer" "put that file back from a backup"
  ! grep -qF -- "#when-removing-the-old-copy-was-cut-off" <<<"$line" \
    || fail "the refusal line in $ns points to finishing a removal: $line"
  return $?
}

# Runs the cleanup while the database is stopped, as the docs show, and checks that it refuses: code 1, one
# line naming $2/global/pg_control as unreadable and saying to put it back from a backup, and every file on the
# volume as it was. The documented commands stop at the failed cleanup, so the pod they started is deleted here.
assert_stopped_cleanup_refuses() {
  local ns="$1" folder="$2" commands output status=0 line
  stop_database "$ns"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  commands="$(stopped_database_cleanup_as_documented)"

  output="$(try_as_documented "$ns" "$commands")" || status=$?

  [[ $status -eq 1 ]] || fail "the cleanup in $ns ended with code $status rather than 1: $output"
  line="$(the_one_line_with "lighthouse-postgres:" "$output")" \
    || fail "the cleanup in $ns did not print exactly one line: $output"
  echo "  cleanup: $line"
  assert_line_says "$ns" "$line" "$folder/global/pg_control is unreadable" \
    "put that file back from a backup before removing anything" "Nothing was removed"
  kubectl -n "$ns" delete pod pgdata-cleanup --ignore-not-found --wait >/dev/null
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# pgdata loses its control file while the database runs on pgdata-18. Every removal takes a copy's PG_VERSION
# before anything else, so a pgdata that still has one did not lose the file to a removal: this is damage, and
# without that file whether pgdata-18 is still current cannot be told. Putting the file back is the way out.
unreadable_original_copy_beside_upgraded_copy_is_refused() {
  local ns="unreadable-original" counts line
  install_with_data "$ns"
  counts="$(public_row_counts "$ns")"
  upgrade_to_new_chart "$ns"
  assert_carried_across "$ns" "$counts"
  write_marker_table "$ns" upgrade_path_written_on_18
  counts="$(public_row_counts "$ns")"
  set_control_file_aside "$ns" pgdata

  stop_database "$ns"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  start_database "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_unreadable_control_refusal "$ns" "$line" pgdata 17 pgdata-18
  assert_volume_unchanged "$ns" "$FINGERPRINT"

  assert_stopped_cleanup_refuses "$ns" pgdata

  put_control_file_back "$ns" pgdata
  start_database "$ns"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  return $?
}

# After a rollback to chart 0.1.17, Postgres 17 runs on pgdata again, so pgdata holds the newest rows and
# pgdata-18 is out of date, though it still counts as a copy. pgdata losing its control file then must never
# lead the start or the cleanup to remove pgdata as an old copy.
rolled_back_copy_without_control_file_keeps_its_rows() {
  local ns="unreadable-rolled-back" counts before_revision line
  install_with_data "$ns"
  before_revision="$(current_revision "$ns")"
  counts="$(public_row_counts "$ns")"
  upgrade_to_new_chart "$ns"
  assert_carried_across "$ns" "$counts"
  helm rollback "$RELEASE" "$before_revision" -n "$ns" >/dev/null
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata
  write_marker_table "$ns" upgrade_path_written_on_17_after_rollback
  stop_database "$ns"
  set_control_file_aside "$ns" pgdata
  FINGERPRINT="$(volume_fingerprint "$ns")"

  upgrade_to_new_chart "$ns"
  start_database "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_unreadable_control_refusal "$ns" "$line" pgdata 17 pgdata-18
  assert_volume_unchanged "$ns" "$FINGERPRINT"

  assert_stopped_cleanup_refuses "$ns" pgdata

  put_control_file_back "$ns" pgdata
  open_kept_copy "$ns" postgres:17-trixie pgdata
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_17_after_rollback kept-copy)" == "1" ]] \
    || fail "the row written on Postgres 17 after the rollback in $ns is no longer in pgdata"
  close_kept_copy "$ns"
  echo "  pgdata still opens on Postgres 17 with the row written after the rollback"
  return
}

# --- group: chain, in the order the scenarios build on each other --------------------------------------
# A Postgres 16 volume upgraded to 17 by the new chart pinned to 17, then to 18 by the chart's defaults,
# because no Postgres 19 image exists yet.

readonly CHAIN_NS="chain-16-17"
readonly CHAIN_PIN_17=(--set postgresql.image=postgres:17-trixie --set postgresql.upgrade.image=postgres:16-trixie)
# The rollback and the pin-back leave the database on the Postgres 17 copy with the Postgres 18 copy out of
# date beside it, and the cleanup leaves no Postgres 17 copy at all: volumes no later Given starts from, so
# they clear every mark and the next scenario that needs an upgraded volume builds the namespace again from
# the start.
CHAIN_UPGRADED_ONCE=""
CHAIN_UPGRADED_TWICE=""
CHAIN_ROLLED_BACK_ONE_CHART=""
CHAIN_REVISION_ON_16=""
CHAIN_REVISION_ON_17=""
CHAIN_COUNTS_ON_17=""

given_chain_upgraded_once() {
  if [[ -z "$CHAIN_UPGRADED_ONCE" ]]; then chain_first_upgrade_removes_nothing; fi
  return $?
}

given_chain_upgraded_twice() {
  if [[ -z "$CHAIN_UPGRADED_TWICE" ]]; then chain_second_upgrade_keeps_every_row; fi
  return $?
}

given_chain_rolled_back_one_chart() {
  if [[ -z "$CHAIN_ROLLED_BACK_ONE_CHART" ]]; then chain_rollback_one_chart_starts_previous_major_and_warns; fi
  return $?
}

forget_chain_volume() {
  CHAIN_UPGRADED_ONCE=""
  CHAIN_UPGRADED_TWICE=""
  CHAIN_ROLLED_BACK_ONE_CHART=""
  return $?
}

# The warning a start on an older copy logs, once, naming the newer copy whose writes it does not have.
assert_newer_copy_warning() {
  local ns="$1" newer_major="$2" log warning
  log="$(kubectl -n "$ns" logs "$POSTGRES_POD" --all-containers)"
  warning="$(the_one_line_with "newer Postgres $newer_major copy" "$log")" \
    || { dump_diagnostics "$ns"; fail "the database log in $ns does not carry exactly one warning about the newer copy"; }
  echo "  $warning"
  assert_line_says "$ns" "$warning" "pgdata-$newer_major" "not in this database"
  return $?
}

volume_sha256() {
  local ns="$1" path="$2"
  in_postgres "$ns" sha256sum "$MOUNT/$path" | cut -d' ' -f1
  return $?
}

# A copy is only current while its note carries the hash of the control file of the copy it was made from,
# so the note must name that copy's major and hash exactly.
assert_note_names_source() {
  local ns="$1" copy="$2" source_major="$3" source_folder="$4" note hash
  note="$(volume_file "$ns" "$copy/.lighthouse-upgrade")"
  hash="$(volume_sha256 "$ns" "$source_folder/global/pg_control")"
  grep -qx "source_major=$source_major" <<<"$note" \
    || fail "$copy in $ns carries no note saying it was upgraded from Postgres $source_major: $note"
  grep -qx "source_pg_control_sha256=$hash" <<<"$note" \
    || fail "the note in $copy in $ns does not carry the hash of $source_folder/global/pg_control ($hash): $note"
  return $?
}

assert_nothing_removed() {
  local ns="$1" log removals
  log="$(upgrade_log "$ns")" || fail "the upgrade log in $ns could not be read"
  removals="$(grep '^lighthouse-postgres:' <<<"$log" | grep -i 'remov' || true)"
  [[ -z "$removals" ]] || fail "the upgrade log in $ns speaks of removing a copy: $removals"
  return $?
}

# What an upgrade from a copy leaves once the copy before last is gone: the copy it read from, the copy it
# built, and in pgdata only the placeholder that keeps an older chart from creating an empty database there.
assert_two_copies_beside_placeholder() {
  local ns="$1" entries
  entries="$(volume_entries "$ns" | tr '\n' ' ')"
  [[ "$entries" == "pgdata pgdata-17 pgdata-18 " ]] \
    || fail "the volume in $ns holds ${entries}rather than pgdata, pgdata-17 and pgdata-18"
  entries="$(in_postgres "$ns" ls -1A "$MOUNT/pgdata" | tr '\n' ' ')"
  [[ "$entries" == "UPGRADED-TO-18-see-kubernetes-docs " ]] \
    || fail "pgdata in $ns holds ${entries}rather than only the placeholder saying the data was upgraded to Postgres 18"
  return $?
}

# The one line the upgrade log carries about removing the Postgres 16 copy, checked for what it says about
# rolling back.
removal_line() {
  local ns="$1" line
  line="$(the_one_upgrade_log_line "$ns" "removed pgdata (Postgres 16)" "naming the removed pgdata (Postgres 16)")" || return 1
  assert_line_says "$ns" "$line" "a rollback to a chart on Postgres 16 is no longer possible" \
    "a rollback to the chart on Postgres 17 still is"
  echo "$line"
  return
}

# The type, mode, owner and content of every file of the two copies the database can still run on.
copies_fingerprint() {
  local ns="$1"
  on_volume "$ns" 'cd /volume && find pgdata-17 pgdata-18 -printf "%y %m %u %g %p\n" | sort && find pgdata-17 pgdata-18 -type f -print0 | sort -z | xargs -0 -r sha256sum'
  return $?
}

# The first link of the chain: 0.1.17 on Postgres 16, then the new chart pinned to 17 with 16 as the
# upgrade source. It must behave exactly as a first upgrade always has.
chain_first_upgrade_removes_nothing() {
  local ns="$CHAIN_NS" counts
  install_with_data "$ns" --set postgresql.image=postgres:16-trixie
  CHAIN_REVISION_ON_16="$(current_revision "$ns")"
  CHAIN_ROLLED_BACK_ONE_CHART=""
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns" "${CHAIN_PIN_17[@]}"
  CHAIN_REVISION_ON_17="$(current_revision "$ns")"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata-17
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  assert_upgrade_log_says "$ns" "upgrading the Postgres 16 data in pgdata to Postgres 17 in pgdata-17; pgdata is kept as it is"
  assert_nothing_removed "$ns"
  assert_note_names_source "$ns" pgdata-17 16 pgdata

  stop_database "$ns"
  open_kept_copy "$ns" postgres:16-trixie pgdata
  [[ "$(public_row_counts "$ns" kept-copy)" == "$counts" ]] \
    || fail "the kept Postgres 16 copy in $ns no longer holds the recorded row counts"
  close_kept_copy "$ns"
  start_database "$ns"
  wait_postgres_ready "$ns"
  wait_api_ready "$ns"
  CHAIN_UPGRADED_ONCE=1
  return $?
}

chain_second_upgrade_keeps_every_row() {
  local ns="$CHAIN_NS" counts line
  given_chain_upgraded_once
  write_marker_table "$ns" upgrade_path_written_on_17
  counts="$(public_row_counts "$ns")"
  CHAIN_COUNTS_ON_17="$counts"

  upgrade_to_new_chart "$ns" --reset-values

  assert_carried_across "$ns" "$counts"
  assert_data_directory "$ns" pgdata-18
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_17)" == "1" ]] \
    || fail "the row written on Postgres 17 in $ns did not reach Postgres 18"
  line="$(the_one_upgrade_log_line "$ns" "$UPGRADING_17_TO_18" \
    "naming pgdata-17 as source and pgdata-18 as target")"
  echo "  $line"
  grep -qF "pgdata-17 is kept" <<<"$line" || fail "the upgrade line in $ns does not say pgdata-17 is kept: $line"
  assert_note_names_source "$ns" pgdata-18 17 pgdata-17
  CHAIN_UPGRADED_TWICE=1
  return $?
}

# The kept copy is opened while the database runs on pgdata-18: nothing runs on pgdata-17, and the copy is
# taken from a read-only mount.
chain_second_upgrade_removes_copy_before_last() {
  local ns="$CHAIN_NS" line
  given_chain_upgraded_twice

  assert_two_copies_beside_placeholder "$ns"
  line="$(removal_line "$ns")"
  echo "  $line"
  open_kept_copy "$ns" postgres:17-trixie pgdata-17
  [[ "$(public_row_counts "$ns" kept-copy)" == "$CHAIN_COUNTS_ON_17" ]] \
    || fail "the kept Postgres 17 copy in $ns no longer holds the rows it had before the second upgrade"
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_17 kept-copy)" == "1" ]] \
    || fail "the kept Postgres 17 copy in $ns lacks the row written on Postgres 17"
  close_kept_copy "$ns"
  return $?
}

# The baseline is a restart of the fresh install, as for the first upgrade's restart.
chain_restart_after_second_upgrade_does_nothing() {
  local ns="$CHAIN_NS" baseline delay counts line handover
  given_fresh_install
  given_chain_upgraded_twice
  counts="$(public_row_counts "$ns")"
  restart_database "$FRESH_NS"
  baseline="$(start_delay_ms "$FRESH_NS")"

  restart_database "$ns"

  line="$(the_one_upgrade_log_line "$ns" "nothing to upgrade and nothing to remove" "saying there is nothing to upgrade or remove")"
  echo "  $line"
  handover="$(kubectl -n "$ns" logs "$POSTGRES_POD" -c pg-old-binaries)"
  if grep -qF "programs are ready" <<<"$handover"; then
    fail "the older major's programs were handed over for a restart in $ns: $handover"
  fi
  delay="$(start_delay_ms "$ns")"
  echo "  start delay: ${delay} ms after the second upgrade, ${baseline} ms on a fresh install"
  [[ $((delay - baseline)) -lt 5000 ]] \
    || fail "the database in $ns started ${delay} ms after its pod was created, ${baseline} ms on a fresh install"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_row_counts "$ns" "$counts"
  return $?
}

chain_rollback_one_chart_starts_previous_major_and_warns() {
  local ns="$CHAIN_NS"
  given_chain_upgraded_twice
  write_marker_table "$ns" upgrade_path_on_18_before_rollback

  helm rollback "$RELEASE" "$CHAIN_REVISION_ON_17" -n "$ns" >/dev/null
  forget_chain_volume

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata-17
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_17)" == "1" ]] \
    || fail "the row written on Postgres 17 before the second upgrade in $ns is missing after the rollback"
  [[ "$(table_count "$ns" upgrade_path_on_18_before_rollback)" == "0" ]] \
    || fail "the row written on Postgres 18 in $ns is there after the rollback to Postgres 17"
  assert_newer_copy_warning "$ns" 18
  wait_api_ready "$ns"
  CHAIN_ROLLED_BACK_ONE_CHART=1
  return $?
}

# Redone from pgdata-17, the upgrade leaves the same volume as the second upgrade did, so the scenarios
# after it start from it as from that upgrade.
chain_upgrade_again_after_one_chart_rollback_starts_afresh() {
  local ns="$CHAIN_NS"
  given_chain_rolled_back_one_chart
  write_marker_table "$ns" upgrade_path_on_17_after_rollback

  upgrade_to_new_chart "$ns" --reset-values
  CHAIN_ROLLED_BACK_ONE_CHART=""

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_upgrade_log_says "$ns" "$UPGRADING_17_TO_18"
  assert_upgrade_log_says "$ns" "pgdata-18 is out of date"
  [[ "$(marker_rows_in "$ns" upgrade_path_on_17_after_rollback)" == "1" ]] \
    || fail "the row written on Postgres 17 after the rollback in $ns did not reach Postgres 18"
  [[ "$(table_count "$ns" upgrade_path_on_18_before_rollback)" == "0" ]] \
    || fail "the row written on Postgres 18 before the rollback in $ns came back"
  assert_marker_row "$ns"
  assert_nothing_removed "$ns"
  assert_two_copies_beside_placeholder "$ns"
  wait_api_ready "$ns"
  CHAIN_UPGRADED_TWICE=1
  return $?
}

# Pins the database image to $2 and deletes the database pod, as the docs say to after pinning, then waits
# for its replacement. When the StatefulSet is already replacing the pod, deleting it again changes nothing.
pin_image_and_replace_pod() {
  local ns="$1" image="$2" old_uid
  old_uid="$(pod_uid "$ns")"
  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" --set postgresql.image="$image" >/dev/null
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --ignore-not-found --wait=false >/dev/null
  wait_pod_replaced "$ns" "$old_uid"
  return $?
}

chain_pin_back_one_major_starts_kept_copy_and_warns() {
  local ns="$CHAIN_NS"
  given_chain_upgraded_twice
  write_marker_table "$ns" upgrade_path_on_18_before_pin

  pin_image_and_replace_pod "$ns" postgres:17-trixie
  forget_chain_volume

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata-17
  assert_marker_row "$ns"
  [[ "$(table_count "$ns" upgrade_path_on_18_before_pin)" == "0" ]] \
    || fail "the row written on Postgres 18 in $ns is there after pinning to Postgres 17"
  assert_newer_copy_warning "$ns" 18
  wait_api_ready "$ns"
  return $?
}

# The refusal line says to set the image back and delete the pod; doing so ends the scenario, so the volume is
# on Postgres 18 again for the scenarios after it.
chain_pin_back_two_majors_refuses_and_touches_nothing() {
  local ns="$CHAIN_NS" line old_uid
  given_chain_upgraded_twice

  fingerprint_then_upgrade "$ns" --set postgresql.image=postgres:16-trixie

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 16" \
    "the data is Postgres 18, in pgdata-18, which is newer than this image" \
    "no Postgres 16 copy of it is left to start on" \
    "set postgresql.image back to Postgres 18 or remove the pin on it" \
    "kubectl delete pod -n $ns $POSTGRES_POD"
  assert_volume_unchanged "$ns" "$FINGERPRINT"

  old_uid="$(pod_uid "$ns")"
  upgrade_to_new_chart "$ns" --reset-values
  delete_stuck_pod "$ns"
  wait_pod_replaced "$ns" "$old_uid"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  return $?
}

chain_rollback_two_charts_fails_loudly() {
  local ns="$CHAIN_NS" counts copies old_uid
  given_chain_upgraded_twice
  counts="$(public_row_counts "$ns")"
  stop_database "$ns"
  copies="$(copies_fingerprint "$ns")"

  helm rollback "$RELEASE" "$CHAIN_REVISION_ON_16" -n "$ns" >/dev/null
  start_database "$ns"

  wait_pod_replaced "$ns" ""
  assert_rolled_back_postgres_fails_on_placeholder "$ns" 16 "the rollback to chart 0.1.17"
  [[ "$(copies_fingerprint "$ns")" == "$copies" ]] \
    || fail "the rollback to chart 0.1.17 in $ns changed pgdata-17 or pgdata-18"
  echo "  pgdata-17 and pgdata-18 unchanged: $(sha256sum <<<"$copies" | cut -c1-16)"

  old_uid="$(pod_uid "$ns")"
  upgrade_to_new_chart "$ns" --reset-values
  delete_stuck_pod "$ns"

  wait_pod_replaced "$ns" "$old_uid"
  assert_carried_across "$ns" "$counts"
  assert_data_directory "$ns" pgdata-18
  return $?
}

# The volume holds pgdata with only placeholder $2 in it, beside the copies $3..., and nothing else.
assert_placeholder_beside() {
  local ns="$1" placeholder="$2" expected="pgdata ${*:3} " entries
  entries="$(volume_entries "$ns" | tr '\n' ' ')"
  [[ "$entries" == "$expected" ]] || fail "the volume in $ns holds ${entries}rather than $expected"
  entries="$(in_postgres "$ns" ls -1A "$MOUNT/pgdata" | tr '\n' ' ')"
  [[ "$entries" == "$placeholder " ]] || fail "pgdata in $ns holds ${entries}rather than only $placeholder"
  return $?
}

chain_cleanup_removes_every_older_copy_and_reruns() {
  local ns="$CHAIN_NS" counts output
  given_chain_upgraded_twice
  counts="$(public_row_counts "$ns")"

  output="$(remove_old_copy_as_documented "$ns")"
  forget_chain_volume
  echo "  $output"
  assert_placeholder_beside "$ns" UPGRADED-TO-18-see-kubernetes-docs pgdata-18

  output="$(remove_old_copy_as_documented "$ns")"
  echo "  run again: $output"
  grep -qF -- "nothing to remove" <<<"$output" || fail "the cleanup run again in $ns did not say it had nothing to remove: $output"
  assert_placeholder_beside "$ns" UPGRADED-TO-18-see-kubernetes-docs pgdata-18

  restart_database "$ns"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  return $?
}

# The start after $2 found pgdata-18 current: it upgraded nothing and started on pgdata-18 as it was.
assert_started_without_upgrading() {
  local ns="$1" what="$2" log
  log="$(upgrade_log "$ns")"
  grep -qF -- "$NOTHING_TO_UPGRADE" <<<"$log" \
    || { dump_diagnostics "$ns"; fail "the start after $what in $ns did not start on pgdata-18 as it was"; }
  ! grep -qF -- "upgrading the Postgres" <<<"$log" || fail "the start after $what in $ns upgraded again"
  return $?
}

# The cleanup is cut off as a removal leaves it once it has started on the Postgres 17 copy: the placeholder
# the second upgrade left is in pgdata, and pgdata-17 has lost PG_VERSION, its first file to go. A start does
# not finish what only the cleanup takes, so pgdata-17 is still there until the cleanup runs again.
chain_cut_off_cleanup_is_finished_by_running_again() {
  local ns="$CHAIN_NS" counts output
  given_chain_upgraded_twice
  counts="$(public_row_counts "$ns")"
  in_postgres "$ns" test -e "$MOUNT/pgdata/UPGRADED-TO-18-see-kubernetes-docs" \
    || fail "pgdata in $ns holds no placeholder after the second upgrade"
  in_postgres "$ns" rm "$MOUNT/pgdata-17/PG_VERSION"
  forget_chain_volume

  restart_database "$ns"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_started_without_upgrading "$ns" "the cut-off cleanup"
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  [[ "$(volume_entries "$ns" | tr '\n' ' ')" == "pgdata pgdata-17 pgdata-18 " ]] \
    || fail "the start after the cut-off cleanup in $ns left $(volume_entries "$ns" | tr '\n' ' ')rather than the half-removed pgdata-17 for the cleanup"

  output="$(remove_old_copy_as_documented "$ns")"
  echo "  $output"
  assert_placeholder_beside "$ns" UPGRADED-TO-18-see-kubernetes-docs pgdata-18
  return $?
}

# A Postgres 16 volume upgraded once to 17 by the new chart, in a namespace of its own. Any extra
# arguments are a command run against the namespace while it is still on Postgres 16.
upgrade_16_to_17_in() {
  local ns="$1"
  install_with_data "$ns" --set postgresql.image=postgres:16-trixie
  if [[ $# -gt 1 ]]; then
    "${@:2}" "$ns"
  fi
  upgrade_to_new_chart "$ns" "${CHAIN_PIN_17[@]}"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  wait_api_ready "$ns"
  return $?
}

# Where the database volume of $1 lies on the kind node.
node_volume_dir() {
  local ns="$1" volume
  volume="$(kubectl -n "$ns" get pvc "data-$POSTGRES_POD" -o jsonpath='{.spec.volumeName}')"
  kubectl get pv "$volume" -o jsonpath='{.spec.hostPath.path}{.spec.local.path}'
  return $?
}

on_node() {
  docker exec "$(kind_node)" "$@"
  return $?
}

# Waits on the kind node itself, without a fork per check, for the removal of the copy before last to write
# its placeholder into pgdata, then freezes the upgrade step: the removal of a few hundred MB takes well under
# a second, far less than one kubectl round trip. The freeze file is found before the wait, so the freeze
# itself is a single write.
freeze_once_removal_starts() {
  local ns="$1" dir pid
  dir="$(node_volume_dir "$ns")"
  wait_upgrade_step_running "$ns"
  pid="$(container_pid "$ns" initContainerStatuses pg-upgrade)"
  on_node timeout "$WAIT_SECONDS" sh -c "
    freeze=/sys/fs/cgroup\$(sed -n 's/^0:://p' /proc/$pid/cgroup)/cgroup.freeze
    [ -w \"\$freeze\" ] || { echo \"no cgroup freeze file for process $pid\" >&2; exit 1; }
    while :; do set -- '$dir'/pgdata/UPGRADED-TO-*; [ -e \"\$1\" ] && break; done
    echo 1 > \"\$freeze\"" \
    || { dump_diagnostics "$ns"; fail "the upgrade step in $ns never began removing the copy before last"; }
  return $?
}

# The one line the upgrade log carries about finishing the removal of the Postgres 16 copy. A removal cut
# off before PG_VERSION went still names its major; one cut off after can only name the folder.
finishing_line() {
  local ns="$1" line
  line="$(the_one_upgrade_log_line "$ns" "finished removing" "saying it finished removing a copy")" || return 1
  [[ "$line" =~ finished\ removing\ (pgdata\ \(Postgres\ 16\)|what\ was\ left\ of\ pgdata)[,\;] ]] \
    || fail "the finishing line in $ns names something other than the Postgres 16 copy in pgdata: $line"
  echo "$line"
  return
}

chain_interrupted_removal_is_finished_by_next_start() {
  local ns="chain-interrupted-removal" counts entries line
  upgrade_16_to_17_in "$ns" seed_bulk_data
  write_marker_table "$ns" upgrade_path_written_on_17
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns" --reset-values
  freeze_once_removal_starts "$ns"
  kubectl -n "$ns" scale statefulset "$POSTGRES_STATEFULSET" --replicas=0 >/dev/null
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --grace-period=1 --ignore-not-found --wait >/dev/null

  entries="$(on_volume "$ns" 'ls -1A /volume | tr "\n" " "; echo; ls -1A /volume/pgdata | grep -vx UPGRADED-TO-18-see-kubernetes-docs | tr "\n" " "; echo; ls /volume/pgdata/UPGRADED-TO-18-see-kubernetes-docs')"
  echo "  cut off with: ${entries//$'\n'/ | }"
  [[ "$entries" =~ ^pgdata\ pgdata-17\ pgdata-18\ $'\n'[^$'\n']+$'\n'/volume/pgdata/UPGRADED-TO-18-see-kubernetes-docs$ ]] \
    || fail "the delete in $ns did not land mid-removal: the volume holds $entries"

  start_database "$ns"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_started_without_upgrading "$ns" "the cut-off removal"
  line="$(finishing_line "$ns")"
  echo "  $line"
  assert_two_copies_beside_placeholder "$ns"
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  return $?
}

# The database is held at no replicas while the frozen pod goes, so the volume can be read as the
# interrupted upgrade left it before the next start redoes the upgrade.
chain_interrupted_second_upgrade_removes_nothing() {
  local ns="chain-interrupted-copy" counts_on_16 counts entries log finished removed copying
  install_with_data "$ns" --set postgresql.image=postgres:16-trixie
  seed_bulk_data "$ns"
  counts_on_16="$(public_row_counts "$ns")"
  upgrade_to_new_chart "$ns" "${CHAIN_PIN_17[@]}"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  wait_api_ready "$ns"
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns" --reset-values
  copying="$(wait_copy_started "$ns")"
  echo "  mid-copy: $copying"
  assert_postgres_not_started "$ns"
  freeze_upgrade_step "$ns"
  kubectl -n "$ns" scale statefulset "$POSTGRES_STATEFULSET" --replicas=0 >/dev/null
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --grace-period=1 --wait >/dev/null

  entries="$(on_volume "$ns" 'cat /volume/pgdata/PG_VERSION; ls -1A /volume | tr "\n" " "')"
  [[ "$entries" == $'16\npgdata pgdata-17 pgdata-18.partial ' ]] \
    || fail "the interrupted upgrade in $ns left $entries rather than the Postgres 16 pgdata, pgdata-17 and an unfinished pgdata-18.partial"
  open_kept_copy "$ns" postgres:16-trixie pgdata
  [[ "$(public_row_counts "$ns" kept-copy)" == "$counts_on_16" ]] \
    || fail "the original Postgres 16 copy in $ns no longer holds the recorded row counts after the interrupted upgrade"
  close_kept_copy "$ns"
  echo "  after the interruption pgdata still opens on Postgres 16 with every row"

  start_database "$ns"
  wait_postgres_ready "$ns"
  log="$(upgrade_log "$ns")"
  grep -qF -- "$DISCARDED_PARTIAL" <<<"$log" \
    || { dump_diagnostics "$ns"; fail "the start after the delete in $ns did not find a partial copy, so the delete did not land mid-copy"; }
  grep -qF -- "$UPGRADING_17_TO_18" <<<"$log" \
    || { dump_diagnostics "$ns"; fail "the next start in $ns did not begin the upgrade again from pgdata-17"; }
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  finished="$(grep -nF "upgrade finished" <<<"$log" | cut -d: -f1 || true)"
  removed="$(grep -nF "removed pgdata (Postgres 16)" <<<"$log" | cut -d: -f1 || true)"
  [[ -n "$finished" && -n "$removed" && "$removed" -gt "$finished" ]] \
    || { dump_diagnostics "$ns"; fail "the Postgres 16 copy in $ns was not removed after the redone upgrade finished"; }
  echo "  $(sed -n "${removed}p" <<<"$log")"
  assert_two_copies_beside_placeholder "$ns"
  return $?
}

readonly CHAIN_SMALL_NS="chain-small-volume"
readonly STOPPED_CLEANUP_DOCS="$DOCS_PAGE#removing-old-copies-while-the-database-is-stopped"

# Shrinks the tmpfs under a volume upgraded once so that the next copy does not fit beside the two it
# holds, by half the room the Postgres 16 copy takes, while the first upgrade, which needed $2 MiB in all,
# would still have fitted. Removing the Postgres 16 copy then frees enough for the next copy.
shrink_below_next_copy() {
  local ns="$1" first_needed_mib="$2" used old_mib needed small_mib
  used="$(volume_used_mib "$ns")"
  old_mib=$(($(in_postgres "$ns" du -sk "$MOUNT/pgdata" | cut -f1) / 1024))
  needed="$(copy_needs_mib "$ns" pgdata-17)"
  small_mib=$((used + needed - old_mib / 2))
  [[ $small_mib -ge $first_needed_mib ]] \
    || fail "a ${small_mib} MiB volume in $ns would not have held the first upgrade, which needed ${first_needed_mib} MiB"
  resize_small_volume "$ns" "$small_mib"
  [[ "$(mount_size_mib "$ns")" == "$small_mib" ]] \
    || fail "the database in $ns sees a ${MOUNT} of $(mount_size_mib "$ns") MiB, expected the ${small_mib} MiB tmpfs"
  echo "  volume: the first upgrade needed ${first_needed_mib} MiB in all; now ${used} MiB used, the Postgres 16 copy takes ${old_mib} MiB and the next copy needs about ${needed} MiB; sized to ${small_mib} MiB"
  return
}

chain_too_little_room_names_cleanup_and_touches_nothing() {
  local ns="$CHAIN_SMALL_NS" first_needed counts line commands output
  provide_small_volume "$ns"
  install_with_data "$ns" "${SMALL_VALUES[@]}" --set postgresql.image=postgres:16-trixie
  first_needed=$(($(volume_used_mib "$ns") + $(copy_needs_mib "$ns" pgdata)))
  upgrade_to_new_chart "$ns" "${SMALL_VALUES[@]}" "${CHAIN_PIN_17[@]}"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_data_directory "$ns" pgdata-17
  wait_api_ready "$ns"
  shrink_below_next_copy "$ns" "$first_needed"
  counts="$(public_row_counts "$ns")"

  fingerprint_then_upgrade "$ns" "${SMALL_VALUES[@]}" --reset-values

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing upgrade 17→18" "MiB free" "need " "$STOPPED_CLEANUP_DOCS"
  assert_volume_unchanged "$ns" "$FINGERPRINT"

  commands="$(stopped_database_cleanup_as_documented)"
  output="$(run_as_documented "$ns" "$commands")"
  echo "  $(grep -F "lighthouse-postgres:" <<<"$output")"

  assert_carried_across "$ns" "$counts"
  assert_data_directory "$ns" pgdata-18
  assert_upgrade_log_says "$ns" "$UPGRADING_17_TO_18"
  assert_placeholder_beside "$ns" UPGRADED-TO-17-see-kubernetes-docs pgdata-17 pgdata-18
  return $?
}

chain_cleaned_volume_moves_on_and_removes_nothing() {
  local ns="chain-cleaned" counts output
  upgrade_16_to_17_in "$ns"
  output="$(remove_old_copy_as_documented "$ns")"
  echo "  $output"
  assert_placeholder_beside "$ns" UPGRADED-TO-17-see-kubernetes-docs pgdata-17
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns" --reset-values

  assert_carried_across "$ns" "$counts"
  assert_data_directory "$ns" pgdata-18
  assert_upgrade_log_says "$ns" "$UPGRADING_17_TO_18"
  assert_nothing_removed "$ns"
  assert_placeholder_beside "$ns" UPGRADED-TO-17-see-kubernetes-docs pgdata-17 pgdata-18
  return $?
}

# Postgres 16 running again on pgdata after the rollback rewrites its pg_control, so pgdata-17 no longer
# matches the note it was made with and pgdata is where the newest data is.
chain_out_of_date_copy_counts_as_older_major_and_refuses() {
  local ns="chain-out-of-date" before_revision line
  install_with_data "$ns" --set postgresql.image=postgres:16-trixie
  before_revision="$(current_revision "$ns")"
  upgrade_to_new_chart "$ns" "${CHAIN_PIN_17[@]}"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  helm rollback "$RELEASE" "$before_revision" -n "$ns" >/dev/null
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 16
  assert_data_directory "$ns" pgdata

  fingerprint_then_upgrade "$ns" --reset-values

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 18" "Postgres 16 in pgdata, 2 majors behind Postgres 18" \
    "pgdata-17 is out of date because Postgres 16 ran after it was made" "one major per chart release" "$MANUAL_PATH"
  if grep -qE 'postgres:17|start pgdata-17' <<<"$line"; then
    fail "the refusal line in $ns offers the out-of-date pgdata-17 as a way back: $line"
  fi
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# The bookworm pair stands in for a Postgres 15 to 16 upgrade by the new chart, because there is no
# postgres:15-trixie image.
chain_gap_from_a_copy_refuses_and_touches_nothing() {
  local ns="chain-gap-from-copy" line
  install_with_data "$ns" --set postgresql.image=postgres:15-bookworm
  upgrade_to_new_chart "$ns" --set postgresql.image=postgres:16-bookworm --set postgresql.upgrade.image=postgres:15-bookworm
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 16
  assert_data_directory "$ns" pgdata-16

  fingerprint_then_upgrade "$ns" --reset-values

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 18" "Postgres 16 in pgdata-16" \
    "one major per chart release" "one at a time" "$MANUAL_PATH" "kubectl delete pod -n $ns $POSTGRES_POD"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# The database runs on pgdata-17, so its control file is taken away from the kind node while it is stopped:
# a running server would rewrite it on its way down.
chain_unreadable_live_copy_refuses_before_writing() {
  local ns="chain-unreadable-live" line
  upgrade_16_to_17_in "$ns"
  stop_database "$ns"
  on_node rm "$(node_volume_dir "$ns")/pgdata-17/global/pg_control"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  upgrade_to_new_chart "$ns" --reset-values
  start_database "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to upgrade Postgres 17 to 18" \
    "pgdata-17 holds Postgres 17 data without a readable pgdata-17/global/pg_control" \
    "Postgres 17 cannot open it" "put that file back from a backup"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# The log of every container of the database pod. kubectl logs --all-containers fails outright while the
# postgres container has never started, which is the case on every refused start.
every_container_log() {
  local ns="$1" container
  for container in $(pod_field "$ns" '{.spec.initContainers[*].name} {.spec.containers[*].name}'); do
    kubectl -n "$ns" logs "$POSTGRES_POD" -c "$container" 2>/dev/null || true
  done
  return $?
}

# Without pgdata's control file the note in pgdata-17 cannot be checked, so whether the database last ran on
# pgdata or on pgdata-17 cannot be told.
chain_unfollowable_chain_is_refused_once_by_upgrade_step() {
  local ns="chain-unreadable-original" line all_logs handover exit_code
  upgrade_16_to_17_in "$ns"
  in_postgres "$ns" rm "$MOUNT/pgdata/global/pg_control"

  fingerprint_then_upgrade "$ns" --reset-values

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_unreadable_control_refusal "$ns" "$line" pgdata 16 pgdata-17
  all_logs="$(every_container_log "$ns")"
  the_one_line_with "refusing" "$all_logs" >/dev/null \
    || { dump_diagnostics "$ns"; fail "the logs of every container in $ns do not carry exactly one refusal line"; }
  grep -qF -- "$line" <<<"$(kubectl -n "$ns" describe pod "$POSTGRES_POD")" \
    || { dump_diagnostics "$ns"; fail "describing the database pod in $ns does not show the refusal line"; }
  handover="$(kubectl -n "$ns" logs "$POSTGRES_POD" -c pg-old-binaries)"
  [[ -z "$handover" ]] || fail "the step handing over the older programs in $ns logged: $handover"
  exit_code="$(pod_field "$ns" '{.status.initContainerStatuses[?(@.name=="pg-old-binaries")].state.terminated.exitCode}')"
  [[ "$exit_code" == "0" ]] || fail "the step handing over the older programs in $ns ended with code ${exit_code:-none} rather than 0"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# A placeholder in pgdata beside a PG_VERSION that still makes pgdata count is no sign of a removal: every
# removal takes PG_VERSION right after it writes the placeholder, before anything else. With pgdata's control
# file gone as well, only damage leaves this volume, so the start and the cleanup both refuse.
chain_cleanup_refuses_past_unreadable_copy_even_with_placeholder() {
  local ns="chain-cleanup-past-unreadable" line
  upgrade_16_to_17_in "$ns"
  in_postgres "$ns" touch "$MOUNT/pgdata/UPGRADED-TO-17-see-kubernetes-docs"
  in_postgres "$ns" rm "$MOUNT/pgdata/global/pg_control"
  stop_database "$ns"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  start_database "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_unreadable_control_refusal "$ns" "$line" pgdata 16 pgdata-17
  assert_volume_unchanged "$ns" "$FINGERPRINT"

  assert_stopped_cleanup_refuses "$ns" pgdata
  return $?
}

# Starts from the volume the refusal before it left: pgdata without its control file beside pgdata-17, and the
# database refused under the chart's defaults.
chain_cleanup_refuses_past_unreadable_copy_not_started() {
  local ns="chain-unreadable-original"
  if ! kubectl get namespace "$ns" >/dev/null 2>&1; then chain_unfollowable_chain_is_refused_once_by_upgrade_step; fi
  assert_stopped_cleanup_refuses "$ns" pgdata
  return $?
}

# pgdata-17 has lost PG_VERSION, so it no longer counts as a copy and the walk up the copies ends at pgdata,
# yet pgdata-17 holds every write made on Postgres 17. An upgrade from pgdata would set it aside as out of
# date and delete it, so the start is refused instead, and the older major's programs are not handed over.
chain_newer_copy_that_no_longer_counts_is_never_set_aside() {
  local ns="chain-uncounted-copy" line handover
  upgrade_16_to_17_in "$ns"
  write_marker_table "$ns" upgrade_path_written_on_17
  stop_database "$ns"
  on_node rm "$(node_volume_dir "$ns")/pgdata-17/PG_VERSION"
  FINGERPRINT="$(volume_fingerprint "$ns")"
  start_database "$ns"

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 17" "found Postgres 16 data in pgdata and pgdata-17" \
    "cannot start on or upgrade by itself"
  handover="$(kubectl -n "$ns" logs "$POSTGRES_POD" -c pg-old-binaries)" \
    || fail "the log of the step handing over the older programs in $ns could not be read"
  [[ -z "$handover" ]] || fail "the step handing over the older programs in $ns logged: $handover"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  return $?
}

# The documented cleanup is cut off right after it writes the placeholder, as if the pod running it was
# killed there: pgdata still holds PG_VERSION and every file, so it still counts as the Postgres 16 copy.
chain_pin_back_after_cut_off_cleanup_keeps_pinned_data() {
  local ns="chain-pin-after-cut-cleanup" restart
  upgrade_16_to_17_in "$ns"
  in_postgres "$ns" touch "$MOUNT/pgdata/UPGRADED-TO-17-see-kubernetes-docs"

  pin_image_and_replace_pod "$ns" postgres:16-trixie
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 16
  assert_data_directory "$ns" pgdata
  assert_newer_copy_warning "$ns" 17
  assert_nothing_removed "$ns"
  write_marker_table "$ns" upgrade_path_written_on_16_after_pin

  for restart in 1 2; do
    restart_database "$ns"
    wait_postgres_ready "$ns"
    assert_server_major "$ns" 16
    assert_data_directory "$ns" pgdata
    [[ "$(marker_rows_in "$ns" upgrade_path_written_on_16_after_pin)" == "1" ]] \
      || fail "the row written on Postgres 16 in $ns is gone after restart $restart"
    assert_nothing_removed "$ns"
  done
  [[ "$(volume_entries "$ns" | tr '\n' ' ')" == "pgdata pgdata-17 " ]] \
    || fail "the volume in $ns holds $(volume_entries "$ns" | tr '\n' ' ')rather than pgdata and pgdata-17"
  echo "  pgdata still opens on Postgres 16 with the row written after the pin; pgdata-17 is still there"
  return
}

# A file marked immutable from the kind node cannot be unlinked even by root, which the upgrade step runs
# as; the mark is taken off again before the scenario ends, so the namespace can be deleted.
chain_unremovable_leftover_never_stops_the_start() {
  local ns="chain-unremovable-leftover" counts leftover line
  upgrade_16_to_17_in "$ns"
  counts="$(public_row_counts "$ns")"
  leftover="$(node_volume_dir "$ns")/pgdata/postgresql.conf"
  on_node chattr +i "$leftover"

  upgrade_to_new_chart "$ns" --reset-values
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
  line="$(the_one_upgrade_log_line "$ns" "could not be fully removed" "warning that a copy could not be fully removed")"
  echo "  $line"
  assert_line_says "$ns" "$line" "warning: pgdata (Postgres 16) could not be fully removed" "Postgres 18 starts on pgdata-18"
  on_node test -e "$leftover" || fail "the immutable file in $ns is gone"

  restart_database "$ns"
  wait_postgres_ready "$ns"
  assert_data_directory "$ns" pgdata-18
  line="$(the_one_line_with "could not be fully removed" "$(upgrade_log "$ns")")" \
    || { dump_diagnostics "$ns"; fail "the restart in $ns did not try the removal again"; }
  echo "  $line"
  assert_line_says "$ns" "$line" "pgdata" "Postgres 18 starts on pgdata-18"

  on_node chattr -i "$leftover"
  restart_database "$ns"
  wait_postgres_ready "$ns"
  assert_data_directory "$ns" pgdata-18
  line="$(finishing_line "$ns")"
  echo "  $line"
  assert_two_copies_beside_placeholder "$ns"
  return $?
}

# Only scenarios that are implemented, in an order where each one's Given is made by itself or left by
# one before it. The legs are split by running time, about ten minutes each on a laptop, well inside the CI
# job's limit; a scenario on a volume of its own can move to any leg that loads its images.
readonly HAPPY=(
  fresh_install_starts_18_without_upgrading
  upgrade_plain_helm_upgrade_keeps_every_row
  restart_after_upgrade_does_not_upgrade_again
  rollback_between_new_chart_revisions_changes_nothing
  kept_copy_holds_pre_upgrade_rows
  rollback_starts_17_on_pre_upgrade_data
  upgrade_again_after_rollback_starts_afresh
  upgrade_with_reset_then_reuse_values_keeps_every_row
  upgrade_by_rendered_manifests_keeps_every_row
  pin_back_after_upgrade_starts_on_kept_copy_and_warns
  reuse_values_stays_on_17_and_says_so
  upgrade_after_unclean_stop_keeps_every_row
  interrupted_upgrade_is_redone_from_start
  foreign_upgrade_source_refuses_and_touches_nothing
)

readonly REFUSALS=(
  too_little_room_refuses_and_touches_nothing
  grown_volume_lets_refused_upgrade_proceed
  two_majors_behind_refuses_and_touches_nothing
  pinning_to_data_major_starts_without_other_step
  cleanup_then_rollback_refuses_empty_database
  newer_data_without_kept_copy_refuses
  unreadable_original_copy_beside_upgraded_copy_is_refused
  rolled_back_copy_without_control_file_keeps_its_rows
)

readonly CHAIN=(
  chain_first_upgrade_removes_nothing
  chain_second_upgrade_keeps_every_row
  chain_second_upgrade_removes_copy_before_last
  chain_restart_after_second_upgrade_does_nothing
  chain_rollback_one_chart_starts_previous_major_and_warns
  chain_upgrade_again_after_one_chart_rollback_starts_afresh
  chain_pin_back_one_major_starts_kept_copy_and_warns
  chain_pin_back_two_majors_refuses_and_touches_nothing
  chain_rollback_two_charts_fails_loudly
  chain_interrupted_second_upgrade_removes_nothing
  chain_unremovable_leftover_never_stops_the_start
)

# The refusals on volumes the new chart upgraded, each on a volume of its own, and a pin-back that must not
# remove anything.
readonly CHAIN_REFUSALS=(
  chain_out_of_date_copy_counts_as_older_major_and_refuses
  chain_gap_from_a_copy_refuses_and_touches_nothing
  chain_unreadable_live_copy_refuses_before_writing
  chain_unfollowable_chain_is_refused_once_by_upgrade_step
  chain_cleanup_refuses_past_unreadable_copy_not_started
  chain_too_little_room_names_cleanup_and_touches_nothing
  chain_newer_copy_that_no_longer_counts_is_never_set_aside
  chain_pin_back_after_cut_off_cleanup_keeps_pinned_data
)

# The documented cleanup and the removals an upgrade makes by itself, on volumes the new chart upgraded. Each
# scenario that removes the Postgres 17 copy leaves a volume the next one has to build again.
readonly CHAIN_CLEANUP=(
  chain_cleanup_removes_every_older_copy_and_reruns
  chain_cut_off_cleanup_is_finished_by_running_again
  chain_cleaned_volume_moves_on_and_removes_nothing
  chain_cleanup_refuses_past_unreadable_copy_even_with_placeholder
  chain_interrupted_removal_is_finished_by_next_start
)

# The namespaces each scenario uses, its Givens included, so a namespace is deleted as soon as no later
# scenario in the run needs it and the pods of a long run do not pile up on the node.
namespaces_of() {
  local scenario="$1"
  case "$scenario" in
    fresh_install_starts_18_without_upgrading) echo "$FRESH_NS" ;;
    restart_after_upgrade_does_not_upgrade_again) echo "$FRESH_NS $UPGRADED_NS" ;;
    upgrade_plain_helm_upgrade_keeps_every_row | rollback_between_new_chart_revisions_changes_nothing \
      | kept_copy_holds_pre_upgrade_rows | rollback_starts_17_on_pre_upgrade_data \
      | upgrade_again_after_rollback_starts_afresh | pin_back_after_upgrade_starts_on_kept_copy_and_warns)
      echo "$UPGRADED_NS" ;;
    upgrade_with_reset_then_reuse_values_keeps_every_row) echo reset-then-reuse ;;
    upgrade_by_rendered_manifests_keeps_every_row) echo rendered-manifests ;;
    reuse_values_stays_on_17_and_says_so) echo reuse-values ;;
    upgrade_after_unclean_stop_keeps_every_row) echo unclean-stop ;;
    too_little_room_refuses_and_touches_nothing | grown_volume_lets_refused_upgrade_proceed) echo "$SMALL_NS" ;;
    two_majors_behind_refuses_and_touches_nothing | pinning_to_data_major_starts_without_other_step) echo "$DATA16_NS" ;;
    cleanup_then_rollback_refuses_empty_database | newer_data_without_kept_copy_refuses) echo "$CLEANED_NS" ;;
    foreign_upgrade_source_refuses_and_touches_nothing) echo foreign-source ;;
    interrupted_upgrade_is_redone_from_start) echo interrupted-upgrade ;;
    unreadable_original_copy_beside_upgraded_copy_is_refused) echo unreadable-original ;;
    rolled_back_copy_without_control_file_keeps_its_rows) echo unreadable-rolled-back ;;
    chain_first_upgrade_removes_nothing | chain_second_upgrade_keeps_every_row \
      | chain_second_upgrade_removes_copy_before_last | chain_rollback_one_chart_starts_previous_major_and_warns \
      | chain_upgrade_again_after_one_chart_rollback_starts_afresh | chain_pin_back_one_major_starts_kept_copy_and_warns \
      | chain_pin_back_two_majors_refuses_and_touches_nothing | chain_rollback_two_charts_fails_loudly \
      | chain_cleanup_removes_every_older_copy_and_reruns | chain_cut_off_cleanup_is_finished_by_running_again)
      echo "$CHAIN_NS" ;;
    chain_restart_after_second_upgrade_does_nothing) echo "$FRESH_NS $CHAIN_NS" ;;
    chain_interrupted_removal_is_finished_by_next_start) echo chain-interrupted-removal ;;
    chain_interrupted_second_upgrade_removes_nothing) echo chain-interrupted-copy ;;
    chain_too_little_room_names_cleanup_and_touches_nothing) echo chain-small-volume ;;
    chain_cleaned_volume_moves_on_and_removes_nothing) echo chain-cleaned ;;
    chain_out_of_date_copy_counts_as_older_major_and_refuses) echo chain-out-of-date ;;
    chain_gap_from_a_copy_refuses_and_touches_nothing) echo chain-gap-from-copy ;;
    chain_pin_back_after_cut_off_cleanup_keeps_pinned_data) echo chain-pin-after-cut-cleanup ;;
    chain_unreadable_live_copy_refuses_before_writing) echo chain-unreadable-live ;;
    chain_unfollowable_chain_is_refused_once_by_upgrade_step | chain_cleanup_refuses_past_unreadable_copy_not_started)
      echo chain-unreadable-original ;;
    chain_cleanup_refuses_past_unreadable_copy_even_with_placeholder) echo chain-cleanup-past-unreadable ;;
    chain_newer_copy_that_no_longer_counts_is_never_set_aside) echo chain-uncounted-copy ;;
    chain_unremovable_leftover_never_stops_the_start) echo chain-unremovable-leftover ;;
    *) ;;
  esac
  return $?
}

# Deletes the namespaces scenario $1 used that none of the scenarios after it, $2..., uses.
delete_namespaces_done_with() {
  local scenario="$1" ns later needed
  for ns in $(namespaces_of "$scenario"); do
    needed=""
    for later in "${@:2}"; do
      if [[ " $(namespaces_of "$later") " == *" $ns "* ]]; then
        needed=1
      fi
    done
    if [[ -z "$needed" ]]; then
      kubectl delete namespace "$ns" --ignore-not-found --wait=false >/dev/null
    fi
  done
  return $?
}

main() {
  local cluster="${1:-}"
  local group="${2:-}"
  [[ -n "$cluster" && -n "$group" ]] || fail "usage: run.sh CLUSTER GROUP [SCENARIO...]"
  shift 2

  kind get clusters | grep -qx "$cluster" || fail "no kind cluster named $cluster"
  [[ -f "$BEFORE_CHART" ]] || fail "$BEFORE_CHART is missing; run from the repository root"
  [[ -d "$NEW_CHART" ]] || fail "$NEW_CHART is missing; run from the repository root"
  kubectl config use-context "kind-$cluster" >/dev/null

  local scenarios=()
  case "$group" in
    happy) scenarios=("${HAPPY[@]}") ;;
    refusals) scenarios=("${REFUSALS[@]}") ;;
    chain) scenarios=("${CHAIN[@]}") ;;
    chain-refusals) scenarios=("${CHAIN_REFUSALS[@]}") ;;
    chain-cleanup) scenarios=("${CHAIN_CLEANUP[@]}") ;;
    *) fail "unknown group $group (happy | refusals | chain | chain-refusals | chain-cleanup)" ;;
  esac
  if [[ $# -gt 0 ]]; then
    scenarios=("$@")
  fi

  preload_images "$cluster" "$group"

  local scenario index started
  for scenario in "${scenarios[@]}"; do
    declare -F "$scenario" >/dev/null || fail "unknown scenario $scenario"
  done
  for index in "${!scenarios[@]}"; do
    scenario="${scenarios[$index]}"
    started=$SECONDS
    echo "▶ $scenario"
    "$scenario"
    delete_namespaces_done_with "$scenario" "${scenarios[@]:index+1}"
    echo "✓ $scenario ($((SECONDS - started)) s)"
  done
  return 0
}

main "$@"
