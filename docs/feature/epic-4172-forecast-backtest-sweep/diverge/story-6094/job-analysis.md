# Job Analysis — Story #6094 (Epic 4172)

**Wave**: DIVERGE, light pass · **Agent**: Flux (`nw-diverger`) · **Date**: 2026-09-26
**Story**: ADO #6094 "The reality check shows every forecast, the actual, and how right each one was"
**Work type**: brownfield redesign of a shipped UI (slices 01+02, pushed at 9851b4ea3). DISCOVER skipped by the maintainer.

## 0. Reading confirmation

| Read (fresh, paged) | What it contributed |
|---|---|
| `recommendation.md` (Epic) | M1 "one sentence, evidence on request"; three honesty requirements; verdict names a region, never a winner |
| `diverge/job-analysis.md` §1-§8 | Epic job, ODI O1-O6, O4 (do not rank incomparable windows) scored highest at 14.9 |
| `diverge/competitive-research.md` §1-§2 | Brown's scoring: correct = delivered at least the forecast; a separate margin shading at ±10% / 10-25% / >25%; percentile carried the signal, window carried none |
| `diverge/taste-evaluation.md` §1-§4 | Epic's substituted criteria; this pass uses the skill's own criteria instead (see taste-evaluation.md) |
| `slices/slice-01-…md`, `slices/slice-02-…md` | Horizons 7/14/28/56, windows 14/30/60/90 plus the Team's own; sweep is 16 or 20 runs; evidence = one panel per window, one band row per horizon; slice 02 OUT: "a matrix or a ranked list" |
| `feature-delta.md` D2, DES-6, DES-8 (ADR-210), DES-14 (rule A), DES-15, DES-16, Response Contract, Architecture Enforcement E1-E7 | held means `actual >= value(P)`, nominal rate = P; `AlwaysHeld` with a miss expected = under-forecasting; no `recommendedWindow`, no per-window scalar (E5); read-only enforced by ArchUnitNET (E1); no Apply control (D4) |
| `docs/product/jobs.yaml` 7342-7460 | `job-forecaster-check-the-forecast-against-what-happened` |
| Frontend: `ForecastRealityCheck.tsx`, `RealityCheckVerdict.tsx`, `RealityCheckEvidence.tsx`, `RealityCheckBandRow.tsx` (glob), `TeamForecastView.tsx:439-440`, `components/Common/Forecasts/ForecastLevel.ts` | What exists today (below) and what "the product's forecast colours" mean |

**What ships today**: inside `InputGroup "Forecast Backtesting"` a "Run reality check" button; on result, `RealityCheckVerdict` (window sentence, four nominal-rate lines, findings, denominator), a "Show the evidence" toggle, and `RealityCheckEvidence` (one fieldset per window, one `RealityCheckBandRow` per horizon, level lines repeated under the panels).

**Two facts from the code that DISCUSS needs, not visible from the story text**:

1. **The actual belongs to the period, not to the cell.** `scoredPeriodStart = anchorDate − horizonDays + 1`, `scoredPeriodEnd = anchorDate`: the scored period depends on the horizon only. Every window in one horizon is scored against the *same* actual. The table has 4 actuals, not 16 or 20.
2. **The product's forecast colours encode confidence, not correctness.** `ForecastLevel` maps a *probability* to Risky (≤50) / Realistic (≤70) / Confident (≤85) / Certain, each with a colour and an icon. Painting a cell "correct" in `certainColor` reuses a colour that already means "95% level" everywhere else in the product.

## 1. Raw request (verbatim, from the orchestrator)

> As a delivery forecaster, I want to see every forecast the reality check made, what the Team actually delivered, and how right each forecast was, so that I can judge how far to trust the numbers I quote.
> Popup dialog instead of inline ("inline is horrible"). A results table modelled on Nick Brown's "The Full Monte" … every period under test, the forecast at each confidence level for each history window, and the actual … Each forecast cell graded on Nick's scale: correct / incorrect × within <10%, 10-25%, >25% of the actual; coloured with the product's forecast colours. A neat result in text summarising what the grading says.

## 2. Job extraction

| # | Why | Answer |
|---|---|---|
| 1 | Why see every forecast next to its actual? | Because the sentence and level lines tell me *that* a level held, not *by how much* it missed or over-shot. |
| 2 | Why does the size of the miss matter? | Because "the 85% number held" is useless if it held by being 40% below what we delivered; I would be quoting a sandbag. |
| 3 | Why does that matter to me? | Because the number I quote is the number I am held to; I need to say how much slack or risk is in it. |
| 4 | Why can't I say that today? | The shipped check reads held / not held per level; magnitude sits unlabelled in a band row's tick position. |

Stop: one more why gives "to be trusted as a forecaster" — the Epic's social job.

| Layer | Statement | Verdict |
|---|---|---|
| Tactical | "Show a graded table in a dialog" | rejected, the request |
| Operational | "See each forecast next to what happened" | rejected, a workflow |
| **Strategic** | **"Know how often, and by how much, the number I quote has been right for this Team lately"** | **the job** |
| Physical | read the error (the middle step of replay → read the error → adjust) | irreducible |

**G1 level check: PASS.**

## 3. Is this a new job? No — the Epic's job, through the trust-calibration lens

The Epic job's functional dimension already reads *"read how far off it was … and, separately, which confidence levels actually held up"*, and DISCUSS recorded the confidence level as *"a choice of which number to say out loud"*. #6094 serves that half of the job, which slices 01+02 served only as held counts. Nothing about the situation, persona, or outcome is new; the story changes **how well the "read the error" step is served** (magnitude, per level). **`docs/product/jobs.yaml` is not edited.**

- **Functional**: when I am about to quote a forecast at some confidence level, I want to see how often and by how much that level's forecasts matched what this Team delivered in recent periods, so I can say how much slack or risk the number carries.
- **Emotional**: from "the 85% held, so it's safe" to "the 85% held, and it was usually within 10%, so it isn't padded".
- **Social**: answer "how sure are you?" with "our 85% number has been within 10% of what we delivered in most recent checks" rather than a percentile name.

**Disruption check**: a per-Team auto-selected confidence level would dissolve the job — declined by the maintainer 2026-09-22 (no per-Team default confidence level). Out of scope.

## 4. ODI outcome statements (story-specific)

| # | Outcome | Imp. | Sat. | Score | Status | Basis (reasoned estimate, not surveyed) |
|---|---|---|---|---|---|---|
| S1 | Minimize the time it takes to see how far each forecast landed from what the Team actually delivered | 7.5 | 2.5 | **12.5** | Under-served | Today magnitude is only a tick position inside a per-row normalised band (DES-6); no number is printed for it |
| S2 | Minimize the likelihood of mistaking an over-cautious forecast for an accurate one | 7.8 | 3.0 | **12.6** | Under-served | `AlwaysHeld` exists as a level reading, but per check "held" looks like success regardless of margin |
| S3 | Minimize the likelihood of reading a large relative miss into a small absolute difference | 6.8 | 2.0 | **11.6** | Under-served | A 7-day period with an actual of 3: one Work Item is 33%, "beyond 25%". Nothing guards this yet |
| S4 | Minimize the effort required to restate the check's finding to a stakeholder | 6.5 | 4.0 | **9.0** | Over-served | The shipped sentence and level lines already exist in quotable form |
| S5 | Minimize the likelihood of treating a level's expected misses as failures | 7.0 | 3.5 | **10.5** | Appropriately served | Level lines print held vs expected; a per-cell correct/incorrect grid could undo that (half the 50% column should be "incorrect") |

**Opportunity reading**: S2 and S1 lead — magnitude and over-caution are what the story adds. S3 and S5 are risks the maintainer's grading could *create*, so they score as guardrails. S4 over-served: the text result should be short, not a new copy system.

## 5. Brown's grading — what is sourced, what is not

Sourced (Epic research §1.2, readmedium mirror, confidence Medium-High on wording): correct = team completed the forecast number or more; incorrect = fewer; a second shading for margin of error at within ±10%, 10-25%, beyond 25%.

**Unverified — do not build on it without opening the article**: (a) whether the margin is measured relative to the actual or to the forecast; (b) how zero or tiny actuals were handled; (c) the exact colours and layout of his table; (d) whether it publishes per-team cell tables or only aggregates (already unverified in the Epic research). The maintainer's words say "of the actual"; that is the working assumption, not Brown's confirmed formula.

**Correct = held.** Brown's "correct" is exactly ADR-210's `held` (`actual >= value(P)`). The grading adds one new concept — the margin band — not two.
