# Taste Evaluation — epic-4172-forecast-backtest-sweep

**Wave**: DIVERGE · Phase 4 (Taste Evaluation)
**Agent**: Flux (nw-diverger)
**Date**: 2026-09-22

> **Weights were locked before any option was scored.** §2 was written and fixed before §4 was started.
> The weights are derived from the ODI opportunity table in `job-analysis.md` §7, not from a preference
> about which option should win. §6 reports what happens to the ranking when they move.

The six options under evaluation are the curated set from `options-raw.md` §7:

| # | Option | Lens |
|---|---|---|
| 1 | **E1 · Sweep in a breath** — synchronous, nothing stored, small multiples | Eliminate |
| 2 | **A1 · The Report object** — generic Report entity, Reports tab, 4x4 matrix | Adapt |
| 3 | **C1 · Front door to the back-test** — replaces the shipped surface's default state, ranked list | Combine |
| 4 | **M1 · One sentence, evidence on request** — server-authored claim, evidence behind a disclosure | Modify |
| 5 | **S1 · Where did reality land?** — percentile landing positions on one axis | Substitute |
| 6 | **R1 · Your setting, on trial** — verdict on the held configuration, remedy on failure | Reverse |

---

## 1. DVF filter

IDEO triage. Any option failing two or more lenses, or totalling below 6, is eliminated before taste
scoring.

| # | Option | **D**esirability | **F**easibility | **V**iability | Total | Verdict |
|---|---|---|---|---|---|---|
| 1 | E1 · Sweep in a breath | 4 | **5** | 4 | **13** | Survives |
| 2 | A1 · The Report object | 4 | 3 | 4 | **11** | Survives |
| 3 | C1 · Front door | **5** | 4 | 4 | **13** | Survives |
| 4 | M1 · One sentence | 4 | **5** | 4 | **13** | Survives |
| 5 | S1 · Where did reality land? | 3 | 4 | 4 | **11** | Survives |
| 6 | R1 · Your setting, on trial | 4 | 3 | 3 | **10** | Survives (closest to the floor) |

### Justification per score, where it is not obvious

- **E1 Feasibility 5.** The shipped `ForecastController.RunBacktest` is a **synchronous** action
  (`public ActionResult<BacktestResultDto> RunBacktest(...)`, no `async`/`await`) that already runs one
  `HowMany` plus two throughput reads inside the request. Every `TeamMetricsService` read goes through
  `GetFromCacheIfExists` keyed on the date range and computes over Work Items already in the database —
  **no work tracking system is contacted**. Sixteen cells is therefore a bounded multiple of something
  the product already does in a request, not a new class of work. This is the single most load-bearing
  measurement in this evaluation.
- **C1 Desirability 5.** The highest of the six, because it removes the documented reason the job is
  unserved: the shipped surface opens with four empty date pickers, so a user must already suspect an
  answer to ask the question. C1 replaces that opening state with answers and keeps the pickers for the
  user who wants them.
- **A1 Feasibility 3.** Entity plus an expand-only migration across every supported provider (via
  `CreateMigration`), a runner registry, a status vocabulary, two new surfaces, and a completion event —
  and the runner question recorded in `options-raw.md` §6 is genuinely open, not a detail.
- **S1 Desirability 3.** The answer is honest but arrives in a unit the user did not ask for. A
  forecaster asking "is my window right?" is handed "your actual landed at the 62nd percentile" and has
  to be taught how to read it before the answer helps.
- **R1 Feasibility 3.** Its stated surface was ADR-127's team-settings advisory channel. **RESOLVED after
  scoring, 2026-09-22 — and not in the direction anticipated.** ADR-127's status line says delivery was
  split to Story #5627 with one objection open; the code says the mechanism has since been **deleted**.
  `ValidationAdvisory.tsx` does not exist anywhere in `Lighthouse.Frontend/src`, and two comments in the
  tree say why: *"US 5612 removed the advisory channel: the only advisory any connector ever returned was
  withdrawn as unactionable at connection scope, so a field nothing writes was deleted rather than kept
  for a caller that might one day appear"* (`ConnectionValidationResult.test.ts:23-26`), and *"#5612
  deleted `SuccessWith` and the Advisory pair it wrote to once they had no producer left; this rung is
  built here rather than reviving them"* (`ServiceNowBoardVerdict.cs:37-41`).
  **The score is left at 3 rather than lowered**, because the channel was a convenience and not a
  requirement — R1 can render its verdict in a small card of its own, as M1 does. What the finding removes
  is the *upside* scenario in §6, not the option. Lowering the score on this news would flatter the
  winner, and the discipline against retroactive adjustment cuts both ways.
- **R1 Viability 3.** Nothing travels out of the product — no artifact, no screenshot worth taking — so
  the conversion objective that C1 (free tier) makes an honest secondary is not served at all.

**No option is eliminated.** That is reported rather than glossed: the DVF filter did not bite because
every option was generated against measured facts from the codebase rather than from imagination, so
none of them is technically implausible. R1 at 10 is the closest to the floor, and the reason is a single
unverified dependency rather than a structural problem.

---

## 2. Weights — locked before scoring

The generic taste criteria (T1 Subtraction, T2 Concept Count, T3 Progressive Disclosure, T4
Speed-as-Trust) are **replaced** by five domain criteria supplied with the task. This is a deliberate,
declared substitution, permitted by the skill on the condition that it is documented. Each replacement
criterion carries the generic principle it stands in for, so nothing from the framework is quietly
dropped:

| Criterion | Weight | Stands in for | Why this weight |
|---|---|---|---|
| **HONESTY** — resists the C4 non-comparability trap and the "lead-gen toy" critique | **30%** | T2 Concept Count + T3 Progressive Disclosure (a concept the user misreads is worse than one they have not met) | **Derived, not preferred.** ODI outcome **O4** — *minimize the likelihood of ranking one sampling window above another when the comparison rests on periods that are not equivalent* — scored **14.9**, the highest opportunity of the six, above the literal ask (O1, 13.2). The product already treats this class of harm as first-order: `forecast-confidence-cap` and `forecast-minimum-data-guard` both exist solely to stop an honest number being read dishonestly. |
| **DECISION-CHANGING** — the output enables a decision rather than displaying a grid | **25%** | Desirability, carried forward as a taste criterion | The irreducible function is *replay → compare → **adjust*** (`job-analysis.md` §2). An option that stops at "compare" has not done the job. Second rather than first because O3 (attempts to reach a fitting configuration) scored **9.7 — over-served**: the decision must be *enabled*, and automating the keystroke is not where the value is. |
| **CHEAP FIRST SLICE** — how quickly slice 01 ships and dogfoods against this project's own instance | **20%** | T4 Speed-as-Trust, plus feasibility | C1 (free tier) caps the investment envelope: this is a conversion instrument, and a conversion instrument that costs a quarter is mispriced. The project's SOLUTION EFFICIENCY rule (skip → reuse → stdlib → … → minimum code) applies with full force. |
| **LOAD-BEARING vs SPECULATIVE** — how much of the Report abstraction is genuinely needed now | **15%** | T1 Subtraction | YAGNI pressure, real but subordinate: the six parked Epics are a roadmap, not a commitment, and none is scheduled. Below CHEAP because a speculative abstraction is recoverable and a slow slice is not. |
| **MARKETING SURFACE FITNESS** *(declared secondary)* | **10%** | Viability | Finding (b): the Epic is tagged Community for the stated reason that it "should convince people of the method". Recorded openly, with real weight, following the `job-assess-forecasting-flow-maturity` precedent. **Deliberately not zero** (it would be dishonest) and **deliberately not decisive** (it is the business's job, not the user's). |

**Profile**: developer-tool-leaning, as the framework's Developer Tool column suggests, but re-cut around
this feature's own evidence rather than taken off the shelf.

Locked: 2026-09-22, before §4.

---

## 3. Scoring rubrics

So that the numbers in §4 are auditable rather than asserted.

**HONESTY (1-5)** — 5: structurally incapable of presenting the sixteen results as comparable trials,
and gives a disqualified result a treatment that cannot be confused with any outcome. 3: honest if the
copy is written carefully, but the default rendering does not enforce it. 1: the visual grammar itself
asserts a comparison the data cannot support.

**DECISION-CHANGING (1-5)** — 5: the output names the change and the control that makes it, in the first
interaction. 3: names the change; acting on it is a separate journey. 1: displays results and stops.

**CHEAP FIRST SLICE (1-5)** — 5: no migration, no new entity, no new surface; dogfoodable against this
project's own instance the day it is written. 3: one migration or one new surface. 1: entity, migration,
runner, multiple surfaces, and an open architectural question.

**LOAD-BEARING vs SPECULATIVE (1-5)** — 5: nothing is built that does not have a caller today. 3: one
speculative element with a named revisit trigger. 1: a general mechanism built for one caller against
unscheduled future callers.

**MARKETING SURFACE FITNESS (1-5)** — 5: produces something a sceptic would screenshot, send, or ask
about. 3: demonstrable in a talk but not distinctive. 1: invisible outside the product.

---

## 4. Scoring matrix

| # | Option | HONESTY (30%) | DECISION (25%) | CHEAP (20%) | LOAD-BEARING (15%) | MARKETING (10%) | **Weighted** | Rank |
|---|---|---|---|---|---|---|---|---|
| 4 | **M1 · One sentence, evidence on request** | 3 | **5** | **5** | **5** | 4 | **4.30** | **1** |
| 6 | **R1 · Your setting, on trial** | **5** | **5** | 3 | 4 | 2 | **4.15** | **2** |
| 1 | **E1 · Sweep in a breath** | 4 | 3 | **5** | **5** | 4 | **4.10** | **3** |
| 5 | S1 · Where did reality land? | **5** | 2 | 3 | **5** | **5** | **3.85** | 4 |
| 3 | C1 · Front door to the back-test | 2 | 4 | 4 | 4 | 3 | **3.30** | 5 |
| 2 | A1 · The Report object | 2 | 3 | 1 | 1 | 3 | **2.00** | 6 |

Arithmetic, shown so it can be checked:

```
M1 = .30(3) + .25(5) + .20(5) + .15(5) + .10(4) = .90 +1.25 +1.00 + .75 + .40 = 4.30
R1 = .30(5) + .25(5) + .20(3) + .15(4) + .10(2) = 1.50 +1.25 + .60 + .60 + .20 = 4.15
E1 = .30(4) + .25(3) + .20(5) + .15(5) + .10(4) = 1.20 + .75 +1.00 + .75 + .40 = 4.10
S1 = .30(5) + .25(2) + .20(3) + .15(5) + .10(5) = 1.50 + .50 + .60 + .75 + .50 = 3.85
C1 = .30(2) + .25(4) + .20(4) + .15(4) + .10(3) = .60 +1.00 + .80 + .60 + .30 = 3.30
A1 = .30(2) + .25(3) + .20(1) + .15(1) + .10(3) = .60 + .75 + .20 + .15 + .30 = 2.00
```

---

## 5. Score breakdown — why each cell reads as it does

### HONESTY

- **R1 = 5.** The only option that **cannot** present a false comparison, because it never ranks. It
  judges the configuration the Team already holds and offers one alternative on failure — and two things
  can be compared honestly in a way sixteen cannot. A Team whose own window cannot be evaluated gets
  "cannot be checked yet" as a first-class verdict, not an absence.
- **S1 = 5.** The only option whose **unit of analysis is the set of landings**, not the individual cell.
  It reports dispersion and centring, which is what a calibration reading honestly supports, and it never
  asserts an ordering. A tick with no dot and an explicit caption is unmistakably distinct from any
  outcome, satisfying ADR-194.
- **E1 = 4.** Four small-multiple panels make four separate statements rather than one statement about a
  coordinate system. Not 5, because four panels side by side still invite "which panel looks best", and
  nothing structural prevents it — only the copy does.
- **M1 = 3.** The sentence can be written honestly and it names the disqualified checks in prose. But the
  default is to **withhold the evidence**, which is the "black box / lead-gen toy" exposure — and
  `job-analysis.md` §4 establishes that legibility is precisely what earns the right to build the
  automatic version later. **This is the recommendation's critical weakness and is flagged as such in
  `recommendation.md`.** It is also the single most sensitivity-prone number in this matrix (§6).
- **C1 = 2.** A ranked list is a league table. It is the most explicit possible statement that the
  sixteen results are commensurable, and C4 says they are not. Good on disqualified cells (a neutral
  chip), bad on the thing that matters more.
- **A1 = 2.** A matrix is a coordinate system; that is what a matrix *means*. Naming the real date span
  in each header mitigates but does not undo it — the reader's eye scans the grid before it reads the
  headers. This is the ADR-194 trap repeated one level up: a rendering whose grammar says something the
  data does not support.

### DECISION-CHANGING

- **M1 = 5** and **R1 = 5.** Both put the named change and the control that makes it in the first
  interaction. Both are cheap to do so, because **`ThroughputQuickSetting` already exists** in the Team
  detail header, on every tab, already writing `throughputHistory` with validation — so "apply" is a
  pre-filled open of a shipped control, not new machinery.
- **C1 = 4.** The answer appears where the user already goes, and the drill-down lets them interrogate
  the claim *before* acting — which, per the disruption argument, is what earns trust for the automatic
  version.
- **E1 = 3.** Names a window; apply is explicitly deferred to slice 02.
- **A1 = 3.** Has a headline block, but it sits behind a Reports tab the user has to discover and open.
- **S1 = 2.** "The 60-day window's landings are the most centred" requires the user to make the
  inferential step themselves, in an unfamiliar unit.

### CHEAP FIRST SLICE

- **M1 = 5, E1 = 5.** No entity, no migration, no new surface, no queue. The recommendation logic is
  common to all six options and therefore not a differentiator between them.
- **C1 = 4.** No new surface and a genuine reuse of the shipped controlled props — but the cached row on
  the Team is a migration, and per the project's expand-only rule that is a real, if small, cost.
- **S1 = 3.** The arithmetic is trivial; the chart and the sentence of copy that teaches the scale are
  not, and a new inverse lookup on `HowManyForecast` is needed.
- **R1 = 3.** Held down by the ADR-127 / Story #5627 dependency being **unverified**. Rises to 4 if
  #5627 has shipped — a check that costs minutes and should be done in DISCUSS.
- **A1 = 1.** Entity, expand-only migration across every provider, runner registry, status vocabulary,
  two surfaces, a completion event, and the open runner question.

### LOAD-BEARING vs SPECULATIVE

- **M1 = 5, E1 = 5, S1 = 5.** None of them builds any part of the Report abstraction. E1 goes further and
  makes the non-building an explicit, recorded decision with a named revisit trigger.
- **R1 = 4.** A small verdict record, needed today so a stale verdict can be seen to be stale.
- **C1 = 4.** One cache row, load-bearing for page navigation, nothing else.
- **A1 = 1.** The definition of speculative: a general mechanism, its registry, and its lifecycle, built
  for exactly one caller, against six parked Epics of which **none is scheduled**. The SOLUTION
  EFFICIENCY rule's first step is *skip (YAGNI)*, and this option skips the skip.

### MARKETING SURFACE FITNESS

- **S1 = 5.** "Where did reality land" is a genuinely novel picture in this market and the most credible
  thing to put in front of a Vacanti-literate audience, because it is the form that field already
  recognises as the honest answer.
- **M1 = 4, E1 = 4.** A quotable sentence and four clean panels; the readiness-assessment precedent shows
  that one number plus one named band travels further than a wall of sub-metrics.
- **A1 = 3, C1 = 3.** A Reports tab is a feature story, but a grid and a ranked list both screenshot as
  "busy".
- **R1 = 2.** A health chip is invisible outside the product. This is R1's real cost and the reason it
  does not win.

---

## 6. Sensitivity analysis

The top three sit within **0.20** of each other. That is a near-tie, not a verdict, and it is reported
rather than presented as a clear win.

**Sensitivity 1 — M1's HONESTY score.** It is the number the ranking turns on.

| M1 HONESTY | M1 weighted | Rank |
|---|---|---|
| 2 | 4.00 | 3rd (behind R1 and E1) |
| **3 (as scored)** | **4.30** | **1st** |
| 4 | 4.60 | 1st, decisively |

A single point on one criterion moves M1 from first to third. The mitigation named in
`recommendation.md` is what would legitimately move it from 3 to 4, and **it is a mitigation to be built,
not a score to be assumed.**

**Sensitivity 2 — the HONESTY weight.** If O4's dominance were read as justifying 40% (with DECISION
falling to 15%):

| Option | Weighted at HON 40 / DEC 15 | Rank |
|---|---|---|
| E1 | **4.20** | 1 |
| R1 | 4.15 | 2 |
| M1 | 4.10 | 3 |

The ranking inverts. **The three top options are therefore better understood as one cluster with
different emphases than as a ranked list**, and DISCUSS should treat the choice among them as live.

**Sensitivity 3 — R1's feasibility dependency. RESOLVED 2026-09-22, and it closes rather than opens.**
The scenario was: if Story #5627 has shipped, R1's Feasibility rises to 4 and CHEAP to 4, giving
**4.35 — first place**. The check was run against the code. **#5627 did not ship, and Story #5612
subsequently deleted the advisory channel entirely**, with a later decision explicitly declining to revive
it (citations in §1). **So the scenario in which R1 overtakes the winner does not exist.** R1 stays at
**4.15, second**.

This is recorded rather than quietly absorbed, because it improves the recommendation's standing on a fact
discovered after scoring, and that is exactly the kind of move that deserves to be visible. No score was
changed; an open branch was closed.

**What no sensitivity changes**: A1 and C1 stay last and second-to-last under every weighting tried,
because they lose on the highest-weighted criterion for structural reasons that no weight can repair —
a matrix and a ranked list both assert a comparison C4 forbids.

---

## 7. Limitations of this evaluation

Recorded so a reviewer does not have to find them.

1. **E1 and M1 are the thinnest pair in the set.** They differ in mechanism (M1's output is a
   server-authored claim; E1's is rendered data) and in assumption (M1 assumes the evidence goes unread;
   E1 assumes it is read), and their cost profiles are close. They pass the three-point diversity test,
   but by the narrowest margin of any pair, and a reviewer is entitled to read M1 as "E1 with the
   evidence collapsed". The recommendation is built on that reading rather than against it.
2. **The opportunity scores the weights derive from are reasoned estimates, not survey data**
   (`job-analysis.md` §7 says so in place). DISCOVER was skipped by design. The weights are therefore
   defensible, not measured.
3. ~~One factual dependency is unverified: whether ADR-127's team-surface advisory rendering (Story #5627)
   has shipped.~~ **RESOLVED 2026-09-22**: it did not ship, and Story #5612 deleted the advisory channel
   altogether. The branch in which R1 overtakes the winner is closed. See §1 and §6.
4. **One performance claim is inferred, not measured**: that sixteen cells complete inside an acceptable
   request budget. The inference is strong — the shipped single-shot back-test is already a synchronous
   controller action over cached, locally-computed data with no tracker I/O — but it is an inference. It
   is the first thing slice 01 should measure, and it is recorded as the recommendation's key risk.
5. **No option was eliminated by DVF.** §1 explains why and names the closest to the floor.

---

## 8. Gate G4 evaluation

| Criterion | Result |
|---|---|
| DVF filter applied to all options, eliminations documented | **PASS** — §1; no eliminations, with the reason and the closest-to-floor option stated |
| Weights locked before scoring | **PASS** — §2, locked and derived from the §7 ODI table of `job-analysis.md`, with each weight's basis named |
| All surviving options scored on all criteria | **PASS** — §4, 6 options x 5 criteria, no gaps |
| Weighted ranking complete | **PASS** — §4, with the arithmetic shown |
| Per-criterion breakdown | **PASS** — §5 |
| Recommendation traceable to scores | **PASS** — `recommendation.md` follows the matrix and flags the winner's weakest criterion explicitly rather than re-weighting to hide it |
| Dissenting case included | **PASS** — `recommendation.md`, R1 at 4.15 |
| Sensitivity reported | **PASS** — §6, three sensitivities, two of which change the winner |

**G4: PASS.**
