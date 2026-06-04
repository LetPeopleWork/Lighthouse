# SPIKE Findings — delivery-metrics Slice-5 fever chart (US-05)

**Date**: 2026-06-04 · **Timebox**: ~half day (probe phase ≤1h) · **Probe**: `/tmp/spike_delivery_metrics_fever/probe.mjs` (throwaway)

## Assumption tested

Can we derive `(schedule-consumed x, buffer-consumed y)` coordinates **and** a green/amber/red
zone classification **purely from the existing `DeliveryMetricsHistory` series** (no new server
field, no new endpoint), such that an on-track fixture's trail stays out of red and an at-risk
fixture's trail enters red — and is the trail renderable with the already-installed MUI-X 9.0.1
(no new charting dependency)?

## Verdict: **WORKS**

All five probe assertions passed:

| Assertion | Result |
|---|---|
| on-track stays out of RED | PASS |
| on-track reaches GREEN by the end | PASS |
| at-risk enters RED | PASS |
| sparse (no `firstSnapshotDate` / no points) ⇒ empty, no bubble | PASS |
| zero-scope (`totalWork == 0`) ⇒ empty, no divide-by-zero bubble | PASS |

Observed trails:
- **On-track**: `green → green → green → green → green → green`
- **At-risk**: `green → amber → red → red → red → red`

## Derivation model (validated)

For each snapshot point (forward-only store; the trail begins at `firstSnapshotDate`):

- `scheduleConsumed% = clamp((point.date − firstSnapshotDate) / (deliveryDate − firstSnapshotDate), 0, 1) × 100`  → **x**
- `remaining% = remainingWork / totalWork × 100`  → **y** (work still to burn = "buffer remaining")
- `deviation = remaining% − (100 − scheduleConsumed%)` → signed distance behind the on-track diagonal `(0,100)→(100,0)`
- Zone: `deviation ≤ 5 ⇒ green`; `≤ 20 ⇒ amber`; else `red` (`GREEN_MAX=5`, `AMBER_MAX=20` percentage-points — **baseline defaults, tunability OUT of scope** per feature-delta).

The zones are diagonal bands around the on-track diagonal, which is the faithful TameFlow/CCPM
fever-chart idiom (top-left = early-but-lots-remaining = red; bottom-right = late-and-nearly-done = green).

## Edge cases discovered

- **Guard `totalWork == 0`** before dividing — yields empty trail (no bubble), same surface as sparse.
- **Guard `span = deliveryDate − firstSnapshotDate ≤ 0`** (delivery date at/before first snapshot) — empty trail.
- **`firstSnapshotDate == null` or `points == []`** — empty trail → empty-state message (US-05 AC3 / milestone-5 scenario 3).
- **The all-green on-track fixture never touches amber.** The milestone-5 scenario *"trails through green, amber, **and** red"* must use a fixture that crosses all three bands (the at-risk-style green→amber→red trail does). DISTILL/roadmap note: the acceptance fixture for scenario 1 needs a trajectory that degrades through all three zones, not a clean on-track one.

## Design implications (for the Slice-5 roadmap / DELIVER)

1. **Pure model fn `deriveFeverTrail(history): { points: FeverPoint[], empty: boolean }`** in `src/models/Delivery/` — fully unit-testable, no MUI. This is where the kill-rate-critical logic lives (zone thresholds, deviation, guards). Mirrors the Slice-4 `DeliveryMetricsHistory` model split.
2. **Rendering**: MUI-X 9.0.1 `ScatterChart` (confirmed installed) plots the trail as a connected/sequenced series; zone bands as colored background regions. No new dependency. The bubble-per-snapshot is a scatter series; the "trail" is the ordered sequence by date.
3. **No backend change** — US-05 is visualization-only over Slice-1/3 data. Confirms feature-delta's "Lighthouse-Clients: N/A" and "no new contract". Zone thresholds are a frontend constant module (candidate to share with `ragRules` if a fever-status badge ever lands).
4. **Gating** reuses the existing premium (`canUsePremiumFeatures`) + RBAC (`useRbac`) wrappers on the Metrics tab — unchanged.
5. **"Animation"** (the slice's stated largest unknown) is a presentational nicety, not load-bearing for decision value. Recommend de-scoping hard animation to a static date-ordered trail (optionally with a sequence highlight) for the committed slice; the decision signal is the zone the latest bubble sits in, which is fully delivered without animation. Flag at the roadmap gate.

## Promotion recommendation

The mechanism is proven against real data shape with sound edge-case guards. Because the derivation
is small and the e2e path is **identical** to the already-shipped Slice-3/4 chart wiring (fetch
`DeliveryMetricsHistory` → pure model fn → MUI chart in the Metrics tab), a separate walking-skeleton
commit adds little over going straight to the Slice-5 roadmap + DELIVER. **Recommend DISCARD-then-DELIVER**:
keep these findings, skip a standalone skeleton commit, and let the first DELIVER step lay the
`deriveFeverTrail` model + acceptance test as the thin e2e slice. (User decides at the gate.)
