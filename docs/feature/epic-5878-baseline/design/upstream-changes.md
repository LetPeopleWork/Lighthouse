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
| DD11, storage | no `limits` member in any capture | yes — slice-03/04/05 scaffolds asserting limits or counts |
| DD12 absence codes | only `no-finished-items` remains | yes — `ReportsAcceptanceTest.Reason`, frontend model, `reasonTexts` |
| DD15 M3, M4; DD35 | `{ average, median, min, max }` for both; median per `PercentileCalculator` | yes — slice-05 scaffolds, frontend fixtures |
| Read contract, DD37 | `beyondThenLimits` dropped; old `work-item-age` keys dropped; `median` added | yes — `Report.ts`, `ReportService.test.ts`, `ReportsTestKit.tsx`, `ReportView.test.tsx` fixtures |
| DD36 rounding | 0 decimals for {WIP} and Total {Work Item Age}; per-day 2 | pins panel copy in slices 04 / 05 |
