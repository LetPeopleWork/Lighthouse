#!/usr/bin/env bash
# Create the 4 slice stories under Epic 5427 (Percentiles Over Time).
#
# Prereq (run in THIS session first):
#   az devops login            # paste a PAT with Work Items: Read & Write
#   # org/project defaults are already configured (letpeoplework / Lighthouse)
#
# Run:
#   bash docs/feature/epic-5427-percentiles-over-time/create-ado-stories.sh
#
# Override the work-item type if the project uses Scrum:
#   STORY_TYPE="Product Backlog Item" bash docs/feature/epic-5427-percentiles-over-time/create-ado-stories.sh
#
# Idempotent: a slice whose title already exists as a child of 5427 is skipped.
set -euo pipefail

EPIC=5427
STORY_TYPE="${STORY_TYPE:-User Story}"

command -v az >/dev/null || { echo "az CLI not found"; exit 1; }

# Inherit Area/Iteration from the Epic.
IFS=$'\t' read -r AREA ITER < <(
  az boards work-item show --id "$EPIC" \
    --query '[fields."System.AreaPath", fields."System.IterationPath"]' -o tsv
)
echo "Epic $EPIC  →  type='$STORY_TYPE'  area='$AREA'  iter='$ITER'"

# Existing child titles (to skip duplicates on re-run).
EXISTING="$(
  az boards query --wiql "SELECT [System.Id],[System.Title] FROM WorkItems \
    WHERE [System.Parent] = $EPIC" --query "[].fields.\"System.Title\"" -o tsv 2>/dev/null || true
)"

create() {
  local title="$1" desc="$2"
  if grep -Fxq "$title" <<<"$EXISTING"; then
    echo "skip (exists): $title"; return 0
  fi
  local id
  id=$(az boards work-item create \
        --type "$STORY_TYPE" --title "$title" \
        --area "$AREA" --iteration "$ITER" \
        --description "$desc" \
        --query id -o tsv)
  az boards work-item relation add --id "$id" --relation-type parent --target-id "$EPIC" >/dev/null
  echo "created #$id  $title"
}

# ---------------------------------------------------------------------------
# Slice 01 — US-01 (value) + US-02 (@infrastructure, lands within the slice)
create \
"Cycle Time percentiles over time (widget + 30/60/90 + daily-snapshot pipeline)" \
'<b>Slice 01 · Walking skeleton · US-01 (value) + US-02 (@infrastructure)</b>
<p><i>Before:</i> today&#39;s CT percentiles are four numbers, with no sense of trend.
<i>After:</i> Team &rarr; Metrics &rarr; Predictability &rarr; "Percentiles Over Time" (default CT-30) shows four dated lines (50/70/85/95, red&rarr;green) across the range.
<i>Decision enabled:</i> did last month&#39;s process change actually tighten cycle time, or just move a single number?</p>
<b>US-01 acceptance criteria</b>
<ul>
<li>AC1: combined "Percentiles Over Time" widget in the Predictability category, team + portfolio scope, toggle row [CT-30 | CT-60 | CT-90] (WIA arrives in US-03).</li>
<li>AC2: selecting a CT horizon renders 50/70/85/95 lines, one point per calendar day, red&rarr;green ramp (D7).</li>
<li>AC3: each day&#39;s value is the CT percentile over that horizon as-of-that-day, read from the persisted daily snapshot (not recomputed live for history).</li>
<li>AC4: on a team with no snapshots yet, the honest empty state "builds forward from today &mdash; no snapshots recorded yet" (D6), never a broken axis.</li>
<li>AC5: switching 30&harr;60&harr;90 re-plots from already-persisted horizon series, no backend recompute of history.</li>
</ul>
<b>US-02 acceptance criteria (forward-only recording, @infrastructure)</b>
<ul>
<li>AC1: a metrics-refresh event records CT percentiles for horizons 30/60/90 for the current day.</li>
<li>AC2: re-running the refresh N times the same day overwrites &mdash; exactly one row per (team, metric, horizon, day).</li>
<li>AC3: recording is forward-only &mdash; no historical backfill of real data.</li>
<li>AC4: a handler failure does not break the refresh path and is observable (structured recording-failed log).</li>
<li>AC5: EF migration is expand-only/additive, generated via the CreateMigration script across all providers.</li>
</ul>
<p><i>Refs: ADR-106/107/108/109; feature-delta epic-5427-percentiles-over-time.</i></p>'

# ---------------------------------------------------------------------------
# Slice 02 — US-03 (value)
create \
"Work Item Age percentiles over time (WIA tab)" \
'<b>Slice 02 · US-03 (value)</b>
<p><i>Before:</i> the over-time widget shows only cycle-time horizons; work-item-age trend is missing.
<i>After:</i> click the WIA toggle on "Percentiles Over Time" &rarr; 50/70/85/95 age percentiles trending day by day.
<i>Decision enabled:</i> is in-progress work ageing worse over time even when finished-item cycle time looks stable?</p>
<b>Acceptance criteria</b>
<ul>
<li>AC1: the toggle row becomes [WIA | CT-30 | CT-60 | CT-90]; WIA has no horizon dimension (age is as-of-today).</li>
<li>AC2: the WIA tab renders 50/70/85/95 daily lines from a persisted WIA-percentile snapshot, reusing the US-02 recording pipeline (no second bespoke recorder).</li>
<li>AC3: empty-state ("builds forward from today &mdash; no snapshots recorded yet", D6) and red&rarr;green colouring (D7) behave identically to the CT tabs.</li>
</ul>
<p><i>Refs: ADR-106/107; feature-delta epic-5427-percentiles-over-time.</i></p>'

# ---------------------------------------------------------------------------
# Slice 03 — US-04 (value)
create \
"Throughput PBC natural process limits over time" \
'<b>Slice 03 · US-04 (value)</b>
<p><i>Before:</i> the Throughput PBC shows one current set of limits; you cannot see when the limits moved.
<i>After:</i> Predictability &rarr; "PBC Over Time" (Throughput selected) &rarr; UNPL / Average / LNPL as three dated lines.
<i>Decision enabled:</i> did a process change genuinely shift the limits, or did the chart just catch a single special-cause point?</p>
<b>Acceptance criteria</b>
<ul>
<li>AC1: a separate "PBC Over Time" widget in the Predictability category with a metric-type toggle showing Throughput (further types in US-05).</li>
<li>AC2: renders UNPL, Average, LNPL as three daily lines from a persisted PBC-NPL snapshot, reusing the US-02 recording pipeline.</li>
<li>AC3: NPL styling matches the existing point-in-time PBC widgets (D7); empty state per D6.</li>
</ul>
<p><i>Refs: ADR-106/107/108; feature-delta epic-5427-percentiles-over-time.</i></p>'

# ---------------------------------------------------------------------------
# Slice 04 — US-05 (value)
create \
"PBC over time — remaining metric-type toggles" \
'<b>Slice 04 · US-05 (value)</b>
<p><i>Before:</i> PBC-over-time only covers Throughput.
<i>After:</i> the type toggle switches to WIA / WIP / CT / Arrivals / Feature Size &rarr; each shows its own UNPL/Average/LNPL over time.
<i>Decision enabled:</i> which behaviour&#39;s process limits are actually drifting, across the full PBC set?</p>
<b>Acceptance criteria</b>
<ul>
<li>AC1: the toggle exposes Throughput, WIA, WIP, Cycle Time, Arrivals, and (portfolio-only) Feature Size.</li>
<li>AC2: each type reads its own persisted NPL daily series; Feature Size stays portfolio-only (D8).</li>
<li>AC3: adding a type does not alter the US-04 Throughput behaviour (regression-guarded).</li>
<li>AC4: each type&#39;s empty state shows the honest D6 copy when no snapshots exist, never a broken chart.</li>
</ul>
<p><i>Refs: ADR-106/107/108/109; feature-delta epic-5427-percentiles-over-time.</i></p>'

echo "Done. Verify: az boards query --wiql \"SELECT [System.Id],[System.Title],[System.State] FROM WorkItems WHERE [System.Parent] = $EPIC\" -o table"
