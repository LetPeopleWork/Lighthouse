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
