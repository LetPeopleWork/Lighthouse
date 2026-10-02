#!/usr/bin/env bash
# SCAFFOLD: true
#
# Kind harness for chart/tests/acceptance/upgrade-bundled-postgres.feature (story 6131). One function per
# @real-io scenario, named after it. Every function still fails with "SCAFFOLD: not yet implemented"; DELIVER
# replaces one body at a time, removes the matching @pending tag from the feature, and wires this script into
# the upgrade-path job of ci_chart.yml with the slice-01 code. Nothing runs it until then.
#
# The same script runs locally and in CI, so the cluster is an argument rather than an assumption.
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

fail() {
  echo "✗ upgrade-path: $*" >&2
  exit 1
}

scaffold() {
  local scenario="$1"
  fail "SCAFFOLD: not yet implemented — $scenario"
}

# --- group: happy (slice 01), in the order the scenarios chain ----------------------------------------

upgrade_plain_helm_upgrade_keeps_every_row() { scaffold "Operator upgrades a Lighthouse with data and finds every row on Postgres 18"; }
kept_copy_holds_pre_upgrade_rows() { scaffold "The previous major's data stays on the volume with exactly the pre-upgrade rows"; }
restart_after_upgrade_does_not_upgrade_again() { scaffold "Restarting the database after an upgrade does not upgrade again"; }
rollback_between_new_chart_revisions_changes_nothing() { scaffold "Rolling back between two releases of the new chart leaves the database as it is"; }
rollback_starts_17_on_pre_upgrade_data() { scaffold "Rolling back after an upgrade starts Postgres 17 on the pre-upgrade data"; }
upgrade_again_after_rollback_starts_afresh() { scaffold "Upgrading again after a rollback starts afresh from the Postgres 17 data"; }
reuse_values_stays_on_17_and_says_so() { scaffold "Reusing the previous values keeps the database on 17 and says how to move it"; }
upgrade_with_reset_then_reuse_values_keeps_every_row() { scaffold "The other ways of applying the new chart: helm upgrade --reset-then-reuse-values"; }
upgrade_by_rendered_manifests_keeps_every_row() { scaffold "The other ways of applying the new chart: rendering the chart and applying the result"; }
fresh_install_starts_18_without_upgrading() { scaffold "A fresh install starts an empty Postgres 18 database with no upgrade step"; }

# --- group: refusals (slices 01 and 02) ---------------------------------------------------------------

upgrade_after_unclean_stop_keeps_every_row() { scaffold "An old database that was stopped abruptly is still carried across"; }
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
