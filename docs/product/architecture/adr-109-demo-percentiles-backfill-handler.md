# ADR-109: Demo tenants get a percentile/PBC backfill handler; real tenants stay forward-only

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: epic-5427-percentiles-over-time (ADO Epic 5427)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Recording is **forward-only** by design (DISCUSS D5, US-02 AC3) — the series starts the day recording begins. On a fresh team every over-time chart therefore reads the honest D6 empty-state until days accrue. That is correct for real tenants, but it means the docs/screenshot E2Es (`@screenshot`) and the demo experience would render *empty* over-time charts — no visible trend, and DoD item 7 (populated demo charts) unmet.

The precedent is exact and already shipped: `DemoBlockedHistoryBackfillHandler` backdates `BlockedCountSnapshot` rows for **demo connections only** (gated on `WorkTrackingSystemConnection.IsDemo`), is idempotent (a backdated snapshot means the owner was already backfilled — checked via `GetAllByPredicate`, not `Exists`), and never runs for real tenants.

## Decision

A new `DemoPercentilesBackfillHandler` mirroring `DemoBlockedHistoryBackfillHandler`:

- **Demo-gated**: runs only for owners on a demo `WorkTrackingSystemConnection`; a real (non-demo) connection is a no-op (verified by an AT, per the blocked-history precedent).
- **Backdates** `PercentilesOverTimeSnapshot` (CT 30/60/90 + WIA) and `ProcessBehaviorSnapshot` (per type) rows across a demo window so the over-time widgets show a populated, trending series in demos and screenshots.
- **Idempotent**: an existing backdated snapshot means the owner was already backfilled — skip (no duplicate/rewrite).
- **Not shipped to real tenants**: real tenants remain forward-only (D5); this handler only fabricates demo data.

## Alternatives considered

- **Empty-state only, no backfill**: ship just the D6 empty-state and let demo/screenshot charts render empty until days accrue. Simpler and one fewer handler, but screenshots then show no trend (the whole point of the feature is invisible in docs), and DoD item 7 is unmet. **Rejected** — the blocked-history precedent already solved this cheaply.
- **Backfill real tenants too**: would fabricate historical percentiles that were never actually measured — dishonest, and a direct violation of the forward-only D5 decision. **Rejected.**

## Consequences

- **Positive**: demo and `@screenshot` E2Es show populated over-time charts (DoD 7 met); real tenants keep an honest forward-only series and the D6 empty-state; reuses a proven, tested demo-backfill pattern.
- **Accepted cost**: a demo-only handler that must be kept in step with the two recording handlers' snapshot shapes — bounded, and covered by the same AT discipline as `DemoBlockedHistoryBackfillHandler`.
- **Reuse verdict**: `DemoBlockedHistoryBackfillHandler` → pattern-reuse → **CREATE NEW handler** (different snapshot tables, same demo-gate + idempotency + backdating idiom).
- Cross-refs [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) (tables backfilled), [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) (forward-only recording this complements), [ADR-069](./adr-069-blocked-count-snapshot-and-over-time-endpoint.md) (blocked-history demo-backfill precedent).


## Amendment (slice-02, 2026-07-25) — the idempotency guard is scoped PER METRIC FAMILY

**Status**: Accepted. Refines the "Idempotent" bullet of the Decision; the demo gate, the
real-tenants-forward-only rule and the rejected alternatives are unchanged.

The Decision says: *"an existing backdated snapshot means the owner was already backfilled — skip"*.
Slice-01 implemented that literally — one guard, keyed on "any `PercentilesOverTimeSnapshot` for this
owner with `RecordedAt < today`". Slice-02 had to narrow it to **per metric family**:

```csharp
BackfillFamily(ownerId, ownerType, MetricType.CycleTime,   CycleTimeHorizons,   today);
BackfillFamily(ownerId, ownerType, MetricType.WorkItemAge, WorkItemAgeHorizons, today);
// guard inside BackfillFamily now also filters `snapshot.MetricType == metricType`
```

**Why the whole-owner guard is a trap.** Every environment that had ever run slice-01 already carried
backdated *cycle-time* rows. Under the un-scoped guard the handler would see "already backfilled" and
return before writing a single work-item-age row — permanently, on every future refresh. The failure
mode is silent and asymmetric:

- unit and integration tests seed a **fresh** owner, so they exercise the first-run path and stay
  green;
- only a *pre-existing* demo owner (i.e. every real demo instance, every screenshot environment)
  takes the skip path, where the new tab renders the honest empty state and looks like a UI bug.

**Rule for slices 03/04 and any future family.** A "has this already run?" guard on a backfill that
grows new families over time must be keyed on the **unit that can independently be missing** — here
the metric family — not on the owner. Whenever a backfill handler gains a family, check whether its
idempotency predicate distinguishes that family, and add a regression test that seeds an owner
already backfilled with the *older* family and asserts the *new* family still lands.
`ProcessBehaviorSnapshot` (slices 03/04) is a separate table, so its guard is naturally scoped — but
its own per-`MetricType` rows are not, and will need the same treatment.

Cross-refs [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) (the `MetricType`
discriminator the guard now filters on), [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md)
(the forward-only recorder whose "`RecordedAt < today` ⇒ backfill ran" signal this relies on).

## Amendment (slice-04, 2026-07-26) — the backfill was NOT extended to the five new process-behaviour families

The amendment above closes with a forward statement: "`ProcessBehaviorSnapshot` (slices 03/04) is a
separate table, so its guard is naturally scoped — but its own per-`MetricType` rows are not, and will
need the same treatment." **Slice 04 did not do that, by maintainer decision (2026-07-26).** Recorded
here so the ADR does not read as a description of shipped code that does not exist.

**What shipped.** Slice 04 appended five families to `ProcessBehaviorMetricType` (`WorkItemAge`, `Wip`,
`CycleTime`, `Arrivals`, `FeatureSize`) and made the "PBC Over Time" toggle offer all six.
`DemoPercentilesBackfillHandler` still backdates **`ProcessBehaviorMetricType.Throughput` only**
(`BackfillProcessBehaviorFamily(..., Throughput, ...)`, one call). The per-family guard idiom is
therefore present in the code but exercised by a single family on this table.

**Consequences, accepted rather than hidden.**

1. On a demo instance the PBC-over-time widget plots a dated triple for Throughput and shows the honest
   forward-only empty copy for the other five families, until a day of real recording accrues. This is
   correct behaviour under D6 — it is not a broken chart — but it is not the "populated trending charts"
   this ADR's Decision section promises for demo tenants. The gap is per-family, not per-widget.
2. Milestone-4's outline scenario ("three dated lines are plotted for \<metric_type\>") cannot be driven
   through the browser for any non-Throughput family. Its plotting assertion was relocated to the read
   port (`Slice04ProcessBehaviorMetricTypesScenarios.cs`), and `PbcOverTime.spec.ts` carries an explicit
   comment at the point the UI assertion is *not* made, naming that fixture and forbidding a future "fix"
   that either weakens it or extends the backfill without re-deciding this.
3. Users are told: `docs/metrics/predictability.md`'s empty-state note now states that the demo data
   ships a backdated history for **Throughput only**, so the other families start empty on demo data too.

**If a future slice extends it**, the slice-02 rule still applies and applies harder here: every demo and
screenshot environment is already Throughput-backfilled, so an owner-scoped or table-scoped guard would
make each newly-added family a permanent no-op while unit tests — which seed fresh owners — stay green.
Key the guard on `(owner, metricType)` and add a regression test that seeds an owner already backfilled
with Throughput and asserts the new family still lands.
