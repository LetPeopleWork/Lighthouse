# Slice 04 — Archive a finished Delivery

**Epic** #5698 · **Story** US-04 · **ADO** #5640 · **Estimate** ≤1 day

## Goal
A finished or cancelled Delivery leaves the active list without being erased, and its record is pinned
at the moment it leaves.

## IN scope
- `IsArchived` plus an archived-on timestamp on `Delivery`; one additive EF migration per supported
  provider via the `CreateMigration` script.
- `POST /api/latest/deliveries/{deliveryId}/archive`, gated `PortfolioWrite`.
- Pinning exactly one closure record at archive time, reusing the `DeliveryMetricSnapshot` shape —
  computed on demand, so it is complete even when the daily recorder has never run for this Delivery.
- The daily snapshot recorder skips archived Deliveries.
- Rule-based re-matching skips archived Deliveries — their Feature set is frozen.
- An Archive action in the Delivery header, with a confirmation that states archiving is reversible
  and is not a delete.
- An Archived section in the Portfolio's Delivery list, collapsed by default, showing name, date and
  the pinned headline numbers.

## OUT of scope
- Rendering the archived Feature grid from the pinned record — Slice 05. This slice shows the pinned
  header only.
- The un-archive *affordance* in the UI — Slice 05. **The endpoint itself moved into this slice**
  (2026-08-22): shipping a way in without a way out leaves anyone who archives by mistake stuck until
  the next release, and the route costs a few lines once `Unarchive()` exists. It is deliberately not
  premium-gated — gating the way in but not the way out is a capability you sell; gating both traps
  people in a state.
- Freezing notes — Slice 05.
- Actual finish date and any calibration read-out (D2).

## Learning hypothesis
**Disproves if it fails**: that archiving can actually stop the machinery. The pin's shape is settled
— it is its own table keyed by DeliveryId, so the collisions this slice was originally written to
probe cannot occur. What is still unproven is whether a Delivery can be frozen while a background
refresh is in flight. `DeliveryRuleService` re-matches Features from a Delivery it loaded before the
archive, so it holds a copy that still looks active. If the concurrency token does not actually stop
that write, or if losing the race surfaces as an exception in the background service rather than a
quiet no-op, then freezing a record needs more than an aggregate guard and the design's central claim
— that an archived Delivery is *unable* to change rather than told not to — is wrong.
**Confirms if it succeeds**: the archive record costs one column, one timestamp and one pinned row,
and the freeze holds against the one writer no controller can see.

## Acceptance criteria
AC-04.1 … AC-04.9 in `../feature-delta.md`.

## Dependencies
Slice 01 (its export is how a pinned record is inspected end to end).

## Reference class
`DeliveryMetricSnapshotRecordingHandler` — the existing writer of exactly this record, once a day.

## Pre-slice SPIKE
Timeboxed to 1 hour, before writing production code: generate the additive migration through the
`CreateMigration` script and apply it to a real SQLite and a real Postgres database that already
carry Delivery rows, confirming the new table and the two new columns land without touching existing
data. The test suite cannot answer this — EF InMemory skips migrations entirely.

The original question — whether a pin can be written for a Delivery that already has a snapshot for
today, and whether archive → un-archive → archive within one day collides — no longer needs asking.
The closure record is its own table keyed by DeliveryId alone, so it never meets the day key that
caused those collisions. Both cases are ordinary upserts.

## Dogfood moment
Archive a Delivery that finished on the team's own Portfolio, same day.
