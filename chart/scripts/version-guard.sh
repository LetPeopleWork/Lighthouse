#!/usr/bin/env bash
# Chart publish guard — epic-5306 slice-04 / ADR-083.
#
# Asserts chart-version + app-version consistency across every surface and refuses to overwrite an
# already-published chart version (no silent overwrite). Runs locally and in ci_chart.yml's publish job.
#
# Usage: version-guard.sh [CHART_DIR] [PUBLISHED_INDEX]
#   CHART_DIR        default: chart
#   PUBLISHED_INDEX  default: docs/charts/index.yaml  (skipped if absent — first publish)
set -euo pipefail

CHART_DIR="${1:-chart}"
INDEX="${2:-docs/charts/index.yaml}"

fail() { echo "✗ publish guard: $*" >&2; exit 1; }

# --- single source of truth: Chart.yaml via helm ----------------------------------------------
chart_meta="$(helm show chart "$CHART_DIR")"
chart_version="$(printf '%s\n' "$chart_meta" | awk '/^version:/    {print $2; exit}' | tr -d '"')"
app_version="$(printf '%s\n'   "$chart_meta" | awk '/^appVersion:/ {print $2; exit}' | tr -d '"')"
[ -n "$chart_version" ] || fail "could not read Chart.yaml version"
[ -n "$app_version" ]   || fail "could not read Chart.yaml appVersion"

# --- 1. appVersion == values-enterprise.yaml image.tag (the pinned production image) -----------
ent_tag="$(awk '/^image:/{f=1} f&&/^[[:space:]]*tag:/{gsub(/"/,"",$2); print $2; exit}' "$CHART_DIR/values-enterprise.yaml")"
[ "$ent_tag" = "$app_version" ] || fail "appVersion ($app_version) != values-enterprise.yaml image.tag ($ent_tag)"

# --- 2. NOTES.txt surfaces the live chart version (templated → agrees by construction) ---------
grep -q '\.Chart\.Version' "$CHART_DIR/templates/NOTES.txt" \
  || fail "NOTES.txt does not surface .Chart.Version (would not agree on the chart version)"

# --- 3. README install snippet pins this exact chart version + appVersion ----------------------
grep -qF "$chart_version" "$CHART_DIR/README.md" \
  || fail "README install snippet does not reference chart version $chart_version"
grep -qF "$app_version" "$CHART_DIR/README.md" \
  || fail "README does not reference appVersion $app_version"

# --- 4. no silent overwrite of an already-published version ------------------------------------
if [ -f "$INDEX" ] && grep -qE "version:[[:space:]]*${chart_version//./\\.}([^0-9]|$)" "$INDEX"; then
  fail "chart version $chart_version already exists in $INDEX — bump Chart.yaml version before publishing (no silent overwrite)"
fi

echo "✓ publish guard OK — chart $chart_version / app $app_version consistent across Chart.yaml, README, NOTES.txt, values-enterprise.yaml; version not yet published"
