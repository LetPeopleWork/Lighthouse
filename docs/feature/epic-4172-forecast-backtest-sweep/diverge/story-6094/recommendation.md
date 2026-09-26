# Recommendation — Story #6094

**Wave**: DIVERGE, light pass (complete) · **For**: `nw-product-owner`, DISCUSS · **Date**: 2026-09-26
Supporting: `job-analysis.md` · `options-raw.md` · `taste-evaluation.md` (this folder); competitive research reused from `../competitive-research.md`.
Job: `job-forecaster-check-the-forecast-against-what-happened`, **unchanged** — this story serves its "read how far off it was" half (`job-analysis.md` §3).

## 0. Decision

> **Confirm the maintainer's direction — the reality check in a dialog, Brown's graded table of every period, level, window and actual, and a result in text — with one change of emphasis: the text result opens the dialog and the table follows as its evidence (Option 5, 4.10).** Plus five refinements for DISCUSS (§3).
>
> **Assuming**: the grading rule is settled in DISCUSS with its small-actual and zero-actual cases, and "correct" is defined as `held` (ADR-210) so the product keeps one word for one event.

Option 5 is not a deviation: it contains every element the maintainer asked for (dialog, table, Brown's grades, colours, text). The matrix moves only the order — the text answers first. That single change lifts Progressive Disclosure from 3 to 5 and is why the maintainer's literal form (Option 1, 3.70) ranks fourth while its content ranks first.

## 1. Top 3

### Option 5 — Words first, table as evidence — 4.10
- **Why**: T3 5 and T2 4 — the dialog opens on one sentence per confidence level ("held in 14 of 16, within 10% in 9"), which is the quotable answer the job asks for; the 64-80 graded cells come second.
- **Trade-off**: two statements of one finding (T1 3).
- **Risk**: the per-level sentences drift into a per-window tally ("60 days was right most often") and break I-a (E5).
- **Hire when**: the forecaster needs a line to say out loud, then proof if challenged.

### Option 3 — Sentence inline, table in a dialog — 4.10 (lost on the locked tie-break)
- **Why**: the cheapest (F 5): the shipped sentence stays, only the dialog is new; answer before any click.
- **Trade-off**: keeps a verdict inline where the maintainer said "inline is horrible".
- **Risk**: two surfaces to keep in step.
- **Hire when**: most forecasters stop at the sentence.

### Option 2 — Graded grid, numbers on demand — 3.85
- **Why**: T1 4, T3 5 — the pattern of grades is visible at a glance.
- **Trade-off**: hides the forecasts and actual the story title asks to see (D 3).
- **Risk**: colour-only grading fails WCAG 1.4.1; a colour grid invites "which column is greenest", a per-window ranking by eye.
- **Hire when**: the table is re-opened often and the reader already knows the scale.

## 2. Dissenting case

**Option 3 tied at 4.10.** Its case: the verdict sentence is shipped, tested and already the answer; putting it behind a dialog adds a click before the forecaster learns anything, and "inline is horrible" may have been about the expanded band panels rather than the one-line answer. It lost only on the tie-break locked before scoring (Desirability), because it keeps inline something the maintainer asked to remove. **If the maintainer says the objection was to the panels, not the sentence, the recommendation becomes Option 3 with the same refinements.**

## 3. Refinements for DISCUSS (each traced to the matrix or the code)

1. **Text leads the dialog** (Option 5; T3 3 → 5). The per-level sentences summarise by level and by period, never by window (I-a, E5).
2. **Lay the table out by period, and print each actual once.** The scored period depends on the horizon only (`scoredPeriodStart = anchorDate − horizonDays + 1`), so every window in a horizon is graded against the same actual: 4 actuals, not 16 or 20. Rows = the four periods; columns = window × level. Cuts reading load (T2) without dropping a number.
3. **Grades carry a glyph or word, not colour alone** (Option 2's risk; accessibility). An unevaluable check keeps its words-not-blank treatment (I-c).
4. **Guard the percentage on small actuals** (Option 6's premise challenge, a risk, not a replacement): a 7-day period with an actual of 3 turns one Work Item into 33% ("beyond 25%"); an actual of 0 has no percentage. Show the miss in Work Items beside the percentage at minimum.
5. **Keep the held-rate reading beside the grades** (Option 6): half the 50% column should be "incorrect" by design, and a 95% cell that is "correct" by more than 25% is caution, not accuracy — exactly what `AlwaysHeld` already calls under-forecasting. Without this, a mostly-green table reads as "trust it" when it may mean "padded".

## 4. Premise risks, flagged (not replacing the direction)

- **Percent-of-actual misleads on small periods** (refinement 4). Brown's reference quantity for the margin (actual or forecast) is **unverified** (`job-analysis.md` §5).
- **"Correct" at every level invites "more green is better"**, which contradicts the nominal-rate reading the Epic built (refinement 5).
- **Density**: 64 graded cells on the standard ladder, 80 when the Team's own window is off it. The Epic's D2 called 64 marks unreadable and slice 02 declined a matrix. The maintainer has now asked for one; DISCUSS records the reversal and dogfoods on real history.
- **Colour collision**: the product's forecast colours (`ForecastLevel`) mean *confidence level* (Risky/Realistic/Confident/Certain), not *correctness*. Painting a correct cell in `certainColor` makes the same colour mean two things.

## 5. Open questions for DISCUSS

1. **Error formula**: margin relative to the actual (the maintainer's words) or to the forecast; what an actual of 0 shows; a minimum actual below which a percentage is not shown; whether the miss is also printed in Work Items.
2. **What counts as "correct" per level**: confirm correct = held (`actual >= value(P)`); how a 50% "incorrect" (expected half the time) and a 95% "correct by more than 25%" (under-forecasting, `AlwaysHeld`) read; whether the band thresholds (10% / 25%) are symmetric for over- and under-delivery.
3. **Fate of the shipped pieces**: the window sentence (rule A region), the four nominal-rate lines, the findings, the denominator copy, and the band-row evidence panels — kept in the dialog, folded into the text result, or retired; and whether rule A's region still leads now that levels do.
4. **Dialog entry point**: what stays in the Forecast Backtesting group (just the button? a one-line result?), whether the dialog opens on press or on result, re-run inside the dialog, and where `TeamForecastRealityCheckRun` fires (unchanged: after a result).
5. **Colour mapping**: reuse the forecast colours (and accept that they already mean confidence level) or use a separate correct/incorrect × margin scale; how unevaluable and not-tested cells look.
6. **Wording of the text result**: per level, per period, or both; the exact sentence shapes; no per-window tallies (E5); "held" not "beaten"; Brown's one-sided scoring attributed to him, the three-way departure owned as ours (D7).
7. **Accessibility of colour-only grading**: glyph or word per grade, table semantics for screen readers, dialog focus and keyboard behaviour, width of 16-20 forecast columns on small screens.
8. **Terminology**: configurable-term defaults only — Team, Work Item, Feature; "throughput" is renameable so it appears in no label or name; no "Epic", "Initiative" or "Story" in any user-facing text.
9. **Reversal on record**: DISCUSS states that the maintainer's ask supersedes slice 02's "no matrix" and D2's density ruling, and that I-a to I-d (`taste-evaluation.md` §2) still hold.
