# Slice 02 — Estimated-portion transparency (forward-fed)

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-01b (how much of the backlog is estimated vs counted, recorded forward into the snapshot store)
**Effort estimate**: 1-2 days
**Reference class**: the same `DeliveryMetricSnapshotRecordingHandler` (reacting to `PortfolioForecastsUpdated`) that Slices 3-4 also extend — it populates one more nullable column on the existing `DeliveryMetricSnapshot` rows; no new event, no new hook. The chart half reuses the Slice-1 `DeliveryBurnupChart.tsx`.

## Premise correction (read first)

The original Slice-2 brief assumed the backlog burnup "reads artificially low" when features aren't broken down, and proposed adding an inferred-estimate line *above* the actual-item backlog. **That premise is false.** `WorkItemService.ExtrapolateNotBrokenDownFeatures` (`WorkItemService.cs:329`) unconditionally extrapolates every non-Done zero-child feature into persisted `FeatureWork`, sized from `EstimatedSize` (else the portfolio default), *before* forecasting. The Slice-1 recorder sums `FeatureWork.TotalWorkItems` with no filter, so **the backlog line already includes the inferred estimate.** There is no artificially-low actual-only line; the inferred-inclusive line is the one already drawn.

So this slice is **reframed to transparency**: record, forward, how much of each day's backlog total is the estimated portion (the extrapolated part), and surface the broken-down-vs-estimated split. See feature-delta `## Changed Assumptions` (2026-06-02).

## Goal (one sentence)

Forward-record the **estimated portion** of each day's backlog (`Σ FeatureWork.TotalWorkItems` over features where `IsUsingDefaultFeatureSize`) and plot it as a **dotted "Estimated (not broken down)" line** on the burnup — falling toward zero as features are broken down, disappearing once none remain, and explaining jumps in the Backlog total when estimates are replaced by real items — so a Delivery Forecaster can see how much of the backlog is still a guess.

## Why this is its own forward-recorded series (D11)

Every series in this feature is forward-only (the store has no backfill — see the feature-delta `## Changed Assumptions`). The estimated portion is one more forward-recorded column: a feature's broken-down state on a past date was never persisted, so the estimated portion on a past day cannot be recomputed and must accrue daily from the day this recorder begins — exactly like the backlog/done line in Slice 1 and the forecast/likelihood lines in Slices 3-4. US-01b is its own slice because it is a distinct, valued backlog lens (estimate transparency), not because of any data-source difference.

## IN scope

- **Corrective migration**: rename the Slice-1 column `EstimatedTotalWork` → `EstimatedItemCount` (nullable int) across Sqlite + Postgres via `Create-Migration.ps1`. The column has never held data (Slice 1 always wrote null), so the rename is safe. DTO field `estimatedTotalWork` → `estimatedItemCount`; FE model field likewise.
- **Forward recorder**: the existing `DeliveryMetricSnapshotRecordingHandler` (reacting to `PortfolioForecastsUpdated`, at most once/day) now also populates `EstimatedItemCount` = `Σ FeatureWork.TotalWorkItems` over features where `IsUsingDefaultFeatureSize` (the extrapolated portion). Record `null` when that sum is 0 (no not-broken-down features — nothing is estimated). No new event or hook; one more column on the same handler. Idempotent on date (get-or-create overwrite, not a `=true` sentinel).
- **Extend `DeliveryBurnupChart.tsx`**: plot `estimatedItemCount` **directly** as a dotted "Estimated (not broken down)" line alongside the unchanged Backlog and Done lines (no subtraction); the line gaps (null) on points where `estimatedItemCount` is null or zero, so it falls to zero and disappears once every feature is broken down. A caption names the latest estimated portion, e.g. "{N} of {M} backlog items are estimated", so it is never silent.
- **Empty/disappear handling**: on points whose `estimatedItemCount` is null or zero the dotted line plots a gap, so once every feature is broken down (or before any estimate has accrued) the dotted line disappears and no caption shows — the chart is exactly the Slice-1 Backlog+Done burnup.

## OUT scope (deferred)

- Forecast-over-time stacked chart (Slice 3) and likelihood-trend chart (Slice 4) — separate forward-fed columns on the same store.
- Any retroactive estimated-portion history — out of scope by design (the store is forward-recorded only; the estimated portion on a past date was never persisted — D11). The series starts empty and accrues.
- Fever chart (Slice 5).

## Learning hypothesis

- **Disproves if it fails**: that the broken-down-vs-estimated split is a credible, useful signal. If the estimated portion swings wildly day to day as features are broken down (so the split reads as noise), forecasters won't use it and the framing needs rework (smoothing, suppressing until enough features are sized, or a different cut).
- **Confirms if it succeeds**: that forecasters use the split to judge whether the delivery's apparent size can be trusted yet — a distinct, valued transparency lens.

## Acceptance criteria

- US-01b AC items from `feature-delta.md` apply unchanged (reframed version).
- Integration test (NUnit + EF InMemory): the recording handler, invoked for a `PortfolioForecastsUpdated` event with a delivery containing one not-broken-down (extrapolated) feature, writes that feature's extrapolated size into `EstimatedItemCount`; re-handling the same day is a no-op; a delivery with all features broken down records `null`.
- Read-API (WebApplicationFactory + real EF): a delivery whose snapshots carry only backlog totals returns points where no point carries an `estimatedItemCount`.
- Vitest + RTL: the burnup plots the dotted "Estimated (not broken down)" line + the caption when a point carries an `estimatedItemCount` (and gaps it on a fully-broken-down point); renders only the backlog/done lines (no dotted line, no caption) when none has accrued.
- `pnpm build` clean; `dotnet build` zero warnings; migration applies on a real Sqlite + Postgres provider; SonarCloud gate passes; mutation ≥80% on new code.

## Carpaccio taste tests (re-run for the reframed Slice 2)

- **Vertical (DB→UI)?** PASS — the recording handler writes `EstimatedItemCount` → endpoint surfaces it → chart plots the dotted estimated-items line + caption.
- **Demoable in one session?** PASS — refresh/seed a delivery with a not-broken-down feature, see the dotted "Estimated (not broken down)" line and the "{N} estimated" caption.
- **User-visible value?** PASS — a distinct transparency lens (how much of the backlog is estimated), not plumbing. Its own user-facing story (US-01b), so the slice-composition gate is satisfied with no `@infrastructure`-only content.
- **Independently shippable?** PASS — depends on the Slice-1 store + chart; ships on its own once they exist.
- **Verdict**: **PASS.**

## Dependencies

- **HARD**: Slice 1 merged (the `DeliveryMetricSnapshot` store, the `PortfolioForecastsUpdated` event + `DeliveryMetricSnapshotRecordingHandler`, and the burnup chart this line attaches to).

## Production data requirement

Run the forward recorder against the dev instance against an early-stage delivery with at least one not-broken-down feature for ≥2 days; confirm the dotted estimated-items line + caption appear (and the line falls as features are broken down) and the idempotency guard holds across refreshes. Screenshot in the PR.

## Cross-cutting (DoR item 7)

- **RBAC**: the forward recorder is a server-side background process gated by no user action; the split is a read view through the existing portfolio read path (`useRbac()` gating, `IRbacAdministrationService`). No new write surface for users. No `/my-summary` fetch.
- **Lighthouse-Clients**: N/A — no new endpoint; `estimatedItemCount` is a renamed field on the Slice-1 `metrics-history` response (already version-gated in Slice 1). The rename ships before any client wraps the field, so no client breakage.
- **Website**: N/A — still pre-launch.

## Pre-slice SPIKE

NOT NEEDED. The extrapolation source (`WorkItemService.ExtrapolateNotBrokenDownFeatures` → persisted `FeatureWork` on `IsUsingDefaultFeatureSize` features) and the Slice-1 recorder/handler precedent are both code-verified. This slice adds one column + one chart line to settled machinery.
