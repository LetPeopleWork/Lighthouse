# Slice 03b — Over-time widgets respect the dashboard date range

**STATUS: SHIPPED 2026-07-26** (ADO #5564) — `e1d22be27..ba24a3799` on `main`. Mutation BE 89.86% /
FE 92.76%; adversarial review found 1 MAJOR (the narrowing E2Es could not fail — fixed, then verified
by sabotaging the repository filter and watching them go red). Learning hypothesis **CONFIRMED**: the
shipped read path absorbed the filter without a shared abstraction, and DDD-8's repository-level
`GetSeries` on the PBC family was placement consistency, not a new seam. Three things deliberately not
fixed — the UTC/local URL round-trip (AC6 unverified on reloaded links outside UTC), the typed
inverted-range blank card, and the stale screenshots — see the DELIVER sections in `feature-delta.md`.

**Sequencing**: runs **after slice 03, before slice 04**. Slice 04 (remaining PBC metric types)
touches the same widget surface, so this lands first to avoid re-touching it twice.

**Brief length**: 136 lines, over the nominal ≤100. Deliberate — the excess is the verified
starting-state survey (four traps with `file:line` anchors) that exists precisely so DESIGN and DELIVER
do not re-derive it.

**Goal**: The date pickers on the metrics dashboard actually apply to "Percentiles Over Time" and
"PBC Over Time". Today both widgets take no date parameters anywhere in the read path, so the pickers
have no effect on them — every recorded day is always plotted.

**Stories**: US-06 (value). Origin: user review of the shipped slice-03 widget, 2026-07-26.

## IN scope

- Optional `startDate` / `endDate` query params on `percentiles-over-time` and
  `process-behavior-over-time`, on BOTH `TeamMetricsController` and `PortfolioMetricsController`.
- Threaded through `IPercentilesOverTimeSeriesQuery.GetSeries` / `IProcessBehaviorSeriesQuery.GetSeries`
  down to the snapshot stores, filtering on `RecordedAt` **inclusive at both ends**.
- Params stay **optional** — omitted means full history, so the shipped contract still holds.
- Frontend: range passed through `MetricsService.getPercentilesOverTime` /
  `getProcessBehaviorOverTime`, then `usePercentilesOverTime` / `usePbcOverTime`.
- Both hook caches re-keyed from selection/metric-type to **selection-plus-range**.
- `BaseMetricsView.tsx` passes `ctx.startDate` / `ctx.endDate` into both widget nodes.
- Empty-state disambiguation (see Decision below).
- Docs: `docs/metrics/predictability.md` — the two **Affected by Filtering** rows.

## OUT of scope

Remaining PBC metric types (slice 04). Any change to the recording path — this is a read-path slice
only. Any change to the forward-only semantics.

## Verified starting state (2026-07-26, do not re-derive)

- **No date params anywhere in the read path.** Confirmed at `API/TeamMetricsController.cs:509,525`,
  `API/PortfolioMetricsController.cs:525,541`, both query ports, and
  `IPercentilesOverTimeSnapshotRepository.GetSeries`.
- **The two read paths are NOT symmetric.** Percentiles: controller → query →
  a bespoke `IPercentilesOverTimeSnapshotRepository.GetSeries(...)`. PBC:
  `ProcessBehaviorSeriesQuery.cs:11` has **no repo-level `GetSeries`** — it filters via
  `GetAllByPredicate(...)` and orders in the query class. Decide: extend that predicate, or add a repo
  `GetSeries` for symmetry. Both correct; it is a consistency call, not a correctness one.
- **No perf trap either way.** `RepositoryBase.GetAllByPredicate` returns `IQueryable<T>`
  (`RepositoryBase.cs:61`), so the existing `.OrderBy().ToList()` already executes server-side and a
  date filter added to the predicate stays server-side.
- **Both hook caches are keyed by selection alone** — `usePercentilesOverTime.ts:36`,
  `usePbcOverTime.ts:37`. Adding a range without re-keying serves a stale series. This is the single
  most likely bug in the slice.
- **Widget nodes pass only `ownerId` + `metricsService`** — `BaseMetricsView.tsx:1122-1134`.
  `ctx.startDate` / `ctx.endDate` exist (typed `Date`, `:873-874`) and already feed a dozen sibling
  widgets, so the wiring is a two-line change per widget.
- **React #185 risk is real but lower than it looks.** `ctx.startDate`/`endDate` come from
  `useState<Date>` (`:1202-1211`), so they are already referentially stable across renders. The loop
  only appears if an inline `new Date()` default is introduced. Dep-array discipline still matters
  inside both hooks — they currently list `cache` itself as a dependency.
- **Blast radius:** changing the two `MetricsService` methods changes the `IMetricsService` interface,
  which every test double implements. Slice-03 hit exactly this — expect
  `BaseMetricsView.test.tsx`, `MockApiServiceProvider.ts`, `useMetricsData.test.ts`,
  `TotalWorkItemAgeWidget.test.tsx`.

## Decisions (CONFIRMED in DISCUSS 2026-07-26 as D9 + D10 — do not re-litigate)

- **D9** — optional additive `startDate`/`endDate`, `RecordedAt` inclusive both ends, server-side.
- **D10 empty-state disambiguation** — a range that predates recording returns zero rows, which today
  renders the forward-only copy; that is misleading. The client cannot tell "never recorded" from
  "nothing in this range" without a second unfiltered request, and a discriminator field would turn the
  bare array into an envelope, which **ADR-108 explicitly rejected**. So decide it in the widget from
  the range it asked for. **DESIGN refined the predicate (DDD-13)**: the discriminator is the range's
  *end*, not "narrowed vs default", because the dashboard has no unfiltered state — its default IS a
  30-day (team) / 90-day (portfolio) window. Empty + range ends **before today** → *"no data recorded in
  the selected range"*; empty + range ends **today or later** → the existing forward-only copy. No
  contract change, no extra request, both messages true.
- Watch: closes over `OUT-5427-empty-state-honesty`, and two shipped E2Es assert the forward-only copy
  **verbatim** (`PERCENTILES_OVER_TIME_EMPTY_COPY`, `PBC_OVER_TIME_EMPTY_COPY`) — they run on the
  default range, which ends today, so they keep returning the old string.
- **DESIGN also reversed one out-of-scope line (DDD-12)**: an inverted window (both params present,
  `startDate > endDate`) now returns **400** with the controllers' existing
  `StartDateMustBeBeforeEndDateErrorMessage`, because both controllers already do exactly that in two
  sibling actions and a silently-swapped window would be mislabelled as honest in-range emptiness.

## Learning hypothesis

**Confirms if it succeeds**: the shipped over-time read path is range-agnostic by omission, not by
design — a filter threads through both families without touching recording, the two snapshot tables or
the forward-only contract, and the honest empty state can be resolved client-side without an envelope.
**Disproves if it fails**: that the two asymmetric read paths (bespoke repo `GetSeries` vs
`GetAllByPredicate`) can absorb the same optional filter without a shared abstraction — failure means
the read side needs a common series-query seam before slice 04 adds five more metric types to it.

## Effort + reference class

≤1 day. Reference class: slice-02 (WIA tab) — same two controllers, same query ports, same two hooks,
same four test doubles; that slice landed in 6 steps. This one is smaller (no entity, no migration, no
new widget) but wider (both families at once), so it prices out the same.

## Prioritization

Before slice 04, after slice 03. Slice 04 adds five metric types to the PBC widget's surface; landing
the range plumbing first means slice 04 inherits it instead of the widget being re-opened twice.
Highest-uncertainty item in the slice is the hook cache re-key, which is cheap to disprove early.

## Carpaccio taste tests

| Test | Verdict |
|---|---|
| Ships 4+ new components? | PASS — 0 new components; threads a param through existing ones |
| Every slice depends on a new abstraction? | PASS — no new abstraction; if one is needed, that is the failure signal above |
| Disproves a pre-commitment? | PASS — disproves "the shipped read path can absorb filtering without a shared seam" |
| Synthetic data only? | PASS — E2E drives the seeded demo backfill (`[today-14, today-1]`), not fixtures |
| Duplicate of another slice at different scale? | PASS — no other slice touches the read path's date dimension |

## Tests

- Hook tests: cache key includes the range; changing the range refetches rather than replaying.
- Widget tests: new props; both empty-state variants.
- Backend: controller, query and repository level; inclusive-at-both-ends boundary cases.
- E2E: one per widget — narrow the range, assert fewer plotted days. Demo backfill covers
  `[today-14, today-1]`, so narrowing to ~7 days reliably reduces the point count.

## Docs

`docs/metrics/predictability.md:57` and `:91` — the two **Affected by Filtering** rows, currently
*"No — the date pickers do not apply to this chart; it always plots every recorded day"*. Change to
**Yes** with a note that the chart plots the recorded days inside the selected range. The forward-only
notes at `:81` and `:117` stay true and need no change. The new **Yes** rows must say what the default
window is, since after this slice the charts show the last 30 days (team, per the team's `dateRange`
setting) / 90 days (portfolio) by default instead of all recorded history — a visible default change,
not just a new capability.

## ADR

**ADR-108 amendment, not supersession.** The endpoints stay read-only; the params are additive and
optional, so the shipped contract is unbroken.

## Dependencies

Slice 03 (shipped: `2d6c73690..3377c038b` on `main`).
