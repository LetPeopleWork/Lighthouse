#!/usr/bin/env bash
# Chart publish — epic-5306 slice-04 / ADR-083.
#
# Guards consistency + no-overwrite, then packages the chart and (re)generates the Helm repo index
# under docs/charts/. The existing pages.yml serves docs/charts/ on the existing GitHub Pages source
# (one Pages source per repo — no gh-pages, no chart-releaser). Commit + push to publish.
#
# Usage: publish.sh [CHART_DIR] [OUT_DIR]
#   CHART_DIR  default: chart
#   OUT_DIR    default: docs/charts
# Env: CHART_REPO_URL  default: https://docs.lighthouse.letpeople.work/charts
set -euo pipefail

CHART_DIR="${1:-chart}"
OUT="${2:-docs/charts}"
REPO_URL="${CHART_REPO_URL:-https://docs.lighthouse.letpeople.work/charts}"
HERE="$(cd "$(dirname "$0")" && pwd)"

"$HERE/version-guard.sh" "$CHART_DIR" "$OUT/index.yaml"

mkdir -p "$OUT"
helm package "$CHART_DIR" -d "$OUT"

if [[ -f "$OUT/index.yaml" ]]; then
  helm repo index "$OUT" --url "$REPO_URL" --merge "$OUT/index.yaml"
else
  helm repo index "$OUT" --url "$REPO_URL"
fi

echo "✓ packaged + indexed into $OUT/ (repo url: $REPO_URL)"
echo "  commit $OUT/ + push; pages.yml publishes it to the existing Pages source."
