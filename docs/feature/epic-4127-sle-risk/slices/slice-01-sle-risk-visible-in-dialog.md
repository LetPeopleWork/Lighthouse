# Slice 01 — SLE Risk readable per item in the work item dialog

**Feature**: `epic-4127-sle-risk` | **Stories**: US-01 | **Estimate**: ~1.5 days

## Goal

A flow coach opening the Work Item Age widget's View Data dialog on a team with an SLE set reads, on every in-flight row, the probability that the item will breach the SLE — and can order the list by it.

## Learning hypothesis

**The empirical conditional is stable enough on a real team's history to be acted on.**

Confirms if it succeeds: the denominator `count(T >= a)` stays large enough at the ages that matter that a displayed risk does not swing wildly from one daily update to the next, and a coach reading the column recognises the ordering as true of their own team. Slices 02, 03 and 04 are then worth building — they are three more surfaces for a number that has earned trust.

Disproves if it fails: that a per-item breach probability is decision-grade at all on a team-sized history. The denominator shrinks as the age grows — in the worked example it is down to 7 closed items at day 9 — so one item entering or leaving the window can move a displayed risk by 15+ percentage points overnight, on precisely the items the coach is being told to prioritise. If that is what real data does, the honest response is to stop and reconsider the presentation (banded rather than exact, or a minimum-sample gate like `forecast-minimum-data-guard` applies to forecasts) **before** three more surfaces inherit the instability. This is why this slice ships alone.

`OUT-4127-risk-stability` is the measurement, and it explicitly gates slices 02–04.

## Production data

Verified against a **real team on the dev instance restored from a production backup** (`Restore-DbBackup.ps1`), not synthetic fixtures. Two teams are required in the sample:

- one with an SLE set and a healthy history, to read the column and check the ordering against what the coach knows about those items;
- one with an SLE set and a **thin** history (fewer than ~20 closed items in the window), because that is where the denominator collapses and where the learning hypothesis is actually tested.

A third team with **no** SLE set is needed to exercise AC-01.5 — the column being absent rather than empty. Synthetic cycle times would prove the arithmetic and miss the only thing in doubt, which is whether the arithmetic is usable.

## Dogfood moment

Same day: open the Lighthouse dogfood instance's own team metrics view, sort the new column worst-first, and read the top three items against what is actually known about them. Then capture the endpoint response and re-capture it the next day — the first data point of `OUT-4127-risk-stability`.

## IN scope

- A domain calculation taking the item's age, the SLE range and the window's closed cycle times, returning the empirical conditional per D1 and D8: `count(T > R AND T >= a) / count(T >= a)`, with the empty-denominator case returned as a distinguishable "beyond history" result rather than a number.
- A team-scoped HTTP read surface carrying the per-item risk for the selected range. Shape — a dedicated action alongside the existing percentile endpoints, or a field on the in-progress payload — is DESIGN's call; the constraint is that one domain function serves it and slice 04 alike (D5).
- The window follows the caller's `startDate`/`endDate`, matching every other value on the metrics view (D6), using the default started→finished cycle-time definition only (D7).
- An **optional** `sleRiskColumn` descriptor prop on `WorkItemsDialog`, following the `ageBandColumn` precedent (`WorkItemsDialog.tsx:52, 99-102, 321-322`) exactly. Absent, the dialog behaves as today and none of the ~14 other call sites are edited.
- Column header `${sle} Risk` via `TERMINOLOGY_KEYS.SLE` (D10), with a `description` tooltip stating the calculation in the plain-words form: *of every item still open at this age, what fraction went on to exceed the target*.
- Numeric `sortComparator` so the column sorts by risk and not lexically; `Beyond history` ranks off-scale and sorts last worst-first (AC-01.7).
- `valueGetter` returning the rendered label so export carries `86%` / `Beyond history` rather than a raw ratio — the trap `useDataGridExport` sets and #5884 D12 already documented (AC-01.8).
- Column omitted entirely when the owner has no SLE (D3) or is a portfolio (D4) — one predicate, two cases, same code path.
- Backend tests: the four boundary cases of D8 (`a < R`, `a == R`, `a > R`, cycle time exactly `R`), the empty-denominator sentinel, a zero SLE range, and a window with no closed items.
- Vitest coverage: column present / omitted, sort order including the sentinel, export value, header terminology, and a persisted pre-feature column order not hiding the column (AC-01.10).

## OUT scope

- The Items In Progress chip (slice 02), the chart background mode (slice 03) and write-back (slice 04).
- Portfolio scope in any form (D4).
- Any per-state variant of the risk — that is #5884's question and stays there.
- Any persistence of the value: no stored column, no domain event, no history. It is a pure function computed per request.
- Any minimum-sample gate or banded presentation. Adding one now would pre-empt the very question this slice exists to answer; if the data demands it, it becomes a decision informed by evidence rather than a guess.
- Changing the existing age column's rendering, the SLE reference line, or the dot colouring.
- Docs and screenshots — those land at feature finalization.

## Acceptance criteria

Every AC listed under US-01 in `feature-delta.md` (AC-01.1 … AC-01.11).

Plus, specific to this slice: the endpoint response for the dogfood team is captured on two consecutive days and the per-item deltas recorded, as the opening measurement of `OUT-4127-risk-stability`.

## Dependencies

None outstanding. The SLE range, both day-counting helpers, the optional-column precedent and the export-value trap were all verified in code during the pre-DISCUSS reality check.

## Reference class

`story-5884-work-item-age-bands` slice 01 — the same dialog, the same optional-column contract, the same export trap — landed inside a day for the frontend half. This slice adds a backend calculation and a read surface on top, hence ~1.5 days rather than ~1.

## Pre-slice SPIKE

Not needed for feasibility — the arithmetic is two counts over data both sides already hold. The uncertainty is entirely in whether the output is *usable*, which no spike can answer faster than shipping the column and reading it against a real team. That is the slice.
