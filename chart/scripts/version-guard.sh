#!/usr/bin/env bash
# Chart publish guard. Runs from publish.sh, locally and in ci_chart.yml's Release-gated publish job.
#
# Asserts chart-version + app-version consistency across every surface, refuses to ship a move of the
# bundled Postgres to a new major unless the chart also carries the step that upgrades the data on
# disk, and refuses to overwrite an already-published chart version (no silent overwrite).
#
# Usage: version-guard.sh [CHART_DIR] [PUBLISHED_INDEX] [PUBLISHED_DIR]
#   CHART_DIR        default: chart
#   PUBLISHED_INDEX  default: docs/charts/index.yaml  (skipped if absent — first publish)
#   PUBLISHED_DIR    default: docs/charts  (its highest lighthouse-*.tgz is the last published chart)
set -euo pipefail
shopt -s nullglob

CHART_DIR="${1:-chart}"
INDEX="${2:-docs/charts/index.yaml}"
PUBLISHED_DIR="${3:-docs/charts}"

fail() { echo "✗ publish guard: $*" >&2; exit 1; }

# A chart's default postgresql.image ($2 = image) or postgresql.upgrade.image ($2 = upgrade), from a
# chart directory or a packaged .tgz.
postgres_default_image() {
  local chart="$1" key="$2"
  helm show values "$chart" | awk -v key="$key" '
    /^postgresql:/ { inside = 1; next }
    inside && /^[^[:space:]#]/ { exit }
    inside && /^  [^[:space:]#]/ { section = $1 }
    inside && key == "image" && /^  image:/ { print $2; exit }
    inside && key == "upgrade" && section == "upgrade:" && /^    image:/ { print $2; exit }' | tr -d '"'
  return $?
}

# The major in an image reference's tag, or empty when the tag carries none. A registry port or a digest
# is not mistaken for the tag.
tag_major() {
  local ref="${1%%@*}"
  local name="${ref##*/}"
  if [[ "$name" == *:* && "${name#*:}" =~ ^([0-9]+) ]]; then
    echo "${BASH_REMATCH[1]}"
  fi
  return $?
}

postgres_default_major() {
  local chart="$1"
  tag_major "$(postgres_default_image "$chart" image)"
  return $?
}

# --- single source of truth: Chart.yaml via helm ----------------------------------------------
chart_meta="$(helm show chart "$CHART_DIR")"
chart_version="$(printf '%s\n' "$chart_meta" | awk '/^version:/    {print $2; exit}' | tr -d '"')"
app_version="$(printf '%s\n'   "$chart_meta" | awk '/^appVersion:/ {print $2; exit}' | tr -d '"')"
[[ -n "$chart_version" ]] || fail "could not read Chart.yaml version"
[[ -n "$app_version" ]]   || fail "could not read Chart.yaml appVersion"

# --- 1. appVersion == values-enterprise.yaml image.tag (the pinned production image) -----------
ent_tag="$(awk '/^image:/{f=1} f&&/^[[:space:]]*tag:/{gsub(/"/,"",$2); print $2; exit}' "$CHART_DIR/values-enterprise.yaml")"
[[ "$ent_tag" == "$app_version" ]] || fail "appVersion ($app_version) != values-enterprise.yaml image.tag ($ent_tag)"

# --- 2. NOTES.txt surfaces the live chart version (templated → agrees by construction) ---------
grep -q '\.Chart\.Version' "$CHART_DIR/templates/NOTES.txt" \
  || fail "NOTES.txt does not surface .Chart.Version (would not agree on the chart version)"

# --- 3. README install snippet pins this exact chart version + appVersion ----------------------
grep -qF "$chart_version" "$CHART_DIR/README.md" \
  || fail "README install snippet does not reference chart version $chart_version"
grep -qF "$app_version" "$CHART_DIR/README.md" \
  || fail "README does not reference appVersion $app_version"

# --- 4. a new bundled Postgres major ships only together with the in-chart upgrade --------------
# A new Postgres major cannot open the previous major's data, so a tenant moved onto it without the
# upgrade step would be left with a database that does not start.
chart_major="$(postgres_default_major "$CHART_DIR")"
[[ -n "$chart_major" ]] \
  || fail "the default postgresql.image in $CHART_DIR ($(postgres_default_image "$CHART_DIR" image)) has no major in its tag — keep a <major>-<os> tag such as postgres:18-trixie, which the upgrade checks read the major from"
published=("$PUBLISHED_DIR"/lighthouse-*.tgz)
if [[ ${#published[@]} -gt 0 ]]; then
  last_published="$(printf '%s\n' "${published[@]}" | sort -V | tail -n 1)"
  published_major="$(postgres_default_major "$last_published")"
  [[ -n "$published_major" ]] || fail "could not read the Postgres major of postgresql.image in $last_published"
  if [[ "$chart_major" != "$published_major" ]]; then
    render="$(helm template l8e "$CHART_DIR" --set postgresql.auth.password=ci \
      --set encryption.key="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")" \
      || fail "could not render $CHART_DIR to look for the Postgres upgrade step"
    grep -qE '^[[:space:]]*(- )?name: pg-upgrade$' <<<"$render" \
      || fail "the bundled Postgres moves from major $published_major (${last_published##*/}) to major $chart_major without the in-chart upgrade (no pg-upgrade init container) — pin postgresql.image to Postgres $published_major or ship the in-chart upgrade first"
    # The in-chart upgrade carries data across one major only, using the previous major's programs.
    [[ $((chart_major - published_major)) -eq 1 ]] \
      || fail "the bundled Postgres moves from major $published_major (${last_published##*/}) to major $chart_major — the in-chart upgrade only carries data across one major, so move postgresql.image to Postgres $((published_major + 1)) first"
    upgrade_image="$(postgres_default_image "$CHART_DIR" upgrade)"
    [[ "$(tag_major "$upgrade_image")" == "$published_major" ]] \
      || fail "the bundled Postgres moves from major $published_major to major $chart_major, but the default postgresql.upgrade.image ($upgrade_image) is not Postgres $published_major — the upgrade needs the major the data was written with"
  fi
fi

# --- 5. no silent overwrite of an already-published version ------------------------------------
if [[ -f "$INDEX" ]] && grep -qE "version:[[:space:]]*${chart_version//./\\.}([^0-9]|$)" "$INDEX"; then
  fail "chart version $chart_version already exists in $INDEX — bump Chart.yaml version before publishing (no silent overwrite)"
fi

echo "✓ publish guard OK — chart $chart_version / app $app_version consistent across Chart.yaml, README, NOTES.txt, values-enterprise.yaml; Postgres major $chart_major ships safely; version not yet published"
