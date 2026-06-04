# SPIKE Decisions — delivery-metrics Slice-5 fever chart (US-05)

## Assumption Tested
- Can `(schedule-consumed x, buffer-consumed y)` coords + green/amber/red zones be derived purely
  from the existing `DeliveryMetricsHistory` series (no new server field/endpoint), with an
  on-track trail staying out of red and an at-risk trail entering red, rendered on the
  already-installed MUI-X 9.0.1 (no new charting dependency)?

## Probe Verdict
- **WORKS** — all 5 assertions passed (on-track all-green; at-risk green→amber→red; sparse and
  zero-scope yield no bubble). Derivation model + edge-case guards in `findings.md`.

## Promotion Decision
- **DISCARD → roadmap+DELIVER** (user, 2026-06-04). The fever-chart e2e path is identical to the
  already-shipped Slice-3/4 wiring (fetch `DeliveryMetricsHistory` → pure model fn → MUI chart in
  the Metrics tab), so a standalone `@walking_skeleton` commit adds little. Findings are kept; the
  probe dir is deleted; the first DELIVER step lays `deriveFeverTrail` + the acceptance test as the
  thin e2e slice. Proceed straight to the Slice-5 roadmap + DELIVER.

## Scope Decision — animation
- **ANIMATE THE TRAIL** (user, 2026-06-04). The animated bubble traversal across snapshot dates
  stays IN the committed slice (milestone-5 scenario 1 "the trail animates across snapshot dates").
  Animation is presentational; the load-bearing decision signal is still the zone the latest bubble
  sits in. Roadmap must include the animation behavior and keep its test surface honest (assert the
  date-ordered sequence drives the trail; don't over-assert frame-by-frame MUI internals).

## Design Implications
- Pure `deriveFeverTrail(history): { points: FeverPoint[], empty: boolean }` model fn in
  `src/models/Delivery/` holds all kill-rate-critical logic (deviation, `GREEN_MAX=5`/`AMBER_MAX=20`
  thresholds, `totalWork==0` / `span<=0` / null-first-snapshot guards). No MUI in the model.
- `DeliveryFeverChart.tsx` renders via MUI-X `ScatterChart` with diagonal green/amber/red zone bands
  + a date-ordered animated trail; premium (`canUsePremiumFeatures`) + RBAC (`useRbac`) gated on the
  Metrics tab, unchanged.
- **No backend change** — US-05 is visualization-only over Slice-1/3 data (Clients N/A confirmed).
- Zone thresholds are baseline-default constants; tunability OUT of scope (feature-delta).

## Constraints Discovered
- milestone-5 scenario 1 ("trails through green, amber, **and** red") needs a fixture whose
  trajectory degrades through all three bands — a clean on-track fixture stays all-green and will
  not exercise amber/red. Surface this to the acceptance-designer review.
