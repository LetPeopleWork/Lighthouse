#!/usr/bin/env bash
# SCAFFOLD: true
#
# Kind harness for chart/tests/acceptance/upgrade-bundled-postgres.feature. One function per @real-io
# scenario, named after it. A function that still fails with "SCAFFOLD: not yet implemented" belongs to a
# scenario that is still @pending in the feature file.
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

scaffold() {
  local scenario="$1"
  fail "SCAFFOLD: not yet implemented — $scenario"
}

# --- shared steps ------------------------------------------------------------------------------------

chart_app_version() {
  helm show chart "$1" | awk '/^appVersion:/ { gsub(/"/, "", $2); print $2; exit }'
}

# Loads whatever the local Docker already holds, so a local run does not pull through the kind node. In
# CI nothing is cached and the node pulls as usual.
preload_images() {
  local cluster="$1" image
  local images=(
    postgres:17
    postgres:17-trixie
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
    docker image inspect "$image" >/dev/null 2>&1 || continue
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
  helm install "$RELEASE" "$BEFORE_CHART" -n "$ns" "${VALUES[@]}" --wait --timeout 10m >/dev/null \
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
  helm upgrade "$RELEASE" "$NEW_CHART" -n "$ns" "${VALUES[@]}" >/dev/null
}

highest_restart_count() {
  local ns="$1"
  kubectl -n "$ns" get pod "$POSTGRES_POD" \
    -o jsonpath='{range .status.initContainerStatuses[*]}{.restartCount}{"\n"}{end}{range .status.containerStatuses[*]}{.restartCount}{"\n"}{end}' \
    2>/dev/null | sort -n | tail -1
}

wait_postgres_ready() {
  local ns="$1" deadline=$((SECONDS + WAIT_SECONDS)) restarts
  until kubectl -n "$ns" rollout status "statefulset/$POSTGRES_STATEFULSET" --timeout=10s >/dev/null 2>&1; do
    restarts="$(highest_restart_count "$ns")"
    if [[ "${restarts:-0}" -ge 3 ]]; then
      dump_diagnostics "$ns"
      fail "the database pod in $ns is crash-looping"
    fi
    if [[ $SECONDS -ge $deadline ]]; then
      dump_diagnostics "$ns"
      fail "the database in $ns did not become Ready within ${WAIT_SECONDS}s"
    fi
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

psql_in() {
  local ns="$1" sql="$2"
  kubectl -n "$ns" exec "$POSTGRES_POD" -c postgres -- \
    psql -U "$DB_USER" -d lighthouse -XAtq -v ON_ERROR_STOP=1 -c "$sql"
}

seed_marker_row() {
  local ns="$1"
  psql_in "$ns" "CREATE TABLE upgrade_path_marker (marker text PRIMARY KEY);
                 INSERT INTO upgrade_path_marker VALUES ('$MARKER');
                 CREATE TABLE upgrade_path_bulk AS SELECT g AS id, md5(g::text) AS payload FROM generate_series(1, 20000) g;" >/dev/null
}

# One "table|rows" line per table of the public schema, sorted, so two recordings compare as text.
public_row_counts() {
  local ns="$1"
  psql_in "$ns" "SELECT table_name || '|' || (xpath('/row/c/text()',
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

assert_marker_row() {
  local ns="$1"
  [[ "$(psql_in "$ns" "SELECT count(*) FROM upgrade_path_marker WHERE marker = '$MARKER'")" == "1" ]] \
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
  kubectl -n "$ns" exec "$POSTGRES_POD" -c postgres -- cat "$MOUNT/$path"
}

volume_entries() {
  local ns="$1"
  kubectl -n "$ns" exec "$POSTGRES_POD" -c postgres -- ls -1A "$MOUNT"
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

# A sorted sha256sum of every file on the database volume, read by a throwaway pod that mounts the claim,
# so a refused attempt can be shown to have changed nothing.
volume_fingerprint() {
  local ns="$1"
  local overrides
  overrides="$(printf '{"spec":{"containers":[{"name":"fingerprint","image":"postgres:18-trixie","command":["bash","-c","cd /volume && find . -type f -print0 | sort -z | xargs -0 -r sha256sum"],"volumeMounts":[{"name":"data","mountPath":"/volume","readOnly":true}]}],"volumes":[{"name":"data","persistentVolumeClaim":{"claimName":"data-%s"}}]}}' "$POSTGRES_POD")"
  kubectl -n "$ns" run volume-fingerprint --rm -i --quiet --restart=Never \
    --image=postgres:18-trixie --overrides="$overrides"
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
  install_before_chart "$ns"
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

# --- group: happy (slice 01), in the order the scenarios chain ----------------------------------------

upgrade_plain_helm_upgrade_keeps_every_row() {
  local ns="plain-upgrade" counts secret
  install_with_data "$ns"
  counts="$(public_row_counts "$ns")"
  secret="$(secret_fingerprint "$ns")"

  upgrade_to_new_chart "$ns"

  assert_carried_across "$ns" "$counts"
  [[ "$(secret_fingerprint "$ns")" == "$secret" ]] || fail "the database Secret in $ns changed"
  [[ "$(volume_file "$ns" pgdata/PG_VERSION)" == "17" ]] || fail "pgdata/PG_VERSION in $ns no longer reads 17"
}

kept_copy_holds_pre_upgrade_rows() { scaffold "The previous major's data stays on the volume with exactly the pre-upgrade rows"; }
restart_after_upgrade_does_not_upgrade_again() { scaffold "Restarting the database after an upgrade does not upgrade again"; }
rollback_between_new_chart_revisions_changes_nothing() { scaffold "Rolling back between two releases of the new chart leaves the database as it is"; }
rollback_starts_17_on_pre_upgrade_data() { scaffold "Rolling back after an upgrade starts Postgres 17 on the pre-upgrade data"; }
upgrade_again_after_rollback_starts_afresh() { scaffold "Upgrading again after a rollback starts afresh from the Postgres 17 data"; }
reuse_values_stays_on_17_and_says_so() { scaffold "Reusing the previous values keeps the database on 17 and says how to move it"; }
upgrade_with_reset_then_reuse_values_keeps_every_row() { scaffold "The other ways of applying the new chart: helm upgrade --reset-then-reuse-values"; }
upgrade_by_rendered_manifests_keeps_every_row() { scaffold "The other ways of applying the new chart: rendering the chart and applying the result"; }

fresh_install_starts_18_without_upgrading() {
  local ns="fresh-install"
  install_new_chart "$ns"

  wait_postgres_ready "$ns"
  assert_server_major "$ns" 18
  wait_api_ready "$ns"
  assert_upgrade_log_says "$ns" "nothing to upgrade"
  [[ "$(volume_entries "$ns")" == "pgdata" ]] \
    || fail "the volume in $ns holds more than one copy: $(volume_entries "$ns" | tr '\n' ' ')"
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

too_little_room_refuses_and_touches_nothing() { scaffold "Too little room for a second copy stops the upgrade before it writes anything"; }
grown_volume_lets_refused_upgrade_proceed() { scaffold "Once the volume has grown, the refused upgrade goes ahead by itself"; }
two_majors_behind_refuses_and_touches_nothing() { scaffold "Data two majors behind is refused, naming both majors and the manual path"; }
pinning_to_data_major_starts_without_other_step() { scaffold "Pinning the image back to the data's major lets the database start with no other step"; }
cleanup_then_rollback_refuses_empty_database() { scaffold "After the old copy is removed, a rollback refuses to start an empty database"; }
newer_data_without_kept_copy_refuses() { scaffold "Data newer than the image, with no kept copy of the image's major, is refused"; }
interrupted_upgrade_is_redone_from_start() { scaffold "An upgrade interrupted part-way is redone from the start and never serves a partial copy"; }
foreign_upgrade_source_refuses_and_touches_nothing() { scaffold "An upgrade-source image whose programs cannot run beside the database image is refused"; }

readonly HAPPY=(
  upgrade_plain_helm_upgrade_keeps_every_row
  kept_copy_holds_pre_upgrade_rows
  restart_after_upgrade_does_not_upgrade_again
  rollback_between_new_chart_revisions_changes_nothing
  rollback_starts_17_on_pre_upgrade_data
  upgrade_again_after_rollback_starts_afresh
  reuse_values_stays_on_17_and_says_so
  upgrade_with_reset_then_reuse_values_keeps_every_row
  upgrade_by_rendered_manifests_keeps_every_row
  fresh_install_starts_18_without_upgrading
)

readonly REFUSALS=(
  upgrade_after_unclean_stop_keeps_every_row
  too_little_room_refuses_and_touches_nothing
  grown_volume_lets_refused_upgrade_proceed
  two_majors_behind_refuses_and_touches_nothing
  pinning_to_data_major_starts_without_other_step
  cleanup_then_rollback_refuses_empty_database
  newer_data_without_kept_copy_refuses
  interrupted_upgrade_is_redone_from_start
  foreign_upgrade_source_refuses_and_touches_nothing
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
