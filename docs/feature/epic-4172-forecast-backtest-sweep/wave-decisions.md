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
| `recommendation.md` | Top 3, dissenting case, decision statement, ADR-207 framing, carpaccio shape |
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

### DV-7 — No Report abstraction. ADR-207 records the position, not an implementation

Shape (a) on the commitment spectrum — nothing stored, no entity, no migration, no `UpdateType` member,
no runner registry. The project's SOLUTION EFFICIENCY rule at its first step (*skip / YAGNI*).

ADR-207 must nevertheless decide more than "not yet": the accepted consequence (no history, nothing to
email), the named revisit trigger (**the second Report kind**, not a date), the six questions the eventual
Report ADR will face, the runner question with the ADR-195 single-lane measurement behind it, and the
forward-compatibility constraint that the result shape must not hard-code a single Team.

Touches ADR-195, ADR-194, ADR-039, ADR-172, ADR-162, ADR-127, ADR-181/182/186, ADR-046, ADR-145.
Next free number **ADR-207** confirmed (206 ADRs present, highest `adr-206`).

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
  confirm before ADR-207 is written.**
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
- **[D8]** No Report abstraction. ADR-207 is flagged as a **DESIGN-wave deliverable**; Luna does not write it.
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
DESIGN-wave deliverable — **ADR-207**, whose framing is in `recommendation.md` §8 and whose next-free
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
ADR-207 as a DESIGN deliverable (D8); the name (D10); and **R-1 as the single open unknown, still closing
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
