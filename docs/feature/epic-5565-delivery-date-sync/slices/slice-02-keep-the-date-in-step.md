# Slice 02 — Keep the date in step with Jira

**Goal**: a bound Delivery's date, name and membership follow Jira on their own, on the refresh cycle
that already exists.

**Story**: US-03. **This is the slice the Epic was written for.**

## IN scope

- Recompute source-bound Deliveries at the existing seam — `PortfolioUpdater.cs:73-79`, immediately
  beside `RecomputeRuleBasedDeliveries`, between the Feature fetch and the forecast run. No new
  background service, no new schedule, no new setting (D9).
- Accept a remote date in the past (D5). The future-date invariant moves off the `Delivery` constructor
  onto the hand-entry path so Manual creation still rejects it.
- Overdue rendering for a bound Delivery whose date has passed.
- Skip archived Deliveries (#5698 pins a closure snapshot; re-syncing one un-pins it — AC-03.7).
- No-op cleanly: a refresh where nothing changed remotely writes nothing.
- Survive a Jira read failure without failing the Portfolio refresh.

## OUT of scope

- The Release having been **deleted**, or having its date **cleared** (D12) — both are slice 03. This
  slice handles "read failed" (transient, keep last values, no flag), not "resolved to nothing".
- A manual "sync now" control.
- Any change to how the target's history is stored — `TargetDateAtSnapshot` already records it (S12).

## Learning hypothesis

**Disproves D9's "no new scheduler"** if refresh-interval latency turns out to be too slow to trust — if
a forecaster still opens Jira to check the date because Lighthouse might be an hour behind, the sync has
not done its job and a manual refresh or a shorter cadence is needed.

**Confirms** that the existing refresh cycle is the right and only place for this, which is what keeps
the Epic from growing an infrastructure tail.

## Acceptance criteria

AC-03.1 through AC-03.8 in `feature-delta.md`. The three that carry the slice:

- A past date from Jira is accepted; hand entry still rejects one.
- An archived Delivery does not re-sync.
- A read failure leaves last-known values and does not fail the refresh.

## Dependencies

Slice 01b. Also #5698 archiving, which the user has committed to shipping before this Epic starts — if
it is not in the deployed build, AC-03.7 is untestable and must be held, not skipped silently.

## Effort

~5 hours.

## Watch

Moving the future-date invariant off the constructor: any factory or copy-constructor that reconstitutes
a `Delivery` from storage must not re-run it. The work-item sync has already been bitten by an
init-only property silently dropped by a copy-constructor — same class of bug, same file layer.
