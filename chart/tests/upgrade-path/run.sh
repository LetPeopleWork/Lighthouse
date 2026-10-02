#!/usr/bin/env bash
# Kind harness for chart/tests/acceptance/upgrade-bundled-postgres.feature. One function per @real-io
# scenario, named after it.
#
# The same script runs locally and in CI, so the cluster is an argument rather than an assumption. Every
# scenario gets a namespace of its own, so scenarios sharing a cluster never see each other's volume.
#
# Usage: run.sh CLUSTER GROUP [SCENARIO...]
#   CLUSTER   name of an existing kind cluster (kind create cluster --name CLUSTER)
#   GROUP     happy | refusals — the two legs of the CI matrix, each on its own cluster
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
}

chart_app_version() {
  helm show chart "$1" | awk '/^appVersion:/ { gsub(/"/, "", $2); print $2; exit }'
}

# Pulls each image once into the local Docker and loads it into the kind node, so the node never pulls
# from Docker Hub itself: its anonymous pull limit is shared by every job on a CI runner's address.
preload_images() {
  local cluster="$1" image
  local images=(
    postgres:16
    postgres:17
    postgres:17-trixie
    postgres:17-alpine
    postgres:18-trixie
    busybox:1.37
    "ghcr.io/letpeoplework/lighthouse:$(chart_app_version "$BEFORE_CHART")"
    "ghcr.io/letpeoplework/lighthouse:$(chart_app_version "$NEW_CHART")"
  )
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
}

fresh_namespace() {
  local ns="$1"
  kubectl delete namespace "$ns" --ignore-not-found --wait >/dev/null
  kubectl create namespace "$ns" >/dev/null
}

dump_diagnostics() {
  local ns="$1"
  echo "--- diagnostics for namespace $ns ---" >&2
  kubectl -n "$ns" get pods -o wide >&2 || true
  kubectl -n "$ns" describe pod "$POSTGRES_POD" >&2 || true
  kubectl -n "$ns" logs "$POSTGRES_POD" --all-containers --tail=80 >&2 || true
  kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres --previous --tail=40 >&2 || true
}

install_before_chart() {
  local ns="$1"
  fresh_namespace "$ns"
  helm install "$RELEASE" "$BEFORE_CHART" -n "$ns" "${VALUES[@]}" "${@:2}" --wait --timeout 10m >/dev/null \
    || { dump_diagnostics "$ns"; fail "chart 0.1.17 did not install in $ns"; }
}

install_new_chart() {
  local ns="$1"
  fresh_namespace "$ns"
  helm install "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" >/dev/null
}

# No --wait: a database that crash-loops would hold Helm for its whole timeout, and the wait below
# notices a crash loop and reports its log at once.
upgrade_to_new_chart() {
  local ns="$1"
  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" "${@:2}" >/dev/null
}

highest_restart_count() {
  local ns="$1"
  # While the StatefulSet replaces the pod there is briefly none to read, which counts as no restarts
  # rather than ending the run.
  { kubectl -n "$ns" get pod "$POSTGRES_POD" \
    -o jsonpath='{range .status.initContainerStatuses[*]}{.restartCount}{"\n"}{end}{range .status.containerStatuses[*]}{.restartCount}{"\n"}{end}' \
    2>/dev/null || true; } | sort -n | tail -1
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
}

wait_postgres_ready() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS))
  until kubectl -n "$ns" rollout status "statefulset/$POSTGRES_STATEFULSET" --timeout=10s >/dev/null 2>&1; do
    fail_if_crash_looping_or_late "$ns" "$deadline" \
      "the database pod in $ns is crash-looping" \
      "the database in $ns did not become Ready within ${WAIT_SECONDS}s"
  done
  kubectl -n "$ns" wait --for=condition=Ready "pod/$POSTGRES_POD" --timeout="${WAIT_SECONDS}s" >/dev/null
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
}

in_postgres() {
  local ns="$1"
  kubectl -n "$ns" exec "$POSTGRES_POD" -c postgres -- "${@:2}"
}

psql_on() {
  local ns="$1" pod="$2" sql="$3"
  kubectl -n "$ns" exec "$pod" -c postgres -- \
    psql -U "$DB_USER" -d lighthouse -XAtq -v ON_ERROR_STOP=1 -c "$sql"
}

psql_in() {
  local ns="$1" sql="$2"
  psql_on "$ns" "$POSTGRES_POD" "$sql"
}

seed_marker_row() {
  local ns="$1"
  psql_in "$ns" "CREATE TABLE upgrade_path_marker (marker text PRIMARY KEY);
                 INSERT INTO upgrade_path_marker VALUES ('$MARKER');
                 CREATE TABLE upgrade_path_bulk AS SELECT g AS id, md5(g::text) AS payload FROM generate_series(1, 20000) g;" >/dev/null
}

# One "table|rows" line per table of the public schema, sorted, so two recordings compare as text.
public_row_counts() {
  local ns="$1" pod="${2:-$POSTGRES_POD}"
  psql_on "$ns" "$pod" "SELECT table_name || '|' || (xpath('/row/c/text()',
                   query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name), false, true, '')))[1]::text
                 FROM information_schema.tables
                 WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                 ORDER BY table_name"
}

assert_row_counts() {
  local ns="$1" before="$2" after
  after="$(public_row_counts "$ns")"
  [[ "$after" == "$before" ]] || fail "row counts in $ns changed: $(diff <(echo "$before") <(echo "$after") || true)"
}

# A table of its own holding this run's marker, so whether a write made at one point survives can be
# checked later by name.
write_marker_table() {
  local ns="$1" table="$2"
  psql_in "$ns" "CREATE TABLE $table (marker text);
                 INSERT INTO $table VALUES ('$MARKER')" >/dev/null
}

marker_rows_in() {
  local ns="$1" table="$2" pod="${3:-$POSTGRES_POD}"
  psql_on "$ns" "$pod" "SELECT count(*) FROM $table WHERE marker = '$MARKER'"
}

assert_marker_row() {
  local ns="$1" pod="${2:-$POSTGRES_POD}"
  [[ "$(marker_rows_in "$ns" upgrade_path_marker "$pod")" == "1" ]] \
    || fail "the marker row is missing in $ns"
}

assert_server_major() {
  local ns="$1" major="$2" version
  version="$(psql_in "$ns" "SHOW server_version_num")"
  [[ "$version" == "$major"* ]] || fail "the database in $ns runs $version, expected Postgres $major"
}

secret_fingerprint() {
  local ns="$1"
  kubectl -n "$ns" get secret "$DB_SECRET" \
    -o jsonpath='{.data}' | sha256sum
}

volume_file() {
  local ns="$1" path="$2"
  in_postgres "$ns" cat "$MOUNT/$path"
}

volume_entries() {
  local ns="$1"
  in_postgres "$ns" ls -1A "$MOUNT"
}

upgrade_log() {
  local ns="$1"
  kubectl -n "$ns" logs "$POSTGRES_POD" -c pg-upgrade
}

assert_upgrade_log_says() {
  local ns="$1" text="$2" log
  # Read whole before searching: grep -q stops at the first match, and under pipefail the writer it
  # cut off would fail the check.
  log="$(upgrade_log "$ns")"
  grep -qF -- "$text" <<<"$log" || { dump_diagnostics "$ns"; fail "the upgrade log in $ns does not say: $text"; }
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
}

# A sorted sha256sum of every file on the database volume, so a refused attempt can be shown to have
# changed nothing.
volume_fingerprint() {
  on_volume "$1" 'cd /volume && find . -type f -print0 | sort -z | xargs -0 -r sha256sum'
}

# Stops the Postgres 17 server process from the kind node, so it can neither shut down cleanly nor be
# restarted in place. The upgrade that follows replaces the pod; the kubelet's SIGTERM never gets an
# answer and the grace period ends in SIGKILL, which is how a busy 0.1.17 pod usually goes down.
freeze_postgres_server() {
  local ns="$1" container node pid
  container="$(kubectl -n "$ns" get pod "$POSTGRES_POD" -o jsonpath='{.status.containerStatuses[?(@.name=="postgres")].containerID}')"
  node="$(kubectl -n "$ns" get pod "$POSTGRES_POD" -o jsonpath='{.spec.nodeName}')"
  pid="$(docker exec "$node" crictl inspect --output go-template --template '{{.info.pid}}' "${container#containerd://}")"
  [[ -n "$pid" ]] || fail "could not find the Postgres process of $POSTGRES_POD"
  docker exec "$node" kill -STOP "$pid"
}

install_with_data() {
  local ns="$1"
  install_before_chart "$ns" "${@:2}"
  wait_api_ready "$ns"
  seed_marker_row "$ns"
}

assert_carried_across() {
  local ns="$1" counts="$2"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$counts"
  assert_marker_row "$ns"
}

readonly UPGRADED_NS="plain-upgrade"
readonly FRESH_NS="fresh-install"

# What a scenario leaves behind for the ones after it: the row counts recorded before each namespace was
# upgraded, the Helm revision of the upgrade, and whether the fresh install exists. A scenario run on its
# own finds none of them and makes its Given itself. The new chart may still carry the old chart's version
# number, so the upgrade's revision is recorded rather than looked up by chart name.
declare -A RECORDED_COUNTS=()
BEFORE_REVISION=""
UPGRADE_REVISION=""
ROLLED_BACK=""
FRESH_INSTALLED=""

given_upgraded() {
  [[ -n "${RECORDED_COUNTS[$UPGRADED_NS]:-}" ]] || upgrade_plain_helm_upgrade_keeps_every_row
}

given_rolled_back() {
  [[ -n "$ROLLED_BACK" ]] || rollback_starts_17_on_pre_upgrade_data
}

given_fresh_install() {
  [[ -n "$FRESH_INSTALLED" ]] || fresh_install_starts_18_without_upgrading
}

pod_field() {
  local ns="$1" path="$2"
  kubectl -n "$ns" get pod "$POSTGRES_POD" -o jsonpath="$path" 2>/dev/null || true
}

pod_uid() {
  pod_field "$1" '{.metadata.uid}'
}

assert_not_ready() {
  local ns="$1" message="$2"
  [[ "$(pod_field "$ns" '{.status.conditions[?(@.type=="Ready")].status}')" != "True" ]] || fail "$message"
}

stop_database() {
  local ns="$1"
  kubectl -n "$ns" scale statefulset "$POSTGRES_STATEFULSET" --replicas=0 >/dev/null
  kubectl -n "$ns" wait --for=delete "pod/$POSTGRES_POD" --timeout=120s >/dev/null 2>&1 || true
}

start_database() {
  kubectl -n "$1" scale statefulset "$POSTGRES_STATEFULSET" --replicas=1 >/dev/null
}

postgres_started_at() {
  pod_field "$1" '{.status.containerStatuses[?(@.name=="postgres")].state.running.startedAt}'
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
}

# From the pod being created to its postgres container starting: the time the init containers took.
# Both timestamps are whole seconds.
start_delay_ms() {
  local ns="$1" created started
  created="$(date -d "$(pod_field "$ns" '{.metadata.creationTimestamp}')" +%s)"
  started="$(date -d "$(postgres_started_at "$ns")" +%s)"
  echo $(((started - created) * 1000))
}

assert_data_directory() {
  local ns="$1" directory="$2" actual
  actual="$(psql_in "$ns" "SHOW data_directory")"
  [[ "$actual" == "$MOUNT/$directory" ]] || fail "the database in $ns runs on $actual, expected $MOUNT/$directory"
}

assert_upgrade_note() {
  local ns="$1"
  volume_file "$ns" pgdata-18/.lighthouse-upgrade | grep -qx "source_major=17" \
    || fail "pgdata-18 in $ns carries no note saying it was upgraded from Postgres 17"
}

current_revision() {
  local ns="$1"
  helm history "$RELEASE" -n "$ns" -o json | jq -r '.[-1].revision'
}

apply_rendered() {
  local ns="$1" chart="$2"
  helm template "$RELEASE" "$chart" -n "$ns" "${VALUES[@]}" | kubectl -n "$ns" apply -f - >/dev/null
}

# Starts Postgres 17 on a copy of pgdata taken from the volume, mounted read-only, so opening the kept
# data cannot change a byte of it: any start of Postgres 17 on the volume itself rewrites pg_control.
open_kept_copy() {
  local ns="$1" script overrides deadline=$((SECONDS + 300))
  script='cp -a /volume/pgdata /copy/pgdata && chown -R postgres:postgres /copy/pgdata && chmod 0700 /copy/pgdata && exec gosu postgres postgres -D /copy/pgdata'
  overrides="$(jq -cn --arg script "$script" --arg claim "data-$POSTGRES_POD" '{spec: {
    containers: [{name: "postgres", image: "postgres:17-trixie", command: ["bash", "-c", $script],
      volumeMounts: [{name: "data", mountPath: "/volume", readOnly: true}, {name: "copy", mountPath: "/copy"}]}],
    volumes: [{name: "data", persistentVolumeClaim: {claimName: $claim, readOnly: true}}, {name: "copy", emptyDir: {}}]}}')"
  kubectl -n "$ns" run kept-copy --restart=Never --image=postgres:17-trixie --overrides="$overrides" >/dev/null
  until kubectl -n "$ns" exec kept-copy -c postgres -- pg_isready -q >/dev/null 2>&1; do
    if [[ $SECONDS -ge $deadline || "$(kubectl -n "$ns" get pod kept-copy -o jsonpath='{.status.phase}')" == "Failed" ]]; then
      kubectl -n "$ns" logs kept-copy >&2 || true
      fail "Postgres 17 did not open the kept copy in $ns"
    fi
    sleep 2
  done
}

close_kept_copy() {
  kubectl -n "$1" delete pod kept-copy --wait >/dev/null
}

# --- group: happy (slice 01), in the order the scenarios chain ----------------------------------------

fresh_install_starts_18_without_upgrading() {
  local ns="$FRESH_NS"
  install_new_chart "$ns"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  wait_api_ready "$ns"
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  [[ "$(volume_entries "$ns")" == "pgdata" ]] \
    || fail "the volume in $ns holds more than one copy: $(volume_entries "$ns" | tr '\n' ' ')"
  FRESH_INSTALLED=1
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

  assert_upgrade_log_says "$ns" "nothing to upgrade"
  delay="$(start_delay_ms "$ns")"
  echo "  start delay: ${delay} ms after the upgrade, ${baseline} ms on a fresh install"
  [[ $((delay - baseline)) -lt 5000 ]] \
    || fail "the database in $ns started ${delay} ms after its pod was created, ${baseline} ms on a fresh install"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  assert_row_counts "$ns" "${RECORDED_COUNTS[$ns]}"
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
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  [[ "$(marker_rows_in "$ns" upgrade_path_written_on_18)" == "1" ]] \
    || fail "the row written on Postgres 18 in $ns is gone after the rollback"
  wait_api_ready "$ns"
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
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  assert_server_major "$ns" 18
}

table_count() {
  local ns="$1" table="$2"
  psql_in "$ns" "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '$table'"
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
    fail "the warning in $ns does not say what is missing and what removing the pin does: $warning"
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
}

# --reuse-values keeps the 0.1.17 image value, so the database stays on 17 and only the notes speak up.
reuse_values_stays_on_17_and_says_so() {
  local ns="reuse-values" output line
  install_with_data "$ns"

  output="$(helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" --reuse-values)"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 17
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  wait_api_ready "$ns"
  assert_marker_row "$ns"
  line="$(the_one_line_with "behind" "$output")" \
    || fail "the upgrade output in $ns does not carry exactly one line saying the database is behind"
  echo "  $line"
  grep -qF -- "--reset-then-reuse-values" <<<"$line" \
    || fail "the behind line in $ns does not say how to move the database: $line"
}

upgrade_with_reset_then_reuse_values_keeps_every_row() {
  local ns="reset-then-reuse" counts
  install_with_data "$ns"
  counts="$(public_row_counts "$ns")"

  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" --reset-then-reuse-values >/dev/null

  assert_carried_across "$ns" "$counts"
  assert_upgrade_note "$ns"
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
}

# --- group: refusals (slices 01 and 02) ---------------------------------------------------------------

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
}

# A volume whose size is enforced and can be changed in place: a tmpfs on the kind node, offered through a
# PV that is bound to the StatefulSet's claim before the claim exists. The claim asks for more than the
# tmpfs holds; a hostPath PV's capacity is only a label, so the tmpfs size is what the database sees.
readonly SMALL_NS="small-volume"
readonly SMALL_CLASS="lh-small"
readonly SMALL_PV="lh-small-$SMALL_NS"
readonly SMALL_DIR="/mnt/lh-small/$SMALL_NS"
readonly SMALL_VALUES=(--set "postgresql.persistence.storageClass=$SMALL_CLASS")
SMALL_COUNTS=""
SMALL_GROWN_MIB=""

kind_node() {
  kubectl get nodes -o jsonpath='{.items[0].metadata.name}'
}

provide_small_volume() {
  local ns="$1" node
  node="$(kind_node)"
  kubectl delete namespace "$ns" --ignore-not-found --wait >/dev/null
  kubectl delete pv "$SMALL_PV" --ignore-not-found --wait >/dev/null
  docker exec "$node" sh -c "if mountpoint -q '$SMALL_DIR'; then umount '$SMALL_DIR'; fi; mkdir -p '$SMALL_DIR' && mount -t tmpfs -o size=1024m tmpfs '$SMALL_DIR'"
  kubectl apply -f - >/dev/null <<EOF
apiVersion: v1
kind: PersistentVolume
metadata:
  name: $SMALL_PV
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
    path: $SMALL_DIR
    type: Directory
EOF
}

resize_small_volume() {
  local mib="$1"
  docker exec "$(kind_node)" mount -o "remount,size=${mib}m" "$SMALL_DIR"
}

mount_size_mib() {
  local ns="$1"
  in_postgres "$ns" df -Pm "$MOUNT" | awk 'NR == 2 { print $2 }'
}

# Sizes the volume by the same measure the upgrade step uses: room for the data and about half a second
# copy, and after growing, room for two second copies.
shrink_to_one_copy() {
  local ns="$1" data_kib used_kib used_mib needed_mib small_mib
  data_kib="$(in_postgres "$ns" du -sk "$MOUNT/pgdata" | cut -f1)"
  used_kib="$(in_postgres "$ns" df -Pk "$MOUNT" | awk 'NR == 2 { print $3 }')"
  used_mib=$(((used_kib + 1023) / 1024))
  needed_mib=$((data_kib * 11 / 10 / 1024 + 64))
  small_mib=$((used_mib + needed_mib / 2))
  SMALL_GROWN_MIB=$((used_mib + needed_mib * 2 + 64))
  resize_small_volume "$small_mib"
  [[ "$(mount_size_mib "$ns")" == "$small_mib" ]] \
    || fail "the database in $ns sees a ${MOUNT} of $(mount_size_mib "$ns") MiB, expected the ${small_mib} MiB tmpfs"
  echo "  volume: ${used_mib} MiB used, a second copy needs about ${needed_mib} MiB; sized to ${small_mib} MiB, grows to ${SMALL_GROWN_MIB} MiB"
}

upgrade_restarts() {
  pod_field "$1" '{.status.initContainerStatuses[?(@.name=="pg-upgrade")].restartCount}'
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
}

assert_line_says() {
  local ns="$1" line="$2" part
  for part in "${@:3}"; do
    grep -qF -- "$part" <<<"$line" || fail "the refusal line in $ns does not say \"$part\": $line"
  done
}

assert_volume_unchanged() {
  local ns="$1" before="$2" after
  after="$(volume_fingerprint "$ns")"
  [[ "$after" == "$before" ]] \
    || fail "the refused attempt changed the volume in $ns: $(diff <(echo "$before") <(echo "$after") | head -20 || true)"
  echo "  volume fingerprint unchanged: $(sha256sum <<<"$after" | cut -c1-16)"
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
  assert_line_says "$ns" "$line" "MiB free" "need " "postgresql.persistence.size" "postgresql.image"
  message="$(kubectl -n "$ns" describe pod "$POSTGRES_POD")"
  grep -qF -- "$line" <<<"$message" || { dump_diagnostics "$ns"; fail "describing the database pod in $ns does not show the refusal line"; }
  assert_volume_unchanged "$ns" "$FINGERPRINT"
  SMALL_COUNTS="$counts"
}

given_refused_for_room() {
  [[ -n "$SMALL_COUNTS" ]] || too_little_room_refuses_and_touches_nothing
}

grown_volume_lets_refused_upgrade_proceed() {
  local ns="$SMALL_NS"
  given_refused_for_room

  resize_small_volume "$SMALL_GROWN_MIB"

  kubectl -n "$ns" wait --for=condition=Ready "pod/$POSTGRES_POD" --timeout="${WAIT_SECONDS}s" >/dev/null \
    || { dump_diagnostics "$ns"; fail "the database in $ns did not become Ready after the volume grew"; }
  assert_server_major "$ns" 18
  assert_data_directory "$ns" pgdata-18
  wait_api_ready "$ns"
  assert_row_counts "$ns" "$SMALL_COUNTS"
  assert_marker_row "$ns"
}

readonly MANUAL_PATH="https://docs.lighthouse.letpeople.work/Installation/kubernetes.html#moving-data-two-or-more-majors-behind-by-hand"
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
}

pinning_to_data_major_starts_without_other_step() {
  local ns="$DATA16_NS" old_uid
  [[ -n "$DATA16_REFUSED" ]] || two_majors_behind_refuses_and_touches_nothing
  old_uid="$(pod_uid "$ns")"

  upgrade_to_new_chart "$ns" --set postgresql.image=postgres:16
  delete_stuck_pod "$ns"

  wait_pod_replaced "$ns" "$old_uid"
  wait_postgres_ready "$ns"
  assert_server_major "$ns" 16
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  assert_marker_row "$ns"
}

# Runs the cleanup command exactly as the Kubernetes docs print it. The docs leave the namespace to the
# reader's context, so the command runs with a copy of the kubeconfig whose context points at $ns.
remove_old_copy_as_documented() {
  local ns="$1" command kubeconfig status=0
  command="$(awk '/^### Removing the old copy/ { section = 1; next }
                  section && /^```sh$/ { block = 1; next }
                  block && /^```$/ { exit }
                  block { print }' "$KUBERNETES_DOCS")"
  [[ "$command" == "kubectl exec "* ]] || fail "no cleanup command found under \"Removing the old copy\" in $KUBERNETES_DOCS"
  kubeconfig="$(mktemp)"
  kubectl config view --raw >"$kubeconfig"
  KUBECONFIG="$kubeconfig" kubectl config set-context --current --namespace="$ns" >/dev/null
  KUBECONFIG="$kubeconfig" bash -c "$command" || status=$?
  rm -f "$kubeconfig"
  [[ $status -eq 0 ]] || fail "the documented cleanup command failed in $ns with code $status"
}

given_old_copy_removed() {
  [[ -n "$CLEANED_COUNTS" ]] || cleanup_then_rollback_refuses_empty_database
}

postgres_restarts() {
  pod_field "$1" '{.status.containerStatuses[?(@.name=="postgres")].restartCount}'
}

cleanup_then_rollback_refuses_empty_database() {
  local ns="$CLEANED_NS" counts before_revision old_uid deadline log
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
  deadline=$((SECONDS + WAIT_SECONDS))
  until [[ "$(postgres_restarts "$ns")" -ge 1 ]] 2>/dev/null; do
    [[ $SECONDS -lt $deadline ]] || { dump_diagnostics "$ns"; fail "Postgres 17 in $ns neither failed nor restarted after the rollback"; }
    sleep 2
  done
  assert_not_ready "$ns" "the database in $ns became Ready after the rollback"
  # The container restarts every few seconds, so its last run's log is read from whichever of the current
  # and previous container still has it.
  deadline=$((SECONDS + 120))
  until grep -qF "exists but is not empty" <<<"${log:-}"; do
    [[ $SECONDS -lt $deadline ]] \
      || { dump_diagnostics "$ns"; fail "the rolled-back database log in $ns does not say the data folder is not empty"; }
    log="$(kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres 2>/dev/null || true; kubectl -n "$ns" logs "$POSTGRES_POD" -c postgres --previous 2>/dev/null || true)"
    sleep 1
  done
  echo "  $(grep -F "exists but is not empty" <<<"$log" | head -1)"
  [[ "$(on_volume "$ns" 'ls -1A /volume/pgdata')" == "UPGRADED-TO-18-see-kubernetes-docs" ]] \
    || fail "the rollback in $ns created something in pgdata"

  old_uid="$(pod_uid "$ns")"
  upgrade_to_new_chart "$ns"
  delete_stuck_pod "$ns"

  wait_pod_replaced "$ns" "$old_uid"
  assert_carried_across "$ns" "$counts"
  CLEANED_COUNTS="$counts"
}

newer_data_without_kept_copy_refuses() {
  local ns="$CLEANED_NS" line
  given_old_copy_removed

  fingerprint_then_upgrade "$ns" --set postgresql.image=postgres:17-trixie

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "refusing to start Postgres 17" "the data is Postgres 18"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
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
}

# Follows the upgrade step's log until pg_upgrade starts copying the data files. It prints a step's name
# only together with its result, so the copy's own line appears once the copy is over; the step before it
# is the last line printed before the copy begins.
wait_copy_started() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS))
  until [[ -n "$(pod_field "$ns" '{.status.initContainerStatuses[?(@.name=="pg-upgrade")].state.running.startedAt}')" ]]; do
    [[ $SECONDS -lt $deadline ]] || { dump_diagnostics "$ns"; fail "the upgrade step in $ns did not start"; }
    sleep 1
  done
  grep -m1 -F -- "$COPY_STARTED" < <(kubectl -n "$ns" logs -f "$POSTGRES_POD" -c pg-upgrade) \
    || { dump_diagnostics "$ns"; fail "the upgrade step in $ns ended without saying it was copying the data"; }
}

interrupted_upgrade_is_redone_from_start() {
  local ns="interrupted-upgrade" counts old_uid log
  install_with_data "$ns"
  seed_bulk_data "$ns"
  counts="$(public_row_counts "$ns")"

  upgrade_to_new_chart "$ns"
  echo "  mid-copy: $(wait_copy_started "$ns")"
  [[ -z "$(postgres_started_at "$ns")" ]] || fail "Postgres started in $ns while the upgrade step was still copying"
  old_uid="$(pod_uid "$ns")"
  # The upgrade step does not stop for SIGTERM, so a plain delete would give the copy its 30 s grace
  # period to finish. One second cuts it off where it is, as a node going down would.
  kubectl -n "$ns" delete pod "$POSTGRES_POD" --grace-period=1 --wait >/dev/null

  wait_pod_replaced "$ns" "$old_uid"
  wait_postgres_ready "$ns"
  log="$(upgrade_log "$ns")"
  echo "  next start: $(grep -F -- "$DISCARDED_PARTIAL" <<<"$log" || true)"
  # Only a copy the delete cut off before its rename is left as pgdata-18.partial. Had the copy finished,
  # this start would find pgdata-18 already done and say so instead.
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
}

foreign_upgrade_source_refuses_and_touches_nothing() {
  local ns="foreign-source" line
  install_with_data "$ns"

  fingerprint_then_upgrade "$ns" --set postgresql.upgrade.image=postgres:17-alpine

  line="$(refusal_line "$ns")"
  echo "  $line"
  assert_line_says "$ns" "$line" "the Postgres 17 programs" "cannot run beside the Postgres 18 image"
  assert_volume_unchanged "$ns" "$FINGERPRINT"
}

# Only scenarios that are implemented, in an order where each one's Given is made by itself or left by
# one before it.
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
)

readonly REFUSALS=(
  upgrade_after_unclean_stop_keeps_every_row
  too_little_room_refuses_and_touches_nothing
  grown_volume_lets_refused_upgrade_proceed
  two_majors_behind_refuses_and_touches_nothing
  pinning_to_data_major_starts_without_other_step
  cleanup_then_rollback_refuses_empty_database
  newer_data_without_kept_copy_refuses
  foreign_upgrade_source_refuses_and_touches_nothing
  interrupted_upgrade_is_redone_from_start
)

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
    *) fail "unknown group $group (happy | refusals)" ;;
  esac
  if [[ $# -gt 0 ]]; then
    scenarios=("$@")
  fi

  preload_images "$cluster"

  local scenario
  for scenario in "${scenarios[@]}"; do
    declare -F "$scenario" >/dev/null || fail "unknown scenario $scenario"
    echo "▶ $scenario"
    "$scenario"
    echo "✓ $scenario"
  done
  return 0
}

main "$@"
