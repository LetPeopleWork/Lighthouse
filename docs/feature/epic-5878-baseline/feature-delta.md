# Feature Delta — epic-5878-baseline

**ADO**: Epic #5878 *Baseline* (tag `ValueFlow`, tier Community, ICE 54 "NEEDS EVIDENCE" as of 2026-09-20).

**Waves**: DISCOVER (2026-10-02). Desk research, a codebase check and two consultant signals. Lighthouse ran
no Mom Test interviews.

**One line**: pick a baseline window, such as the 90 days before an engagement or improvement effort started,
freeze the flow metrics for that window, and show today's values against them. The point is to show whether
the work changed anything.

**Density**: `lean`, Tier-1 `[REF]` only. `evidence_standard: past_behavior`. Each claim is graded as
**observed behaviour**, **second-hand report**, **stated intent**, or **desk/codebase fact**.

---

## Wave: DISCOVER / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` | **Primary.** The consulting variant is the one `flow-coach.yaml` already describes for Story 5884: it "has to leave an artifact behind". It sets the baseline at engagement start and shows the result. For Thrivve, the consultant pays, not the Team. |
| `flow-coach` (internal) | **Primary, second shape.** A Team or coach running its own improvement experiment and asking "did this change anything". There is no direct evidence for this shape yet. |
| `delivery-lead-rte` | **Reader.** The management audience the result has to reach. It is the "expand" step of land-and-expand. |
| *(candidate)* `flow-consultant` | **Not created.** It is split out from `flow-coach` only if DISCUSS finds that the payer's job (prove my value to the client) differs from the coach's job (see whether flow changed). The evidence does not yet separate them. |

---

## Wave: DISCOVER / [REF] Opportunity statement

The outcome: someone who changed how a Team or Portfolio works can show, with numbers management trusts,
whether flow changed since the change began, and the "before" does not move under them.

| # | Opportunity (customer-outcome form) | Source | Grade |
|---|---|---|---|
| O1 | Minimise the time it takes to produce a defensible before/after of flow metrics for an engagement or improvement effort | #5878; Thrivve built and use it | second-hand report of past behaviour |
| O2 | Minimise the likelihood that the "before" numbers change after the fact (frozen, not drifting) | #5878 differentiation | desk |
| O3 | Minimise the time it takes to show "where you are today vs where you were" in a short assessment with almost no "after" window | consultant #2, 2026-10-02 | stated intent, present context |
| O4 | Minimise the effort it takes to get the comparison in front of management | land-and-expand hypothesis; prospect asked for email reports | hypothesis + counter-signal |
| O5 | Minimise the likelihood of presenting noise as improvement | existing PBC stability / breach count in the field set | desk |

The opportunity algorithm cannot be applied because no importance or satisfaction ratings exist. The ICE
score is the only ranking, and the maintainer produced it, not customers (see G2).

---

## Wave: DISCOVER / [REF] Validated assumptions

| # | Assumption | Evidence | Confidence |
|---|---|---|---|
| V1 | Consultants need a before/after of flow metrics to prove their value to a client. | Thrivve Partners **built** the ValueFlow baseline and **use** it in client engagements. Paul Brown demoed it on 2026-08-31 and says clients react to it most. Building and using it is past behaviour that cost them effort. "Clients react most" is Thrivve's own account, which we did not observe. | **Medium** that the job exists for consultants. **Low** that it transfers to Lighthouse users. |
| V2 | Every metric in the ValueFlow field set can be computed for an arbitrary past window from stored Work Items, with no snapshot infrastructure. | `TeamMetricsService` / `PortfolioMetricsService` take `(startDate, endDate)` for cycle time percentiles, Throughput, WIP over time and total Work Item Age by day. PBC methods take `asOf` for stability state and breach counts. Story 6053 already recomputes past days with the same service calls on a shifted window (`ReconstructionMemo`, `ReconstructOverTimeHistoryAcceptanceTest`). | **High** (feasibility of computing it; see X1 to X3 for limits) |
| V3 | A pinned "before" window is already a concept in Lighthouse. | Every Team and Portfolio carries `ProcessBehaviourChartBaselineStartDate`/`EndDate` (`WorkTrackingSystemOptionsOwner.cs:62-64`). Settings exposes it as "Set Baseline for Process Behaviour Chart". `BaselineValidationService` requires at least 14 days, no future end, and a start inside the cutoff. | **High** as product precedent. **None** as demand evidence: there is no usage telemetry for this setting. |
| V4 | A frozen baseline pinned to an engagement start is a differentiator. | Desk check (#5878): ActionableAgile and Nave ship rolling-window comparison (previous month, 3 or 6 months, with RAG). No frozen baseline was found. The codebase has no "previous period" comparison either (no hits in frontend `*.tsx`). | **Medium**: absence of evidence across two competitors. |
| V5 | Retroactive baselining works from day one, with no waiting for forward recording. | Same as V2. The window only has to lie inside stored history. | **High** |

---

## Wave: DISCOVER / [REF] Invalidated assumptions

| # | Assumption (as stated) | Evidence against |
|---|---|---|
| X1 | "Ease 6: no snapshot infra, **no migration**." | Computing the values needs no migration (V2). **Freezing** them does. You have to store either the values or at least the baseline definition (dates, scope), and today the only stored baseline is the PBC pair of columns on the owner. Reusing that pair avoids a migration but freezes nothing (X2) and overloads a setting with a different purpose. Ease 6 holds for computation. A stored baseline adds a small expand-only migration. **Revised Ease 5-6.** |
| X2 | "A baseline recomputed from history is a frozen baseline." | Story 6053's accepted residual risk is that recomputation reflects **today's** state mappings, cycle-time definitions, blocked rules, blackout config and item set. Deleted or re-parented Work Items change the result. So a recomputed "before" can silently move, which is the opposite of the promise. |
| X3 | "A baseline stays available for as long as the engagement runs." | `DoneItemsCutoffDays` defaults to 365 (`Team.cs:23`, `Portfolio.cs:35`), and `BaselineValidationService` already rejects a start outside it. For a 90-day baseline that ends at the engagement start, recomputation stops being possible about **275 days after the engagement started**. That is within the span of a long engagement. |
| X4 | "Before/after is the shape for every consultant." | **Challenged, not disproven.** Consultant #2 is embedded for 4 weeks to assess. Compare a 90-day rolling "current" window 4 weeks after the intervention, and about 69% of it (62 of 90 days) predates the intervention, so the comparison mostly measures itself. His value is more likely in a snapshot of where you are today versus where you were, or in a baseline handed over at the end of the engagement. |
| X5 | "Confidence fix: if 2 of 3 consultant connections say they would pay, Confidence goes to 5.0." | As a test design, this asks for future intent ("would you pay"), which the Mom Test does not count as evidence. It is replaced by a commitment test (see Riskiest assumptions). |

---

## Wave: DISCOVER / [REF] Dropped options

| Option | Why dropped |
|---|---|
| Synthesising baseline values where history is thin | ADR-109 rejected fabricated values. Story 6053 narrowed that rejection to synthesis and kept it. Missing data stays visibly absent. |
| Forward-only recording, where the baseline starts accruing on the day it is created | It defeats the job. The engagement start is usually today or in the past, so "before" has to come from history (V5). |
| Copying the ValueFlow dashboard export field for field as the spec | The field set is schema input only. DIVERGE decides what is core and what is nice-to-have (Q8). |
| Reusing the PBC Baseline setting as the feature's storage | It freezes nothing (X2). It has a different purpose (the window natural process limits are computed from), and changing it would move every PBC on the owner. |

---

## Wave: DISCOVER / [REF] Decision gate

| Gate | Status | Why |
|---|---|---|
| G1 Problem | **FAIL on threshold** | No Mom Test interviews by Lighthouse, against a threshold of 5 with more than 60% confirming. There are two signals: one consulting firm's past behaviour (V1, second-hand) and one consultant's stated interest after being pitched, which is future intent and was pitched before the problem was explored. The job is real for Thrivve. It is not shown for Lighthouse users, for Teams running their own experiments, or as something anyone pays for. |
| G2 Opportunity | **FAIL** | There are five desk opportunities (O1 to O5) but no importance or satisfaction data, so no score above 8 can be shown. O3 shows a shape divergence that is still unresolved. |
| G3 Solution | **FAIL** | Nothing tested. ValueFlow is Thrivve's solution, and Lighthouse users have not tested anything. |
| G4 Viability | **PARTIAL** | Feasibility is green for computation (V2) and amber for freezing (X1 to X3). The tier is decided (Community). Viability rests on an unvalidated land-and-expand chain: free result → reaches management → Premium conversation. There is one counter-signal: an evaluating prospect asked for signals and email reports, not a baseline. The success metric has no usage event yet (that is DEVOPS's job). |

**Proceeding is a maintainer-accepted risk.** On 2026-10-02 the maintainer decided to go on to DIVERGE on this
evidence. That makes this wave a learning investment, not a validated build case. The interview protocol below
runs alongside DIVERGE and DISCUSS and does not block them. If its results contradict DIVERGE's direction,
they reopen it.

---

## Wave: DISCOVER / [REF] Constraints established

| # | Constraint | Evidence source |
|---|---|---|
| C1 | Community tier. The Premium boundary is open (Q9). | Maintainer, 2026-09-20 (#5878) |
| C2 | User-facing copy uses configurable terms: Team, Portfolio, Delivery, Feature, Work Item, Cycle Time, Throughput, WIP, Work Item Age, SLE. Never "Epic" or "Story". | `TerminologySeeder.cs`; project rule |
| C3 | "Baseline" is not a Terminology key and already names the **PBC Baseline** in Settings. The feature's name must not collide with it silently. | `FlowMetricsConfigurationComponent.tsx:666`, `:687`, `:711` |
| C4 | No synthesised values. Where data is too thin for a window, it is shown as absent. | ADR-109 as narrowed by Story 6053 |
| C5 | Anything computed from history reflects today's configuration and item set. If a baseline promises to be "frozen", it has to store its values or say plainly that it does not. | Story 6053, X2 |
| C6 | A baseline window has to lie inside `DoneItemsCutoffDays` (default 365) at the moment it is computed. | `BaselineValidationService`, X3 |
| C7 | Any new storage is an expand-only migration created with `CreateMigration`. | Project rule; feedback on expand-only migrations |
| C8 | DEVOPS answers which usage-data event shows the feature is used. This is the only way to measure land-and-expand. | Project rule |

---

## Wave: DISCOVER / [REF] Key decisions

- [D1] One workspace, `epic-5878-baseline` (see: maintainer dispatch 2026-10-02).
- [D2] Proceed to DIVERGE with G1 to G3 failing, as a maintainer-accepted risk. This is a learning investment (see: maintainer decision 2026-10-02; Decision gate).
- [D3] ICE Confidence revised from 1.5 to **2.0**, and ICE from 54 to **72** (Impact 6 × Confidence 2.0 × Ease 6). The Thrivve evidence was already counted on 2026-09-20. The only new evidence is one stated interest given after a pitch, which is worth +0.5 at most (see: wave-decisions.md D3).
- [D4] Consultant #2 is graded as stated interest in a present, real context. His 4-week assessment is recorded as a shape divergence (O3, X4), not as confirmation of before/after (see: maintainer evidence 2026-10-02).
- [D5] Ease is verified for computation and amber for freezing (X1 to X3). DIVERGE must choose between stored, recomputed and hybrid baselines with these limits in view (see: codebase check 2026-10-02).
- [D6] The counter-signal (a prospect asked for signals and email reports) is kept, not dismissed. Email reports may be how the result reaches management (O4) (see: #5878 ICE note).

---

## Wave: DISCOVER / [REF] Pre-requisites

- Stored Work Item history that covers the baseline window and lies inside `DoneItemsCutoffDays`.
- Mapped To Do, Doing and Done states on the owner, plus the existing minimum-data guards for percentiles and Throughput.
- An SLE, or a decided fallback, if "SLE 85th percentile" is a core metric (Q8). SLE is optional on owners.
- An intervention or engagement date, supplied by the user. Lighthouse has no such event today.

---

## Wave: DISCOVER / [REF] Riskiest assumptions & cheapest next test

Risk = Impact×3 + Uncertainty×2 + Ease of testing×1.

| # | Assumption | Cat. | Score | Cheapest next test |
|---|---|---|---|---|
| R1 | Consultants other than Thrivve already produce before/after evidence by hand, so there is a workaround that costs them something. | Value | 3·3+3·2+1 = **16** | Protocol below with 5 consultant-shaped connections, starting with "Tell me about the last time…" |
| R2 | Showing the result to the client's management leads to a Premium conversation (land-and-expand). | Viability | 3·3+3·2+3 = **18** | Ask Thrivve, about past engagements, what happened after management saw the baseline. Did it lead to more work or a purchase? |
| R3 | Teams outside consulting run improvement experiments and want a frozen "before". | Value | 2·3+3·2+1 = **13** | 2-3 questions added to existing user conversations: "When did you last change how you work? How did you tell whether it helped?" |
| R4 | A short-assessment shape (4 weeks) can use the same feature. | Value/Usability | 2·3+3·2+1 = **13** | Consultant #2: ask what he will show the client at the end of the 4 weeks, and how. Ask to see the artefact. |
| R5 | Users trust a recomputed "before" that might drift. | Value | 2·3+2·2+2 = **12** | DIVERGE option comparison; spike: how often does a 90-day percentile move when re-run a month later on the dev database? |

### Interview protocol (open action, non-blocking)

Target: 5 consultant-shaped people, including consultant #2 and at least one sceptic or non-user, plus 2-3
Teams. Do not mention the feature until the last block.

1. "Tell me about your last engagement or improvement effort. When did it start, and what did you change?"
2. "At the end, how did you show the client (or yourself) whether it worked? Can you show me what you used?" Look for an artefact, such as a slide or spreadsheet.
3. "How long did putting that together take? What was the hardest part?"
4. "Which numbers did management ask about? What did they do after seeing them?" This tests R2.
5. "Did anyone doubt the 'before' numbers? What happened?" This tests R5.
6. Short-assessment variant: "At the end of your 4 weeks, what will you hand over? Who will use it after you leave?"
7. Commitment, only after 1-6: "Would you run your current or next engagement's baseline in Lighthouse and show us the result?" Also ask for an introduction to another consultant. Count actions, not "yes".

Confidence moves on **commitments**. At 3 of 5 consultants describing a manual workaround (R1) and 2
committing to use it on a live engagement, Confidence becomes about 4.0 and ICE about 144. If any of them
shows the artefact to client management and reports what followed (R2), Confidence reaches 5.0 and ICE
about 180. Fewer than 1 in 5 describing a workaround (under 20%) is a kill or pivot signal for the consulting
shape.

---

## Wave: DISCOVER / [REF] Open questions for DIVERGE

1. **Scope**: Team, Portfolio or Delivery? Does one baseline cover several owners, as in an engagement that spans Teams? Delivery metrics are recorded forward-only (`DeliveryMetricSnapshot`), so check whether a past Delivery window can be recomputed at all.
2. **One or many baselines** per owner: one per engagement or experiment, named and dated? Many baselines suit consultants who come back, or a Team running several experiments.
3. **Frozen-stored, recomputed through history, or hybrid** (stored at creation, re-computable on request with a "differs from frozen" flag)? Weigh against X1 to X3 and C5/C6.
4. **What is "current"**: today's rolling window of the same length, a "since the intervention" window, or both? A rolling window is mostly pre-intervention for a long time (X4).
5. **Surface**: a page or widget, a printable report or PDF (Spike 6052's server-side chart rendering is the closest prior work), an export, or an email report (O4, D6). Which one reaches management?
6. **The 4-week-assessment shape**: is it a retroactive "where you are today vs where you were", or a baseline frozen at engagement end and handed to the client? Same feature or a separate one?
7. **Signal vs noise**: should a difference be judged against PBC limits or stability before it is called an improvement (O5)?
8. **Core vs nice-to-have metrics** from the ValueFlow field set. Candidates for core: cycle time percentiles (50/70/85), Throughput weekly median, WIP average and range, total Work Item Age plus average Work Item Age, intervention date and days since. Nice-to-have: a separate 30-day SLE, WIP streak counts, PBC breach count and stability state.
9. **Community vs Premium boundary**: the comparison is Community (C1). Candidates for a Premium lever: many baselines, cross-owner baselines, a branded or exported management report, scheduled email. The land-and-expand logic argues against gating the artefact that is supposed to reach management.
10. **Naming and terminology**: how does it relate to the existing PBC Baseline (C3)? Does "Baseline" or "Intervention" need a Terminology key? All copy uses the configured terms (C2).
11. **Who may create a baseline** (RBAC: settings editor or reader), and is it in scope for CLI/MCP (DISCUSS checklist)?

---

## Wave: DIVERGE / [REF] Maintainer decisions on the open questions

Recorded 2026-10-02 as D8-D16 in `wave-decisions.md` (source: maintainer 2026-10-02). They close questions 1-6
and 11 and reshape 7.

| Q | Decision |
|---|---|
| 1 | Teams and Portfolios, flow metrics mainly; the forecast comparison is a Team-only maybe (D8). Delivery is not in scope. |
| 2 | Many reports per owner (D9). |
| 3 | Stored and frozen at creation with the settings then in force (D10). |
| 4 | Now is a selectable rolling window; the Then window is picked at creation (D11) and may end in the past, rebuilt once from history inside the cutoff (D12). |
| 5 | Reports tab → Create Report → Template; live in-app first, PDF/email later (D13). |
| 6 | The 4-week assessment is covered by a selectable Now window plus a Then frozen at hand-over (D14). |
| 7 | The user selects comparison items from a list; each item has a rule that judges it good or not; v1 is limited and the catalog extensible (D16). |
| 11 | Editors create, readers view; CLI/MCP out of scope (D15). |

---

## Wave: DIVERGE / [REF] Recommendation

Full text: `recommendation.md` · artifacts: `diverge/` · decisions DV-1..DV-9: `wave-decisions.md`.

- **Direction**: Option 3, **"Claims checklist"** (4.35 of 5; runner-up "Signal catalog" 4.00; dissent
  "Threshold scorecard" 3.90). The selectable unit is a plain-language claim. The metric, its direction of good
  and its rule form one registered definition. The report lists the chosen claims with Holds / Does not hold /
  No change yet / Not enough data, shows Then → Now values on the row, and opens the chart on expand.
- **Q7, rules**: "trending" means a process-behaviour-chart shift against XmR limits **frozen from the Then
  window**, using the existing `XmRCalculator.Calculate(thenValues, nowValues)`. It is not a slope and not a
  percentage. There are four rule kinds: shift in direction, no shift against, no signals in window, and a
  threshold change only where no series exists.
- **Q7, v1 catalog**:
  - C1 {Cycle Time} trending down;
  - C2 {Throughput} trending up;
  - C3 {WIP} stable or down;
  - C4 Total {Work Item Age} stable or down;
  - C5 {Cycle Time} predictable;
  - C6 85th percentile {Cycle Time} lower by 10% or more.
- **Q7, seam**: one `ComparisonItemDefinition` per claim, and one class per rule kind. Templates are preset
  selections, and the catalog is independent of Reports.
- **Q8**: core values are Cycle Time 50/70/85th, Throughput weekly median, WIP average and range, Total Work Item
  Age plus average Work Item Age, and days since Then (header). Nice-to-have: SLE breaches, streaks, arrivals,
  Feature size, and the Team-only forecast lens.
- **Q9**: Community gets every claim and rule and **2 reports per Team/Portfolio**; Premium is unlimited. Rules
  and claims are never gated. The export tier is decided when export is built.
- **Q10**: the template is called **"Then & Now"** (runner-up "Before & After"). No user-facing "Baseline"
  (C3). Every metric word comes from Terminology.
- **Freezing**: every applicable claim is frozen at creation. Claims added later show "Not captured for this
  report". A settings fingerprint flags a Now side computed under changed settings.
- **Job**: `job-flow-coach-show-whether-flow-changed` (new in `docs/product/jobs.yaml`).

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent; the maintainer was not
available mid-run, so questions are collected below with provisional assumptions. Peer review is dispatched by the
coordinator, not run here. Config: user-facing (full stack), brownfield walking skeleton, research depth
comprehensive (reusing DIVERGE), JTBD on, density lean.

| Read | Status |
|---|---|
| `docs/product/jobs.yaml` (`job-flow-coach-show-whether-flow-changed`) | ✓ |
| `docs/product/journeys/epic-5878-baseline.yaml` | ✓ (refined this wave) |
| `docs/product/vision.md` | ⊘ does not exist |
| `feature-delta.md` DISCOVER + DIVERGE sections | ✓ |
| `discover/wave-decisions.md` (D1–D7) | ✓ |
| `wave-decisions.md` (D8–D16, DV-1..DV-9, "Resolved by the maintainer (2026-10-02)") | ✓ |
| `recommendation.md` | ✓ |
| `diverge/job-analysis.md` | ✓ (light JTBD: job reused, not re-derived) |
| `diverge/competitive-research.md`, `options-raw.md`, `taste-evaluation.md` | ⊘ not read; the recommendation's summary was enough |
| House layout: `epic-5510-5881-refinement` DISCUSS sections, slice-01 brief, `discuss/wave-decisions.md` | ✓ |
| Personas `flow-coach`, `delivery-lead-rte` | ✓ |
| Brownfield code (targeted, below) | ✓ |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| # | What exists | Where | Consequence |
|---|---|---|---|
| S1 | Team tabs: Features, Forecasts, Metrics, Settings (admins), Access (admins, RBAC on). Portfolio tabs: {Features}, {Deliveries}, Metrics, Settings, Access | `TeamDetail.tsx:506-515`, `PortfolioDetail.tsx:481-487` | A **Reports** tab is one more `<Tab>` on each page; gating copies `rbac.isTeamAdmin` / `rbac.isPortfolioAdmin`. |
| S2 | RBAC requirements `TeamRead/TeamWrite/PortfolioRead/PortfolioWrite`; roles SystemAdmin, TeamAdmin, PortfolioAdmin, Viewer. With RBAC off, `useRbac()` is permissive | `RbacGuardRequirement.cs`, `UserRole.cs`, `useRbac.ts` | "Edit rights" = Write (Team/Portfolio Admin). Create and delete are Write; viewing is Read. Auth off: everyone may create. |
| S3 | `XmRCalculator.Calculate(baselineValues, displayValues)` classifies display points against limits from the baseline values; four special-cause rules; lower limit clamped to 0 (and a clamped line disables the rules that need it) | `XmRCalculator.cs:15-55` | The verdict engine exists. **`XmRResult` rounds average and limits to integers**, so a daily {Throughput} average of 0.6 reads as 1 — the report must freeze the Then **series**, not only the rounded limits (D26). |
| S4 | PBC builders take the owner's PBC Baseline (`ProcessBehaviourChartBaseline*`) when set, else the display window | `BaseMetricsService.cs:584-760` | The report must take its limits from **Then**, never from the owner's PBC Baseline setting, or changing that setting would move the "frozen" yardstick (C5). |
| S5 | Series per owner: {Throughput}, {WIP}, Total {Work Item Age} (daily) and {Cycle Time} (per finished {Work Item}) PBCs, with `asOf`; {Cycle Time} percentiles over `(start, end)` | `TeamMetricsService.cs:178-310`, `PortfolioMetricsService.cs:35-71` | C1–C6 are computable for Team and Portfolio from existing calls (DISCOVER V2). |
| S6 | `BaselineValidationService`: ≥14 days, end not in the future, start inside `DoneItemsCutoffDays` | `BaselineValidationService.cs` | Reused as the Then-window rule (D21); its messages say "Baseline", so the report needs its own copy (C3). |
| S7 | Metrics window presets: Team 7/14/30/90, Portfolio 30/90/180 days; step-back by 7/28 days; calendar days in the viewer's zone (Bug #5566) | `dateWindow.ts:19-33` | Then and Now lengths reuse these presets (D20, D22). |
| S8 | Community/Premium count cap precedent: `CanUsePremiumFeatures() \|\| count < 2` | `AdditionalFieldsHelper.cs` | The 2-report cap copies it (D29). |
| S9 | Usage data: `TeamTabOpened`/`PortfolioTabOpened` carry a route key; the enum ends at `TeamForecastRealityCheckRun = 11` on this checkout | `UsageDataEventName.cs`, `UsageDataRoutePatterns.cs` | Tab opens need new **route keys**, not event names. New names append; DEVOPS checks `main` for the next free integer. |
| S10 | Terminology keys: workItem(s), feature(s), cycleTime, throughput, wip, workItemAge, sle, team(s), portfolio(s), delivery/deliveries … no "report" | `TerminologySeeder.cs` | No new key needed (D32). |
| S11 | Demo data dates are relative to load day. History depth: Team Lightspeed ≈100 days, Team Gravity ≈35, Portfolios 7–10 finished {Features} over ≈85 days | `DemoDataFactory.cs:110-126`, `Factories/DemoData/*.csv` | A 90-day Then window fits only Lightspeed; Portfolio demos will mostly read "Not enough data" — useful for the honesty path, poor for a Holds screenshot. |

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` (consulting) | **Primary.** Elena Kovacs, external flow coach engaged with Team Lightspeed since 1 Aug 2026; Team Admin on that Team. Freezes Then at the engagement start and shows the result to the sponsor. |
| `flow-coach` (internal) | **Primary, second circumstance.** Priya Raman, coach of Team Gravity, testing a WIP limit the Team adopted on 14 Sep 2026. |
| `delivery-lead-rte` | **Reader.** Martin Achterberg, Head of Delivery, Viewer on Team Lightspeed. Reads the verdicts; never creates. |
| `config-admin` | Present only as the editor role (Team/Portfolio Admin); often the coach. |

No new persona. `flow-consultant` stays uncreated: nothing in this wave separates the payer's job from the coach's.

## Wave: DISCUSS / [REF] JTBD One-Liner

- `job-flow-coach-show-whether-flow-changed` (DIVERGE, reused unchanged): when the way work flows has changed and
  someone asks whether it helped, set today's flow against a "before" that cannot move, judged by rules agreed before
  looking, so the answer is evidence — not noise presented as progress. Every story below carries this `job_id`.
- Reader job (candidate, not a `jobs.yaml` entry): `delivery-lead-rte` reads in one look which claims hold and
  whether to trust them. Served by the verdict layout; no story of its own.

---

## Wave: DISCUSS / [REF] Changed Assumptions

DISCOVER documents are not edited; these supersede them for DESIGN onward.

| Was (source) | Now | Why |
|---|---|---|
| "The codebase has no 'previous period' comparison" (DISCOVER V4) | **Partly contradicted.** Story 5914 shipped Metrics window presets with step-back (`shiftWindow`), and some widgets already compare the current period with the previous one (Work Item Age percentiles, predictability score). None of it is frozen. | Codebase, `dateWindow.ts`, `useMetricsData.test.ts:1155`. The differentiation (a frozen Then) stands; the habit force is stronger than DISCOVER assumed. Copy never calls Then "previous period". |
| Step 1 "pick the engagement start date"; `intervention_date` artifact (DISCOVER journey) | No intervention-date field. **The Then window's end date is the anchor**, and the header shows days since Then ended. | Converged direction (D11, D12, recommendation §3). One date fewer to enter and to keep consistent. |
| Step 3 "hand over a result management reads without Lighthouse open" (DISCOVER journey) | v1 is read **in Lighthouse**, by a Viewer. PDF/email is out of scope, not blocked. | D13. |
| Journey job ids `job-flow-coach-prove-engagement-value`, `job-flow-coach-hand-over-a-starting-point` | Both re-pointed to `job-flow-coach-show-whether-flow-changed`. The short assessment is a Then that ends today, frozen at hand-over. | DIVERGE note; D14. |
| "Freeze the Then values and the frozen limits" (recommendation §3) | Freeze the Then **series** as well; limits are recomputed from it on each view. | S3: `XmRResult` rounds limits to integers. |
| "Holds: ≥1 special cause on the good side" (DV-3 table) | **Provisional:** "trending" holds only on a *sustained* signal; a lone point beyond a limit is shown but does not make a trend. | D24, Q3: a single batch-closing day would otherwise read "{Throughput} is trending up". |

---

## Wave: DISCUSS / [REF] Locked Decisions

Numbering continues after D16 (DIVERGE). "Provisional" = autonomous call pending the maintainer (see Open questions).

- [D17] **One job, every story**: `job_id: job-flow-coach-show-whether-flow-changed`. No new job or persona.
- [D18] **Tab placement.** Team: Features · Forecasts · Metrics · **Reports** · Settings · Access (Refinement from
  epic-5510, when it lands, sits between Metrics and Reports). Portfolio: {Features} · {Deliveries} · Metrics ·
  **Reports** · Settings · Access. The tab is always enabled for anyone with read. Empty state: editors see
  "Create Report"; readers see "No reports yet. A {Team} Admin can create one." (Portfolio: "{Portfolio} Admin").
- [D19] **Create flow**: Create Report → choose a Template (one card in v1: "Then & Now — set today's flow against
  a window that stays as it was") → Then window → claims (C1–C4 preselected) → name, defaulted to
  "Then & Now — 90 days to 31 Jul 2026" → Create. The new report opens. Renaming later is out of v1 (provisional, Q7).
- [D20] **Then lengths** are the Metrics presets of at least 14 days: Team 14 / 30 / 90, Portfolio 30 / 90 / 180.
  No custom length in v1 (provisional, Q1). Windows are calendar days, inclusive at both ends, in the instance's
  time zone: 90 days ending 31 Jul 2026 = 3 May – 31 Jul 2026.
- [D21] **Then validation** at creation: end date not after today; whole window inside `DoneItemsCutoffDays`
  (default 365). Refusal names the earliest allowed start: "The Then window must start on or after 2 Oct 2025 —
  older finished {Work Items} are no longer kept." Never silently partial (DISCOVER journey failure mode).
- [D22] **Now** is a rolling window ending today. Default length = Then length. Anyone viewing may pick another preset
  (Team 14/30/90, Portfolio 30/90/180); the choice lives in the page address like the Metrics tab and is not saved on
  the report (provisional, Q6).
- [D23] **Now never reaches back into Then** (provisional, Q2). Now starts no earlier than the day after Then ends.
  While nothing of Now lies after Then (a report frozen today), every PBC claim reads **No change yet** with
  "Now begins after 2 Oct 2026". The header names the clipped window: "Now: last 90 days, from 1 Aug 2026 (after Then)".
- [D24] **Verdict rules** (provisional on the "sustained" point, Q3). Special-cause classes are the four in
  `XmRCalculator`; Now points are classified against limits computed from the frozen Then series.
  - *Trending {dir}* (`ShiftInDirection`): **Holds** when a sustained signal (2 of 3 beyond 2σ, 4 of 5 beyond 1σ, or
    8 in a row) is on the good side and nothing is on the bad side; **Does not hold** on any signal on the bad side
    (both sides firing = Does not hold, detail lists both); otherwise **No change yet**. A lone point beyond a limit
    on the good side is named in the detail ("1 {Work Item} finished unusually fast") and does not make a trend.
  - *Stable or {dir}* (`NoShiftAgainst`): **Does not hold** on any signal on the bad side, a lone point included —
    claiming improvement needs sustained evidence, while any warning counts against. Otherwise **Holds**.
  - *Predictable* (`NoSignalsInWindow`): Now on its own limits; any signal = Does not hold.
  - *Lower by ≥ 10%* (`ThresholdChange`): Then vs Now value; within ±10% = No change yet.
- [D25] **Not enough data** (provisional numbers, Q4) — shown as "—" with a reason, never as 0 (C4): per-{Work Item}
  claims (C1, C5) need ≥ 8 finished {Work Items} in Then (the run rule's length) and ≥ 1 in Now; daily claims (C2–C4)
  need a Then series that is not all zero; C6 uses the existing percentile minimum-data guard on each side.
- [D26] **What is frozen at creation**, for every claim that applies to the owner (not only the selected ones,
  DV-6): the Then series, the Then values shown on the row, Then's own signal state, readiness; plus the creation
  date, the window, the template key, the selection and a snapshot of the settings that shape the values (D31).
  Then is never recomputed. Creation either completes or fails whole; nobody sees a half-frozen report.
- [D27] **Claims added to the catalog after a report was created** show "Not captured for this report" and cannot be
  selected on it (DV-6).
- [D28] **Zero-clamp honesty** (R-D): when Then's lower limit sits at zero, the downward claim's detail says "The lower
  limit is at zero, so a fall in {Cycle Time} shows mainly as a run of {Work Items} below the Then average."
- [D29] **Cap**: Community holds **2 reports per {Team}/{Portfolio}, counted across all templates**; Premium is
  unlimited. At the cap, Create Report is disabled with "Community includes 2 reports per {Team}. Delete one, or use
  Premium for unlimited reports." The server refuses too. On licence lapse every existing report stays viewable and
  deletable; creating stays blocked until the count is below 2. Frozen data is never deleted by the licence. A
  report is deleted with its {Team}/{Portfolio} (provisional, Q5).
- [D30] **Delete**: editors only, confirm dialog naming the report; when Then now starts before the data cutoff the
  dialog adds "This Then window can no longer be rebuilt."
- [D31] **Settings-changed notice**: the snapshot covers what changes a v1 claim's values — {Work Item} types, the To Do
  / Doing / Done state mapping, the query, blackout days. When today's differ: "Settings changed since this report
  was created (state mapping). Then stays as frozen on 2 Oct 2026; Now uses today's settings." Shown once in the
  header, not per row.
- [D32] **Words**: template "Then & Now"; tab "Reports"; windows "Then" / "Now". No user-facing "Baseline" (C3).
  "Report", "Then", "Now" are not Terminology keys; every metric word is a Terminology token. Copy never says Epic,
  Story or Initiative.
- [D33] **Usage data** (DEVOPS designs): tab opens reuse `TeamTabOpened` / `PortfolioTabOpened` with new route keys;
  name-only candidates for creation, deletion and the cap refusal (see Checklist).
- [D34] **Release safety**: slices 01–07 are safe on trunk before the cap (08) — D29's lapse rule already covers
  owners holding more than 2 reports. Release notes, docs and website copy wait until slice 08 is in.
- [D35] **No intervention date.** The header reads "Then: 90 days to 31 Jul 2026 — frozen · 63 days since Then ended".

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS, at the boundary.** 11 stories (one over 10), ~10 days, one new module (reports and the claim catalog)
standing on existing metrics, licence and RBAC; one user outcome; nothing ships independently of the Reports tab.
Two signals fire marginally (story count, ~2 weeks). **Provisional proposal, no Epic split:** keep all slices in
#5878 and cut the release after slice 09; slices 10 (change the shown claims) and 11 (claim chart) are cancellable
follow-ups the maintainer can move to a later Story set without touching the rest.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone (flow coach, then reader)**: *open Reports* → *freeze Then* → *choose claims* → *read Then & Now* →
*trust the verdict* → *manage reports*.

| Open Reports | Freeze Then | Choose claims | Read Then & Now | Trust the verdict | Manage |
|---|---|---|---|---|---|
| **01 tab + create (Team)** | **01 Then ends today** | **01 C1 values** | **01 header + row** | 03 C1 verdict, Now after Then | 08 delete + cap |
| 06 Portfolio | 02 Then ended in the past | 04 C2–C4 + picker | 07 Now window + days since | 09 settings notice | 10 change shown claims |
| | | 05 C5 + C6 | | 11 claim chart | |

**Walking skeleton = slice 01** (brownfield): existing Team page and tab pattern, existing metrics calls, a new stored
report. It crosses every activity thinly — tab → create → freeze → view Now live — and proves the freeze-and-store
shape that every later slice extends.

| # | Slice | Est. | Learning hypothesis — disproves … if it fails |
|---|---|---|---|
| 01 | Reports tab; create a Then & Now report for a Team; Then ends today; Then → Now {Cycle Time} 50/70/85 | 1d | "Freezing at creation is fast and simple enough" — if creating a 90-day report on the dev instance's busiest Team takes > 10 s, freezing must move off the request |
| 02 | A Then window that ended in the past | 1d | "History rebuilds a past Then credibly" — if a Then ending 31 Jul rebuilt today differs from the Metrics tab for the same dates, the rebuild is wrong |
| 03 | "{Cycle Time} is trending down" verdict from limits frozen at Then; Now starts after Then | 1d | "PBC rules give a usable verdict on real Teams" — if on the dev instance C1 is No change yet / Not enough data for every Team over 63 days, the zero clamp and low counts make the claim mute and the rule (D24/D25) must change |
| 04 | Pick claims: C2 {Throughput} up, C3 {WIP} stable or down, C4 Total {Work Item Age} stable or down; C1–C4 preselected; every claim frozen | 1d | "Coaches keep the preselection" — if dogfood users deselect two or more of C1–C4, the preselection is wrong |
| 05 | C5 {Cycle Time} predictable; C6 85th percentile lower by ≥ 10% (with {SLE} reference) | 1d | "A sponsor needs the familiar percentage beside the verdict" — if C6 is selected on fewer than 1 in 3 dogfood reports, it can leave the default catalog later |
| 06 | Reports on Portfolios | 1d | "Claims say something at Portfolio level" — if every demo and dev Portfolio reads Not enough data on C1, Portfolio needs other claims (e.g. {Feature} size) before it is useful |
| 07 | Choose the Now window; days since Then ended | ½d | "The rolling Now answers the 4-week shape" (D14) — if consultant #2's case still needs a since-the-start window, D11 is reopened |
| 08 | Delete; Community cap of 2 across templates; Premium unlimited; lapse | 1d | "2 reports is enough for Community" — if dogfood or early instances hit the cap within a month, the cap is either a Premium lever (good) or a blocker (check K4) |
| 09 | Settings-changed notice | 1d | "Users understand a frozen Then under changed settings" — if the dogfood coach rebuilds the report after the notice instead of reading on, the wording fails |
| 10 | Change which claims a report shows | ½d | "Freezing every claim pays off" — if nobody changes a selection in a month, DV-6's extra freezing was not needed (keep, but stop extending) |
| 11 | Open a claim's chart: Now points against Then limits | 1d | "The verdict needs its chart to be trusted" — if readers never expand a row, the chart can stay out of later templates |

Briefs: `slices/slice-NN-*.md`.

### Prioritisation rationale

Order 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11.

- **01** is the walking skeleton.
- **02, 03 next — riskiest first.** The past-dated rebuild (D12, X2) and the verdict on real, low-count,
  zero-clamped series (R-D, R-C) are the two assumptions that could make the whole report wrong or mute. 02 comes
  first because a verdict can only be dogfooded against a Then that ended weeks ago; 03 brings the verdict and the
  rule that Now starts after Then (D23), without which a verdict would judge Then against itself.
- **04, 05** complete the Team catalog (value). **06** Portfolio after the claims exist (same rules, thinner data).
- **07** before **08** because the Now choice serves the 4-week shape (D14); **08** before release (D34).
- **09** guards trust over time; it matters only once reports are weeks old. **10, 11** are cancellable.

### Slice taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice? | Pass — the largest (01) adds a tab, a create dialog and a report view. |
| Every slice depends on a new abstraction? | Pass — the claim definition is born in 01 with one claim, its first consumer. |
| Disproves a pre-commitment? | Pass — 02 tests D12, 03 DV-3, 07 D14, 08 DV-7. |
| Synthetic-data-only slices? | Pass — each is dogfooded on the dev instance (`:5169`, real history); demo data covers E2E and screenshots only. |
| Two slices alike except scale? | Pass — 06 is the same report on a different owner with different data shape, kept separate on purpose. |

---

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-5878-baseline.yaml` (refined this wave). Emotional arc: **exposed → anchored →
vindicated or honestly informed**. The reader's arc: **sceptical → oriented in one look**.

```
Team Lightspeed › Reports › Then & Now — engagement start
┌────────────────────────────────────────────────────────────────────────────┐
│ Then: 90 days to 31 Jul 2026 — frozen        63 days since Then ended      │
│ Now:  [last 30 days ▾] 3 Sep – 2 Oct 2026                                  │
│ ⚠ Settings changed since this report was created (state mapping).          │
├────────────────────────────────────────────────────────────────────────────┤
│ ● Holds          Cycle Time is trending down                              │
│                  50/70/85th: 6 / 11 / 21 days → 4 / 7 / 12 days            │
│ ● Holds          WIP is stable or down        avg 9 (6–12) → 7 (5–9)        │
│ ○ No change yet  Throughput is trending up    weekly median 4 → 5           │
│ ● Does not hold  Total Work Item Age is stable or down   avg 61 → 88 days   │
│ — Not enough data  Cycle Time is predictable  (6 finished Work Items in Now)│
└────────────────────────────────────────────────────────────────────────────┘
```

Shared artifacts (registry, single source each):

| Artifact | Source of truth | Consumers |
|---|---|---|
| `then_window` (start, end, length) | the stored report | header, create dialog default name, days-since, Now clipping, delete warning |
| `then_series` / `then_values` | frozen at creation (D26) | every claim row, verdict limits, claim chart |
| `now_window` | page address (D22), clipped by `then_window` (D23) | header, verdict, row values |
| `now_values` | existing metrics calls over `now_window`, today's settings | rows, verdicts |
| `settings_snapshot` | frozen at creation | settings-changed notice (09) |
| `report_count` per owner | stored reports of that owner, all templates | Create Report enablement, cap message (08) |
| Terminology tokens | Settings → Terminology | every claim sentence and message |

Integration checkpoints: Then rows never change between views (slice 01 onwards); the Then window in the header
equals the one used for limits; the cap counts every template; the Now window shown is the one judged.

---

## Wave: DISCUSS / [REF] User Stories

<!-- markdownlint-disable MD024 -->

System constraints (all stories): Team and Portfolio only (D8) · every metric word through Terminology · RBAC: create
and delete need Write on the owner, viewing needs Read; UI gating via `useRbac()`, never fetching
`/api/latest/authorization/my-summary` · works with auth off · Then limits come from the frozen Then series, never
the owner's PBC Baseline (S4) · no synthesised values; absent data shows "—" with a reason · expand-only migrations
via `CreateMigration` · Community unless stated · calendar days in the instance time zone, windows inclusive at both
ends · no CLI/MCP, no export.

### US-01 — Freeze a Then & Now report for a Team (slice 01, walking skeleton)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena Kovacs, Team Admin, Team Lightspeed)

**Problem**: Elena starts an engagement with Team Lightspeed today. To show later whether flow changed she exports
{Cycle Time} percentiles into a spreadsheet; nothing in Lighthouse keeps today's numbers from moving.

#### Elevator Pitch

Before: a coach can read today's {Cycle Time} on the Metrics tab, but nothing keeps it as it was.
After: Team Lightspeed → **Reports** → **Create Report** → "Then & Now", Then 90 days ending today → sees the report
"Then: 90 days to 2 Oct 2026 — frozen" with {Cycle Time} 50/70/85th "6 / 11 / 21 days → 6 / 11 / 21 days".
Decision enabled: whether to use this frozen Then as the engagement's yardstick and come back to it.

**Examples**: (1) Elena creates "Then & Now — engagement start" on Team Lightspeed; Then 4 Jul – 2 Oct 2026 shows
6/11/21 days. (2) Martin Achterberg (Viewer) opens the Reports tab and sees the report but no Create Report button.
(3) Team Zenith has no reports; Elena sees the empty state with Create Report; Jonas Weber (Viewer) sees "No reports
yet. A Team Admin can create one."

```gherkin
Scenario: A coach freezes today's Cycle Time for a Team
  Given Team Lightspeed has finished Work Items between 4 Jul and 2 Oct 2026
  When Elena creates a "Then & Now" report with a Then of 90 days ending today
  Then the report shows "Then: 90 days to 2 Oct 2026 — frozen"
  And Cycle Time 50th / 70th / 85th percentile for Then and for Now

Scenario: The frozen Then does not move when history changes
  Given Elena created the report on 2 Oct 2026 with Then 85th percentile 21 days
  And a Work Item finished in September is later removed from the Team's query
  When Elena opens the report
  Then Then still shows 21 days at the 85th percentile

Scenario: A reader sees reports but cannot create one
  Given Martin is a Viewer on Team Lightspeed
  When Martin opens the Reports tab
  Then he sees "Then & Now — engagement start" and no Create Report button
```

**AC**: AC-1.1 Reports tab on every Team after Metrics (D18), readable with Team read. AC-1.2 Create Report (Write
only, server-guarded) offers the one template "Then & Now"; Then length 14/30/90, ending today. AC-1.3 The report
stores frozen Then values (and series, D26) for {Cycle Time} and a settings snapshot; Then never recomputes. AC-1.4 Now
= same length ending today, computed on view. AC-1.5 Role-specific empty state (D18). AC-1.6 Default name per D19.
**KPI**: K1. **Tech**: first migration (report storage, expand-only); claim-definition seam born with one claim;
tab-open route key `TeamDetail_Reports`.

### US-02 — Freeze a Then that ended in the past (slice 02)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena)

**Problem**: Elena's engagement started on 1 Aug 2026, two months before anyone thought of a report. The "before" is
already history.

#### Elevator Pitch

Before: Then always ends today.
After: Create Report → Then 90 days, **ends on 31 Jul 2026** → sees "Then: 90 days to 31 Jul 2026 — frozen"
with values rebuilt from history.
Decision enabled: whether the engagement start can still serve as the yardstick, or whether it is too late.

**Examples**: (1) 90 days to 31 Jul 2026 = 3 May – 31 Jul, rebuilt, 6/11/21 days. (2) 90 days ending 15 Nov 2025 is
refused: "The Then window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept."
(3) An end date of 5 Oct 2026 cannot be picked (future).

```gherkin
Scenario: A coach freezes a Then that ended at the engagement start
  Given Team Lightspeed's engagement started on 1 Aug 2026
  When Elena creates a report with a Then of 90 days ending 31 Jul 2026
  Then the report shows Then 3 May – 31 Jul 2026 with values matching the Metrics tab for those dates

Scenario: A Then reaching past the data cutoff is refused with the earliest start
  Given Team Lightspeed keeps finished Work Items for 365 days
  When Elena picks 90 days ending 15 Nov 2025
  Then creation is refused naming 2 Oct 2025 as the earliest allowed start

Scenario: A future end date cannot be chosen
  When Elena opens the Then end-date picker on 2 Oct 2026
  Then no date after 2 Oct 2026 can be picked
```

**AC**: AC-2.1 End-date picker, default today, bounded by D21. AC-2.2 Rebuilt values equal the Metrics tab's for the
same window and today's settings. AC-2.3 Refusal copy names the earliest start; never "Baseline". AC-2.4 Rebuilt once;
later views read the frozen values.
**KPI**: K1, K5. **Tech**: same calls with a shifted window (Story 6053 precedent); calendar-day handling per Bug #5567.

### US-03 — Know whether {Cycle Time} is trending down (slice 03)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena) and reader Martin

**Problem**: Martin sees 21 → 12 days and asks "is that real or a good month?". Elena has no answer Lighthouse backs.

#### Elevator Pitch

Before: the report shows Then → Now numbers with no judgement.
After: Team Lightspeed → Reports → the report → sees "● Holds — Cycle Time is trending down · 8 Work Items in a row
finished below the Then average of 9 days".
Decision enabled: whether to tell the sponsor {Cycle Time} improved, or that it has not changed yet.

**Examples**: (1) Lightspeed: 8 Work Items in a row below the Then average → Holds. (2) Team Gravity: Now has two
Work Items above the Then upper limit (34 days) → Does not hold, detail names them. (3) Team Pulsar: 5 finished Work
Items in Then → "— Not enough data · 5 finished Work Items in Then; 8 needed". (4) A report frozen today → No change
yet, "Now begins after 2 Oct 2026".

```gherkin
Scenario: A sustained fall in Cycle Time holds
  Given Team Lightspeed's Then average Cycle Time is 9 days
  And the last 8 Work Items finished in Now all took less than 9 days
  When Elena opens the report
  Then "Cycle Time is trending down" reads Holds

Scenario: A rise against the frozen limits does not hold
  Given Team Gravity's Then upper limit is 34 days
  And two Work Items finished in Now took 41 and 38 days
  When Priya opens the report
  Then "Cycle Time is trending down" reads Does not hold and names both Work Items

Scenario: Thin history is stated, not shown as zero
  Given Team Pulsar finished 5 Work Items in Then
  When Priya opens the report
  Then the claim reads Not enough data with "5 finished Work Items in Then; 8 needed"

Scenario: Then is never judged against itself
  Given Then ends on 2 Oct 2026 and today is 2 Oct 2026
  When the consultant opens the report
  Then "Cycle Time is trending down" reads No change yet with "Now begins after 2 Oct 2026"

Scenario: The zero lower limit is disclosed
  Given Team Lightspeed's Then lower limit for Cycle Time is at zero
  When Elena expands the claim
  Then she reads that a fall shows mainly as a run of Work Items below the Then average
```

**AC**: AC-3.1 Verdict per D24 against limits recomputed from the frozen Then series. AC-3.2 Four verdict states;
colour only for Holds / Does not hold. AC-3.3 Not enough data per D25, "—" plus reason. AC-3.4 Zero-clamp text per
D28. AC-3.5 A lone point beyond a limit on the good side is named but reads No change yet. AC-3.6 Now starts no
earlier than the day after Then ends (D23); the header shows the clipped window.
**KPI**: K2. **Tech**: reuse `XmRCalculator.Calculate(frozenThen, now)`; rule kind `ShiftInDirection` as its own
class. Demo data: make one demo Team's history yield a Holds (S11).

### US-04 — Choose the claims a report makes (slice 04)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Priya Raman, Team Gravity)

**Problem**: Priya's Team adopted a WIP limit on 14 Sep 2026. The sponsor cares about {WIP} and {Throughput}, not
only {Cycle Time}, and Priya wants the claims fixed before she sees the numbers.

#### Elevator Pitch

Before: a report makes one claim.
After: Create Report → claims list with C1–C4 ticked → sees four rows: Cycle Time trending down, Throughput trending
up, WIP stable or down, Total Work Item Age stable or down, each with its verdict.
Decision enabled: which flow claims the Team commits to before looking at Now.

**Examples**: (1) Priya keeps C1–C4: four rows. (2) Elena unticks C2: three rows; C2 is still frozen (D26).
(3) Priya unticks everything: Create stays disabled with "Choose at least one claim".

```gherkin
Scenario: The four everyday claims are preselected
  When Priya creates a report for Team Gravity
  Then Cycle Time trending down, Throughput trending up, WIP stable or down and Total Work Item Age stable or down are ticked

Scenario: WIP that rose against Then does not hold
  Given Team Gravity's Then WIP upper limit is 12
  And WIP was 13 or more on 3 days of Now
  When Priya opens the report
  Then "WIP is stable or down" reads Does not hold

Scenario: Throughput up from a single busy day is not a trend
  Given one day in Now closed 9 Work Items, above the Then upper limit of 6
  And no other signal is on either side
  When Priya opens the report
  Then "Throughput is trending up" reads No change yet and names the one unusual day

Scenario: A report needs at least one claim
  When Priya unticks every claim
  Then Create is disabled with "Choose at least one claim"
```

**AC**: AC-4.1 Claims picker; C1–C4 preselected. AC-4.2 Values per recommendation (C2 weekly median + total, C3
average + range, C4 average daily total + average {Work Item Age}). AC-4.3 Every applicable claim frozen whatever the
selection. AC-4.4 Rules per D24. AC-4.5 Reports created before this slice show C2–C4 as "Not captured for this
report" (D27).
**KPI**: K2. **Tech**: rule kind `NoShiftAgainst` as a class; one registration per claim.

### US-05 — Say whether {Cycle Time} is predictable and whether the 85th percentile fell (slice 05)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena) for reader Martin

**Problem**: Martin knows "85th percentile" from the SLE conversation and asks for it by name; a verdict alone looks
evasive to him (R-A).

#### Elevator Pitch

Before: no claim speaks the sponsor's familiar number.
After: Create Report → tick C5 and C6 → sees "● Holds — 85th percentile Cycle Time is lower · 21 → 12 days (−43%) ·
SLE 85% within 14 days" and "Cycle Time is predictable".
Decision enabled: whether to report the SLE-style number to the sponsor alongside the PBC verdicts.

**Examples**: (1) 21 → 12 days = −43% → Holds. (2) 21 → 20 days = −5% → No change yet. (3) Team without an SLE: no
SLE reference, the claim still works. (4) Now with a point beyond its own limit → "Cycle Time is predictable" Does
not hold.

```gherkin
Scenario: A fall of 10% or more in the 85th percentile holds
  Given Then 85th percentile Cycle Time is 21 days and Now is 12 days
  When Elena opens the report
  Then "85th percentile Cycle Time is lower" reads Holds with "21 → 12 days (−43%)"

Scenario: A small fall is no change yet
  Given Then is 21 days and Now is 20 days
  When Elena opens the report
  Then the claim reads No change yet

Scenario: Now with an outlier is not predictable
  Given one Work Item in Now took 45 days, beyond Now's own upper limit
  When Elena opens the report
  Then "Cycle Time is predictable" reads Does not hold and shows Then's frozen signal count for reference
```

**AC**: AC-5.1 C6 `ThresholdChange(down, 10%)`, percentage shown, {SLE} shown when set. AC-5.2 C5 on Now's own limits;
Then's signal state frozen and shown. AC-5.3 Both unticked by default.
**KPI**: K2. **Tech**: rule kinds `NoSignalsInWindow`, `ThresholdChange` as classes.

### US-06 — Make Then & Now claims about a Portfolio (slice 06)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena, Portfolio Admin, Project Apollo)

**Problem**: Elena's engagement spans a Portfolio, and the sponsor asks about {Features}, not {Work Items}.

#### Elevator Pitch

Before: only Teams have Reports.
After: Portfolio Project Apollo → **Reports** → Create Report → Then 90 days → sees claims about {Features}, e.g.
"Cycle Time is trending down — Not enough data · 3 finished Features in Then; 8 needed".
Decision enabled: whether the Portfolio's claims carry enough evidence to show, or whether to report Team-level instead.

**Examples**: (1) Project Apollo, Then 180 days: C2–C4 verdicts, C1 Not enough data. (2) A Viewer on the Portfolio
sees but cannot create. (3) Then length 7 days is not offered on a Portfolio (30/90/180 only).

```gherkin
Scenario: A Portfolio Admin creates a Then & Now report
  When Elena creates a report for Project Apollo with a Then of 90 days ending today
  Then the report shows Feature-level claims with Then and Now values

Scenario: A Portfolio with few finished Features says so
  Given Project Apollo finished 3 Features in Then
  When Elena opens the report
  Then "Cycle Time is trending down" reads Not enough data

Scenario: A Portfolio reader views without creating
  Given Martin is a Viewer on Project Apollo
  When he opens its Reports tab
  Then he sees the reports and no Create Report button
```

**AC**: AC-6.1 Reports tab on Portfolios (D18), Portfolio Write to create. AC-6.2 Same catalog, Portfolio series.
AC-6.3 Lengths 30/90/180. AC-6.4 Words per Terminology ({Feature}, {Portfolio}).
**KPI**: K1, K2. **Tech**: route key `PortfolioDetail_Reports`.

### US-07 — Choose how long Now is, and see how long ago Then ended (slice 07)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (consultant on a 4-week assessment, Team Gravity)

**Problem**: A consultant's 4-week assessment ends today; the client will compare against it later. A 90-day Now on
the report would mostly measure the weeks before the change (DISCOVER X4).

#### Elevator Pitch

Before: Now always has Then's length.
After: the report → Now **[last 30 days ▾]** → picks 14 days → sees "Now: 19 Sep – 2 Oct 2026 · 63 days since
Then ended" and verdicts recomputed for that window.
Decision enabled: which recent stretch to judge against the frozen Then.

**Examples**: (1) Then to 31 Jul, Now 30 days → 3 Sep – 2 Oct, 63 days since. (2) Then to 31 Jul, Now 90 days →
clipped to 1 Aug – 2 Oct (D23, from slice 03). (3) A Portfolio report offers 30 / 90 / 180 days, not 14.

```gherkin
Scenario: A reader shortens Now
  Given the report's Then ended 31 Jul 2026
  When Martin picks "last 14 days"
  Then the header reads "Now: 19 Sep – 2 Oct 2026" and every verdict uses that window

Scenario: Now never reaches back into Then
  Given Then ended 31 Jul 2026
  When Elena picks "last 90 days"
  Then Now starts on 1 Aug 2026 and the header says so

Scenario: The header says how long ago Then ended
  Given the report's Then ended 31 Jul 2026 and today is 2 Oct 2026
  When Martin opens the report
  Then the header reads "63 days since Then ended"
```

**AC**: AC-7.1 Now presets per owner (D22), choice in the address, not saved. AC-7.2 The clipping of slice 03 (D23)
holds for every preset. AC-7.3 Header days since Then ended (D35).
**KPI**: K3.

### US-08 — Delete reports, within Community's two (slice 08)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena) on a Community instance

**Problem**: Elena wants a hand-over report next to the engagement-start one, and a third for a second experiment.

#### Elevator Pitch

Before: reports can only be added.
After: Reports → report menu → **Delete** → confirms "Delete 'Then & Now — engagement start'?" → the list shows one
report; at two reports Create Report reads "Community includes 2 reports per Team. Delete one, or use Premium for
unlimited reports."
Decision enabled: which reports to keep, and whether more than two justify Premium.

**Examples**: (1) Two reports on Community: Create disabled with the message. (2) Premium: a fifth report is created.
(3) Licence lapses with four reports: all four open and can be deleted; Create stays disabled until only one is left.
(4) Delete when Then is now beyond the cutoff warns "This Then window can no longer be rebuilt."

```gherkin
Scenario: Community stops at two reports per Team
  Given Team Lightspeed has 2 reports and the instance has no Premium licence
  When Elena opens the Reports tab
  Then Create Report is disabled and says Community includes 2 reports per Team

Scenario: A lapsed licence keeps every report readable
  Given Team Lightspeed has 4 reports created under Premium
  And the licence has expired
  When Martin opens the Reports tab
  Then all 4 reports open and Create Report is disabled

Scenario: Deleting a report that cannot be rebuilt warns first
  Given a report's Then starts on 1 Sep 2025, before the 365-day cutoff
  When Elena chooses Delete
  Then the confirmation says this Then window can no longer be rebuilt
```

**AC**: AC-8.1 Delete for Write only, confirmed (D30). AC-8.2 Cap 2 per owner across all templates, UI and server
(D29). AC-8.3 Premium unlimited. AC-8.4 Lapse keeps all viewable and deletable. AC-8.5 Deleting an owner deletes its
reports (provisional, Q5).
**KPI**: K4. **Tech**: copies `AdditionalFieldsHelper` (S8).

### US-09 — See when Now runs under different settings than Then (slice 09)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Priya) and reader Martin

**Problem**: Six weeks after freezing, a Team Admin mapped a new Doing state. Now's {Cycle Time} changed partly
because of the mapping, and Priya cannot tell.

#### Elevator Pitch

Before: Now silently uses today's settings.
After: the report → sees "Settings changed since this report was created (state mapping). Then stays as frozen on
14 Sep 2026; Now uses today's settings."
Decision enabled: whether a verdict reflects the process or the configuration change.

**Examples**: (1) "Review" added to Doing → notice names state mapping. (2) Blackout days added → notice names
blackout days. (3) Nothing changed → no notice.

```gherkin
Scenario: A changed state mapping is flagged
  Given Priya created the report on 14 Sep 2026
  And a Team Admin later added "Review" to the Doing states
  When Priya opens the report
  Then the header notes that the state mapping changed and Then stays as frozen

Scenario: Unchanged settings show no notice
  Given no setting that shapes the claims changed
  When Martin opens the report
  Then no settings notice is shown

Scenario: Several changes are listed once
  Given the Work Item types and the blackout days both changed
  When Priya opens the report
  Then one notice names both
```

**AC**: AC-9.1 Snapshot compare per D31. AC-9.2 One header notice. AC-9.3 Reports frozen before this slice compare
against the snapshot stored since slice 01.
**KPI**: guardrail G3.

### US-10 — Change which claims a report shows (slice 10, cancellable)

`job_id: job-flow-coach-show-whether-flow-changed` · persona `flow-coach` (Elena)

**Problem**: The sponsor asked about {Throughput} after Elena had unticked it.

#### Elevator Pitch

Before: the selection is fixed at creation.
After: the report → **Edit claims** → ticks "Throughput is trending up" → sees its row with Then values frozen on
2 Oct 2026, nothing recomputed.
Decision enabled: which already-frozen claims to show the sponsor now.

**Examples**: (1) Ticking C2 adds its row from the frozen values. (2) A claim registered after creation shows
"Not captured for this report", not tickable. (3) A Viewer has no Edit claims.

```gherkin
Scenario: Showing a claim that was frozen but not selected
  Given the report froze Throughput values on 2 Oct 2026 without showing them
  When Elena ticks "Throughput is trending up"
  Then the row shows the Then values frozen on 2 Oct 2026

Scenario: A later claim cannot be added
  Given a claim was added to the catalog after the report was created
  When Elena opens Edit claims
  Then that claim reads "Not captured for this report" and cannot be ticked

Scenario: Readers cannot change the selection
  When Martin views the report
  Then there is no Edit claims action
```

**AC**: AC-10.1 Write only. AC-10.2 No recompute of Then. AC-10.3 At least one claim stays selected.
**KPI**: K2.

### US-11 — Open the chart behind a verdict (slice 11, cancellable)

`job_id: job-flow-coach-show-whether-flow-changed` · persona reader `delivery-lead-rte` (Martin)

**Problem**: Martin trusts a verdict only once he has seen the points it rests on.

#### Elevator Pitch

Before: a verdict is a word and two numbers.
After: the report → expands "Cycle Time is trending down" → sees Now's finished Work Items plotted against the Then
average and limits, the run of 8 highlighted.
Decision enabled: whether to accept the verdict or ask the coach about a specific Work Item.

**Examples**: (1) Holds via the run rule: the 8 points highlighted. (2) Does not hold: the two points above the
limit highlighted, ids shown. (3) Not enough data: no chart, the reason only.

```gherkin
Scenario: The verdict's points are visible
  Given "Cycle Time is trending down" holds on a run of 8
  When Martin expands the claim
  Then he sees Now's points against the Then average and limits with the run highlighted

Scenario: Outliers can be traced to Work Items
  Given the claim does not hold because of LS-412 and LS-415
  When Martin expands it
  Then both points are highlighted and name LS-412 and LS-415

Scenario: No chart without data
  Given the claim reads Not enough data
  When Martin expands it
  Then he sees the reason and no chart
```

**AC**: AC-11.1 Chart uses the frozen Then limits, not the owner's PBC Baseline. AC-11.2 Signal points highlighted.
AC-11.3 No chart for Not enough data.
**KPI**: K2. **Tech**: reuse the existing PBC chart component.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every UAT scenario of the story passes as an automated acceptance test.
2. Unit and component tests green; backend `dotnet test` (filtered per CLAUDE.md) and frontend `pnpm test` green.
3. `dotnet build` and `pnpm build` with zero warnings; Biome clean; no new SonarCloud issues.
4. RBAC: create/delete refused server-side without Write; UI gating via `useRbac()` only.
5. Copy uses Terminology tokens; no "Baseline", "Epic", "Story", "Initiative" in UI strings.
6. Any migration is expand-only and made with `CreateMigration` for every provider.
7. One E2E walking skeleton (Page Object Model, demo data) covers create-and-read; no more E2E than that.
8. Mutation testing (Stryker, both stacks) ≥ 80% on the feature's code, run last on frozen code.
9. At feature finalization: docs, one `@screenshot` per theme, demo data and the usage-data entry in
   `docs/settings/usagedata.md` are in, per the Checklist below.

---

## Wave: DISCUSS / [REF] Out of Scope (v1)

CLI/MCP (D15) · PDF / email / scheduled export (D13) · Delivery level (D8) · cross-owner reports (one report spans
several Teams) · tuning rules or thresholds per report (Option 4) · custom Then lengths (provisional, Q1) · renaming
a report (provisional, Q7) · an intervention-date field (D35) · recomputing Then on request · **nice-to-have claims**:
{Throughput} predictable, fewer {SLE} breaches, arrivals balanced with {Throughput}, {WIP} streaks, {Feature} size
stable (Portfolio), the Team-only forecast comparison ("more {Work Items} likely in the next 30 days at 85%") · a
second template (Epics 5882, 5935) · any user-facing "Baseline".

---

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

**Walking skeleton**: brownfield; slice 01 on Team Lightspeed (demo) and the dev instance's busiest Team. It touches
UI tab → create → freeze/store → view with live Now. E2E: one Playwright spec through a Page Object on demo Team
Lightspeed (create a 30-day report, see the {Cycle Time} row).

**Driving ports (user-invocable entry points)**: the Reports tab on Team and Portfolio detail pages; HTTP operations
DESIGN names — list templates and claims, list an owner's reports, create a report, read a report for a Now window,
delete a report, change a report's shown claims. All under the owner's Read/Write guards. No background job is
implied; creation completes or fails whole (D26).

**Pre-requisites**: mapped To Do / Doing / Done states; stored history covering Then inside `DoneItemsCutoffDays`;
the existing minimum-data guards. In a worktree, the premium licence fixture is copied from the main checkout before
cap tests (slice 08). None block slice 01.

**NFRs**: creating a 90-day report on a Team with 365 days of history ≤ 10 s; opening a report ≤ 2 s; the Metrics
tab and its PBCs unchanged (no shared setting altered).

---

## Wave: DISCUSS / [REF] Outcome KPIs

Objective: coaches and Teams freeze a "before" in Lighthouse and come back to it to show whether flow changed.
**North star: K2.** DEVOPS turns each "Measured by" into a usage-data design (name-only preferred).

| # | Who | Does what | Target | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | Opted-in instances with ≥ 1 Team | create ≥ 1 report | ≥ 10% within 90 days of release | 0 | name-only "report created" ÷ instances reporting | Leading (activation) |
| K2 | Instances that created a report | open a Reports tab again on ≥ 2 later days, ≥ 7 days after their first report | ≥ 40% | 0 | `TeamTabOpened`/`PortfolioTabOpened` with the Reports route keys, per instance, against the first "report created" | Leading (north star: the yardstick is used) |
| K3 | Reports viewed | are viewed with Now shorter than Then at least once | informational; ≥ 20% would confirm D14's 4-week shape | 0 | DEVOPS decides (route key alone cannot see it; drop if it needs more than a closed enum) | Leading (learning) |
| K4 | Community instances that created a report | hit the 2-report cap within 90 days | learning: ≥ 15% = the cap is a real Premium lever | 0 | name-only "report creation refused at cap" | Leading (land-and-expand) |
| K5 | Consultants in the interview protocol | show a Then & Now report to client management and report what followed | ≥ 2 within 90 days | 0 | maintainer conversation log (DISCOVER R1, R2) | Qualitative |

Guardrails: G1 reports deleted within 1 day of creation ≤ 20% of created (re-rolling to cherry-pick, or confusion;
name-only "report deleted"); G2 open ≤ 2 s, create ≤ 10 s; G3 no report shows 0 where data is absent; Metrics tab
PBCs unchanged.

Hypothesis: we believe a frozen Then judged by PBC claims will make coaches return to Lighthouse to show change. We
will know when ≥ 40% of report-creating instances open Reports again a week or more later (K2).

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

No silent N/A — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **New guards, no new requirement.** Create, delete and edit-claims = `TeamWrite` / `PortfolioWrite` (Team/Portfolio Admin, System Admin); list and view = `TeamRead` / `PortfolioRead`. UI: `rbac.isTeamAdmin(id)` / `rbac.isPortfolioAdmin(id)` from `useRbac()`; nothing fetches `/api/latest/authorization/my-summary`. Auth off or RBAC off: everyone may create (permissive). Docs: `docs/settings/rbac.md` gains the Reports row at finalize. |
| **Lighthouse-Clients CLI/MCP** | **N/A, because the maintainer excluded CLI/MCP on 2026-10-02 (D15).** All new endpoints are additive; no existing response shape changes, so no client release is forced. |
| **Website / marketing surface** | **Owed at finalize (after slice 08, D34)**: features list entry (Community; Premium = unlimited reports), and a ValueFlow-style "show what changed since the engagement started" line for the consulting audience. Website is a separate repo that hot-links `docs/assets` from `@main`, so new screenshots go live there on push. Confirm copy with the maintainer before editing. |
| **Docs + screenshots** | **Owed at finalize, not batched**: new page for Reports and the Then & Now template (claims, rules, verdicts, the zero-limit note, the cap); `docs/teams/detail.md` and `docs/portfolios/detail.md` (new tab); `docs/licensing/licensing.md` (2 reports Community); `docs/settings/rbac.md`. `@screenshot` one per theme: a Team report with Holds / Does not hold / Not enough data rows, and the create dialog. Docs wait for the maintainer's confirmation. |
| **Demo data** | **Owed in slice 03** (verdict) and checked again at finalize: demo history is relative to load day and short (S11). Adjust Team Lightspeed's CSV (≈100 days) so a 30-day Then and the Now after it yield one Holds and one No change yet; the Portfolios stay thin on purpose, showing Not enough data. No pre-seeded report — the E2E creates one, and a seeded report would freeze on load day. |
| **Terminology** | **No new key.** "Report", "Then", "Now", "Then & Now" are product words, not a tracker's noun (D32); a key can be added later, additively. Every claim sentence uses {Cycle Time}, {Throughput}, {WIP}, {Work Item Age}, {Work Item(s)}, {Feature(s)}, {Team}, {Portfolio}, {SLE}. |
| **Usage-data event** | **Wanted (D33), designed in DEVOPS.** Tab opens: existing `TeamTabOpened` / `PortfolioTabOpened` with new route keys `TeamDetail_Reports`, `PortfolioDetail_Reports` (K2). New name-only candidates appended to `UsageDataEventName` (next free integer on `main`, never renumber): report created (K1), report deleted (G1), report creation refused at cap (K4). A closed-enum template property only once a second template exists. Emitted in the slice that first makes each usable (01, 08), listed in `docs/settings/usagedata.md`. |
| **EF migrations** | **Owed in slice 01**: new report storage (owner, template key, Then window, created date, selection, frozen claim payloads, settings snapshot), additive, cascade with its owner (provisional, Q5). Later slices store inside that shape; DESIGN confirms no further migration. Expand-only, `CreateMigration`, all providers. |
| **Premium gating** | **Count cap only (D29)**: Community 2 reports per owner across all templates, Premium unlimited, lapse keeps everything readable and deletable. Claims, rules and windows are never gated (DV-7). |
| **ADO** | Epic #5878 stays; one Story per slice once the maintainer confirms. Not touched by this wave. |

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem clear, domain language | PASS | Every story opens with Elena, Priya or Martin's situation; no "implement X" titles. |
| 2 | Persona specific | PASS | `flow-coach` consulting (Elena, Team Admin, Lightspeed, engagement from 1 Aug 2026) and internal (Priya, Gravity, WIP limit from 14 Sep); reader `delivery-lead-rte` (Martin, Viewer). |
| 3 | 3+ domain examples, real data | PASS | Each story lists ≥ 3 examples with Team names, dates (2 Oct 2026, 31 Jul 2026, 2 Oct 2025 cutoff) and values (6/11/21 → 4/7/12 days). |
| 4 | UAT G/W/T, 3–7 | PASS | US-03: 5 scenarios, US-04: 4; every other story 3. Each includes an error or boundary path. |
| 5 | AC from UAT | PASS | AC-n.m per story trace to its scenarios and to D-decisions. |
| 6 | Right-sized | PASS | Every slice ≤ 1 day (07, 10: ½ day). Scope at the boundary, no split (Scope Assessment). |
| 7 | Technical notes | PASS | Inventory S1–S11; rounding in `XmRResult` (S3); PBC Baseline independence (S4); NFRs. |
| 8 | Dependencies tracked | PASS | 02–11 depend on 01; 05, 10 on 04; 06 on 04; 09 on 01's snapshot; 08 before release (D34). Seven provisional decisions tracked as Q1–Q7. |
| 9 | Outcome KPIs | PASS | K1–K5 with targets, baselines, measurement; guardrails G1–G3. |

**DoR status: PASSED**, with seven provisional decisions awaiting the maintainer (none blocks slice 01; Q2–Q4 must
be confirmed before slice 03 is accepted).

---

## Wave: DISCUSS / [REF] Open questions for the maintainer

Each has a labelled provisional assumption already applied above.

1. **Q1 — Then lengths.** Presets only (Team 14/30/90, Portfolio 30/90/180), or also a custom length? *Provisional
   (D20)*: presets only; Thrivve's 90 and 30 are both covered.
2. **Q2 — Now overlapping Then.** Should Now be clipped to start after Then (so a report frozen today reads No change
   yet until days pass)? *Provisional (D23)*: yes — otherwise Then is judged against itself.
3. **Q3 — What makes "trending" hold.** A sustained signal only (2 of 3, 4 of 5, run of 8), or also one point beyond
   a limit (DV-3 as written)? *Provisional (D24)*: sustained only for Holds; any signal counts against.
4. **Q4 — Minimum data.** ≥ 8 finished {Work Items} in Then for C1/C5 (the run length), ≥ 1 in Now? *Provisional
   (D25)*: yes; DESIGN confirms on the dev instance in slice 02.
5. **Q5 — Owner deletion.** Do reports go with a deleted {Team}/{Portfolio}? *Provisional (D29)*: yes; "frozen data
   is never deleted" applies to the licence, not to owner deletion.
6. **Q6 — Saving the Now choice.** Per view in the address (like Metrics), or saved on the report by its editor?
   *Provisional (D22)*: per view, not saved.
7. **Q7 — Renaming a report.** *Provisional (D19)*: a name with a default at creation; renaming later is out of v1.

---

## Wave: DISCUSS / [REF] Handoff

To DESIGN (`nw-solution-architect`): this section, `slices/`, `discuss/wave-decisions.md`, journey SSOT. Open for
DESIGN: report storage shape for both owner kinds (expand-only, cascade, D26 contents); the claim-definition registry
and rule-kind classes (DV-5); freezing the Then series and recomputing limits from it (S3); keeping Then limits
independent of the owner's PBC Baseline (S4); the settings snapshot contents (D31); calendar-day windows (Bug #5567);
creation time budget (≤ 10 s). DEVOPS: K1–K5, G1 events and route keys (Checklist).
