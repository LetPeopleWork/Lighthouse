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

## Note — shipped tests superseded, by design

`blockedTrend.test.ts` currently asserts `noBaseline === true` for empty/null history. Slice 02 deliberately
changes that behaviour (D2), so DELIVER **removes** that expectation when it un-skips the new block — but
see UPSTREAM-4 first: that block's shape is not final. Same for the `reference-line-Work Item Age {p}%`
expectations in `WorkItemAgingChart.test.tsx`, superseded by slice 01 (D9) — there are **seven** such tests,
not two (corrected 2026-07-19; the full list is in that file's inline marker). Both are called out inline at
the point of change so the deletion is not mistaken for a regression.
