# Upstream Changes — DESIGN — epic-5878-baseline

DISCUSS artifacts were not edited by the first DESIGN pass; each item below quotes the original and says what DESIGN
found in the code. **On 2026-10-03 the maintainer answered DESIGN's decisions to confirm (D45–D48)**, and DISCUSS was
then updated in place: `feature-delta.md` (DISCUSS sections), `discuss/wave-decisions.md`, slices 01, 04, 05 and 09,
and the journey SSOT. Status per item below.

## 1. There is no percentile minimum-data guard (D41, AC-1.6, US-01 ex. 5, US-06) — RESOLVED by D45

**Original**: D41 "a value shows '—' with a short reason exactly where Lighthouse's Metrics page already refuses one —
the percentile minimum-data guard, and a process-behaviour chart with too few points. No new thresholds." US-01
example 5 and US-06: 3 finished items show "—".

**Found**: `PercentileCalculator.CalculatePercentile` returns a value for any non-empty list and 0 for an empty one;
`BuildPercentiles` always returns four entries; the `CycleTimePercentiles` widget shows "No data available" only for
an empty array. No minimum exists.

**Resolution (D45)**: every value is shown with its sample size ("85th: 12 days · 3 {Work Items}"); "—" with a reason
only when truly empty — no finished item, fewer than 2 points for limits, a collapsed band, a lower limit of 0 for the
below-count (Q8), a Then of 0 for % (Q9); no threshold. Applied to US-01 (example 5, two scenarios, AC-1.3, AC-1.6),
US-03 (example 4, scenario, AC-3.5) and US-06 (examples, two scenarios). DESIGN: DD12, ADR-221 §6.

## 2. `BaselineValidationService` cannot be the Then-window rule (S6, D21, D37) — handled in DESIGN

**Original**: S6 "`BaselineValidationService` … Reused as the Then-window rule (D21, D37); its messages say 'Baseline',
so the report needs its own copy".

**Found**: it measures `endDate − startDate` in days, so D37's inclusive 14-day window (e.g. 1–14 Sep) measures 13 and
is refused; and it encodes the PBC Baseline's contract.

**Change (DD13, ADR-222)**: a separate pure `ReportWindowPolicy` with D21/D37's rules and inclusive counting; an
ArchUnit rule keeps the Reports module off `BaselineValidationService`. No AC changes; DISTILL adds a boundary
scenario "exactly 14 days is accepted".

## 3. Settings-changed notice (D31, US-09) — RESOLVED by D46: dropped

**Original**: D31's notice over the owner's {Work Item} types, state mapping, query and one more category. DESIGN had
proposed narrowing what it covered.

**Resolution (D46)**: the notice is dropped entirely and nothing about settings is stored. Slice 09 and US-09 are
dropped (brief kept as a stub; ADO #6167 is the maintainer's call). DESIGN: DD16 withdrawn; nothing about settings in
the payload or the read.

## 4. {Throughput} as a per-week statistic (D39 M2, US-04, mockup) — RESOLVED by D47

**Original**: a per-week median with the number of weeks in each sample. DESIGN had proposed how to bucket the weeks.

**Resolution (D47)**: total {Work Items} finished in the window and the average per day, Then and Now; change and %
change on the per-day average. Applied to D39, US-04 (pitch, example 5, two scenarios, AC-4.2, Tech), the mockup and
slice 04. DESIGN: DD15, ADR-221 §7.

## 5. What "average {Work Item Age}" is (D39 M4, AC-5.2) — RESOLVED by D48

**Original**: "total and average {Work Item Age} Then and Now"; Q10 defined the total only.

**Resolution (D48)**: two values per side, like the total — the window average (mean over the window's days of that
day's total age ÷ that day's {WIP}; days without {WIP} left out) and the last day's value (last day's total age ÷ that
day's {WIP}). Applied to US-05 (pitch, example 5, scenario, AC-5.2) and slice 05. DESIGN: DD15, ADR-221 §7.

## 6. ADR-209 superseded in its deferral

ADR-209 deferred any Report abstraction to the second Report kind; this Epic is it. ADR-219 answers ADR-209's six
questions and supersedes the deferral; ADR-209 carries a status note. The Forecast Reality Check is unchanged.
