# ADR-208: A past day is computed by the recorder's own code, with every today-anchor made explicit, and one absence rule for both paths

**Status**: Accepted (DESIGN, 2026-09-22; interaction mode PROPOSE)
**Date**: 2026-09-22
**Feature**: story-6053-reconstruct-over-time-history (ADO User Story #6053)
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

D1 says a reconstructed day is *the value the recorder would have written that day*, and D6 says there
is therefore no reason to mark it as different. SPIKE-01 confirmed the mechanism — four recorded days
reproduced exactly through a shifted-window call — while recording that the probe discriminates weakly
(a near-degenerate distribution, effectively two distinct observations, and **no configuration change
in the covered period**).

D1 and D6 are claims about *sameness*. A design that computes a past day through a second code path
makes them claims to be tested; a design that computes it through the same code path makes them true by
construction. That is the whole of this decision.

Four facts about the shipped recorders, read on 2026-09-22.

*The family sets live in the recorders.* `PercentilesOverTimeRecordingHandler` holds
`CycleTimeHorizons = [30, 60, 90]` and `WorkItemAgeHorizons = [NoHorizon]`.
`ProcessBehaviorRecordingHandler` holds `TeamReaders(team)` (five families) and
`PortfolioReaders(portfolio)` (six — Feature Size is portfolio-only because there is no team-side read
method), plus `LookbackDaysFor`. Slice 03 AC1 requires reconstruction's family sets to match those
"exactly".

*The write policy is latest-write-wins.* Both recorders `GetByPredicate` on the natural key and update
in place if found. That is right for today, whose value changes as the day progresses.

*The percentile path writes zeros for an empty day.* `BuildPercentiles` over an empty list yields four
zeros and `ValueFor(percentiles, n) ?? 0` persists them. The PBC path already refuses its equivalent:
`Status != Ready` → no row; `Average == 0 && Unpl == 0` → no row, with `Lnpl` deliberately excluded
because a busy process clamps to zero.

*Baseline validation is anchored to today.*
`BaselineValidationService.Validate(baselineStart, baselineEnd, DoneItemsCutoffDays, Clock.Today)` runs
on all four PBC builders. `DoneItemsCutoffDays` is likewise a cutoff measured back from today.

## Decision

### 1. One day-writer, two write policies

The per-day computation and the family descriptors move out of the two recording handlers into two
shared writers — one per snapshot table — each offering exactly two operations:

| Operation | Caller | Policy |
|---|---|---|
| record today | the ADR-107 recording handlers | overwrite in place; today's value legitimately changes during the day |
| fill a past day | the ADR-207 filler | **write only if absent**; a day already carrying a row is left exactly as it is |

Two named operations, not one operation with a mode flag. The two policies are two different
statements about time, and a boolean parameter is where a reader stops being able to tell which one a
call site meant.

The family descriptors — the horizon lists, the reader tuples, `LookbackDaysFor` — move with them.
There is then **one list per scope**, so slice 03 AC1's "matches `TeamReaders` / `PortfolioReaders`
exactly" is an assertion about one list rather than an agreement between two.

This makes D1 and D6 structural: a reconstructed value cannot drift from a recorded one by an edit to
one path, because there is one path.

### 2. Every today-anchor becomes an explicit as-of day, defaulting to today

The PBC read path gains an as-of day threaded through to `BaselineValidationService.Validate` and to
the `DoneItemsCutoffDays` cutoff, defaulting to `Clock.Today`. The forward recorder and every
point-in-time widget pass the default and are unchanged on the wire and in behaviour.

Reconstruction passes **D**. Validating day D's baseline against today asks "is this baseline valid
*now*" when the question is "was it valid *then*", and the today-anchored answer is not an
approximation of the right one — it is an answer to a different question.

The failure this prevents is silent and asymmetric, which is why it gets a decision rather than a
comment. An owner with a **pinned** `ProcessBehaviourChartBaselineStartDate`/`EndDate` can validate
against today and not against D. `BaselineInvalid` hits the `Status != Ready` gate, no row is written,
and the whole PBC reconstruction produces nothing while looking exactly like "the stored data did not
support it". An owner with **no** pinned baseline gets `baselineStart = startDate, baselineEnd =
endDate` — the reconstruction window — which is already correct per day, and is the case a happy-path
test will accidentally prove. The second passing is not evidence about the first.

**The pinned-baseline owner's correct series is flat limits, not an empty one.** A pinned baseline does
not move, so every reconstructed day carries the same UNPL/Average/LNPL. That is a true reading of a
fixed baseline and it is what slice 03 AC3 asks the test to state.

### 3. The absence gate applies to both paths, not only to reconstruction

D7 gates the reconstruction path and DISCUSS left the forward recorder to DESIGN. It is gated too, in
the shared writer, so there is one honesty rule.

**Gating only one path would make D6 false.** If the recorder writes `P50=P70=P85=P95=0` for a day the
filler would have refused, then that day's row is not "the value the recorder would have written" — it
is a value only one of the two paths produces. Worse, fill-if-absent means the zero row is permanent:
reconstruction will never replace it. The asymmetry would be a standing, invisible exception to the
decision the whole story rests on.

SPIKE-01 measured one zero-day in 365 on the dev fixture and named the case that fixture cannot show:
a team that closes nothing for a month — ordinary over a holiday — hits the zero-write path repeatedly
and paints exactly the false floor the gate exists to prevent. That team is the one the gate is for.

The predicate is **all four percentiles zero**, matching the PBC gate's shape and its reasoning: a
single zero is a real reading, an all-zero tuple is the absence of a reading wearing a number.

### 4. What this changes for an existing instance, stated rather than discovered

- A forward-recorded all-zero day stops being written. On a low-throughput owner the chart gains gaps
  where it had a floor of zeros. That is the point, and it is a **behaviour change to a shipped
  feature**: it belongs in the release notes and in `docs/metrics/predictability.md`.
- All-zero rows already in the database stay. No data-repair migration: EF migrations here are
  expand-only, and a repair that deletes rows a user may have screenshotted is a separate decision with
  its own evidence. The dev DB's seven all-zero rows belong to an owner in neither `Teams` nor
  `Portfolios` and are already filed as their own defect.
- `DemoPercentilesBackfillHandler` is untouched and stays synthesis for demo connections only. It
  backdates `RecordedAt < today` rows, so fill-if-absent means the filler steps over them rather than
  correcting them — the two never fight, and a demo instance keeps the synthetic Throughput series
  ADR-109's slice-04 amendment describes.

### 5. The read cache is invalidated once, at the end of the pass

A pass reads up to 90 historical windows per family through `ITeamMetricsService` /
`IPortfolioMetricsService`, each of which warms the shared metrics cache under an `(owner, window)` key
no UI will ever ask for. The recorders already carry `finally { invalidateReadCache(); }` for a
narrower reason; the filler carries it too, once per pass rather than per day, in a `finally` so a
failed pass does not leave the cache holding what it warmed.

The accepted cost is that invalidating the owner's metrics also discards the live entries the dashboard
is using, so the next widget read recomputes. On the recorder that lands after a refresh, when those
entries are stale anyway; after a pass it is gratuitous. It is accepted rather than solved because the
alternative — evicting only the keys the pass warmed — depends on a per-key eviction the cache may not
expose, and that is recorded as an open question rather than assumed.

## Alternatives Considered

**Give the recording handlers a second entry point taking a day.** The smallest diff. Rejected: the
handler is an `IDomainEventHandler` whose whole shape is "an event happened, record now", and a second
entry point with a different write policy, a different cache-invalidation reason and a different
failure containment makes one class two things. Extraction gets the same single-code-path guarantee
without that.

**A separate reconstruction service with its own family tables.** Rejected on slice 03 AC1: duplicating
`TeamReaders`/`PortfolioReaders` turns "the sets match exactly" into an agreement between two lists that
a later slice will break in one of them, silently, exactly as ADR-109's slice-02 amendment records
happening to the demo backfill's idempotency guard.

**One writer with a `WritePolicy` enum or an `overwrite: bool`.** Rejected: at the call site
`Write(owner, day, values, true)` says nothing, and the two policies are the one thing a reader of this
code most needs to be able to see.

**Leave the baseline anchored to today and accept the empty series.** Rejected: it is silent, and slice
03 names it as the hazard the slice exists to confront. Its failure mode is indistinguishable from the
honest "no data supported this".

**Refuse PBC reconstruction for pinned-baseline owners**, with copy saying so. Honest and cheap, and
kept as the **named fallback** if threading the as-of day proves to reach further than slice 03's
budget. Rejected as the first choice because it removes the feature from exactly the owners who cared
enough to pin a baseline.

**Change `BaselineValidationService` itself to be day-relative.** Rejected as out of proportion: this
story needs the anchor to be *chooseable*, not to be *different*. Threading a parameter that defaults
to today leaves the service's semantics and every existing caller untouched.

**Gate the zero-write only on reconstruction, as D7's literal text says.** Rejected for the reason in
§3: it would make D6 false for exactly the days the gate is about.

## Consequences

**Positive.** D1 and D6 stop being properties to test for and become properties of there being one
code path. One family list per scope. One honesty rule for both paths. The today-anchor becomes a
parameter with a default, so every existing caller is byte-identical and the one caller that needed a
different answer can ask for it.

**Negative / accepted.**

- **A shipped behaviour changes**: the forward recorder stops writing all-zero percentile rows. Named
  in the release notes and in the metrics docs; not silently shipped.
- **Historic all-zero rows survive**, so a chart can still show a zero floor for days recorded before
  this release. Absence and a stale zero will coexist on one line, and nothing distinguishes them.
- **Four PBC builders gain a parameter.** Additive with a default, but it is four signatures and their
  tests.
- **The metrics cache is invalidated after every pass**, costing the owner's dashboard one recompute.
- **Extraction moves code the mutation suite already covers.** Mutation scores on
  `PercentilesOverTimeRecordingHandler` and `ProcessBehaviorRecordingHandler` (BE 85–90% across epic
  5427) are scores on line ranges that this moves; the run has to be repeated on frozen code after the
  move, not compared against the old numbers.

## Earned Trust

| Assumption | Probe |
|---|---|
| A reconstructed day equals the recorded day | Reconstruct one of the four genuinely recorded days on the restored dev DB and assert equality against the persisted row. SPIKE-01 did this read-only; it becomes a standing test. Note what it does **not** prove: SPIKE-01 recorded that the distribution is near-degenerate and three of the four days share a tuple. |
| Fidelity survives a configuration change | **No probe exists, and this is the one open half of D6.** Nothing on the available instance changed configuration in the covered period, and reconstruction runs against today's state mappings, cycle-time definitions and item set. Carried as a named risk with a docs note, not closed. |
| There is genuinely one code path | ArchUnit-style test: no type outside the shared writer holds a horizon list or a PBC reader tuple. A second list appearing is the failure this guards. |
| Fill-if-absent never overwrites | Seed a recorded day with a distinctive value, run a pass over a window containing it, assert the row is byte-identical. |
| The absence gate fires on both paths | Drive the recorder and the filler over an owner with no closed items in the window and assert no row from either. Asserting only the filler passes against the asymmetry that breaks D6. |
| A pinned-baseline owner gets flat limits, not an empty series | Explicit test with a pinned baseline whose window predates the reconstruction range, asserting a row per day and identical limits. The negative half matters more: assert the series is **not** empty. |
| An owner with no pinned baseline gets moving limits | Same window, no pin; assert the limits differ across days. Without this the previous test passes against an implementation that always returns the same triple. |
| The as-of day actually reaches the validator | Reconstruct a day for which today's anchor and D's anchor disagree, and assert the row exists. A test where both anchors agree cannot fail. |
| WIA reconstructs as-of D, not as-of today | An item started 2026-08-01 and closed 2026-09-10, reconstructed for 2026-09-01, contributes its age on 2026-09-01 — not zero and not its age today. |
| The cache is invalidated on the failure path too | Force a pass to throw mid-family and assert invalidation still ran. The recorders already pin this on both paths; the filler inherits the requirement, not the test. |

## Cross-reference

- [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) — the `NoHorizon = 0` sentinel and
  the append-only `MetricType` ordinal, both of which a reconstructed row must honour for the same
  mechanical reasons the recorder does.
- [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) — the recorders this
  extracts from. Their trigger, placement, per-family failure containment and cache guard are unchanged.
- [ADR-109](./adr-109-demo-percentiles-backfill-handler.md) — the demo synthesis, which stays synthesis
  and stays demo-only; amended there to say what reconstruction is not.
- [ADR-207](./adr-207-read-triggered-reconstruction-on-its-own-filler.md) — who calls the fill-a-past-day
  operation, and when.
