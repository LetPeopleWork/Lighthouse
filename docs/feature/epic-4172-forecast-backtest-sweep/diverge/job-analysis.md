# Job Analysis — epic-4172-forecast-backtest-sweep

**Wave**: DIVERGE · Phase 1 (JTBD)
**Agent**: Flux (nw-diverger)
**Date**: 2026-09-22
**ADO**: Epic #4172 "The Full Monte" — state Planned, tag **Community**, priority 2

---

## 0. Reading confirmation

Prior-wave consultation performed by the orchestrator and **not redone here**:

| Artifact | State |
|---|---|
| `docs/product/` | exists — no migration gate |
| `docs/product/vision.md` | does not exist |
| `docs/project-brief.md` | does not exist |
| `docs/stakeholders.yaml` | does not exist |
| `docs/feature/epic-4172-*/discover/` | does not exist — DISCOVER deliberately skipped; problem already evidenced in `jobs.yaml` |
| `docs/product/jobs.yaml` | present, 135 jobs |
| `docs/product/outcomes/registry.yaml` | present |
| `docs/product/architecture/brief.md` | present, 872 KB — searched, never read whole |

Read by Flux in this phase, all at absolute paths under the worktree:

- `docs/product/jobs.yaml` lines 303-382, 1097-1146, 1195-1312, 1-120, 7230-7337 (paged; never read whole)
- `docs/product/personas/delivery-forecaster.yaml`, `flow-coach.yaml`, `forecasting-prospect.yaml`
- `docs/product/outcomes/registry.yaml` (lines 1-60, shape only)
- `Lighthouse.Backend/Lighthouse.Backend/API/ForecastController.cs` (lines 150-260)
- `Lighthouse.Backend/Lighthouse.Backend/Models/Team.cs` (via symbol search — the setting a proposal would write)
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastService.cs` (lines 28-60)
- `Lighthouse.Backend/Lighthouse.Backend/Models/Forecast/ForecastSimulationLimits.cs`
- `.../BackgroundServices/Update/UpdateQueueService.cs`, `UpdateType.cs`, `UpdateStatus.cs`
- `.../Services/Interfaces/Update/IUpdateQueueService.cs`, `IUpdateStatusStore.cs`
- `Lighthouse.Backend/Lighthouse.Backend/API/UpdateController.cs`
- `Lighthouse.Frontend/src/pages/Teams/Detail/TeamForecastView.tsx` (400-465), `BacktestForecaster.tsx` (signatures)
- `docs/product/architecture/adr-194-*.md`, `adr-195-*.md` (full), `adr-172-*.md` (headings)
- ADR index: 206 ADRs present, highest `adr-206` → **next free number was ADR-207**, confirmed (renumbered to 209 on 2026-09-24, because story 6053 had taken 207 and 208 in a parallel DESIGN wave).

**Skill loading**: `nw-jtbd-analysis` loaded, `nw-brainstorming` loaded, `nw-taste-evaluation` loaded.

`[SKILL MISSING] dataviz` — no `dataviz` or `nw-dataviz` skill exists under the accessible skills root
(`ctx_tree` refuses to scan `/home/benjamin/.claude/skills`; `ctx_read` on both candidate paths returns
file-not-found). **Substituted** with this project's own visual-honesty precedents, which are stronger
grounding anyway because they are about *this* chart family: ADR-194 (a blank region already carries a
meaning on this product's charts — see §5), ADR-188 (the pace-band ladder shared by chart geometry and
dialog value), ADR-205 (a chart answers one question at a time), ADR-020 (band rendering). The
substitution is declared rather than silently skipped.

---

## 1. Raw request (verbatim)

> Back-test a Team across a sweep of forecast horizons (2/4/6/8 weeks) x historical throughput windows
> (2 weeks / 30 / 60 / 90 days). Visualise each cell as under-forecast / over-forecast / within range.
> End with a PROPOSAL of how to configure the Team for best matches. Runs async in the background and
> reports back when done. Eventually a "Report" that could be emailed.

Source method: Nick Brown (ASOS), *The Full Monte*.
Working title: "The Full Monte" — the user has said it "may not be the best name =)".

This is **a proposed solution, not a job**. A sweep is a mechanism. A grid is a rendering. "Async" is an
implementation posture. Each is a guess. The job is extracted below.

---

## 2. Job extraction — the 5 Whys

**Activity observed**: a user runs a forecast back-test.

**Reject the activity as the job**: nobody wakes up wanting to run a back-test. Nobody wakes up wanting
to see sixteen coloured squares.

| # | Why | Answer |
|---|---|---|
| 1 | Why sweep at all, instead of running the one back-test that already ships? | Because a single back-test answers for one (horizon, window) pair, and the user does not know which pair to ask about. The shipped `POST /api/forecast/backtest/{teamId}` requires four dates as input — **you have to already know the answer to use the tool that gives you the answer.** |
| 2 | Why does the window matter enough to search over it? | Because `Team.ThroughputHistory` (int, days, **default 30**) is a setting *every* forecast the Team produces samples from — feature forecasts, team forecasts, portfolio delivery likelihood, SLE risk, the lot. Set it wrong and every forecast is wrong, in one consistent direction. |
| 3 | Why can't the forecaster tell that it is wrong today? | Because Lighthouse has **no feedback loop on its own output**. A forecast is issued, time passes, and nothing anywhere in the product ever says "the number you gave six weeks ago turned out to be high, low, or right". The forecast is unfalsifiable in normal use. |
| 4 | Why is the absence of that loop expensive? | Because the forecaster's standing rests entirely on the credibility of the number (`delivery-forecaster.yaml`: *"Cares ... more about whether the percentile dates it produces are HONEST — i.e. defensible to stakeholders who will hold them to the number"*). Without a loop, their only defence is "Monte Carlo is a good method" — an appeal to authority, not evidence about **this team**. |
| 5 | Why does that matter to the business? | Because the same missing evidence is exactly why a prospect does not believe probabilistic forecasting works at all. **The proof and the configuration are the same artifact.** That is why ADO #4172 is tagged Community with the stated reason "should convince people of the method, so they flock to use the tool". |

Stop condition reached: one more "why" produces "so that people plan work better", which is a
life-goal-shaped answer.

### Strip to the irreducible function

Remove every tool, every chart, every entity. What remains is:

> **Replay the model against held-out reality → read the error → adjust the model.**

Three steps. Calibration. This is the physical-level function; it is what a meteorologist, a quant and
an ML engineer all do, and it is what Nick Brown described.

The request's own words map onto it: *sweep* is the replay, *under/over/within* is the error read,
*proposal* is the adjust. **The request already contains all three steps — which is why it is a good
request even though it is phrased as a solution.** What the request does *not* contain is any account of
how honest the error read can be, which is where DIVERGE has to do its work.

### Abstraction level check

| Layer | Statement at that layer | Verdict |
|---|---|---|
| Tactical | "I want to see a 4x4 grid of coloured cells" | rejected — the raw request |
| Operational | "I want to run many back-tests without typing dates sixteen times" | rejected — still a workflow |
| **Strategic** | **"I want evidence that the configuration behind my forecasts matches how my team actually delivers, before I stake my credibility on a number it produces"** | **the job** |
| Physical | "Replay, compare, adjust" | the irreducible function |

**Gate G1 level check: PASS** — the job statement below is at strategic level and contains no reference
to sweeps, grids, cells, reports, async, or any other proposed mechanism.

---

## 3. Job statements

### Functional (required)

> **When I am about to publish a forecast from a Team whose sampling configuration I set once and have
> never checked, I want evidence from that Team's own completed history about whether that configuration
> would have got the last few periods right, so I can either stand behind the number or change the
> configuration before anyone anchors on it.**

### Emotional

> Move from *"Monte Carlo is a sound method, so this is probably fine — I set the window to whatever the
> default was"* to *"I have checked this against what my team actually did, and I can say what I checked
> and what it showed."*

The specific feeling being removed is not fear. It is **unexamined confidence** — which is why it does
not present as acute pain (see §7). The forecaster is not suffering; they are exposed and do not know it.

### Social

> Be the forecaster who, when a leader asks *"how do you know this is right?"*, answers with a
> reproducible check on the team's own history rather than with a methodology name.

And its top-of-funnel twin, stated openly rather than buried (see §7):

> Be the sceptic who was shown, on data they recognise, that the method held up — and therefore
> downloads the tool.

---

## 4. Disruption check

**Is there a higher-level job that would make this entire job unnecessary?**

Yes, and it is worth naming precisely because it is deliberately out of scope.

> *"I never think about the sampling window at all, because the tool keeps it correct."*

That job — continuous re-calibration with auto-adjusting Team settings — would dissolve this one
entirely. It is explicitly **out of MVP scope** (constraint C2: on-demand/manual only; scheduled re-runs
and auto-adjustment are named as possible later premium work).

The strategic consequence, which belongs in the DISCUSS conversation rather than being discovered later:

- **This feature is the manual, visible, inspectable precursor to the automatic one.** Nobody will let a
  tool silently rewrite the setting that drives every forecast they publish until they have watched it
  make that recommendation by hand a few times and agreed with it.
- Therefore the *legibility* of the recommendation is not a nicety — it is what earns the right to build
  the automatic version. A direction that produces a correct recommendation opaquely scores worse against
  the disruption path than one that produces the same recommendation legibly, even though both are
  "right" today.
- **Do not foreclose it.** Whatever is built must leave room for the same computation to run on a
  schedule and for its output to be applied without a human in the loop. Nothing in MVP should assume a
  browser is attached.

**No disruption risk from outside**: the alternative is a spreadsheet, and it is strictly worse (see §6).
There is no adjacent product that makes this job disappear.

---

## 5. Two prior findings this analysis composes with

These were extracted upstream and are **built on, not contradicted**.

### (a) The minimum-data bar is already set — and sufficiency is a *per-cell* property here

`forecast-minimum-data-guard` (jobs.yaml 1243-1312, ADO #5125, ADR-039) fixed the bar at **at least 5
distinct days with at least 1 completed work item** in the throughput window. Below it, every forecast
surface **suppresses** the number and shows a plain-language "not enough data yet". Constraint C5: this
feature composes with that rule and must never invent a second bar.

The wrinkle this feature introduces, which did not exist for any prior surface: **each cell carries its
own history window, so sufficiency varies within a single report.** A 2-week history window on a
low-volume Team fails the bar while the same Team's 90-day window passes. One artifact therefore contains
both answerable and unanswerable cells at the same time.

**This is a first-class design problem, not a rendering detail**, and ADR-194 is the reason. That ADR
retired the SLE-risk background ladder on the Work Item Aging chart because *a blank region on that chart
already meant something else* — the pace-percentile background leaves a region unpainted when history is
too thin, documented in `docs/metrics/flow-metrics.md`, so "calm" and "unknowable" rendered identically
and a reader could not tell which they were looking at. The same trap is set here, one level up: a cell
that is *within range* and a cell that *could not be evaluated* must not be confusable, and "leave it
empty" is the option ADR-194 already ruled out for this product.

**How each direction renders a disqualified cell is scored as a differentiator**, not assumed.

### (b) Honest framing — the lead-gen precedent

`job-assess-forecasting-flow-maturity` (jobs.yaml 1097-1146) is labelled in place as a top-of-funnel
**lead-gen instrument**: business-primary objective is qualify-and-route, the visitor's own job is graded
"genuine but light", and `opportunity_score` is openly conversion-weighted with the rationale saying so
out loud (importance **3**, satisfaction **2**, gap **1**).

Epic 4172 is tagged Community for the identical stated reason. **This job is framed the same honest way.**
It is not inflated into acute user pain. Concretely that means:

- Importance is **3**, not 5. A forecaster with a mis-set window is not blocked, not escalating, and not
  filing a ticket. They do not know.
- There is **no named customer** asking for this, unlike `filter-forecast-throughput` (Liz / JLP, Epic
  4896) or `forecast-minimum-data-guard` (Liz). Its absence is stated rather than papered over.
- The conversion objective is recorded as a **secondary** with real weight, not smuggled in as if it were
  user value.

---

## 6. Current alternatives (the habit being displaced)

| What people do today | Why it persists | Why it fails the job |
|---|---|---|
| Nothing. Accept the default `ThroughputHistory = 30`. | It is the default; nothing ever challenges it. | The modal case. The configuration is never examined, so the job is never consciously hired. |
| Run the shipped single-shot back-test (Team → Forecast → "Forecast Backtesting") | It exists and it works. | Demands four dates up front. You must already suspect an answer to ask the question, and a single pair proves nothing about the pair you did not try. |
| Export throughput to a spreadsheet and run Monte Carlo offline | Named in `delivery-forecaster.yaml` frustrations as the live workaround for the *adjacent* filter job. | Breaks the integrated forecast trail; is not repeatable; and — decisively — nobody does a **sweep** by hand, because sixteen manual Monte Carlo runs is not a thing a human does on a Tuesday. |
| Trust the method by reputation (Vacanti / ProKanban / "Monte Carlo is sound") | Socially adequate in most rooms. | Says nothing about *this* team. Collapses the moment a leader asks the second question. |

The third row is the important one: **the sweep has no manual equivalent.** That is unusual, and it is
the strongest single argument that the feature creates capability rather than saving effort — and also
why estimated satisfaction is low while estimated importance is only moderate.

---

## 7. ODI outcome statements

Format: `[Direction] + [Metric] + [Object] + [Context]`. No solution references, no compound statements,
no forbidden words.

| # | Outcome statement |
|---|---|
| **O1** | Minimize the time it takes to determine whether a Team's throughput sampling window produces forecasts that match what that Team actually delivered. |
| **O2** | Minimize the likelihood of publishing a forecast whose sampling window has never been compared against a completed period. |
| **O3** | Minimize the number of configuration attempts required before a Team's forecast settings match its observed delivery. |
| **O4** | Minimize the likelihood of ranking one sampling window above another when the comparison rests on periods that are not equivalent. |
| **O5** | Minimize the effort required to show a sceptical stakeholder that probabilistic forecasting held up on a Team's own history. |
| **O6** | Minimize the likelihood of reading a calibration result for a period whose history cannot support one. |

Six statements; the gate requires three.

### Opportunity scoring

Formula `Score = Importance + max(0, Importance - Satisfaction)`, 1-10 scale.

> **These figures are reasoned estimates, not survey results.** No customer interviews exist for this
> Epic (DISCOVER was skipped by design). They are derived from the shipped code, the catalogued sibling
> jobs, and the persona files, and each carries its basis. They are honest about being estimates and must
> not be quoted downstream as measured.

| Outcome | Importance | Satisfaction | Score | Status | Basis for the estimate |
|---|---|---|---|---|---|
| **O4** — do not rank incomparable windows | 8.2 | 1.5 | **14.9** | **Under-served** | Highest importance of the six *and* the lowest satisfaction: C4 makes every cell a different real-world period, nothing in the product guards against reading them as trials, and the most natural rendering of the request (a heatmap) actively invites the error. Sibling precedent: `forecast-confidence-cap` and `forecast-minimum-data-guard` both exist purely to stop an honest number being read dishonestly — this product already treats that class of harm as importance 4 of 5. |
| **O1** — time to know whether the window fits | 7.8 | 2.4 | **13.2** | **Under-served** | The literal ask. Satisfaction above floor because the single-shot back-test genuinely ships and genuinely works; it is the date-entry precondition that holds satisfaction near 2. |
| **O2** — never publish an unchecked window | 7.1 | 2.0 | **12.2** | **Under-served** | The unexamined-confidence case. Importance below O4 because the harm is diffuse and delayed rather than immediate. Satisfaction 2.0: technically checkable today, practically never checked. |
| **O5** — show a sceptic it held up | 6.9 | 2.2 | **11.6** | Under-served (marginal) | Conversion-weighted, and **declared as such** per finding (b). Importance is business-primary but user-secondary, so it sits below the three above by construction rather than by accident. |
| **O3** — attempts to reach a fitting configuration | 6.4 | 3.1 | **9.7** | **Over-served — do not invest** | Once O1 is answered, changing `ThroughputHistory` is one number in a settings field the user already knows. The diagnosis is scarce; the act of applying it is not. **This is the evidence that an "apply the proposal automatically" button is not MVP** — it optimises the step that is already cheap. |
| **O6** — do not read an unsupportable cell | 7.4 | 6.8 | **8.0** | **Over-served — do not invest** | High importance, but satisfaction is *already high* because `forecast-minimum-data-guard` shipped and suppresses exactly this across every forecast surface. **This is the evidence for C5**: compose with the shipped bar, build no new mechanism. The residual work is rendering the per-cell state (finding (a)), which is presentation, not capability. |

### What the scoring changes about the request

Three findings the raw request did not contain, each traceable to a row above:

1. **O4 outranks O1.** Staying honest about non-comparability scores higher than answering the question.
   A direction that answers the question and invites the false comparison is *worse than one that answers
   it more modestly*. This is the evidence base for weighting HONESTY highest in Phase 4 — the weight is
   derived, not preferred.
2. **O3 is over-served, so the auto-apply button is not MVP.** The user asked for "a button that will
   automatically adjust the team settings" — and it would be neat, but the scoring says the scarce thing
   is the diagnosis, not the keystroke. Directions are judged on whether they *enable* the decision, not
   on whether they *automate* it.
3. **O6 is over-served, so build nothing new for data sufficiency.** Reuse the shipped guard; spend the
   effort on how a disqualified cell *reads*, per ADR-194.

---

## 8. Elevated job for the SSOT

Appended to `docs/product/jobs.yaml` as **`job-forecaster-check-the-forecast-against-what-happened`**,
persona `delivery-forecaster`, feature_context `epic-4172-forecast-backtest-sweep`, with
`opportunity_score: importance 3 / current_satisfaction 1 / gap 2` on the repository's own 1-5 scale, and
a `note` that states the lead-gen framing in the open, as `job-assess-forecasting-flow-maturity` does.

The 1-5 repository figures and the 1-10 ODI figures above are two scales of one reading and do not
conflict: importance 3 of 5 corresponds to roughly 6-8 of 10 across the outcome set; satisfaction 1 of 5
reflects that the *job as a whole* is unserved even though one of its six outcomes (O6) is well served by
a shipped sibling.

**Secondary persona**: `forecasting-prospect` — the same artifact, read by someone who does not yet use
the product. Recorded on the job rather than given a job of its own, because inventing a second job for a
marketing read would be exactly the inflation finding (b) warns against.

**Not this persona**: `flow-coach` — their jobs are diagnostic (stuck items, blocked duration, pace
outliers), and forecast-model calibration is not among them. `config-admin` — they own the setting, but
owning a field is not the same as knowing what to put in it.

---

## 9. Gate G1 evaluation

| Criterion | Result |
|---|---|
| Job at strategic or physical abstraction level | **PASS** — §2 navigates tactical to operational to strategic; the statement in §3 contains no mechanism reference (no sweep, grid, cell, report, async, matrix, heatmap). |
| No feature references in the job statement | **PASS** — verified word by word against the §3 functional statement. |
| Minimum 3 ODI outcome statements | **PASS** — 6 produced, all in `[Direction][Metric][Object][Context]` form, all checked against the forbidden-word and forbidden-pattern lists. |
| Functional + emotional + social dimensions | **PASS** — all three in §3; the social dimension carries its declared top-of-funnel twin. |
| Disruption check performed | **PASS** — §4, with the strategic consequence for MVP scope recorded rather than merely noted. |

**G1: PASS.** Phase 2 (competitive research) may proceed.
