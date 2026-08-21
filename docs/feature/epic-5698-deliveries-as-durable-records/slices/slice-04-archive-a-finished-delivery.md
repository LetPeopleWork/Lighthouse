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
- Un-archiving — Slice 05.
- Freezing notes — Slice 05.
- Actual finish date and any calibration read-out (D2).

## Learning hypothesis
**Disproves if it fails**: that the pin can reuse the snapshot shape. The unique key is
`(DeliveryId, RecordedDay)`, so an archive on a day the recorder already ran collides with an existing
row, and an un-archive-then-re-archive on the same day collides with the previous pin. If reuse cannot
survive that key, D1's "reuse, do not denormalise" is wrong and a dedicated closure table is the
answer — which is exactly the choice DESIGN was asked to make, now decided by evidence.
**Confirms if it succeeds**: the archive record costs one flag, one timestamp and one pinned row, and
the whole Epic needs no new encoding of a Feature grid.

## Acceptance criteria
AC-04.1 … AC-04.9 in `../feature-delta.md`.

## Dependencies
Slice 01 (its export is how a pinned record is inspected end to end).

## Reference class
`DeliveryMetricSnapshotRecordingHandler` — the existing writer of exactly this record, once a day.

## Pre-slice SPIKE
Timeboxed to 1 hour, before writing production code: confirm on the dev instance that a pin can be
written for a Delivery that already has a snapshot for today without violating the unique key, and
that the same holds for archive → un-archive → archive within one day. If it cannot, stop and take
the question back to DESIGN rather than working around the key.

## Dogfood moment
Archive a Delivery that finished on the team's own Portfolio, same day.
