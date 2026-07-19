# DISTILL — upstream issues (widget-loose-ends, Story 5508)

Raised 2026-07-18 while writing the acceptance tests. Three findings. **All three resolved the same day**
— UPSTREAM-1 and UPSTREAM-2 by user decision, UPSTREAM-3 absorbed directly by the ATs.

---

## UPSTREAM-1 — RESOLVED 2026-07-18 — slice-03 domain example 2 contradicts the primitive DESIGN forbids changing

**Where:** `slices/slice-03-work-item-age-as-of-selected-date.md` → *Domain examples*, example 2:

> 2. Range ends 06 July → item included, age 6.

(for the item started 01 July, closed 06 July)

**The conflict.** `BaseMetricsService.WasItemProgressOnDay` (`BaseMetricsService.cs:869-888`) decides the
population, and its closed-side test is strictly greater-than:

```csharp
var wasClosedOnOrAfterDay = !item.ClosedDate.HasValue || item.ClosedDate.Value.Date > day.Date;
```

An item closing **on** the selected day is therefore **not** in progress on that day, and the example's
"included, age 6" cannot hold. DESIGN D14 explicitly forbids touching this primitive ("Both already
select the historically-correct population… changing them would be churn without behaviour change, and
would risk the one thing that currently works"), and CI6 pins the new projection to
`GenerateTotalWorkItemAgeByDay`, which uses the same predicate and already drives the shipped
Total-Work-Item-Age-over-time chart.

So the example cannot be satisfied without either (a) changing a shipped primitive that other widgets
depend on, or (b) introducing a second, divergent definition of "in progress on a day" — which is
exactly what CI6 exists to prevent.

**DISTILL's reading:** the domain example is a drafting slip, not a requirement. Every other example
(1, 3, 4, 5), US-04 AC1, and the journey YAML are all consistent with the shipped predicate. Exclusion on
the closing day is also the more defensible semantic: the item finished that day, so it was not ageing
*through* it.

**Pinned as:** `TeamWorkItemAgeAsOfDateTest.GetWorkItemAgePercentiles_ItemClosedOnTheSelectedDayItself_IsExcluded`,
tagged with a pointer to this file.

**Resolved (user, 2026-07-18):** the example was wrong. An item closing on the selected day is excluded.
The slice brief has been corrected in place with a note recording the change. D14 stands, the shipped
primitive is untouched, and the AT needs no change.

---

## UPSTREAM-2 — RESOLVED 2026-07-18 — D16 omits the portfolio half of the aging-chart fix, so CI2 cannot be met as designed

**Where:** DESIGN decision D16.

> `WorkItemDto` gains an optional `DateTime? asOf = null` constructor parameter … Only the `/wip` endpoint
> (`TeamMetricsController.cs:117-130`) passes it.

**The gap.** The portfolio `/wip` endpoint (`PortfolioMetricsController.cs:95-105`) does **not** return
`WorkItemDto` — it returns `FeatureDto`, which derives from `WorkItemDto` via
`: base(feature, FeatureIsBlocked(feature), namedCycleTimes ?? [])` (`FeatureDto.cs:11-12`), i.e. through
the three-argument overload that has no `asOf`. It also does not accept an `asOfDate`-carrying construction
path today.

Consequences if D16 ships as written:

- Team scope: the Work Item Aging chart's dot heights become as-of-`endDate`. ✅
- Portfolio scope: the dot heights stay today-anchored, while the Work Item Age Percentiles card on the
  same page moves to as-of-`endDate`. The two surfaces then **disagree for the same range** — which is
  precisely what US-04 AC3 forbids ("the two surfaces never disagree").

This also breaks **CI2** ("a widget fixed for teams and left broken for portfolios is not done").

**Fix shape (small):** thread the same optional `asOf` through `FeatureDto`'s constructor to the
`WorkItemDto` base, and pass `asOfDate` from `PortfolioMetricsController`'s `/wip`. No new endpoint, no
contract change for existing callers — same bounded-blast-radius argument D16 already makes for the team
side.

**Resolved (user, 2026-07-18):** D16 is extended to `FeatureDto` + `PortfolioMetricsController./wip`. The
decision text in `feature-delta.md` has been amended accordingly, and slice 03's file list grows by those
two. The portfolio parity ATs already written against this assumption stand.

---

## UPSTREAM-3 — slice-02 undercounts the `noBaselineTrend()` return sites (absorbed, no decision needed)

**Where:** `slices/slice-02-blocked-trend-zero-baseline.md` → IN scope:

> replace **the two** `noBaselineTrend()` returns that fire on missing history / missing boundary snapshot

`blockedTrend.ts` has **three**: empty/null history, missing boundary snapshot, and missing *current*
snapshot. The slice's own IN-scope bullet two lines down and its domain example 5 both describe the third
case correctly ("if there is no current snapshot either, current is 0 → `flat`"), so the intent is
unambiguous and only the count is wrong.

**Absorbed:** the ATs cover all three paths. No decision required — noted so the brief can be corrected
when it is next touched.

---

## UPSTREAM-4 — BLOCKER: D2 is built on a misdiagnosis; the blocked trend is broken by a fetch-window off-by-one

**Raised:** 2026-07-19, by the DISTILL final-wave review gate. The PO reviewer flagged that D2 was LOCKED
before slice 02's own stated diagnostic had been run. The diagnostic has now been run against the code, and
it **disproves the premise D2 rests on**.

**The evidence chain:**

1. `useMetricsData.ts:381-388` fetches the history with the *dashboard's own* range:
   `getBlockedCountHistory(entity.id, startDate, endDate)`.
2. `TeamMetricsController.cs:459-461` (and `PortfolioMetricsController.cs:449` identically) filters
   `s.RecordedAt >= start && s.RecordedAt <= end`. The returned history therefore **never contains a
   snapshot earlier than `startDate`**.
3. `BaseMetricsView.tsx:1744-1748` passes that same `startDate`/`endDate` pair to `computeBlockedTrend`.
4. `blockedTrend.ts` looks for the baseline at `boundary = startDate − 1 day`, i.e. **exactly one day
   outside the window that was fetched**. `latestAtOrBefore(history, boundary)` is therefore `undefined`
   by construction, and `noBaselineTrend()` returns.

**Consequence:** the Blocked trend renders the neutral "—" placeholder on **every instance, for every
range, always** — not only on young ones. Snapshot history age is irrelevant; a five-year-old instance
behaves identically. This is a plain defect, not the forward-only-data limitation the `noBaselineTrend`
docstring describes.

**Why this blocks D2 as written:** D2 assumes the missing baseline is a *young-instance* condition and
substitutes `0` for it. Against the real behaviour that substitution would make **every** instance render
"+N since we started recording", permanently, and the true previous-period comparison would never appear at
all. The widget would go from visibly broken to invisibly wrong — the exact failure mode this feature
exists to remove. Slice 02's learning hypothesis ("disproves that 'trend doesn't work' is the `noBaseline`
path if the trend stays blank after the change") has fired: the `noBaseline` path *is* what renders, but
the cause is upstream of it.

**Recommended correction (needs user sign-off):** fix the window, then keep D2 for the case it was actually
meant for.

- Widen the baseline fetch so the boundary day is inside it — fetch from `startDate − 1 day` (cheapest: one
  extra day per request, no new endpoint, no contract change), or add a dedicated single-day baseline read.
- With a real baseline available, `noBaseline` becomes genuinely rare — it then means what its docstring
  says: a forward-only history that truly predates the boundary.
- D2's "treat a missing baseline as 0" then applies only to that residual young-instance case, which is
  what the user locked on 2026-07-18 and which remains defensible there.

**Impact if accepted:** slice 02 grows from a pure frontend selector change to a fetch-window fix plus the
selector change (still ~0.5d — one changed argument plus its tests). Scenarios 17-23 need revision: they
currently pin baseline behaviour against hand-built histories that already contain a pre-boundary snapshot,
so they pass in isolation while the shipped wiring can never supply one. At least one scenario must pin the
**fetch window itself**, at the `useMetricsData` seam, or the same defect ships again untested.

**Resolved (user, 2026-07-19):** the recommended correction is adopted in full. D2 is re-decided in
`feature-delta.md` as two ordered changes — widen the fetch to `startDate − 1 day`, then keep the
zero-baseline fallback for the residual young-instance case. US-03 gains **AC0** (fetch window) and slice 02
gains **scenario 16b**, authored as a skipped RED scaffold in `useMetricsData.test.ts` — the only layer that
can catch this class of defect, since the selector suite was fully green throughout. Scenarios 17-23 are
un-frozen and stand as written; the slice-02 docstring in `blockedTrend.test.ts` now states the two-step
order explicitly so DELIVER cannot ship step 2 alone.

**Also filed as a standalone ADO Bug: [#5521](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5521)**
— "Blocked Items trend never renders: baseline day is outside the fetched history window". The trend has been
broken for every user on every instance since it shipped, which is a defect in its own right rather than a
loose end of #5508. The fix ships inside #5508 slice 02; the Bug carries the root-cause writeup so it is
findable independently of this feature's workspace.

**Status: RESOLVED.**

---

## UPSTREAM-5 — RESOLVED IN PLACE 2026-07-19 (DELIVER slice 01) — three slice-01 ATs carried oracles the render cannot satisfy

**Raised:** during DELIVER of slice 01, when the DISTILL scenarios were un-skipped. Scenarios 34, 35 and 39
failed against a correct implementation. All three were **test defects, not code defects** — the production
change (D9 label, two `buildViewData` keys) was right on the first pass, and scenario 36 (AC3, empty range)
passed unmodified throughout, which is what isolated the fault to the oracles rather than the behaviour.

**Defect A — scenarios 34 and 35 compare against widgets that are not on the screen.**

Both ATs set the category to `flow-overview` in `beforeEach`, then assert:

```ts
expect(screen.getByTestId("widget-view-data-count-totalThroughput")).toHaveTextContent(
    screen.getByTestId("widget-view-data-count-throughput").textContent ?? "",
);
```

`totalThroughput` / `totalArrivals` are Flow **Overview** widgets; `throughput` / `arrivals` are **chart**
widgets in a different category and are never rendered on that screen. `getByTestId` therefore throws before
the comparison is reached. The intent — "the same payload the sibling chart widget already supplies" — reads
naturally in prose but is not observable from a single rendered category.

*Fix:* assert against the fixture directly. The throughput run chart carries `[3, 5]` (8 completed items) and
the arrivals run chart `[4, 6]` (10 started items). This is a stronger oracle than the original: it is
independent of a second widget's rendering, in the same spirit as scenario 12b's hand-computed ages, which
DISTILL added for exactly this reason on the backend side.

**Defect B — scenario 39 asserts list equality where the fixtures guarantee inequality.**

```ts
expect(ageLabels).toEqual(cycleTimeLabels);
```

The cycle-time fixture carries 50/85/95; the work-item-age fixture carries 50/70/85/95. The two label lists
differ by the 70th entry **whether or not a term prefix is rendered**, so the assertion could never pass and
never depended on the behaviour under test. AC1+AC2 are about label **format** ("exactly `{percentile}%`, no
term prefix", identical across sources), not about the two sources holding the same percentile set.

*Fix:* assert every rendered reference-line label matches `/^\d+%$/` under both sources, with a non-empty
guard on each list so the check cannot pass vacuously on an empty render.

**Collateral, worth recording:** the `ChartsReferenceLine` mock keyed its `data-testid` off the label alone.
Once both sources label identically that mock can no longer tell them apart, which would have quietly turned
several *surviving* tests into vacuous passes — `queryByTestId("reference-line-Work Item Age 50%")` is
trivially absent once no such id is ever emitted. The mock now also exposes `data-y`, and the two
source-discriminating tests assert the rendered **values** (cycle time 50→3/85→7/95→12 versus work item age
50→4/70→6/85→9/95→14) rather than id presence. This is the same `WRONG_ASSERTION` class DISTILL's own RED
classification caught in scenario 23 — a check that passes without the behaviour existing.

**Why this was not caught by the RED gate.** DISTILL ran the frontend suite with markers stripped and
recorded "14 failed of the 24 un-skipped, all `AssertionError` on the expected value". These three were
inside that 14 and their failure was read as MISSING_FUNCTIONALITY. Two of them in fact failed with a
`getElementError` (element not found) rather than a value mismatch, and one failed on a list-shape
difference — distinguishable from a genuine red only by reading the failure text, not the count. **Lesson for
future RED classification: a scenario that fails because the queried element does not exist is not the same
signal as one that fails on an expected value, and the two should be reported separately.**

No decision required — the ACs are unchanged and every fix strengthens the oracle. Recorded here because the
scenario table in `feature-delta.md` describes assertions that no longer match the shipped tests.

**Status: RESOLVED (in place, DELIVER slice 01).**

---

## UPSTREAM-6 — OPEN 2026-07-19 (DELIVER slice 05) — scenario 47 (AC7) is a category-wide invariant attached to a single slice

**Raised:** during DELIVER of slice 05, by the crafter, and independently verified by the orchestrator.

**The problem.** Scenario 47 iterates `getWidgetsForCategory("flow-overview", "portfolio")` and asserts every
widget key renders a `widget-header-*`. That set includes `workItemAgePercentiles`, whose RAG rule
(`computeWorkItemAgePercentilesRag`) and `buildWidgetFooters` registration are **slice 04's** deliverables.
**No correct implementation of slice 05 can make scenario 47 pass.** It is not a missing-functionality red
against slice 05; it is a red against work that is out of slice 05's scope by design.

This bit twice. The first crafter was cornered: it could not implement the missing footer without shipping a
defect, could not skip the AT (source writes were blocked for its session), and could not decline to commit
(the stop hook demanded one). It committed a red tree and — correctly — logged `GREEN` as `EXECUTED / FAIL`
rather than record a pass that had not happened. That refusal is the system working; the trap it was in is
what needs fixing.

**Why forcing it green was refused.** `computeWorkItemAgePercentilesRag` is a `__SCAFFOLD__` stub returning
hardcoded `{ragStatus: "red", tipText: ""}`. Wiring it to satisfy scenario 47 would ship a widget that
permanently displays a red chip with no explanatory text — precisely the misleading-status harm US-06 AC2/AC3
exist to prevent, and a user-visible defect strictly worse than the missing chip slice 05 set out to fix. It
would also have destroyed slice 04's RED signal by pre-empting step 04-01's scope.

**Resolution applied (interim).** Scenario 47 is `it.skip` with an inline comment naming the blocker, the
un-skip trigger (**slice 04 step 04-02**), and the reasoning. This follows the project's standing practice —
skip a not-yet-passing AT to keep the bar green, un-skip to resume — and the comment is what keeps it honest
rather than a silent deletion. Commit `410c1285`, on top of the correct and untouched slice-05 commit
`93eafd3a`.

**Recurrence risk — the part worth acting on.** This is not a one-off. Scenario 47 asserts a **category-wide
structural invariant**, so it will fail again inside whichever slice next adds a Flow Overview widget before
that widget's RAG rule lands. The same diagnosis would then be run a third time, by whoever happens to own
that slice. The KPI itself is sound and worth keeping — it is exactly the guard that fails the moment a Flow
Overview widget ships without a status, which is the one genuinely new system-wide claim this feature makes.
Only its **slice attachment** is wrong.

**Recommendation (needs a decision, hence OPEN):** when slice 04 un-skips this, move scenario 47 out of the
slice-05 describe and into a standalone structural-guard step that runs after the last widget-contributing
slice — the DISTILL "Outcomes registry" section already identifies this assertion as the place the
system-wide claim is registered, so it should not live inside any one slice's block.

**Status: OPEN — interim skip applied; permanent re-siting deferred to slice 04.**

---

## Note — shipped tests superseded, by design

`blockedTrend.test.ts` currently asserts `noBaseline === true` for empty/null history. Slice 02 deliberately
changes that behaviour (D2), so DELIVER **removes** that expectation when it un-skips the new block — but
see UPSTREAM-4 first: that block's shape is not final. Same for the `reference-line-Work Item Age {p}%`
expectations in `WorkItemAgingChart.test.tsx`, superseded by slice 01 (D9) — there are **seven** such tests,
not two (corrected 2026-07-19; the full list is in that file's inline marker). Both are called out inline at
the point of change so the deletion is not mistaken for a regression.
