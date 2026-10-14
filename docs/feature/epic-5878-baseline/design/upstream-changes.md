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
only when truly empty — no finished item, ~~fewer than 2 points for limits, a collapsed band, a lower limit of 0 for the
below-count (Q8),~~ a Then of 0 for % (Q9); no threshold. *(The three limit reasons superseded by D49 (2026-10-14):
they went with the beyond-limits count — see §8.)* Applied to US-01 (example 5, two scenarios, AC-1.3, AC-1.6),
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

## 5. What "average {Work Item Age}" is (D39 M4, AC-5.2) — RESOLVED by D48, *superseded by D51 (2026-10-14)*

**Original**: "total and average {Work Item Age} Then and Now"; Q10 defined the total only.

**Resolution (D48)**: two values per side, like the total — the window average (mean over the window's days of that
day's total age ÷ that day's {WIP}; days without {WIP} left out) and the last day's value (last day's total age ÷ that
day's {WIP}). Applied to US-05 (pitch, example 5, scenario, AC-5.2) and slice 05. DESIGN: DD15, ADR-221 §7.

*Superseded by D51 (2026-10-14):* there is no per-item average age and no last-day value. The panel is "Total {Work
Item Age}": Average, Median, Min and Max of the window's daily total age, each with its own change; nothing in
progress is a real 0. DESIGN: DD15 M4, DD35, ADR-221 amendment.

## 6. ADR-209 superseded in its deferral

ADR-209 deferred any Report abstraction to the second Report kind; this Epic is it. ADR-219 answers ADR-209's six
questions and supersedes the deferral; ADR-209 carries a status note. The Forecast Reality Check is unchanged.

## 7. DISTILL's UI decisions and upstream issues absorbed (2026-10-13)

DT1–DT7 (maintainer, 2026-10-13) are final and replaced DESIGN's frontend shape: `CreateReportDialog`,
`EditReportDialog` and `DeleteReportDialog` are gone; Create and Edit are one wizard on a stepper frame extracted
from `CreateWizardShell` (DD30); the list is `DataGridBase`; delete is `DeleteConfirmationDialog` with an optional
warning line. DESIGN now answers each item of `distill/upstream-issues.md` (marked there):

| DISTILL item | DESIGN answer | Changes a test? |
|---|---|---|
| 1 list carries `thenRebuildable` | adopted, at `thenAndNow.thenRebuildable` on the row, the same path as the read (DD24) | yes — the row path |
| 2 `then` / `now` on the wire | renamed on the wire to `atThen` / `atNow` (DD27) | yes — backend helper, frontend mapping removed |
| 3 member names inside a side | adopted unchanged (Driving ports → Read contract) | no |
| 4 refusal codes; open statuses | adopted unchanged; cap 403, unknown stored type 422, edit with Then members 200 (DD25, DD28) | no (statuses were left open) |
| 5 `CreateWizardShell` cannot host the steps | extract `WizardStepperFrame` (DD30) | no (tests drive the dialog) |
| 6 owner-delete count source | confirmed: the owner's list endpoint (DD23) | no |
| 7 report address and where it renders | confirmed as pinned (DD22) | no |
| 8 unknown type has no look | designed (DD32), copy to confirm | adds a frontend case |
| Copy: list failure | a sentence added beside Retry ("Loading and failure looks"), copy to confirm | yes, if confirmed |
| Wizard bounds | `minimumLengthDays` added to the templates read; `earliestStart` nullable for cutoff 0 (DD26) | yes — test kit fixture and model |

**Round 2 (2026-10-13, second-pass review).** No wire-contract change. Three test-visible consequences only:

| Change | Changes a test? |
|---|---|
| Window refusal copy interpolates `minimumLengthDays` / `earliestStart` from the bounds (NH2) | yes — a Vitest check with non-default bounds; today's assertions ("at least 14 days" with the fixture's 14) stay as they are |
| Cutoff-0 row in E12's shared boundary table: `earliestStart` null / cutoff 0, an old start, no refusal (NM1) | yes — one row in `reportWindowBoundaries.json`, run by both stacks |
| `WidgetShell`'s exported body look gains optional `failureMessage` and `onRetry` (NH1) | the report surfaces' failure sentence + Retry are as DISTILL pinned; dashboard `WidgetShell` tests unchanged |
| List failure copy "Reports couldn't be loaded. Reload to try again." + Retry is the working copy (NL3) | no — already pinned; DELIVER's sketch may re-word it |

## 8. No counts beyond Then's limits; {WIP} and total age as four lines; a summary header (D49–D52, 2026-10-14)

**Origin**: the maintainer's decisions after the DISTILL sketch review, recorded in DISCUSS (D49–D52). Not an upstream
finding of DESIGN's; DESIGN applies them. **This one changes the wire contract** (DD37).

| DESIGN item | Change | Changes a test? |
|---|---|---|
| DD9 `XmRCalculator.Limits`, E5 | withdrawn; the extraction (c206d65bd) was reverted (058b4ba48) | no — no property test was added |
| DD10 `BeyondLimits`, E15 | not built; E15 covers `ReportWindowPolicy`, `ReportCapPolicy` and DD35's summary | no — the ArchUnit case list never had `BeyondLimits` |
| E2 | extended: the Reports module must not depend on `XmRCalculator` | yes — one type added to an existing ArchUnit rule |
| DD11, storage | no `limits` member in any capture | yes — **done by DISTILL** (6f2bc352e, DST-16): `Slice03CycleTimeBeyondThenLimitsScenarios` deleted, not re-pointed; the slice-04 days-above-the-limit scenario and the frontend's slice-03 cases deleted too |
| DD12 absence codes | only `no-finished-items` remains | yes — `ReportsAcceptanceTest.Reason`, frontend model, `reasonTexts` |
| DD15 M3, M4; DD35 | `{ average, median, min, max }` for both; median per `PercentileCalculator` | yes — slice-05 scaffolds, frontend fixtures |
| Read contract, DD37 | `beyondThenLimits` dropped; old `work-item-age` keys dropped; `median` added | yes — `Report.ts`, `ReportService.test.ts`, `ReportsTestKit.tsx`, `ReportView.test.tsx` fixtures |
| DD36 rounding | 0 decimals for {WIP} and Total {Work Item Age}; per-day 2 | pins panel copy in slices 04 / 05 |
| DD36 binary doubles (peer review 2026-10-14) | accepted: `roundForDisplay(1.005, 2)` is 1, not 1.01 | yes — one row `[1.005, 2, 1]` in `thenNowComparison.test.ts` (DST-20) |
| DD33 → DT9 (2026-10-13) | Then freezes the {Cycle Time} 85th, 70th and 50th; a report stored before DT9 holds the 85th only and reads Then's 70th and 50th "Not captured for this report" (D27). DD33's "85th only" and "no Not-captured sentence for Then's 70th / 50th" are struck | yes — `A_report_that_froze_only_the_85th_percentile_says_nothing_of_Thens_70th_and_50th` (backend, DST-21); `CycleTimePanel.test.tsx` already shows the sentence |
| D52 summary header (DD12) | frontend only, no wire change. Each side's window and "N {Work Items} closed": from slice 04 the {Throughput} `total` when the report captured {Throughput}, else the {Cycle Time} sample size (items closed with a cycle time > 0); no count line when neither was captured; "0 … closed" when the source is 0 or `no-finished-items`. Sample size shows here, not per line. "Days since Then ended" waits for slice 07's sketch; `daysSinceThenEnded` stays on the wire | yes — DST-18 cases; slice 07's `it.todo` (DST-19) |

## 9. The header's CHANGE segment and the {Cycle Time} 95th (D53, D54, 2026-10-14; slice 05b)

**Origin**: the maintainer's decisions after the slice-04 live check, recorded in DISCUSS (D53, D54, US-05b, Q11). Not
an upstream finding of DESIGN's; DESIGN applies them. **Wire change**: the `cycle-time` sides gain `p95`; nothing
else.

| Decision | DESIGN item | Changes a test? |
|---|---|---|
| D54 the 95th on both sides | DD38, DD15 M1, DD12 (same sample size as the other percentiles), DD33 lineage (extends DT9), read contract and storage gain `p95`; no migration; `CycleTimePercentiles` gains `P95`; `CycleTimeReportMetric` adds `p95` | yes — backend `CycleTimeReportMetricTest` (freeze-and-show gains the 95th; a capture without `p95` reads back without it), `ReportsAcceptanceTest` helpers (`P95`), new Cycle Time scenarios for slice 05b (freeze and show; a report stored before the 95th; parity with the Metrics endpoint's 95th), `ThenAndNowPayloadRoundTripTest` (payload with and without `p95`) |
| D54 old reports | a report stored before D54 has no Then `p95`: `WindowedReportMetric` reads back only the Then members Now produces and the capture holds, so the member is missing and reads "Not captured for this report" — DT9's path, no code added | yes — one backend scenario and one frontend case pin it |
| D54 the panel | 95th row above the 85th (Q11d); `new ForecastLevel(95)` = Certain, as the Metrics tab maps it (`CycleTimePercentiles.tsx:57-58`) | yes — `CycleTimePanel.test.tsx` gains the 95th row, its icon, change and the not-captured case; `ReportView.test.tsx`'s certainty-icon case gains the 95th; `ReportsTestKit.tsx` fixtures gain `p95` |
| D53 CHANGE segment | DD39: frontend only, never stored or sent; `changeLines` per registry entry; `lineComparisonsOf` (per line, null for a dash or not captured) feeds the panels' change columns and `summariseChange` (new `panels/changeSummary.ts`); the header adds a third column; "—" when nothing is comparable (Q11a); catalog order inside a group (Q11b); "Not captured" ignored like a dash (Q11c); a long group wraps (Q11e); loading and failure from the report body's `WidgetBody` | yes — new `changeSummary.test.ts`; `thenNowComparison.test.ts` gains `lineComparisonsOf` cases; a new registry parity test (E16); `ReportView.test.tsx` gains the header's CHANGE cases (US-05b's scenarios, the "—" case, renamed Terminology, a key with no panel, loading and failure) |
| D53 enforcement | E16: the header cannot judge a line differently from its panel (table test, registry parity, purity scan) | yes — the three Vitest checks above |

Nothing in DISCUSS needed correcting: US-05b's Tech note ("DESIGN confirms no migration"; "derived from the panels'
marks in the frontend, never stored") is confirmed as written. Q11's five provisional details are adopted as the
working shape and still go to the maintainer's sketch at the start of DISTILL.
