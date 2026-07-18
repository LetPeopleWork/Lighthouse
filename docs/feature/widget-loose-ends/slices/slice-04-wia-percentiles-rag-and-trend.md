# Slice 04 — Work Item Age Percentiles: RAG status and previous-period trend

**Story**: 5508 Cleanup Widget lose Ends | **Group**: C (data) | **Job**: `job-flow-coach-read-every-widget-the-same-way`

## Goal (one sentence)
Give the Work Item Age Percentiles widget the two chrome slots it is missing — an Act/Observe/Sustain status computed by the shipped SLE-attainment rule applied to in-progress ages, and a previous-period trend derived from slice 03's as-of-date computation.

### Elevator Pitch
Before: the Work Item Age Percentiles card is the only Flow Overview widget with neither a status colour nor a trend — I have to judge for myself whether "85th: 18 days" is good.
After: open **Team → Metrics → Flow Overview** → the card shows an **Act/Observe/Sustain** chip stating what share of my WIP is still within the SLE, plus a trend arrow versus the previous period.
Decision enabled: decide whether ageing WIP is a problem *and* whether it is getting worse, without holding last period's numbers in my head.

### The rule (clarified 2026-07-18)
Reuse `calculateSLEStats` from `ragRules.ts` **verbatim**, swapping only the population: in-progress ages (as-of-`endDate`, per D3) instead of completed cycle times. SLE is `{percentile: P, value: V}`, e.g. "85% within 14 days". Compute the share of in-progress items whose age is within V, compare against P, and apply the shipped bands from `computeCycleTimePercentilesRag` (`ragRules.ts:174-218`):

- share ≥ P → GREEN "Sustain"
- share short of P by more than 20pp → RED "Act"
- otherwise → AMBER "Observe"
- no SLE configured, or zero items → RED with the "define an SLE in settings" tip

No invented band, no new threshold, no new setting.

### Domain examples (SLE = 85% within 14 days)
1. 40 in-progress items, 30 within 14d → 75% vs 85% target, 10pp short → AMBER "Observe".
2. 40 items, 20 within 14d → 50% vs 85%, 35pp short → RED "Act".
3. 40 items, 36 within 14d → 90% ≥ 85% → GREEN "Sustain", tip suggests tightening the target.
4. No SLE configured → RED with the "define an SLE" tip (matches the cycle-time rule, not a bespoke neutral).
5. Zero in-progress items → RED with the same tip; no crash, no divide-by-zero.
6. Share 75% today vs 60% at the previous-period boundary → trend `up` (improving attainment).

### Outcome KPI
Overview widget chrome parity: 100% of Flow Overview widgets expose a RAG status, asserted as a test over `getWidgetsForCategory("flow-overview")` vs the keys registered in `buildWidgetFooters`.

## IN scope
- New rule in `ragRules.ts` reusing `calculateSLEStats` and the `computeCycleTimePercentilesRag` band logic over the in-progress age population. Extract the shared band logic rather than copy-pasting it — two rules with identical bands over different populations is the same *knowledge*, so DRY applies here.
- Register `workItemAgePercentiles` in `buildWidgetFooters` (`BaseMetricsView.tsx:~282`).
- Flip `trendPolicies.workItemAgePercentiles` from `"none"` to `"previous-period"` (`categoryMetadata.ts:117`) and supply a `TrendPayload` computed from slice 03's as-of-date computation evaluated at `startDate − 1 day` versus `endDate`.
- Widget status guidance entry in `widgetInfoMetadata.ts` (sustain/observe/act text), consistent with the other widgets that carry one.
- Vitest for every domain example above, including the no-SLE and empty-population paths.
- Team and Portfolio scope.

## OUT of scope
- New UI — the trend renders through the existing `WidgetShell` trend chrome (`trend` prop), no new component.
- Any new RAG band, threshold, or tunable. The bands are the shipped ones.
- A WIP-specific SLE distinct from the cycle-time SLE. If the learning hypothesis fires, that is a separate conversation, not a scope extension here.
- Changing which percentiles are computed (50/70/85/95 stays).
- Retro-persisting historical percentiles — the previous-period value is computed on demand from slice 03.

## Learning hypothesis
- **Disproves if it fails**: that the cycle-time SLE is a fair yardstick for *in-progress* age. The measure is optimistic-biased — a 2-day-old item counts as "within 14 days" though it may still breach — so a team with young WIP can read green while trending badly. If the chip stays green through a visibly deteriorating period, the attainment framing is wrong for live WIP and the widget needs a different signal (e.g. share already *past* the SLE).
- **Confirms if it succeeds**: the SLE the team already understands doubles as the WIP-age alarm, one rule serves both percentile widgets, and no new tunable is needed.

## Acceptance criteria
1. Given an SLE `{P, V}` and a share of in-progress items within V that is more than 20pp below P, the header renders RED "Act" with a tip stating the achieved share against the target.
2. Given the share meets or exceeds P, the header renders GREEN "Sustain"; given it is short by 20pp or less, AMBER "Observe".
3. Given no SLE is configured, or zero items are in the population, the header renders RED with the "define an SLE in settings" tip — matching the cycle-time rule, with no divide-by-zero.
4. The trend compares the as-of-`endDate` value against the same computation at `startDate − 1 day` (D5), rendered through the existing `WidgetShell` trend chrome.
5. Colour is not the only signal — the Act/Observe/Sustain label and tip carry the same information.
6. The band logic is shared with `computeCycleTimePercentilesRag`, not duplicated — a change to the 20pp boundary lands in one place.
7. Holds at Portfolio scope as well as Team scope.
8. E2E (demo data, POM-mediated): a demo team whose WIP-age SLE attainment falls short shows a RED status chip on the Work Item Age Percentiles widget; the POM asserts the status attribute, not a pixel.

## Dependencies
**Hard dependency on slice 03.** The previous-period trend is slice 03's as-of-date computation evaluated at a second date; without it there is no honest previous-period value (the pre-fix code would age both periods to today, making every trend `flat`).

## Effort / reference class
~0.5–1 day, assuming slice 03 has landed. Reference class: slice-07 of epic 5074 (blocked RAG) plus slice-06 (blocked previous-period trend) — the same two chrome slots on a different widget. Frontend-weighted; the backend side is a second call into slice 03's computation.

## Pre-slice SPIKE
None required, but **check the learning hypothesis early**: before wiring the rule, compute WIP-age SLE attainment on demo data and on a real team across a period the coach considers to have deteriorated. If the chip reads green throughout, the optimistic bias is material — stop and re-open D6 rather than shipping a signal that reassures during a decline.
