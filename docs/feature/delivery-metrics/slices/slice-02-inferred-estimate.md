# Slice 02 — Inferred-estimate enrichment (forward-fed)

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-01b (estimated size for not-yet-broken-down features, recorded forward into the snapshot store)
**Effort estimate**: 1-2 days
**Reference class**: the same `DeliveryMetricSnapshotRecordingHandler` (reacting to `PortfolioForecastsUpdated`) that Slices 3-4 also extend — it populates one more nullable column on the existing `DeliveryMetricSnapshot` rows; no new event, no new hook. The chart half reuses the Slice-1 `DeliveryBurnupChart.tsx`.

## Goal (one sentence)

Add the "total backlog INCLUDING the inferred size of not-yet-broken-down features" line to the burnup — recorded FORWARD into the `DeliveryMetricSnapshot` store on each refresh — so a Delivery Forecaster tracking an early-stage delivery sees an honest projected total rather than a backlog line that reads artificially low when features aren't broken down yet.

## Why this is its own forward-recorded series (D11)

Every series in this feature is forward-only (the store has no backfill — see the feature-delta `## Changed Assumptions`). The inferred estimate is one more forward-recorded column: a feature with zero children contributes its configured size to the inferred-estimate line, accruing daily from the day this recorder begins — exactly like the backlog/done line in Slice 1 and the forecast/likelihood lines in Slices 3-4. US-01b is its own slice because it is a distinct, valued backlog lens, not because of any data-source difference.

## IN scope

- Forward recorder: the existing `DeliveryMetricSnapshotRecordingHandler` (reacting to `PortfolioForecastsUpdated`, at most once/day) now also populates the inferred-estimate column on that day's `DeliveryMetricSnapshot` row — a feature with zero child work items contributes its configured default/estimated size; broken-down features contribute their real item count. No new event or hook; one more column on the same handler. Idempotent on date (guard on `(deliveryId, recordedAt)`, not a `=true` sentinel).
- Extend `DeliveryBurnupChart.tsx`: draw the "total backlog including inferred estimate" line alongside the actual-item backlog line, annotated "{N} of backlog is estimated (features not yet broken down)" so the inferred portion is never silently inflated.
- Empty/sparse handling: before any forward snapshot carries an inferred estimate, the chart shows only the actual-item backlog line (the Slice-1 behavior) with the honest "estimated total builds forward from today" note; the line accrues as snapshots accumulate.
- When all features are broken down, no estimated portion shows and the line collapses onto the actual-item backlog.

## OUT scope (deferred)

- Forecast-over-time stacked chart (Slice 3) and likelihood-trend chart (Slice 4) — separate forward-fed columns on the same store.
- Any retroactive inferred-estimate history — out of scope by design (the store is forward-recorded only; the inferred size on a past date was never persisted — D11). The line starts empty and accrues.
- Fever chart (Slice 5).

## Learning hypothesis

- **Disproves if it fails**: that the inferred estimate is stable/credible enough to display next to the actual backlog. If the inferred total jumps around as features are broken down (so the line is noisy or reads as untrustworthy), forecasters won't rely on it and the framing needs rework (smoothing, a clearer "estimated vs counted" split, or suppressing it until enough features are sized).
- **Confirms if it succeeds**: that forecasters trust the projected total — the inferred-estimate line is a distinct, valued lens that keeps an early-stage delivery from reading artificially small.

## Acceptance criteria

- US-01b AC items from `feature-delta.md` apply unchanged.
- Integration test (NUnit + EF InMemory): the recording handler, invoked for a `PortfolioForecastsUpdated` event with a delivery of one not-broken-down feature, writes that feature's configured estimated size into the inferred-estimate column of the day's snapshot row; re-handling the same day is a no-op; a delivery with all features broken down records no estimated portion.
- Vitest + RTL: the burnup renders the inferred-estimate line with the estimated-portion annotation when a forward snapshot carries one; renders only the actual-item line (with the forward-only note) when none has accrued yet.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud gate passes; mutation ≥80% on new code.

## Carpaccio taste tests (re-run for the new Slice 2)

- **Vertical (DB→UI)?** PASS — the recording handler writes the column → endpoint surfaces it → chart draws the inferred line.
- **Demoable in one session?** PASS — refresh a delivery with a not-broken-down feature, see the estimated line + annotation appear.
- **User-visible value?** PASS — a distinct backlog lens (projected total), not plumbing. This slice has its own user-facing story (US-01b), so the slice-composition gate is satisfied with no `@infrastructure`-only content.
- **Independently shippable?** PASS — depends on the Slice-1 store + chart; ships on its own once they exist.
- **Verdict**: **PASS.**

## Dependencies

- **HARD**: Slice 1 merged (the `DeliveryMetricSnapshot` store, the `PortfolioForecastsUpdated` event + `DeliveryMetricSnapshotRecordingHandler`, and the burnup chart this line attaches to).

## Production data requirement

Run the forward recorder against the dev instance against an early-stage delivery with at least one not-broken-down feature for ≥2 days; confirm the inferred-estimate line and annotation appear and the idempotency guard holds across refreshes. Screenshot in the PR.

## Cross-cutting (DoR item 7)

- **RBAC**: the forward recorder is a server-side background process gated by no user action; the inferred-estimate line is a read view through the existing portfolio read path (`useRbac()` gating, `IRbacAdministrationService`). No new write surface for users. No `/my-summary` fetch.
- **Lighthouse-Clients**: N/A — no new endpoint; the inferred-estimate is an extra field on the Slice-1 `metrics-history` response (already version-gated in Slice 1). No new client wrapper.
- **Website**: N/A — still pre-launch.

## Pre-slice SPIKE

OPTIONAL (~1h): confirm the configured default/estimated feature-size source. The recorder trigger is settled (Slice 1's `PortfolioForecastsUpdated` handler) — this slice only adds a column to it. Skip if the feature-size source + Slice-1 handler precedent is clear.
