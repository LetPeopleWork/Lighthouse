# Wave Decisions — epic-4172-forecast-backtest-sweep

ADO Epic **#4172** "The Full Monte" — Planned, tag **Community**, priority 2.
Source method: Nick Brown (ASOS), *The Full Monte*, ASOS Tech Blog, Jan 2024.

---

## DIVERGE

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-09-22 · **Interaction mode**: autonomous (subagent)
**Predecessor**: DISCOVER deliberately skipped — the problem was already evidenced in
`docs/product/jobs.yaml`, so Phase 1 elevated catalogued jobs rather than running discovery.
**Successor**: DISCUSS (`nw-product-owner`, Luna).

### Artifacts produced

| Path (relative to the feature workspace) | What it holds |
|---|---|
| `diverge/job-analysis.md` | Elevated strategic job, 5-Why chain, 6 ODI outcome statements with opportunity scores, disruption check, G1 gate |
| `diverge/competitive-research.md` | 32 external + 5 local sources; the source method reconstructed; 10 named products; 4 non-obvious categories; naming study; G2 gate |
| `diverge/options-raw.md` | HMW, all 7 SCAMPER lenses, 4 Crazy 8s, curation to 6, diversity test, G3 gate |
| `diverge/taste-evaluation.md` | DVF filter, locked weights with derivation, 6x5 scoring matrix, per-criterion breakdown, 3 sensitivity analyses, G4 gate |
| `recommendation.md` | Top 3, dissenting case, decision statement, ADR-209 framing, carpaccio shape |
| `diverge/review.yaml` | Peer-review verdict (Prism, `nw-diverger-reviewer`) |
| `wave-decisions.md` | this file |

### SSOT updates

`docs/product/jobs.yaml`:

1. `updated:` 2026-09-21 → **2026-09-22**
2. `feature_context:` += `epic-4172-forecast-backtest-sweep`
3. New job appended at the end of the `jobs:` list:
   **`job-forecaster-check-the-forecast-against-what-happened`** — persona `delivery-forecaster`,
   `created: 2026-09-22`, `opportunity_score: importance 3 / current_satisfaction 1 / gap 2`.

*Changelog convention note*: `jobs.yaml` carries no separate changelog block. Its changelog mechanism is
the file-level `updated:` date, the `feature_context:` entry, and the per-job `created:` + `note:` fields
— all three were written. No `docs/product/CHANGELOG*` file exists; none was invented.

The job's `note:` records the honest framing in the open, following the
`job-assess-forecasting-flow-maturity` precedent: business-primary objective is demonstrating the method,
the forecaster's own job is genuine but light, and **there is no named customer asking for this** — stated
rather than papered over.

---

## Decisions taken in this wave

### DV-1 — The job is elevated, not discovered

The request ("sweep a grid, visualise cells, propose a configuration") is a solution. The job extracted
and catalogued is: *"When I am about to publish a forecast from a Team whose sampling configuration I set
once and have never checked, I want evidence from that Team's own completed history about whether that
configuration would have got the last few periods right, so I can either stand behind the number or
change the configuration before anyone anchors on it."*

Irreducible function: **replay → compare → adjust**. The request contains all three steps — which is why
it is a good request — but contains no account of how honest the *compare* can be, which is where the
wave did its work.

### DV-2 — The job is framed honestly as conversion-weighted, not inflated

Importance **3**, not 5. Nobody is blocked; they do not know the window is mis-set. No named customer.
The Community tag's stated purpose ("convince people of the method") is recorded as a real secondary
objective with real weight (10% of the taste matrix), not smuggled in as user value.

### DV-3 — Staying honest about non-comparability outranks answering the question

ODI outcome **O4** (*minimize the likelihood of ranking one sampling window above another when the
comparison rests on periods that are not equivalent*) scored **14.9**, above **O1** (*time to determine
whether the window fits*, **13.2**). The 30% HONESTY weight in Phase 4 derives from this, not from
preference.

Corroborated independently in Phase 2: Bailey et al. (*Notices of the AMS*, 2014) show that grid-searching
N configurations against one finite historical sample makes the argmax expected-noise well below N = 16
for a sample of months.

### DV-4 — Two over-served outcomes changed the scope

- **O3** (*attempts to reach a fitting configuration*) scored **9.7 — over-served.** Changing
  `ThroughputHistory` is already one number in a control the user owns. **Therefore the auto-apply button
  is not where the value is**; directions were judged on whether they *enable* the decision, not automate
  it. (Apply still ships, because it is nearly free — `ThroughputQuickSetting` already exists.)
- **O6** (*don't read an unsupportable cell*) scored **8.0 — over-served**, because
  `forecast-minimum-data-guard` shipped. **Therefore build no new data-sufficiency mechanism** (C5
  confirmed by evidence, not just by fiat). The residual work is how a disqualified cell *reads*, governed
  by ADR-194.

### DV-5 — The recommended direction

**M1 · "One sentence, evidence on request"**, weighted **4.30**, shipped as **Forecast Reality Check**:
synchronous, one-click, no new entity, in the existing "Forecast Backtesting" group on the Team's Forecast
tab. Verdict names a *region* of acceptable windows; sixteen results one click behind it as small
multiples; Apply pre-fills the existing header `ThroughputQuickSetting`.

Conditional on three honesty requirements being built (region-not-winner; denominator +
non-comparability copy; coverage against a printed nominal percentile). **If DISCUSS will not commit to
all three, the decision changes to R1.**

### DV-6 — Dissent recorded: R1 · "Your setting, on trial" (4.15)

Scores 5/5 on the two highest-weighted criteria and is the only option *structurally incapable* of the
false comparison. Lost on MARKETING (2) and CHEAP (3).

One cheap check could have flipped it to first place — *has Story #5627 (ADR-127's team-settings advisory
channel) shipped?* **It was run, and it went against the dissent.** #5627 did not ship, and **Story #5612
deleted the advisory channel entirely**: `ValidationAdvisory.tsx` is absent from the whole frontend, and
two comments in the tree say so and say why (`ConnectionValidationResult.test.ts:23-26`,
`ServiceNowBoardVerdict.cs:37-41`), the second explicitly building a later rung *"rather than reviving
them"*. **R1's score was deliberately left at 4.15 rather than lowered** — the channel was a convenience,
not a requirement, and marking a losing option down on a post-scoring fact would flatter the winner.

### DV-7 — No Report abstraction. ADR-209 records the position, not an implementation

Shape (a) on the commitment spectrum — nothing stored, no entity, no migration, no `UpdateType` member,
no runner registry. The project's SOLUTION EFFICIENCY rule at its first step (*skip / YAGNI*).

ADR-209 must nevertheless decide more than "not yet": the accepted consequence (no history, nothing to
email), the named revisit trigger (**the second Report kind**, not a date), the six questions the eventual
Report ADR will face, the runner question with the ADR-195 single-lane measurement behind it, and the
forward-compatibility constraint that the result shape must not hard-code a single Team.

Touches ADR-195, ADR-194, ADR-039, ADR-172, ADR-162, ADR-127, ADR-181/182/186, ADR-046, ADR-145.
Next free number **ADR-207** confirmed (206 ADRs present, highest `adr-206`); renumbered to 209 on 2026-09-24, because story 6053 had taken 207 and 208 in a parallel DESIGN wave.

### DV-8 — The name

**User-facing: "Forecast Reality Check"** (in-app "Reality Check"). **Internal codename: "The Full
Monte"**, kept, with attribution in the launch post. Runner-up "Hindsight".

Rejected: **"Forecast Calibration"** — borrows a word whose standard (reliability diagrams over many
trials) this artifact cannot meet. **"The Full Monte"** as a *user-facing* name — "Full" claims
completeness on sixteen non-comparable cells, it is a gambling pun on a probabilistic feature, and it is
Nick Brown's article title.

**C6 extended by this wave**: configurable terms — including **throughput**, Team, Delivery, Cycle Time,
WIP, Blocked, SLE — are unsafe *inside a feature name*, because they render as the user's own word. One
option's name candidate was corrected on this ground.

### DV-9 — Two research findings escalated to DISCUSS

1. **Epic 4172 sweeps the axis that carried no signal and omits the one that carried all of it.** Brown's
   study swept horizon × window × **percentile**; percentile moved the correct-rate 68% → 90% while the
   window moved it by four points of noise, a finding Brown restated himself in 2025. **Consequence: the
   feature must be able to return a null result** — "your window barely matters in this range" — which a
   ranked list or a heatmap with a gold cell cannot say.
2. **Brown's scoring is one-sided** (over-delivery counts as correct). The Epic's under / over / within
   trichotomy is a **deliberate departure**, arguably more honest, but must never be attributed to him.

### DV-10 — Defect found in passing, escalated separately

`Team.ThroughputHistory` defaults to **30** (`Team.cs:17`) while `CreateTeamWizard.tsx:35` and
`EditTeam.tsx:80` both seed **90**. The product disagrees with itself about the right default.
**Raise as its own ADO item — do not fold it into this Epic.** It is also, incidentally, an argument for
the feature: nobody currently knows which value is right.

---

## Constraints honoured (none reopened)

| | Constraint | How |
|---|---|---|
| C1 | Free / Community | No premium gate proposed; conversion recorded as a declared 10% secondary criterion |
| C2 | On demand / manual only | One button. No schedule, no continuous mechanic. The auto-adjusting future is named in the disruption check as explicitly out of scope and deliberately not foreclosed |
| C3 | Filter is a run config, not a sweep axis | The request body carries at most `applyFilterOverride`; the grid stays horizon × window |
| C4 | Single END anchor at today; cells not comparable | No date picker anywhere — which is what makes one-click viable. Non-comparability is handled by three build requirements, not a caption. C4 is **not** reopened; the research showing every adjacent field rolls the origin is recorded as a *measured cost* and a later-slice candidate (R-7) |
| C5 | Compose with the shipped 5-active-days bar | No second bar. Per-cell sufficiency is a *rendering* problem governed by ADR-194. Research confirmed the shipped guard is a market differentiator worth protecting |
| C6 | Configurable terminology | Extended and applied to names (DV-8) |

---

## Open items handed to DISCUSS

| # | Item | Cost to resolve |
|---|---|---|
| R-1 | Does the sixteen-cell run fit an acceptable request budget? The recommendation's load-bearing assumption. | One hour — slice 01, before any UI |
| R-2 | ~~Has Story #5627 shipped?~~ **RESOLVED in-wave: no — and Story #5612 deleted the advisory channel altogether.** Side finding: **ADR-127 is stale**, describing a mechanism no longer in the tree and carrying no note saying so. Worth its own status correction. | Done |
| R-3 | Which percentile does a cell score against? (Recommended: 85th, printed.) | A decision |
| R-4 | Is the three-way verdict kept, given it departs from the source method? | A decision |
| R-5 | Confirm the source article from medium.com directly — all of research §1 is mirror-sourced | One human page-load |
| R-6 | The `ThroughputHistory` 30-vs-90 default disagreement (DV-10) | Its own ADO item |
| R-7 | Rolling-origin evaluation as a later slice, if the honesty mitigations prove insufficient in use | Recorded, not scheduled |

---

## Gates

| Gate | Phase | Verdict |
|---|---|---|
| G1 | JTBD | **PASS** — strategic-level job, no mechanism references, 6 ODI statements (3 required) |
| G2 | Competitive research | **PASS** — 10 named products, 4 non-obvious categories, every claim cited, 3 claims explicitly UNVERIFIED |
| G3 | Brainstorming | **PASS** — all 7 SCAMPER lenses, 4 supplements, 6 curated, diversity test with the two closest pairs checked explicitly and the narrowest margin declared |
| G4 | Taste evaluation | **PASS** — DVF applied, weights locked and derived before scoring, 6x5 matrix complete, arithmetic shown, 3 sensitivity analyses of which 2 change the winner |
| Peer review | Prism (`nw-diverger-reviewer`) | **APPROVED** — all five dimensions PASSED, 0 blocking issues, 1 iteration of a permitted 2. Full record, including three factual defects found *in the review itself* and the in-wave resolution of R-2, in `diverge/review.yaml`. |

---

## Tooling notes for the next wave

- **`[SKILL MISSING] dataviz`** — no `dataviz` or `nw-dataviz` skill exists under the accessible skills
  root. Substituted with this project's own visual-honesty precedents (ADR-194, ADR-188, ADR-205,
  ADR-020), declared in `job-analysis.md` §0. DESIGN may want the same substitution.
- **`ctx_search` skips `brief.md`** for size, so the absence of an existing `Report` concept from the
  architecture brief could not be confirmed by search. The 206 ADR titles contain none. **DESIGN should
  confirm before ADR-209 is written.**
- **`@mui/x-charts` 9.0.1 is the only charts package installed**; there is no `@mui/x-charts-pro`, so a
  Heatmap component is unavailable. A sixteen-cell matrix needs no charting component regardless.

---

## DISCUSS

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-09-22 · **Interaction mode**: autonomous (subagent)
**Predecessor**: DIVERGE (complete, peer-approved). **Successor**: DESIGN (`nw-solution-architect`).
**Density**: lean, `expansion_prompt: ask-intelligent`.

> **This section records the FIRST DISCUSS pass and is partly superseded.** The maintainer dropped the
> Apply control later the same day. Where this section says Apply ships, that a slice 03 carries it, that
> there are four slices, or that the per-Team confidence level is "escalated as its own ADO item", read
> **DISCUSS — revision, 2026-09-22** at the foot of this file instead. It is left unedited rather than
> rewritten so the decision history stays legible.

### Artifacts produced

| Path (relative to the feature workspace) | What it holds |
|---|---|
| `feature-delta.md` | All Tier-1 DISCUSS sections — persona ids, JTBD with the ODI table, surface inventory, D1-D13, scope assessment, four user stories with Elevator Pitches and embedded ACs, story map with priority rationale, out of scope, the project DISCUSS checklist, outcome KPIs, pre-requisites, DoR, DoD, SSOT updates, risks, Tier-2 trigger evaluation |
| `slices/slice-01-one-sentence-about-your-sampling-window.md` | The probe, the endpoint, the verdict with all three honesty requirements, the card |
| `slices/slice-02-the-evidence-you-can-look-at.md` | Four small-multiple panels, the unevaluable-row state, the nominal-rate lines |
| `slices/slice-03-change-it-here-or-be-told-there-is-nothing-to-change.md` | Apply, conditional; the two-axis asymmetry copy |
| `slices/slice-04-the-answer-travels.md` | The Markdown one-pager — severable |

### SSOT updates

| File | Change |
|---|---|
| `docs/product/journeys/epic-4172-forecast-reality-check.yaml` | **Created.** One journey, five steps, emotional arc, nine resolved design decisions, TUI mockups, per-step failure modes, shared-artifact registry, integration validation |
| `docs/product/jobs.yaml` | `job-forecaster-check-the-forecast-against-what-happened` — `dimensions.functional` **corrected** (it said the answer names the window that "would have fitted best", which names a winner and is what O4 exists to prevent), and a DISCUSS note appended recording the four-percentile decision, the absent percentile setting, the owned departure from Brown, and the no-persistence position. Patched by anchor; the 7432-line file was not rewritten |
| `docs/product/personas/delivery-forecaster.yaml` | The SSOT job appended to `primary_jobs` — DIVERGE created the job but never linked it from the persona |
| `docs/product/personas/forecasting-prospect.yaml` | The same job appended, marked as the secondary read |

### Decisions taken in this wave

Full text in `feature-delta.md` under Locked Decisions. In brief:

- **[D1]** Score all four confidence levels (50/70/85/95), not Brown's three and not one fixed 85th. Free
  — `CreateForecastDtos(50, 70, 85, 95)` already runs on every backtest — consistent with every
  neighbouring surface, and the 95th is the teaching cell for the nominal-rate lesson.
- **[D2]** The confidence level is a **position in a band**, not a fifth panel. A Monte Carlo forecast read
  at four percentiles is one distribution, not four forecasts. Four panels stay four; the confidence
  dimension costs zero panels and zero controls. This is the wave's main UX problem and it dissolves.
- **[D3]** The printed denominator states **two** numbers and the correlation between them — 16 runs, 64
  scores, four levels per run sharing one simulation. A better disclosure than §4.2 asked for, and it
  exists only because the percentile override forced the question.
- **[D4]** **Two verdicts, one button.** Searched the tree: there is **no Team setting for the forecast
  confidence level.** `Team` has no percentile field; `ServiceLevelExpectationProbability` is a cycle-time
  SLE consumed only by `GetSleRiskForTeam`. So the artifact says the asymmetry out loud — the sampling
  window is a setting and has a control, the confidence level is a choice of which number to quote and has
  none. Whether such a setting *should* exist is **escalated as its own ADO item**, not invented here.
- **[D5]** Apply appears only when the current window is outside the sound region.
- **[D6]** Today is the end anchor; no date picker. Cells are not repeated trials and must not be ranked.
- **[D7]** The three-way verdict is kept and the departure from Brown is owned out loud, in an AC.
- **[D8]** No Report abstraction. ADR-209 is flagged as a **DESIGN-wave deliverable**; Luna does not write it.
- **[D9]** Per-cell sufficiency reuses `ForecastDataSufficiencyPolicy.HasEnoughData` unchanged
  (`MinimumActiveDays = 5`). No second bar.
- **[D10]** Name: user-facing **Forecast Reality Check**; internal codename **The Full Monte**, attributed.
- **[D11]** **Re-sliced.** `recommendation.md` §5.5's slices 01 and 02 were both `@infrastructure`-only with
  no user-visible value, which this workflow hard-gates on. Merged into one value-bearing slice 01, with
  both things Flux was protecting preserved: the R-1 probe still runs first, and all three honesty
  requirements still land in the first slice — they *are* the sentence, so they cannot be dropped under UI
  pressure. Four slices result, every one user-visible, slice 04 severable.
- **[D12]** No CLI/MCP client exposure in this Epic, with the precondition for later recorded.
- **[D13]** R-6 out of scope with its own ADO item.

### Scope Assessment: PASS — 4 stories, 2 modules, estimated ~20h (≈3 days)

Assessed before journey visualisation, per the early gate. No oversized signal fires: 4 stories (not >10),
2 modules (not >3), no walking skeleton (brownfield, Strategy B), ~20h (not >2 weeks), one user outcome
serving one job. No split required. Slice 04 is severable on its own merits rather than because the
feature is oversized.

### Found and escalated, not folded in

| # | Item | Disposition |
|---|---|---|
| R-6 / D13 | `Team.ThroughputHistory` defaults to **30** (`Team.cs:17`) while `CreateTeamWizard.tsx:35` and `EditTeam.tsx:80` both seed **90** | **Out of scope, its own ADO item.** Independently verified by the orchestrator. Incidentally an argument *for* this feature: nobody currently knows which value is right |
| R-8 / D4 | There is no Team setting for the forecast confidence level | **Resolved as a design finding, not a gap.** Whether one should exist is **its own ADO item** |
| Standing | **ADR-127 is stale** — it describes a team-settings advisory channel Story #5612 deleted, and carries no note saying so | Worth a status correction in its own right. **Do not reach for that mechanism** in DESIGN or DELIVER |

### Gates

| Gate | Verdict |
|---|---|
| Scope assessment (Elephant Carpaccio) | **PASS** — right-sized, no split |
| Journey artifacts | **PASS** — one journey, five steps, emotional arc, TUI mockups, shared artifacts with sources, integration validation, per-step failure modes |
| Coherence validation | **PASS** — four shared artifacts each with a single source and documented consumers; the one genuine coherence threat (a button for the axis with no signal) resolved by D4 rather than papered over |
| Story map | **PASS** — backbone present, no walking skeleton by design (Strategy B, brownfield), slices ordered by learning leverage with priority rationale |
| Anti-pattern detection | **PASS** — no implement-X story, no generic data, no technical AC, no technical scenario title, no oversized story |
| LeanUX / job traceability | **PASS** — all four stories carry `job_id: job-forecaster-check-the-forecast-against-what-happened`; none is `@infrastructure`; all four carry an Elevator Pitch |
| Definition of Ready (9 items) | **PASS**, with one qualification recorded rather than engineered away: US-01 carries 8 ACs against a 3-7 band, one of which is a measurement gate |
| Per-wave peer review | **Not run.** DoR surfaced no genuine ambiguity; the mandatory consolidated review fires at the end of DISTILL |
| Tier-2 expansion | **No trigger fired.** No menu offered, no expansion rendered |

### Handoff to DESIGN

`nw-solution-architect` receives: the four user stories with their ACs, the story map and four slice
briefs, the journey YAML with its nine resolved design decisions, the outcome KPIs, and one named
DESIGN-wave deliverable — **ADR-209**, whose framing is in `recommendation.md` §8 and whose next-free
number (207) is confirmed. DESIGN should also confirm, per Flux's verification note, that no `Report`
concept already exists in `brief.md`; `ctx_search` skips that file for size, so it could not be confirmed
absent by search.

**R-1 remains the single open unknown and is closed inside slice 01 before any UI is written.**

---

## DISCUSS — revision, 2026-09-22

Taken after the first DISCUSS pass was committed (`5ca5cd257`). Planning only; no implementation.

### DR-1 — Apply is dropped from the feature. Removed, not deferred

**The maintainer's decision, and the reasoning is theirs, in their order of weight:**

1. **Apply structurally contradicts honesty requirement §4.1.** The verdict says *"anything between 30 and
   90 days would have behaved about the same."* An Apply button must write **one** number, so
   `Apply 60 days` names a winner — precisely the claim the Bailey et al. reasoning says a sixteen-cell
   search against months of history cannot support, and precisely the thing the region language exists to
   prevent. **The button would quietly undo the feature's central honesty discipline in the single
   interaction the user is most likely to trust.**
2. **The control is already on screen.** Verified in the tree: `ThroughputQuickSetting` sits in
   `QuickSettingsBar` inside `DetailHeader`'s `quickSettingsContent` (`TeamDetail.tsx:402-405`) — the page
   shell, so it renders on **every** Team tab including the Forecast tab where the check lives. A user
   reading "your 14 days is outside the sound range" already has the throughput control visible on the
   same screen. Apply was a shortcut to something already in front of them, bought at the cost of point 1.

**This tension was present from DIVERGE onward and nobody caught it.** Apply scored 5/5 on
DECISION-CHANGING, and that score obscured what it was costing on HONESTY — the higher-weighted criterion,
and the one the whole recommendation was made conditional on. Carried forward as **R-9**, because the
failure mode is general rather than specific to this button: *a criterion scored in isolation can hide
what it costs on a higher-weighted one.*

**For DESIGN: do not reintroduce Apply as an obvious improvement.** It was considered, it scored well, and
it was removed on purpose. It is listed under Out of Scope as declined rather than deferred, in
`feature-delta.md`, the journey YAML and every slice brief that could plausibly host it.

#### A consequence the revision brief did not anticipate: the winning margin moves

**M1 won the DIVERGE matrix at 4.30 partly *because of* Apply.** `recommendation.md` §2 gives its
DECISION-CHANGING score as **5**, justified explicitly: *"it is the only option that puts the named change
**and the control that makes it** in the first interaction"*. §3 repeats it — *"it names the change and
pre-fills the control, and a human presses the button."* Remove the control and that justification is
half gone.

The runner-up, **R1 · "Your setting, on trial", scored 4.15 — a gap of 0.15.** Dropping Apply plausibly
closes or reverses it on the matrix as written, and `taste-evaluation.md` §6 already showed the top three
sit within 0.20 and that one point on HONESTY reorders them.

**This does not reopen the decision, for two reasons that are on the record and unchanged:**

1. **R1 is not available.** Its surface was ADR-127's advisory channel, which Story #5612 deleted;
   `ValidationAdvisory.tsx` is absent from the frontend and a later rung was built *"rather than reviving
   them"*. DIVERGE ran that check explicitly and it went against the dissent. **The branch in which R1
   overtakes does not exist**, whatever the arithmetic now says.
2. **The trade was HONESTY-positive**, and HONESTY is the higher-weighted criterion (30%) against
   DECISION-CHANGING (15%). M1's known weakness was HONESTY 3, and the recommendation was made
   *conditional* on the three §4 requirements being built. Removing the one element that contradicted
   §4.1 moves the score in the direction the conditional was about.

**But it should be said plainly rather than discovered later**: the feature as now planned is no longer
the M1 that scored 4.30. It is M1 minus its DECISION-CHANGING justification and plus a HONESTY point —
which is, as it happens, close to **E1 · "Sweep in a breath" (4.10)**, the third-place option whose
description was *"M1's evidence view, promoted to the default"* and whose recorded trade-off was precisely
*"it names a window but defers Apply — the user reads an answer and then has to go and act on it
somewhere else."* **That is now an accurate description of what this Epic builds.** E1's hire criterion —
*"a user who wants to look at the data themselves and distrusts a tool that summarises"* — is worth
keeping in view during DESIGN, because it is closer to the real audience than M1's was.

No re-scoring is performed here; DIVERGE is closed and retroactive adjustment would flatter the outcome.
This is recorded so nobody reads "M1, 4.30" downstream and assumes the thing they are building is what
earned it.

### DR-2 — D4 rewritten, D5 deleted

**D4** was *"Two verdicts, one button"*; it is now *"Two findings, no buttons. The check reports; the human
acts."* The asymmetry it existed to explain is gone — neither axis has a control — so the copy states two
findings plainly instead of apologising for a missing button. The substance is unchanged and still rests on
the same verified fact: the sampling window is a Team setting, the confidence level is not.

**D5** (*"Apply appears only when the current window is outside the sound region"*) is **deleted**. The
conditional-button logic, the "what do we render when there is nothing to apply" case and the no-op-button
reasoning went with it.

**D5's number is left as a tombstone rather than renumbered.** D6-D13 are cross-referenced from the journey
YAML, the slice briefs and this file; shifting them silently would break every reference for no gain.

### DR-3 — Re-sliced to three. The `@infrastructure` gate re-checked

| | Before | After |
|---|---|---|
| Stories | US-01 … US-04 | US-01 … US-03 |
| Slices | 4 | **3** |
| Estimate | ~20h | **~17h** |

- **US-03 (Apply) is deleted.** Nothing user-visible survived, so it collapsed rather than shrinking — it
  was not left as an `@infrastructure` husk.
- **The one piece worth keeping** — the copy naming the sampling window a setting and the confidence level
  a reading — **moved into US-01 as AC-1.9**, where the verdict sentence already lives. US-01 goes from 8
  ACs to 9; the sizing note is updated rather than gamed.
- **US-04 (the export one-pager) is promoted to US-03**, ACs relabelled AC-4.x → AC-3.x, slice 04 → slice
  03. Renumbered rather than left with a gap because no ADO items exist yet and nothing external
  references the old numbers.
- **`@infrastructure`-only hard gate re-checked and passes**: slice 01 ships a sentence, slice 02 the
  evidence view, slice 03 the one-pager.

### DR-4 — The feature is now read-only end to end

Worth stating rather than letting it disappear into the diff. The RBAC answer previously had two halves — a
read endpoint plus a write that borrowed `canUpdateTeamData` through the shipped `ThroughputQuickSetting`.
**The second half is gone.** No write endpoint, no borrowed write path, no control anywhere in the feature
that mutates a Team.

Consequences, all simplifications: a single permission (Team read) governs the whole feature; there is no
differential rendering by permission, because there is no control to show or hide; and the standing rule
that all UI gating derives from `useRbac()` with no direct fetch of `/api/latest/authorization/my-summary`
still holds, with this feature adding no new gating and therefore no new way to break it.

A new DoD item makes it enforceable: **if any slice introduces an endpoint or control that mutates a Team
setting, Apply has been reintroduced and D4 has been violated.**

**One wrinkle found while verifying the maintainer's point 2, not previously noted anywhere**: the Team
header's `QuickSettingsBar` is itself wrapped in `showWriteControls ?`, so a **read-only user sees no
throughput control on the page at all**. This does not break the decision — it makes the read-only path
cleaner, since for such a user the check is purely informational and there is no control anywhere to be
inconsistent with. It does mean "the control is already on screen" is true *for users who can act on it*,
which is the only audience the argument needs.

### DR-5 — Per-Team default confidence level: DECLINED

The previous pass escalated *"should Lighthouse gain a per-Team default forecast confidence level?"* (R-8 /
D4) as a candidate ADO item. **The maintainer declined to raise one, 2026-09-22.**

Recorded here as considered-and-declined **so a later wave does not re-raise it as though it were
unconsidered.** The consequence, stated plainly: the window/level asymmetry — one axis is a setting, the
other is a reading the human makes — is now **permanent by decision rather than by oversight.**

### DR-6 — Reference corrections

| Item | Was | Now |
|---|---|---|
| R-6 / D13 — `ThroughputHistory` 30-vs-90 default disagreement | "raise as its own ADO item" | **Bug #6071**, raised. Still out of scope for this Epic |
| ADR-127 — the deleted team-settings advisory channel | A standing caution: the ADR describes a mechanism no longer in the tree and carries no note saying so | **Discharged.** ADR-127 now carries a SUPERSEDED-BY-EVENTS status note recording that Story #5612 deleted the channel. Still do not reach for that mechanism — but read the corrected ADR rather than our caution |

### Unchanged by this revision

All three honesty requirements (§4.1 region-not-winner, §4.2 denominator and non-comparability, §4.3
coverage against a printed nominal rate) stand as hard ACs. **Dropping Apply strengthens §4.1 rather than
relaxing it.** Also unchanged: all four confidence levels side by side (D1); today as the end anchor with
no date picker (D6); cells not comparable; per-cell sufficiency composing with the shipped ≥5-active-days
rule (D9); one-click synchronous trigger; no new entity; small multiples (D2); no Report abstraction and
ADR-209 as a DESIGN deliverable (D8); the name (D10); and **R-1 as the single open unknown, still closing
first inside slice 01.**

### Files touched by the revision

`feature-delta.md` · `slices/slice-01-…md` · `slices/slice-02-…md` ·
**`slices/slice-03-the-answer-travels.md` (new)** · `docs/product/journeys/epic-4172-forecast-reality-check.yaml` ·
`docs/product/jobs.yaml` (the DISCUSS note's control clause corrected) · this file.

**Two files need `git rm`** — they carry tombstones because the revising agent could not delete files:
`slices/slice-03-change-it-here-or-be-told-there-is-nothing-to-change.md` (the deleted Apply slice) and
`slices/slice-04-the-answer-travels.md` (renumbered to 03).

### Gates, re-validated

| Gate | Verdict |
|---|---|
| Scope assessment | **PASS** — 3 stories, 2 modules, ~17h |
| `@infrastructure`-only slice gate | **PASS** — re-checked after the removal; every slice carries a user-visible story |
| Anti-pattern detection | **PASS** — no story was hollowed out; the Apply story was removed whole |
| Definition of Ready (9 items) | **PASS** — re-validated. The removal broke no item; the single qualification is now US-01 at 9 ACs |
| Per-wave peer review | **Still not run.** The revision resolved ambiguity rather than creating it; the mandatory consolidated review fires at the end of DISTILL |
| Tier-2 expansion | **No trigger fired** on re-evaluation — every count moved down or held |

---

## ADO work items — created 2026-09-22

Created after the Apply revision, so the board mirrors the three-slice plan rather than the four-slice
first pass. All three are children of Epic #4172, state `New`, priority 2.

| Story | Slice | ADO |
|---|---|---|
| US-01 — one sentence that says whether the configuration behind my forecasts is sound | `slice-01-one-sentence-about-your-sampling-window.md` | [#6072](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6072) |
| US-02 — the evidence, when the sentence is not enough | `slice-02-the-evidence-you-can-look-at.md` | [#6073](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6073) |
| US-03 — the answer travels to the person who asked the question (**severable**) | `slice-03-the-answer-travels.md` | [#6074](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6074) |

**Epic #4172 deliberately left in `Planned`.** This is planning for a later release; no implementation is
authorised and no story has been started.

Raised separately and **not** children of this Epic, because neither is caused by it:
[Bug #6071](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6071) — the `ThroughputHistory`
30-vs-90 default disagreement (DV-10 / R-6). The per-Team default forecast confidence level was
**declined** as an item by the maintainer on 2026-09-22 (DR-3), which makes the window/level asymmetry
permanent by decision rather than by oversight.

---

## DESIGN

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-09-22 · **Interaction mode**: Propose
(autonomous — the maintainer stepped away; options weighed and called, questions recorded rather than
blocked on)
**Predecessor**: DISCUSS (complete, including the 2026-09-22 revision DR-1…DR-6).
**Successor**: DEVOPS — **held.** This is planning for a later release; no implementation is authorised.
**Design scope**: **Application / components**, determined on evidence. DISCUSS locked no entity, no
table, no migration, no `UpdateType` member, no queue work, no bounded context and no infrastructure
change, which rules out system, domain and platform scope.

### Artifacts produced

| Path | What it holds |
|---|---|
| `feature-delta.md` (appended, not rewritten) | Prior-wave consultation, DES-1…DES-12, the R-1 cost finding and its two contingencies, C4 L1 + L2, component decomposition, the 24-row Reuse Analysis, driving/driven ports, the full response contract, technology choices, architecture enforcement, quality attributes, OQ-1…OQ-5, what DESIGN found wrong upstream, the handoff |
| `docs/product/architecture/adr-209-a-report-is-a-response-not-a-record.md` | **NEW.** The no-Report position. Accepted |
| `docs/product/architecture/adr-210-a-forecast-level-holds-or-it-does-not-and-its-nominal-rate-is-the-level.md` | **NEW.** The held/nominal-rate measurement definition. Raised PROPOSED; **Accepted by the maintainer 2026-09-22** |
| `docs/product/architecture/brief.md` | `## Application Architecture — epic-4172-forecast-backtest-sweep` appended by anchored patch |
| `wave-decisions.md` | this section |

**No SSOT change** beyond the brief and the two ADRs. The journey YAML and `jobs.yaml` are untouched —
the corrections DESIGN found are escalated as OQ-1 rather than applied, because they change acceptance
criteria the maintainer locked.

### The two verification notes DIVERGE and DISCUSS handed forward — both discharged

- **"Confirm no `Report` concept exists in `brief.md`."** `ctx_search` skips that file for size, so it was
  done with `grep`: **four** case-sensitive `Report` matches in 8 747 lines, all ordinary English (a
  mutation report, a field report, "report success", a readability report). No entity, aggregate, table,
  store, repository, kind or payload. The 206 ADR titles contain none. **Confirmed absent.**
- **"Next free number is ADR-207."** Re-verified by listing: 206 files, highest `adr-206`, no `adr-207`. Later renumbered to 209 on 2026-09-24, because story 6053 had taken 207 and 208 in a parallel DESIGN wave; the pair is now 209 and 210.
  **Confirmed.** ADR-210 is taken in the same wave for the measurement decision.

### Decisions taken in this wave

Full text in `feature-delta.md` under Design Decisions. In brief:

- **[DES-1]** The response is one **self-describing envelope**, discharging ADR-209's
  no-hard-coded-Team constraint without returning a list of one. It carries **facts, never a rendered
  sentence** — the standing `story-6055` rule in the brief is that terminology stays in the browser, and
  every string here contains a renameable term.
- **[DES-2]** **No `recommendedWindow`, and no way to add one by accident.** The sound region is an
  `int[]` filtered from the fixed ascending ladder, so it cannot carry an order that is not the ladder's,
  and no per-window score exists to sort by. The bug class "the response ranks the windows" is
  non-representable rather than untested.
- **[DES-3]** **Bounds are derived in the client.** A `lowerBound`/`upperBound` pair asserts that
  everything between them is sound; on a non-contiguous set that is a lie in a field nobody inspects. A
  set cannot lie.
- **[DES-4]** The current setting's standing is a **tri-state**, not a boolean. `Team.ThroughputHistory`
  is a free integer, and a boolean would force the artifact to answer for a value it never checked.
- **[DES-5]** **Effect isolation.** The sweep service takes the resolved `Team` as a parameter and is
  never injected with `IRepository<Team>`, so it structurally cannot write a Team setting. DoD item 13
  becomes an ArchUnit rule instead of a review promise.
- **[DES-6]** **No charting component** — and the reason is grammar, not cost. A shared axis is the visual
  claim that two rows are commensurable, which D6 forbids. Each row is normalised to its own extent.
- **[DES-7]** **The export precedent is premium-gated.** `useDataGridExport` refuses without a licence
  (`DataGridToolbar.tsx:72-76`). This is a Community feature whose slice 03 *is* the marketing surface, so
  ADR-172/ADR-162 are followed in placement (client-side) and not reused as code.
- **[DES-8]** **CORRECTION → ADR-210 (raised PROPOSED, since ACCEPTED).** "Beaten" is retired. A level *holds* iff
  `actual >= value(P)`, and its nominal rate is `P`, not `100 − P`.
- **[DES-9]** **CORRECTION.** `GetProbability` returns `-1`, so a degenerate forecast is a **second**
  unevaluable reason. Not a second sufficiency threshold — `MinimumActiveDays` is untouched.
- **[DES-10]** Cell outcome and sufficiency are closed enums rendered through exhaustive `Record<…>` maps,
  so a new state cannot reach the screen without someone writing its copy.
- **[DES-11]** The sampled ladder is **printed, not assumed**. ~~Not widened to include an off-ladder Team
  setting.~~ **The second half was REVERSED by the maintainer on 2026-09-22 — see DR-D1 and DES-13.** The
  first half stands and DES-13 makes it load-bearing: the ladder is now Team-dependent, so a client that
  assumed `[14, 30, 60, 90]` would be wrong for every off-ladder Team.
- **[DES-13]** *(added 2026-09-22)* **The Team's own sampling window is always swept** — sixteen cells
  on-ladder, twenty off it.
- **[DES-12]** The contract survives R-1 failing: no job id, no status, no progress field, so moving to a
  `202 Accepted` later changes the transport and not one response field.

### R-1: still open, and now sharper

**Not resolved. Not assumed away.** It remains the first task of slice 01, before any UI.

DESIGN adds the shape of the cost, read from the tree. `GetThroughputForTeam` caches per window, but its
repository predicate is **window-independent** — the same "all closed items for this Team" query runs on
every miss. A cold sweep issues up to **twenty** identical queries (sixteen history windows + four scored
periods). Two mitigating facts: the four actual-completed reads are per *horizon*, shared four ways, so an
implementation reading them per cell is already 25% over; and no work tracking system is contacted at any
point.

**Probe instruction sharpened: run it cold as well as warm, and count queries, not only wall clock.**

**If it fails — Contingency A, not B.** A is a request-scoped single read of the closed-item set, with the
run charts projected in memory: no engine change, no new dependency, no response-field change.
B is the `UpdateQueueService` fallback DISCUSS named, and it is worse than the record suggests, because
the ADR the record rests on describes lanes that no longer exist — see OQ-3 below.

### Found and escalated, not folded in

| # | Item | Disposition |
|---|---|---|
| **OQ-1** | **ADR-210 changes AC-1.6 and AC-2.4 and the journey's worked example.** The mockup's expected counts use `(100 − P)`; the engine's descending "at least N items" ordering makes the correct figure `P`. Three of four rows are wrong; the 95% row — the feature's headline lesson — by an order of magnitude. The 50% row is identical under both formulas, which is why it survived review | **ACCEPTED 2026-09-22**, after verification against `HowManyForecast`'s descending comparer. ADR-210 status is `Accepted`; both ACs are settled and testable. Closed |
| **OQ-2** | A Team with an off-ladder `ThroughputHistory` (e.g. 45) is checked against 14/30/60/90, none of which is theirs | ~~Recommended: tell them plainly via `currentSettingWasTested: false`~~ — **RESOLVED 2026-09-22 AGAINST this recommendation. See the DESIGN revision section below.** |
| **OQ-3** | **ADR-195 is stale.** It reads `Accepted` with three lanes and a Forecast lane; the lanes shipped and were reverted (`f216ef558`), and only the `story-5877-update-queue-lanes` brief section records that. Anyone evaluating the R-1 queue fallback from the ADR will conclude a calibration run gets its own lane. **False at HEAD** — one channel, one sequential reader | **Own status correction, not this Epic's work.** This is the second feature running to be misled by an ADR describing a deleted mechanism; ADR-127 was the first, and it got exactly this fix |
| **OQ-4** | The D12 MCP precondition says "AC-1.2 puts the verdict sentence in the response as a field". It cannot — terminology stays in the browser | **Rewrite the precondition.** The answer (no CLI/MCP in this Epic) is unchanged and better supported |
| **OQ-5** | Should the cost finding be probed before slice 01 or as its first task? | ~~As its first task~~ — **CLOSED by measurement 2026-09-22.** It was probed ahead of both options and the prediction came back exact |

### Locked decisions honoured, none reopened

Synchronous one-click with no configuration dialog and no date picker · today as the END anchor · cells
not comparable and the verdict naming a region, never a winner · all four levels side by side as one band
with one mark · ~~four panels stay four~~ (amended — one panel per window swept; see below) · the
denominator with its correlation disclosure · per-cell
sufficiency composing with `MinimumActiveDays = 5`, no second bar · ADR-194 governing the unevaluable
rendering · **no Apply, no write path, read-only end to end** · the three-way verdict never attributed to
Nick Brown · configurable terminology throughout · the name.

**Apply was not reintroduced, and the design makes reintroducing it fail a build** (DES-5, enforcement
rule E1).

### Gates

| Gate | Verdict |
|---|---|
| Design scope determined on evidence | **PASS** — application/components; system, domain and platform ruled out by what DISCUSS locked |
| Reuse Analysis (hard gate) | **PASS** — 24 overlapping components, 19 reused or extended, 5 CREATE NEW each justified by impossibility or unacceptable coupling. No justification is "it's complex" or "too many dependencies" |
| C4 diagrams | **PASS** — System Context (L1) and Container (L2) in Mermaid, every arrow verb-labelled. L3 deliberately omitted: seven boxes, and the decomposition table carries more |
| ADR quality | **PASS** — ADR-209 carries three alternatives with rejection rationale; ADR-210 carries three. Both carry context, decision, consequences and relationships |
| Outcome Collision Check | **RUN** — `0 outcomes checked, 0 collisions found across 0 outcomes`. The delta's KPIs are a prose table rather than registry-shaped blocks, so the checker had nothing to match. Recorded as a clean run with a caveat, not as a pass |
| Architecture enforcement recommended | **PASS** — ArchUnitNET (already in the tree, five existing `*ArchUnitTest` classes) plus the TypeScript compiler; seven named rules E1–E7 |
| Dependency-inversion compliance | **PASS** — one driving adapter, five existing driven ports, no new port and no new adapter |
| External integrations / contract tests | **N/A, explicitly** — the feature contacts no third party and reads only data already in the instance. No contract-test annotation is owed |
| Simplest-solution check | **PASS** — no new container, no new dependency, no migration, no queue. Three backend types and four frontend components |
| Per-wave peer review | **Not run.** Triggers evaluated: no contested ADR (207 ratifies a locked decision; 208 is escalated rather than contested), no novel pattern, no security boundary change. The mandatory consolidated review fires at the end of DISTILL |

### Handoff

**DEVOPS**: almost nothing. No container, no dependency, no migration, no queue, no external integration,
no configuration, no secret. **No contract tests owed.**

**DISTILL** (`nw-acceptance-designer`): the response contract is the test surface, and the three honesty
requirements are testable as structural properties — no rankable field (E5), the denominator identity
`scoresEvaluated = runsEvaluated × levelsPerRun`, and the completeness invariant
`cells.length == sampledWindowDays.length × sampledHorizonDays.length` — **16 or 20** (DES-13).
**AC-1.6 and AC-2.4 are settled**: ADR-210 is `Accepted`, so they specify "held" with
`expectedHeldCount = evaluated × P/100` and can be turned into acceptance tests directly.
**Both cell counts need coverage** — every Team in the worked examples is on the ladder, so a suite
written from them alone would exercise only the sixteen-cell path.

**Paradigm**: object-oriented, per this project's `CLAUDE.md`. `@nw-software-crafter` implements.

**Epic #4172 stays in `Planned`, and its three Stories stay `New`.** DESIGN is planning; no
implementation is authorised and no story has been started.

---

## DESIGN — revision, 2026-09-22

Taken after the DESIGN pass was committed, as the maintainer worked through the Open Questions and as R-1
resolved by measurement. Planning only; no implementation.

### DR-D1 — OQ-2 RESOLVED **against** DESIGN's recommendation: the Team's own sampling window is always swept

**The maintainer's decision.** DESIGN recommended telling an off-ladder Team plainly that its own setting
was not checked. **That was rejected, and the sweep now tests the Team's own window as a fifth window.**

**The sweep is 16 cells for a Team on the standard 14/30/60/90 ladder and 20 for a Team whose window is
off it.** `sampledWindowDays` is the standard ladder union `Team.ThroughputHistory`, sorted ascending.

**Both of DESIGN's objections failed, and one of them was the orchestrator's error rather than DESIGN's:**

1. **The cost objection was measured away.** R-1 resolved after the DESIGN pass:
   `RealityCheckWallClockProbe` (`7e0da58d0`) measures **701 ms cold / 612 ms warm** on a real-sized Team
   against a 5,000 ms budget, with the Monte Carlo floor (~615 ms) dominating. Four more cells cost
   roughly 150 ms. When DES-4 and DES-11 were written the cost was unknown and caution was correct; it is
   not unknown now. **Caution that survives its own evidence is just a habit.**
2. **The denominator objection confused a principle with a constant.** The DESIGN dispatch listed *"the
   denominator copy states 16 runs / 4 levels / 64 scores"* among the LOCKED constraints, and DESIGN
   treated the constant as the requirement. The actual honesty requirement (§4.2) is **state your
   denominator** — say what was checked. A report reading *"20 runs, each read at 4 confidence levels — 80
   scores in all"* satisfies it exactly as well, and arguably better, since it reports the check the user
   got rather than a check a different Team got. **The principle was locked; the constant never was.**

**The substantive reason, which outranks both objections**: the Team's own setting is the single most
decision-relevant cell in the report, and *"we checked four windows and could not determine anything about
yours"* invites the obvious reply. The check can answer it for about 150 ms.

**A general lesson worth keeping beside R-9**, because the failure mode is not specific to this number:
**a constant quoted inside a locked principle acquires the principle's authority without earning it.**
R-9 recorded that a criterion scored in isolation can hide what it costs on a higher-weighted one; this is
its sibling.

### DR-D2 — What the reversal changed, and what it did not

**Changed:**

- **The denominator copy is a template**, not a fixed string. D3 amended. `${runCount}` is 16 or 20,
  `${scoreCount}` 64 or 80. The correlation sentence and the non-comparability sentence are unchanged and
  neither depends on the count.
- **A fifth *panel* in the evidence view** for an off-ladder Team. AC-2.1 amended. **This is the one place
  "four panels stay four" genuinely moves** — D2's rule was that the *confidence level* must not become an
  axis, and the panel axis has always been the sampling window. The confidence dimension still costs zero
  panels and zero controls.
- **AC-1.1, AC-1.2, AC-1.4, AC-2.1 and AC-3.1** now state the count conditionally. Worked examples on
  named Teams (Ocean Explorer at 30, Coastal Survey, Deep Current at 14) stay at sixteen, because all
  three are genuinely on-ladder.
- **The response gains `standardWindowDays`** beside `sampledWindowDays`, so a reader can see which ladder
  was swept and which window was added. `denominator.runsAttempted` (16 or 20) is the at-a-glance
  discriminator. **No `ladderKind` enum** — it would be derivable from the two arrays and could drift from
  them.
- **The cold query count** rises from 20 to 24 for an off-ladder Team.

**Not changed — and DES-2 was re-checked rather than assumed:**

**`soundWindowDays` is still a filter of `sampledWindowDays`, which is still a fixed ascending sequence
for any given run** — merely computed per Team instead of per product. Filtering an ascending sequence
cannot produce a different order however that sequence was computed, and **the fifth window adds no
scalar**: there is still no per-window score anywhere in the response to sort by. *"The response ranks the
sampling windows" remains non-representable.* The enforcement rule E5 was widened to assert it directly
(no per-window scalar anywhere) rather than relying on the subsequence check alone.

Also unchanged: today as the END anchor; cells not comparable; the region never a winner; all four
confidence levels as one band with one mark; sufficiency composing with the shipped ≥5-active-days rule
with no second bar; ADR-194 governing the unevaluable render; **no Apply, no write path, read-only end to
end**; the three-way verdict never attributed to Brown; terminology; the name; ADR-209 and ADR-210.

### DR-D3 — DES-4's tri-state survives and gets sharper

`currentSettingStanding` is **not** removed. But `NotDetermined` **narrows**, and the whole field gets
simpler:

- It used to cover two cases: all cells unevaluable, **and** the off-ladder window never checked. **The
  second case no longer exists.**
- It now means exactly one thing: the Team's own window was swept and none of its cells could be
  evaluated — the shipped ≥5-active-days bar, or a degenerate forecast.
- **`currentSettingStanding` becomes a set-membership test rather than an interval test**, because the
  Team's window is always in the ladder. No contiguity reasoning is needed to decide the standing. That is
  a simplification DESIGN did not anticipate when it argued against the fifth window.

**DES-3's contiguity rule gets *more* load, not less.** An off-ladder window sits between two standard
ones, so the sound set can now have a hole exactly where the user is standing — `{14, 30, 60, 90}` sound
with 45 not. That is a sharp, useful finding, and a bounds pair would have erased it by reporting `14–90`
and calling the user's setting inside. Before the reversal this was a rare shape; it is now the
characteristic shape of the case the fifth window exists to detect.

### DR-D4 — OQ-4 and OQ-5 closed; the table now has nothing blocking

- **OQ-4 CLOSED — the D12 MCP precondition is rewritten.** It assumed AC-1.2 would put the verdict
  sentence in the response as a field. It cannot: every user-facing string here carries a renameable term
  and the standing rule is facts on the wire, never a rendered clause. The precondition now turns on the
  **client** composing the artifact from facts — including resolving terminology itself and not exposing
  the cell grid as its default output. **The answer is unchanged: no CLI or MCP surface in this Epic.**
- **OQ-5 CLOSED by measurement.** It asked whether to probe before slice 01 or as its first task; it was
  probed ahead of both. `RealityCheckWallClockProbe` does exactly the sharpened thing — cold and warm,
  counting executed commands rather than only wall clock — and **DESIGN's prediction was exact rather than
  approximate: twenty queries, at every data volume and on every run, with the count not growing with Team
  size.**

**Nothing is open that blocks DISTILL, and no architectural question remains undecided.** Two items are
carried, stated rather than forced to read RESOLVED: **OQ-3** (ADR-195's status note — another document's
bookkeeping, and since R-1 kept this feature off the queue it can no longer mislead *this* Epic) and
**OQ-6** (below — a product question with a safe default already specified).

### DR-D5 — The thing the decision broke that neither of us anticipated: a Team with no rolling window

**`Team.UseFixedDatesForThroughput`** (`Team.cs:11`, default `false`) switches a Team from a rolling
sampling window to a fixed `ThroughputHistoryStartDate`/`ThroughputHistoryEndDate` pair.
`TeamMetricsService.ComputeBlackoutAwareThroughput` and `Team.GetThroughputSettings` both branch on it.

**For such a Team `ThroughputHistory` does not drive its forecasts at all**, so there is no "the Team's own
sampling window" to add as a fifth window, and the feature's premise — *"the sampling window behind every
forecast this Team publishes"* — does not describe it.

**Under the old decision this was latent**, because the sweep ignored the Team's setting anyway. Making
that setting load-bearing is what surfaced it. It is handled rather than designed around: a fixed-dates
Team sweeps the standard four windows, sixteen cells, and `currentSettingWasTested` is `false` with its
reason. Raised as **OQ-6** because whether such a Team should see the control at all is a product
question, not an architectural one — and the specified behaviour ships correctly either way.

### DR-D6 — R-1's status, restated precisely

**RESOLVED for the sixteen-cell case, by measurement.** 701 ms cold median on a real Team against a
5,000 ms budget; twenty queries, not growing with Team size; warm cost flat at ~615 ms; only the cold path
scales, and the budget would not be threatened until roughly 80,000 closed Work Items on one Team.

**The twenty-cell case is extrapolated, not measured — ~870 ms cold.** `RealityCheckWallClockProbe`
currently exercises sixteen cells. **Recommended, not done here: give the probe a twenty-cell case.**
DESIGN does not change the probe. The reason it is worth adding is not the wall clock — linear
extrapolation of a Monte Carlo cost is almost certainly right — but the **query count**, where 24 assumes
the fifth window misses the cache exactly once and adds no actual-completed read. An implementation that
read the actual per cell would show 25, and the probe would catch it immediately.

**AC-1.1 stays in slice 01, with changed character.** The measurement is SQLite, in-process, one machine,
no Kestrel, no serialisation, no concurrent load. It measures the sweep's own cost, which is what R-1
asked, but a loaded production instance will be slower. **AC-1.1 is now a confirmation on real hardware,
no longer a gate that could change the design.**

### Files touched by this revision

`feature-delta.md` (D3, D4's Bailey clause, the D12 MCP precondition, AC-1.1/1.2/1.4/2.1/3.1, US-02's
narrative, the carpaccio check, DES-2, DES-3, DES-4, DES-11, **new DES-13**, the R-1 cost section, the
component decomposition, reuse rows 4/6/7/18, the response contract, quality attributes, enforcement rule
E5, the Open Questions table, "What DESIGN Found Wrong", the handoff, and the R-1 measured section) ·
`docs/product/architecture/brief.md` · this file.

**No implementation code written. Epic #4172 stays `Planned`; its three Stories stay `New`.**
