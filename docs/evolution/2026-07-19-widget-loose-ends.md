# Evolution: widget-loose-ends

- **Date finalized**: 2026-07-19
- **ADO**: Story #5508 ("Cleanup Widget lose Ends") and Bug #5521 (the Blocked trend had never rendered on any instance). Not transitioned by this run — left for manual review.
- **Status**: Delivered on `main` (DISCUSS → DESIGN → DISTILL → DELIVER; 14/14 roadmap steps, 10 of them RED→GREEN→COMMIT under DES). Mutation ≥ 80% on the feature surface — backend **83.3%** feature-scoped, frontend **88.2%** feature-scoped (`ragRules` 100%, `workItemAgePercentilesTrend` 98.5%, `blockedTrend` 82.6%).
- **Workspace (history)**: `docs/feature/widget-loose-ends/`
- **Builds on**: [work-item-age-percentiles](./2026-06-09-work-item-age-percentiles.md) (the widget this feature gave a status and a trend), [wait-states-flow-efficiency](./2026-06-05-wait-states-flow-efficiency.md) (the tile this feature made presentational and gave a status), [flow-overview-named-cycle-time](./2026-07-18-flow-overview-named-cycle-time.md) (the RAG/trend chrome conventions reused throughout).

## What shipped

Six reported gaps on the Flow Overview, collected as one feature because they share a page and
a shape: a widget that answers a question slightly differently from the way it is asked.

1. **View Data on Total Throughput and Total Arrivals** — both were rollup-only; both now drill
   through to the items behind the number. A registration gap, not a data gap: no new source or
   fetch was needed.
2. **Unprefixed percentile labels on the aging chart** — the percentile-source toggle above the
   chart already says which population is active; repeating it on every reference line made the
   two sources read differently for no reason.
3. **A Blocked trend that reads from day one** (Bug #5521) — the fetch window was one day short
   of the baseline day on *every* instance for *every* range, so the trend had never once
   rendered since it shipped. Fixed in the two ordered steps the design required: widen the
   fetch, then treat a still-absent baseline as zero rather than as a dash.
4. **Work Item Age as of the selected range end** — every Work-Item-Age surface aged its items
   to *today* even when the range ended months ago. Now answered on the range end via a new
   `WorkItemBase.AgeOnDay`, with the `WorkItemAge` property left byte-for-byte untouched so
   work-tracking write-back semantics could not move.
5. **Status and previous-period trend on Work Item Age Percentiles** — see below; this is the
   slice where the design changed.
6. **Status on Flow Efficiency** — the tile joined the shared `useMetricsData` batch, became
   presentational, and got a RAG footer. Unconfigured wait states now read Act with a "define
   wait states in settings" prompt instead of going quiet.

## The decision worth remembering: a validation gate that actually fired

Slice 05's status rule opened with a human validation gate written into the slice brief before
any code: a named coach picks two historical periods on a real instance they independently judge
to have deteriorated, the rule runs over both, and **if the chip reads green on either, the
decision re-opens** — a status that reassures during a decline is worse than the absent chip it
replaces.

It fired. Run against team 34 with a 90% / 2-day SLE, the period 2026-07-11..17 read **green**
on a period nominated as deteriorated. The root cause was not a threshold that needed nudging:
that period had zero throughput *and* zero WIP, so the deterioration was throughput-shaped, not
age-shaped — and the rule's share-based band was computing a percentage over a population of
one, which is noise wearing a number's clothing.

The rule was replaced rather than tuned. **D6-REVISED** bands on absolute counts against the
SLE's *day value*, discarding the SLE percentile entirely: no SLE → Act; more than one item
older than the value → Act; exactly one older, or any item exactly on it → Observe; otherwise
Sustain, including an empty population. Three properties are deliberate and documented in the
code so nobody "fixes" them: the percentile is discarded (a probability is meaningless at these
WIP sizes), the bands do not scale with WIP (two breaching items read Act at WIP 3 and at WIP
40), and an empty board is green rather than neutral, because the WIP RAG already says it once.

The cost was real: DISTILL scenarios 24–28 became invalid, 29 inverted, 31/31b were superseded,
and design decision D19 (extract a band helper shared with the cycle-time rule) was withdrawn —
the two rules no longer share any band knowledge, so the shared helper would have coupled rules
that had deliberately diverged. Re-authoring acceptance tests inside DELIVER is normally a
boundary violation; here the product owner changed the acceptance criteria, so it was the
correct response, and it is recorded as such in the wave delta.

## The correctness cascade nobody ordered

Slice 03 made Work Item Age as-of-date. Verifying it surfaced **UPSTREAM-7**: the percentiles
went empty on far-back ranges while the WIP-over-time chart showed items in progress for the
same period. Chasing it exposed the same class of bug three more times on the same endpoint —
each one a rule evaluated against *today's* item while answering a question about a past day:

- the item's **state** was today's state, not the state it held on the range end;
- **"was it blocked"** matched on today's tags and state, so an item unblocked since read as
  never blocked — now answered from blocked-transition history, with the live rule retained as
  the only available answer for items that predate blocked capture;
- **staleness** compared an as-of `currentStateEnteredAt` against today, adding every day since
  the range closed, so on a month-old range anything that had sat still read as stale.

None of these were in scope when the feature started. All three are the same mistake, and the
first one was only visible because slice 03 made the population honest.

## Adversarial review and mutation testing

Feature-scoped mutation testing was worth more than the score suggested at first. The initial
frontend run reported 60.8%, but over a scope that included wide ranges of `BaseMetricsView` —
mostly view-level presentation and pre-existing code that happened to sit inside the range.
Narrowed to the logic this feature owns, four real holes appeared, every one a test that passed
for a weaker reason than it claimed:

- the blocked-history fetch asserted its start was `<=` the baseline day, which an epoch-zero
  date also satisfies — the one-day widening, the entire fix for Bug #5521, was unpinned;
- the no-baseline case asserted only "not an arrow", and a zero-vs-zero comparison also reads
  `none`, so the whole branch could have been deleted unnoticed;
- nothing asserted the trend labels, which became the *only* assumed-vs-measured signal when the
  explanatory tooltip was dropped by user decision;
- both singular branches of the status tip were unguarded — "1 Work Items sit" would have shipped.

It also found the Work Item Age trend selector reachable only by rendering `BaseMetricsView`,
where the trend chrome is a **mock** `WidgetShell`: every branch survived. The selector moved to
its own module beside `blockedTrend.ts` and is now tested directly at 98.5%. On the backend,
`GetWorkItemIdsWithBlockedHistory` was mocked at every call site and had no test of its own,
despite deciding which items may fall back to the live blocked rule.

Two backend survivors were accepted as **equivalent mutants**: the `age > 0` guard on the
percentile projection (the population filter already excludes anything not started by the range
end, so no item there can have age 0) and `item.Team ?? team` (every caller passes the owning
team). Killing either means inventing a caller that does not exist.

Review found one duplication worth closing: `FlowEfficiencyWidget.ts` still carried its own copy
of the RAG status union, the Act/Observe/Sustain labels, `toRagStatus` and the chip locators that
slice 04 had extracted into `RagChip.ts`.

## Lessons

- **Write the abort criterion before the code, and mean it.** The D6 gate cost a rule and a
  batch of acceptance tests — and it caught a status that would have reassured a team during a
  decline. A gate that never fires is decoration.
- **A percentage over four items is noise.** Bands that work on a population of hundreds do not
  transfer to a WIP board. Count-based rules read honestly at the sizes the widget actually sees.
- **"As of today" hides in more places than you expect.** One as-of fix exposed three more on the
  same endpoint. When a population becomes date-correct, every rule evaluated over it needs the
  same audit — state, blocked, staleness, and anything else that reads "current".
- **Mock chrome makes trend tests vacuous.** `widget-trend-direction-*` exists only on the mock
  `WidgetShell`; the real one emits `widget-trend-${key}`. No widget in the product has real unit
  coverage of its trend chrome. Carried forward as its own item — it is feature-wide and
  pre-existing.
- **Scope a mutation run to the code the feature owns.** A wide `mutate` range produces a
  confident aggregate that describes mostly other people's code, and buries the survivors that
  matter.
- **Denying a tool globally disables the agents that depend on it.** `permissions.deny` for
  `Read`/`Grep`/`Glob` is inherited by subagents but the compensating MCP allows are not, so
  four steps had to be implemented by the orchestrator and carry no DES trace. Left unlogged
  rather than back-filled with a RED→GREEN cycle that never happened.

## Permanent artifacts

- User docs — `docs/metrics/widgets.md`: corrected six stale descriptions (Total Throughput /
  Total Arrivals drill-in, Blocked and Stale as-of-range-end, Total Work Item Age, the Blocked
  trend baseline) and documented the Work Item Age Percentiles status bands, its trend, and the
  Flow Efficiency unconfigured state. Screenshots regenerated for the three affected images.
- E2E — `tests/specs/flow/TotalThroughputViewData.spec.ts`, `BlockedItems.spec.ts`,
  `WorkItemAgeAsOfRangeEnd.spec.ts`, `FlowEfficiency.spec.ts`,
  `WorkItemAgePercentilesStatus.spec.ts`; POMs `RagChip.ts`, `WorkItemAgePercentilesWidget.ts`.
- Frontend selectors — `blockedTrend.ts`, `workItemAgePercentilesTrend.ts`,
  `computeWorkItemAgePercentilesRag` in `ragRules.ts`.
- Backend — `WorkItemBase.AgeOnDay`, `Models/Metrics/StateAsOf.cs`, the as-of projections in
  `TeamMetricsService` / `PortfolioMetricsService`, and the historic blocked read on
  `TeamMetricsController` backed by `IWorkItemBlockedTransitionRepository`.
- CI learnings — two E2E locator rules added to `docs/ci-learnings.md`.
