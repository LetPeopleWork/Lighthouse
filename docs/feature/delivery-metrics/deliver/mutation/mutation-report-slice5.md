# Mutation Report — Slice 5 (US-05 Fever Chart, STRETCH)

Feature: delivery-metrics (Epic 3993) · Slice 05-fever-chart-stretch · 2026-06-04
Strategy: `per-feature` (CLAUDE.md) · Gate: ≥80% kill on new code.

## Result — PASS (frontend-only slice)

| Stack | Config | Score | Killed | Survived | Verdict |
|---|---|---|---|---|---|
| Frontend | `stryker.config.delivery-metrics-slice5.mjs` | **88.81%** | 119 | 15 | PASS — survivors are equivalents or MUI-presentational |

Backend: **N/A** — Slice 5 is visualization-only. No server code changed (the fever
trail is derived entirely client-side from the `DeliveryMetricsHistory` series shipped
since Slice 1).

## Scope

Mutated all three new files whole (not logic-region-scoped — unlike the Slice-4 chart —
so the presentational survivors are explicit and individually justified rather than hidden
by line-range exclusion):

- `src/models/Delivery/FeverTrail.ts` — `deriveFeverTrail`, `zoneFor`, `clamp`, `toFeverPoint`.
- `src/components/Common/Charts/useFeverTrailAnimation.ts` — the date-ordered reveal hook.
- `src/components/Common/Charts/DeliveryFeverChart.tsx` — the chart component.

Tests: `FeverTrail.test.ts` (11), `DeliveryFeverChart.test.tsx` (17 — chart + animation hook).

### Per-file

| File | Score | Killed | Survived |
|---|---|---|---|
| `FeverTrail.ts` | **98.15%** | 53 | 1 (equivalent) |
| `useFeverTrailAnimation.ts` | **89.47%** | 17 | 2 (equivalent) |
| `DeliveryFeverChart.tsx` | 80.33% | 49 | 12 (presentational) |

## Hardening pass

The first run scored 86.57% with three genuine *logic* survivors. A targeted pass killed all three:

1. **Green-deviation boundary** (`FeverTrail.ts:27` `deviation <= GREEN_MAX` → `<`). Killed by a
   fixture sitting exactly on the threshold (`deviation === 5.0`, IEEE754-exact: schedule 25%,
   remaining 80% from 16/20) asserting the point is green. The amber boundary was already killed
   by the degrading fixture.
2. **`span <= 0` boundary** (`FeverTrail.ts:45` `<=` → `<`). Killed by a fixture where the delivery
   date equals the first snapshot (`span === 0`), asserting an empty trail — the prior test used a
   delivery date strictly before the first snapshot, which left the `< 0` mutant alive.
3. **Animation dependency** (`useFeverTrailAnimation.ts:28` `[pointCount]` → `[]`). Killed by
   re-rendering the hook with a changed point count and asserting the reveal restarts — the React
   #185 dependency-correctness the hook exists to get right.

## Survivors (15) — all justified

### Equivalent (3) — cannot be killed by any behavioural test

1. `FeverTrail.ts:40` — `!firstSnapshotDate || history.points.length === 0` → `… || false`. Removing
   the empty-points short-circuit is **equivalent**: the downstream `points.filter(...).length === 0`
   path produces the same empty trail (`{ points: [], empty: true }`) for an empty input, so the
   observable result is identical.
2. `useFeverTrailAnimation.ts:6` — the lazy `useState(() => Math.min(pointCount, 1))` initializer
   arrow. **Equivalent**: the mount effect runs synchronously and overwrites `visibleCount` before
   any render is observed, so the initial value is never visible.
3. `useFeverTrailAnimation.ts:7` — the `Math.min(pointCount, 1)` inside that same initializer. Same
   reasoning — overwritten on mount, unobservable.

### Presentational (12) — MUI styling/labels a mocked `ScatterChart` cannot assert

All in `DeliveryFeverChart.tsx`, none altering behaviour:

- `70:10` "Latest" series label string; `81:10` default `"Delivery Fever"` title string.
- `73:26` the latest-datum `id` arithmetic (`points.length - 1`) — a React key, not asserted output.
- `89:14`, `92:61`, `105:8`, `108:9`, `117:60` — `sx` style `ObjectLiteral`s (padding, `borderRadius`,
  `height`, `flexDirection`, caption margin).
- `105:41`, `108:19`, `108:36`, `108:59` — layout/style `StringLiteral`s inside those `sx` objects.

The chart's *behaviour* (zone-partitioned series, per-bubble zone colour, axis bounds, latest-bubble
emphasis, empty-state branch, no-empty-series guard, animated reveal count) is fully killed; the RTL
test mocks the chart primitive and asserts the mapped series objects, so MUI `sx`/label mutants are
out of reach by construction — consistent with the Slice-1/2/4 precedent.

## Configs

- `Lighthouse.Frontend/stryker.config.delivery-metrics-slice5.mjs`
- `Lighthouse.Frontend/vitest.stryker.delivery-metrics-slice5.config.ts`
