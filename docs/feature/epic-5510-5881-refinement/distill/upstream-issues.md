# Upstream issues — DISTILL — E2 Refinement need (Epic #5881)

**Date**: 2026-10-04 · **Raised by**: DISTILL (Quinn) · **Source**: the maintainer's sketch decisions of 2026-10-04,
recorded in `feature-delta.md` → "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)".

These back-propagate decisions the maintainer took in DISTILL into the DISCUSS and DESIGN artifacts they supersede.
None blocks DELIVER; the E2 scenarios already follow the new decisions. The earlier artifacts are left as written so
the history stays readable; where they disagree with this file, this file and the maintainer block win.

## 1. Stages come from rules only — the per-state stage is gone

| Superseded | Was | Now |
|---|---|---|
| US-03, AC-3.1 | Each refinement state carries exactly one stage, default Waiting | No stage per state. Stage comes only from two optional rules, "Ready when" and "Being refined when"; anything unmatched is Waiting |
| AC-3.4 | Expand-only storage change for the per-state stage | Nothing new to store per state. The stored `RefinementStateSetting.Stage` (only `Waiting = 0` exists) stays in the JSON value, unused, so nothing is dropped (expand-only); a later cleanup may stop writing it |
| US-03 example "A Team without a Ready state is told so" | A hint with a link for Team admins | No hint. Rules that match nothing Ready read "· 0 ready" |
| US-08 / slice 08 (#6146) | A separate slice for stage rules | Folded into slice 03; its Pulsar case is a slice-03 scenario. #6146 is to be marked Removed after the maintainer confirms (orchestrator) |
| DESIGN build order row 14 (08 stage rules) | `StageRules` JSON + `StageRuleMatcher` + `DeliveryRuleBuilder` reuse in slice 08 | The same pieces, in slice 03 |

## 2. Stage and votes are two independent signals

| Superseded | Was | Now |
|---|---|---|
| DD-5, DSN-14 ("a matching rule decides stage **and** readiness") | A rule overrides the vote outcome | A rule decides the stage only. The votes' readiness is unchanged on every row (`readiness`, `madeReady`, `readyByVotesCount` keep their slice-13 meaning) |
| AC-3.2, AC-5.7 ("stage-Ready added to the vote-ready Work Items") | One ready count adding both | The ready count is the votes' on a Team without rules (`readySource: Votes`), the stages' on a Team with rules (`readySource: Stages`). Never a sum |
| — (new) | — | A row carries `signalsDisagree` only when votes have been cast and disagree with the stage: stage Ready meets votes cast that do not say Ready, or votes Ready meet a stage that is not Ready. A row nobody has voted on is never flagged |
| DSN-14 precedence | Overlapping rules: Ready > Being refined > Waiting | Kept: when both rules match, Ready wins |

The heading reads "N Work Items in Refinement · R ready by votes" without rules (unchanged) and "… · R ready" with
rules; the E2E page object's heading pattern needs widening only once demo data gives a Team stage rules.

## 3. The "enough for" line follows the displayed order

| Superseded | Was | Now |
|---|---|---|
| DD-4, AC-6.1 ("N = high end, counted over all refinement Work Items" in backlog order) | The first N in backlog order are highlighted | The first N rows **as shown** are numbered (a "#" column) and the line sits after the N-th shown row; backlog order is only the default. A sort is not kept between visits |
| US-06 example 2 ("fewer Work Items in Refinement than may be needed") | That wording | "All 6 Work Items in Refinement are needed before Thu 8 Oct." after the last row |
| DESIGN's tab facts (a line position on the wire) | The server says after which Work Item the line falls | The server returns the high end (never cut to the listed count), its percentile and the date; the browser places the line |

## 4. Settled copy and layout replacing DISCUSS examples

US-04's hint "Set a Refinement cadence to see how many Work Items are needed" becomes two role-aware hints (editors:
"…in Settings…"; readers: "A Team admin can set…"); the next Refinement also says how far off it is ("· in 4 days",
"· tomorrow"). US-05's above-range message is a warning like below-range, in the same MUI Alert. Full copy in the
maintainer block.

## 5. Answered by the maintainer (2026-10-04)

- A stage-Ready row that nobody has voted on carries **no** ⚠: no votes is no opinion. The marker appears only when
  votes have been cast and disagree with the stage (DST-39). Slice 03 pins both sides: *A Ready stage the votes cast
  do not back yet is marked as disagreeing* and *A Ready stage nobody has voted on shows no disagreement*.
