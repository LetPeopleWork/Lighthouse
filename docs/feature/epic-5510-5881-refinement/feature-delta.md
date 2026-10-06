# Feature Delta — epic-5510-5881-refinement

**ADO**: Epic #5510 *Sizing Poker* (New, Options, tag `Community`, ICE 90) and Epic #5881 *Refinement
Need Chart* (New, tag `ValueFlow`, ICE 60). One combined workspace; expected to split into several
Epics at DISCUSS.

**Waves**: DISCOVER (2026-10-02, light, desk research only — no live interviews).

**One line**: a Team's refinement meeting runs long from both ends — nobody knows how many Work Items
actually need refining before the next replenishment, and every Work Item gets discussed whether or
not anyone doubts it fits the Team's SLE.

**Density**: `lean`. Tier-1 `[REF]` only. `evidence_standard: past_behavior` — every claim below is
tagged with what was actually *observed*, and anything that is only someone's intent is marked as such.

---

## Wave: DISCOVER / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` | **Primary.** Facilitates refinement/replenishment for one Team; reads "how many more do we need" and runs the sizing session. |
| `product-owner` | **Secondary.** Owns the backlog order; decides *which* Work Items get refined once the count is known. |
| `config-admin` | Configures refinement states and readiness rules on the Team's Settings (edit rights). |
| *(candidate)* `team-member-voter` | **Not yet created.** A developer who casts a sizing vote, possibly without a Lighthouse account (async link) or with auth off. Create at DISCUSS only if the voter's job differs from `flow-coach`'s; the evidence below does not yet show it does. |

---

## Wave: DISCOVER / [REF] Opportunity statement

Minimise the time a Team spends in refinement while keeping just enough Work Items ready for the next
replenishment — no fewer (the Team starves) and **no more** (the ready queue grows, which LPW teaches
against).

Two opportunities, attacking the same meeting from opposite ends:

| # | Opportunity (customer-outcome form) | Source | Ranking available today |
|---|---|---|---|
| O1 | Minimise the time spent discussing Work Items nobody doubts fit the SLE | #5510 | ICE 90 (maintainer, 2026-09-20) |
| O2 | Minimise the likelihood of refining too many — or too few — Work Items before the next replenishment | #5881 | ICE 60 (maintainer, 2026-09-20) |

Opportunity-algorithm scores (importance/satisfaction from interviews) **do not exist**; the ICE scores
are the only ranking and were produced by the maintainer, not by customers. See G2.

---

## Wave: DISCOVER / [REF] Validated assumptions

| # | Assumption | Evidence (past behaviour) | Confidence |
|---|---|---|---|
| V1 | Practitioners actively look for sizing/planning-poker tooling. | Weekly online posts asking for / advertising Planning Poker tools (#5510 description; frequency asserted, not counted). | Medium |
| V2 | An SLE-anchored "fits / fits-if / doesn't fit" sizing method lands with the target community. | A competing free tool is well received in ProKanban Slack; a consultant forwarded it to Delivery Managers at a client; LPW's pre-Lighthouse MVP at letpeople.work/sizing-poker was registered by PK peers unprompted (#5510 ICE comment). Forwarding and unprompted registration are behaviour, not opinion. | Medium |
| V3 | Backlog/refinement volume is a felt, costed problem. | r/agile 2026-08-22 "our backlog has 500 plus items", 60+ comments, refinement cost priced in-thread (#5881 ICE). Evidences the *problem*, not the chart. | Medium (problem) / Low (solution) |
| V4 | A throughput-vs-ready-queue chart is a recognised practice, not an LPW invention. | Thrivve Partners' ValueFlow ships it; demoed by Paul Brown 2026-08-31 (#5881). | Medium |
| V5 | The forecasting engine needed for O2 already exists. | `ForecastService.HowMany(throughput, days)` → `HowManyForecast`, already used by manual forecasts, backtest and Reality Check (`ForecastController.cs:137`, `:207`). | High (feasibility) |
| V6 | A rule engine for "which Work Items are in refinement" already exists. | `WorkItemRuleSet` (and/or, ≤20 conditions over field/operator/value) is persisted per Team as `BlockedRuleSetJson` on `WorkTrackingSystemOptionsOwner`. | High (feasibility) |

---

## Wave: DISCOVER / [REF] Invalidated assumptions

| # | Assumption (as stated in the idea) | Evidence against |
|---|---|---|
| X1 | "Every Team has an SLE to size against." | `ServiceLevelExpectationProbability` and `…Range` default to `0` on `WorkTrackingSystemOptionsOwner` (L40-42) — SLE is optional, so a fallback or a block is required. |
| X2 | "Refinement states are naturally a subset of the Team's To Do states." | Only partly. "Refined, waiting to be pulled" is To Do by definition, but Teams that map an `Analysis`/`Refinement` state to Doing already count refinement in WIP and cycle time. And a state left unmapped makes the Work Item disappear entirely, with no warning. A refinement selector must accept To Do **and** Doing states, and must say which category each one sits in. |
| X3 | "Who voted / revoke my vote" works the same in every deployment. | With auth off every caller shares one subject, so per-user votes collapse to one voter. Anonymous mode needs a self-declared name or a per-browser token. |
| X4 | "Live sync sessions can reuse the existing push channel as is." | SignalR exists (Redis backplane for hosted, `Program.cs:333`), but `UpdateNotificationHub` is `[Authorize]` and only carries refresh status. A voter who has no Lighthouse account (async link/code) cannot connect to it. |
| X5 | "Throughput Monte Carlo = number of Work Items that need refining." | Not proven, and probably biased high: throughput counts *every* finished Work Item, including expedites and bugs that never pass through refinement. Need the share of finished Work Items that passed through a refinement state before trusting the count (see Riskiest). |

---

## Wave: DISCOVER / [REF] Dropped options

| Option | Why dropped |
|---|---|
| Portfolio-level refinement (Feature sizing against Portfolio SLE / Feature size / reference class forecasting) | **Deferred** by the maintainer — Team level only for now. |
| Story-point / Fibonacci estimation poker | Contradicts LPW's SLE-based right-sizing; the competitor field already serves it ("won't win the vote-in-ten-minutes crowd"). |
| A chart that only says "refine more" | Rejected by the maintainer as a mandatory constraint: the ideal range needs an **upper** bound that says "stop refining". |
| Copy-paste Work Items into a standalone poker tool | Defeats the one advantage over zero-setup competitors: Lighthouse already holds the Work Items from the work tracking system. |
| A thin v1 shipped fast into the endorsed-competitor channel | ICE caution: a thin version has a reputational cost in the best acquisition channel — "worth doing properly or not at all". Not a decision to do it big; a decision not to do it thin. |

---

## Wave: DISCOVER / [REF] Decision gate

| Gate | Status | Why |
|---|---|---|
| G1 Problem | **PARTIAL** | Behavioural desk signals (V1-V4: weekly tool-seeking posts, forwarding, unprompted MVP registration, a 60+-comment thread) but **zero** Mom Test interviews, so the 5-interview / >60% threshold is not met. The problem is real for *the community*; that it is felt by *Lighthouse users* is not shown. |
| G2 Opportunity | **PARTIAL** | Two opportunities, with maintainer ICE scores only — no importance/satisfaction scores from customers, so the opportunity-algorithm >8 cannot be evaluated. The synergy between them (shared readiness selector, the same meeting attacked from both ends) is a sound argument, but it is not evidence. |
| G3 Solution | **FAIL** | Nothing tested. The existing MVP page's analytics have not been looked at, and they are the cheapest solution evidence available. |
| G4 Viability | **FAIL** | Tier now decided (Community, D10); the reputational risk in an occupied channel is unmitigated; the success metric (WAU) has no usage-data event yet. Feasibility is the only green risk (V5, V6). |

**Recommendation**: proceed to DIVERGE as a *learning* investment, not a build commitment — the
riskiest assumptions below are cheap to test and should be run before or during DISCUSS. Do not treat
this as having passed G1-G4.

---

## Wave: DISCOVER / [REF] Constraints established

| # | Constraint | Evidence source |
|---|---|---|
| C1 | Team level only; Team SLE is the yardstick. | Maintainer scope decision, 2026-10-02. |
| C2 | Must work hosted **and** standalone, including standalone on one shared screen. | Maintainer scope; #5510 "standalone must work". |
| C3 | Must work with auth off; voter identity there is self-declared or per-browser, never a shared subject. | Maintainer scope; auth-off shares one subject (known platform fact). |
| C4 | RBAC: readers may vote and comment; edit rights cover refinement *settings* only. | Maintainer scope. |
| C5 | The ideal-range band has an upper bound and says "stop refining" as loudly as "refine more". | #5881 ICE comment (mandatory design constraint). |
| C6 | Framing must not license a bigger ready queue. | #5881 description (LPW teaches small batches and just-in-time refinement). |
| C7 | Refinement selection reuses the existing rule machinery rather than inventing a second one. | `WorkItemRuleSet` / `BlockedRuleSetJson` (V6). |
| C8 | Refinement-state selection must cover To Do and Doing states, and only mapped states. | X2; `ToDoStates`/`DoingStates` on `WorkTrackingSystemOptionsOwner`. |
| C9 | User-facing copy uses configurable terms (Work Item, Team, SLE, Throughput). | Project rule (Terminology settings). |
| C10 | Votes and comments are kept as a log, not overwritten. | #5510 "store inputs as a log". |

---

## Wave: DISCOVER / [REF] Key decisions

- [D1] One combined workspace for #5510 and #5881, to be split at DISCUSS: both share the readiness selector, and each attacks one end of the same meeting (see: maintainer scope; #5510 ICE "reuses #5881's ready state selector").
- [D2] Team level only; Portfolio level deferred: keeps the yardstick to one SLE per Team (see: maintainer scope).
- [D3] Hosted, standalone and auth-off are all first-class: the standalone shared-screen session is also the in-person mode (see: maintainer scope; #5510).
- [D4] Readers vote and comment; editors change settings: a vote does not change the Work Item (see: maintainer scope).
- [D5] Sizing question = "doable within the Team's SLE?" with three answers (Yes / Yes, but… / No), and comments carry the conditions: SLE right-sizing, not estimation (see: #5510; lunarlogic estimation inspiration).
- [D6] Refinement-need band carries an upper bound: guards against reading "refine 6 more" as licence for a big ready queue (see: #5881 ICE, C5/C6).
- [D7] No SLE → fall back to the 85th percentile of the Team's **default** cycle time, with a hint pointing to setting an SLE: the feature works out of the box and nudges towards the better yardstick (see: maintainer answer 2026-10-02; X1).
- [D8] Async voting is the target and ships first; live sessions (remote and in-person) follow as a gateway for teams that run Planning Poker meetings today. All modes are wanted eventually (see: maintainer answer 2026-10-02).
- [D9] Votes and comments stay in Lighthouse only; nothing is written back to the work tracking system for now (see: maintainer answer 2026-10-02).
- [D10] Community tier, with room for Premium-only restrictions later as a conversion lever (see: maintainer answer 2026-10-02).
- [D11] With auth off, anyone who can reach the instance may join and vote; it already grants them everything else (see: maintainer answer 2026-10-02).
- [D12] CLI/MCP is in scope; a Lighthouse-Clients version bump is expected (see: maintainer answer 2026-10-02).
- [D13] The letpeople.work/sizing-poker MVP still draws visitors with little recent promotion — a qualitative R1/R4 signal; votes-over-time has not been read (see: maintainer answer 2026-10-02).
- [D14] With auth on, joining needs an account in this version — no guest links (see: maintainer answer 2026-10-02; R5).
- [D15] Basic setup lists every refinement state (To Do or Doing) so items appear in the view; rules that split them into waiting / being refined / ready are optional, for Teams whose states don't already make the split (see: maintainer answer 2026-10-02).
- [D16] Readiness is a Team setting: minimum Yes votes and minimum voters, optionally a veto after x No or "Yes, but…" votes; a configured rule overrides the vote outcome (see: maintainer answer 2026-10-02).
- [D17] Replenishment cadence is a Team setting (see: maintainer answer 2026-10-02).
- [D18] Usage-data events are wanted: the opt-in catalogue should show whether Teams use the refinement view, vote, and run sessions; DEVOPS designs them (name-only preferred, closed-enum properties only) (see: maintainer correction 2026-10-02; G4 success metric WAU).
- [D19] MVP votes arrive in one meeting window — R4 (async is used) is NOT supported, but the audience was invited to live sessions, so it is not refuted either (see: maintainer answer 2026-10-02).
- [D20] The refinement-need number uses total Team Throughput via the existing `HowMany` Monte Carlo: expedites and bugs are refined too and share the same flow, so R2 is invalidated (see: maintainer override 2026-10-02).
- [D21] Async voting is pull-based: votes live in Lighthouse, people browse the Refinement view, readiness ("x votes needed") moves a Work Item on and the missing votes are raised in the Team's own rituals. No push channel (see: maintainer override 2026-10-02).
- [D22] Sizing calibration (how often Work Items voted "Yes" actually finished within the SLE) is Premium (see: maintainer answer 2026-10-02).
- [D23] A refinement overview across Teams is not in the first versions; its tier is decided if and when it is built (see: maintainer answer 2026-10-02).
- [D24] The SLE fallback only ever uses the Team's default cycle time — no choice of cycle-time definition is built, Community or Premium (see: maintainer answer 2026-10-02; refines D7).
- [D25] Tier split: everything a single Team needs to refine just enough is Community — refinement states, the Refinement tab, the need number, async voting with comments, the readiness rule including its optional veto and rule override, the optional rules that split refinement into sub-states, live sessions and the in-person mode. Premium is named votes (comes with authentication, nothing to build) and sizing calibration (D22). Rule-based options stay free like the blocked-items rules; only the forecast filter sets a Premium precedent, and splitting one readiness setting across tiers would undermine trust in the green signal (see: maintainer answer 2026-10-02; supersedes the DIVERGE Premium shortlist P4/P9).
- [D26] "Refinement" becomes a configurable Terminology term (default "Refinement"); it names both the tab and the session the need number counts towards, and a Kanban Team may rename it, e.g. to "Replenishment". The cadence that matters is how often the Team refines, not how often it pulls: pulling is continuous and already captured by Throughput, and refining in small batches between pulls is fine (see: maintainer answer 2026-10-02; supersedes the "replenishment" wording in D17 and the DIVERGE recommendation).
- [D27] The Refinement cadence is set as weekdays plus "every N weeks", so the next Refinement has an exact date; the need number forecasts the Team's Throughput from now until that date and compares it with the Work Items that are ready (see: maintainer answer 2026-10-02; refines D17).
- [D28] The band's percentiles are a Team setting in the Refinement settings section; the high end defaults to 85% ("only a 15% chance this Team pulls more than this before the next Refinement"). The low end's default (median proposed) is settled in DISCUSS (see: maintainer answer 2026-10-02).
- [D29] No Refinement cadence set → the tab still shows the list and the votes but no need number, with a hint to set the cadence, mirroring the missing-SLE hint (see: maintainer answer 2026-10-02).
- [D30] Minimum Yes votes is at least 1; there is no lazy-consensus ("ready unless someone objects") mode. A Yes costs nothing extra — whoever reads a Work Item to decide whether it is a No or a "Yes, but…" can just as well say Yes — so green always means somebody looked (see: maintainer answer 2026-10-02).
- [D31] The ProKanban-endorsed tool is SLE Poker Planning (planning-poker-revisited.web.app): synchronous, facilitator-led, three SLE cards, Work Items not known to the tool. It is not treated as a threat to design around: Lighthouse already knows the Team's Work Items, SLE and Throughput, so nothing is typed in by hand (see: maintainer answer 2026-10-02; diverge/competitive-research.md addendum).

---

## Wave: DISCOVER / [REF] Pre-requisites

- Team throughput history long enough for a Monte Carlo (existing minimum-data guard applies).
- At least one mapped state the Team designates as refinement (or a rule), else the Refinement tab stays disabled with a hint.
- A Team SLE — or an agreed fallback (open question Q1).
- For live (sync) sessions: a push channel a voter can reach, including one without an account (X4).

---

## Wave: DISCOVER / [REF] Riskiest assumptions & cheapest next test

Risk = Impact×3 + Uncertainty×2 + Ease×1.

| # | Assumption | Cat. | Score | Cheapest next test |
|---|---|---|---|---|
| R1 | Teams will configure refinement states/rules at all (setup friction vs zero-setup competitors). | Value/Usability | 3·3+3·2+1 = **16** | Read the letpeople.work/sizing-poker MVP analytics: sessions per visitor, repeat use, drop-off. Then a fake-door "Refinement" tab hint, counted by a name-only usage event. |
| R2 | Throughput Monte Carlo is a fair count of the Work Items that need refining (X5). | Value | 3·3+2·2+1 = **15** | Desk query on demo or dogfood data: what share of finished Work Items passed through a To Do→refinement state? If it is well below 100%, the count needs that ratio applied. |
| R3 | Lighthouse users (not only the PK community) run SLE-based refinement today. | Value | 3·3+3·2+2 = **17** | 5 Mom Test conversations with existing Team users: "Tell me about your last refinement — how did you decide how many Work Items to refine?" |
| R4 | Async-first voting is used without a live meeting (the core #5510 bet). | Value | 3·3+3·2+2 = **17** | MVP analytics again: do votes arrive spread over hours/days, or all within one meeting window? |
| R5 | Unauthenticated hosted voters (link/code) can be admitted without weakening tenant security. | Feasibility/Viability | 3·3+2·2+2 = **15** | Short spike or ADR review against the existing embed-nonce accepted risk before DESIGN commits to link-based joining. |

---

## Wave: DISCOVER / [REF] Open questions for DIVERGE/DISCUSS

Answered 2026-10-02 and moved to Key decisions: no-SLE fallback (D7), v1 session mode (D8),
write-back (D9), tier (D10), auth-off joining (D11), CLI/MCP (D12).

Answered 2026-10-02: sub-states (D15), readiness rule (D16), cadence (D17), usage events wanted (D18),
auth-on joining (D14), MVP analytics (D19).

1. **Premium restrictions** that would convert without hollowing out the Community version (D10) — maintainer wants proposals.
2. **Async adoption** stays the riskiest bet (R4, D19): what would make a Team vote outside a meeting?

---

## Wave: DIVERGE / [REF] Recommendation

Full text: `recommendation.md` · artifacts: `diverge/` · decisions DV-1..DV-8: `wave-decisions.md`.

- **Direction**: Option 4, **"Stop sign first"** (4.30 of 5; runner-up "Always-open votes" 4.00). The Refinement tab opens on
  one verdict: below, in or above range before the next replenishment. Votes (Yes / Yes, but… / No, with a comment) are
  solicited only for the gap, sit on the Work Item (no rounds) and are due at the D17 cadence date. The in-person
  presenter view comes first among live modes.
- **Need number**: ~~`HowMany` over commitment-point Throughput, meaning Work Items leaving refinement per day.~~
  **Overridden by D20**: total Team Throughput, existing `HowMany` unchanged. Both bounds come from one run.
- **Open question 2**: ~~async voting needs a push into an inbox; build push next if votes stay inside live sessions.~~
  **Overridden by D21**: async is pull — votes live in Lighthouse, readiness moves a Work Item on; no push channel.
- **Open question 1, Premium proposals**:
  - Lead lever: sizing calibration (votes compared with outcomes against the SLE).
  - Named, accountable votes come with authentication, which is already Premium.
  - Candidate: a cross-Team overview.
  - Rejected: live sessions as Premium, history caps, voter caps.
- **Persona**: create `team-member-voter` at DISCUSS.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent; per-wave peer review
skipped by the coordinator (to be decided by the maintainer). Config: user-facing, brownfield walking skeleton,
JTBD on (every story carries a `job_id`), density lean.

| Read | Status |
|---|---|
| `feature-delta.md` DISCOVER + DIVERGE (D1–D31 settled, not re-opened) | ✓ |
| `recommendation.md` (the maintainer overrides at the top win over the body) | ✓ |
| `wave-decisions.md` (DIVERGE DV-1..DV-8) | ✓ |
| `diverge/job-analysis.md` | ✓ |
| `diverge/competitive-research.md`, including the SLE Poker Planning addendum | ✓ (§1 skimmed, I1–I6 and addendum read) |
| `diverge/options-raw.md` | ✓ skimmed (outline only) |
| `diverge/taste-evaluation.md` | ⊘ not read; the recommendation's §2 summary was enough |
| `docs/product/jobs.yaml` (`job-flow-coach-refine-just-enough`) | ✓ |
| `docs/product/journeys/epic-5510-5881-refinement.yaml` | ✓ |
| House style: epic-4172 "Project DISCUSS Checklist", epic-5375 DISCUSS sections and slice-01 brief | ✓ |
| `CLAUDE.md` (terminology, comments, usage data, no silent N/A) | ✓ |
| Brownfield code: `TeamDetail.tsx` tabs, `WorkTrackingSystemOptionsOwner`, `ForecastService.HowMany`, `UsageDataEventName`/`UsageDataEventShapes`, `TerminologyKeys.ts`, `DemoDataFactory` + demo Team CSVs | ✓ (targeted reads, below) |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| # | What exists | Where | Consequence |
|---|---|---|---|
| S1 | A Team tab that is **disabled with a tooltip** until a precondition holds (the Features tab when the Team has no Features) | `TeamDetail.tsx:491-509` | The Refinement tab copies this exact pattern; no new component. |
| S2 | Tabs today: Features, Forecasts, Metrics, Settings (TeamAdmin only), Access (TeamAdmin, RBAC on) | `TeamDetail.tsx:510-515`, `:122-124` | "Editors" = whoever sees Settings (`rbac.isTeamAdmin`). Readers see no Settings tab, so their disabled-tab tooltip must not tell them to go there. |
| S3 | Mapped states per Team: `ToDoStates`, `DoingStates` (+ Done); SLE fields default `0` | `WorkTrackingSystemOptionsOwner.cs:27-42` | Refinement states are a subset of To Do ∪ Doing (C8); SLE missing is common (X1). |
| S4 | A per-Team rule set, `BlockedRuleSetJson`, with an editor in `FlowMetricsConfigurationComponent` | `WorkTrackingSystemOptionsOwner.cs:50` | Stage rules (US-08) reuse the same `WorkItemRuleSet` shape and editor (C7). |
| S5 | `ForecastService.HowMany(throughput, days)` → `HowManyForecast` | `ForecastService.cs:33` | The need number is this call, unchanged (D20). |
| S6 | `TeamTabOpened` already exists and carries the page's route key | `UsageDataEventName.cs:12`, `UsageDataEventShapes.cs:30` | "Refinement tab opened" needs a new **route key**, not a new event name. |
| S7 | Terminology keys exist for Work Item, Team, SLE, Throughput, cycle time, WIP, blocked; none for Refinement | `TerminologyKeys.ts:5-26` | "Refinement" is a new key + seeder row (D26). |
| S8 | Demo Teams map To Do = `Backlog`, Doing = `Next`, `Analysing`, `Implementation`, …; SLE 85% / 7 days | `DemoDataFactory.cs:47-56` | Demo data already carries natural refinement states (`Backlog` waiting, `Analysing` being refined, `Next` ready) — e.g. Team Gravity GR-051 *Advanced reporting module* (Analysing), GR-058 *User activity tracking* (Next), GR-073 *Configuration management* (Backlog). Only Team **settings** need seeding, no CSV change. Note `Next`/`Analysing` are **Doing** states, which is X2 in the flesh. |
| S9 | Only SignalR hub is `[Authorize]`, refresh status only | X4 | Live remote sessions start with a spike (slice 19). |

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` | Primary. Reads the need, decides refine-more vs stop, facilitates the Refinement. |
| `team-member-voter` | **NEW** (`docs/product/personas/team-member-voter.yaml`). Gives a sizing view on a Work Item; often a reader (Viewer) or, on Community, an anonymous caller with a self-declared name. |
| `config-admin` | Team admin who sets refinement states, cadence, band and readiness. |
| `product-owner` | Owns backlog order; the highlighted "next N" are in that order. |

## Wave: DISCUSS / [REF] JTBD One-Liners

- `job-flow-coach-refine-just-enough` (existing, DIVERGE): keep just enough right-sized Work Items ready for the next Refinement, and discuss only the doubted ones.
- `job-team-member-give-sizing-view` (**NEW**, `jobs.yaml`): when I'm asked whether a Work Item fits our SLE, give my view in seconds from where I already am, so I don't sit through discussion of Work Items I don't doubt.

---

## Wave: DISCUSS / [REF] Changed Assumptions

| Was (source) | Now | Why |
|---|---|---|
| Votes are solicited **only for the shortfall** when the gauge is below range (DIVERGE Option 4, recommendation §0, DV-2) | **Votes are always open** on every Work Item in a refinement state, at any time. The need number only **highlights** the next N Work Items in backlog order, with an "enough for ‹date›" line in the list. The gauge and its verdict, including the loud "stop refining", stay. | Maintainer decision 2026-10-02 (DD-1). |
| The count runs on Work Items **leaving** refinement (recommendation §1 finding 3; job functional dimension) | Total Team Throughput, `HowMany` unchanged | Already overridden in D20 / DV-3; the job text in `jobs.yaml` is corrected in this wave. |
| Journey step "open or join a session via link/code with deadline" (DISCOVER journey) | No sessions, links, codes or deadlines for async; the Refinement tab **is** the place to vote | D14, D21, DD-1. |
| Journey job ids `job-flow-coach-size-the-ready-queue` / `job-flow-coach-shorten-refinement` | Re-pointed to `job-flow-coach-refine-just-enough` (+ the voter job) | DIVERGE note. |
| "Replenishment" as the deadline word | "Refinement" (configurable term) and dates | D26. |

---

## Wave: DISCUSS / [REF] Locked Decisions

- [DD-1] **Votes always open** on every Work Item in a refinement state, whatever its stage and whatever the verdict; the need number highlights the next N in backlog order with an "enough for ‹date›" line (see: maintainer 2026-10-02; supersedes Option 4 gating).
- [DD-2] **Band low end defaults to the median (50%)**, high end to 85% (D28). Below the median it is more likely than not that the ready Work Items run out before the next Refinement; above the 85% end there is only a 15% chance the Team pulls that many. A higher low end would narrow the band towards "always refine more" (C6) (see: D28 asks DISCUSS to settle).
- [DD-3] **Verdict** compares the *ready* count with the band: below (< low), in (low..high inclusive), above (> high). Copy weights "stop" exactly like "refine more" (same component, same size, same position) (see: C5).
- [DD-4] **The "enough for ‹date›" line sits after the Nth Work Item, N = the band's high end**, counted in backlog order over every Work Item in a refinement state (ready or not). Work Items above the line are highlighted; below it reads "not needed before ‹date›" (see: DD-1).
- [DD-5] **Where "ready" comes from, in order**: (1) an optional stage rule that matches the Work Item decides; (2) otherwise the stage of its state (each refinement state is tagged *Waiting* / *Being refined* / *Ready*, default *Waiting*); (3) a Work Item not Ready by (1) or (2) becomes Ready when its votes meet the readiness setting. One rule set serves both D15 (sub-state split) and D16 (rule overrides the vote outcome) (see: D15, D16).
- [DD-6] **Next Refinement date**: the first cadence date strictly after today, in the instance's time zone; on a Refinement day the count already looks to the following one, because today's session is the moment to top up. "Every N weeks" needs a starting week, set with the cadence (see: D27).
- [DD-7] **"Yes, but…" counts as a Yes** towards the minimum Yes votes; the optional veto counts No and/or "Yes, but…" as the Team configures (see: D16, D30).
- [DD-8] ~~Readiness defaults: minimum Yes 2, minimum voters 2, veto off.~~ **Superseded by DD-21.** Minimum Yes may be set to 1 but never 0 (D30) — still holds.
- [DD-9] **Vote log**: every vote, comment and revocation is an entry; the current view is the latest entry per voter per Work Item. Changing your mind adds an entry, never overwrites one (see: C10).
- [DD-10] **Identity**: auth on → the signed-in account, no name field. Auth off (every Community instance, DV-5) → a self-declared name held per browser, sent with each vote, shown but not verified. Revocation only from the same account / same browser (see: D11, D14, X3, ADR-191 precedent).
- [DD-11] **No fourth "can't tell yet" answer.** A comment without a vote is allowed; it marks the Work Item "open question" in the list and counts for nothing. Rationale: in a sync poker round the "unclear" card triggers talk; async, a written question does that better, and a fourth card would dilute "green means somebody looked" (D30). **Confirmed by DD-17.**
- [DD-12] **Others' votes hidden until you cast yours** on that Work Item (the tally shows "3 votes" without the split). Carried from the DIVERGE recommendation (anchoring, O4); built as its own small slice so it can be dropped. Presenter mode is exempt — the room sees everything.
- [DD-13] ~~Clients (CLI/MCP) are read-only.~~ **Superseded by DD-19** (clients may vote). What still holds: facts on the wire; the client composes the sentence (epic-4172 precedent).
- [DD-14] **Refinement settings live in a new "Refinement" section of the Team's Settings tab** (editors = Team admins); everything else is on the Refinement tab, readable by anyone with Team read.
- [DD-15] **Tab order**: Features · Forecasts · Metrics · **Refinement** · Settings · Access. Disabled tooltip differs by role: editors "Choose refinement states in Settings → Refinement"; readers "A Team admin needs to choose refinement states first".
- [DD-16] **Usage data**: tab opening reuses `TeamTabOpened` with a new route key; other events are designed in DEVOPS against the KPIs below.
- [DD-17] **No "can't tell yet" answer** — DD-11 confirmed as proposed (see: maintainer answer 2026-10-02; resolves Q1).
- [DD-18] **Epic split accepted**: E1 its own new Epic; #5881 retitled for E2; #5510 retitled for E3; E4 and E5 new. The maintainer applies it in ADO (see: maintainer answer 2026-10-02; resolves Q2).
- [DD-19] **Clients may vote.** The CLI and MCP can cast a vote (Yes / Yes, but… with condition / No, plus a comment, and a comment-only question per DD-11), and can take one back once slice 16 exists. Identity follows the UI rules (DD-10): auth on → the account behind the client's credential; auth off → a self-declared name the client supplies. Voting is a write, so it is a Lighthouse-Clients **minor** bump. Hidden split (DD-12) applies to the client caller exactly as to a browser. **DESIGN must decide**: (a) whether the client credential resolves to a *person* — if an API key belongs to the instance or a service rather than to a user, a vote under it has no voter and must be refused, not attributed; (b) where the auth-off name lives on the client side (flag, config file, MCP tool argument) and that it is required, never defaulted; (c) whether a client-cast vote is marked as such in the log (recommended, so a reader can tell; also a candidate closed-enum property for DEVOPS); (d) that the MCP tool's description says the vote is the *user's* judgement, so an assistant asks before casting. The tension with D30 ("green means somebody looked") is accepted by the maintainer and mitigated only by (a) and (d) (see: maintainer answer 2026-10-02; supersedes DD-13; resolves Q3).
- [DD-20] **Presenter mode never records votes on anyone else's behalf.** It is a shared-screen view of the Refinement tab, one Work Item at a time, splits visible (DD-12 exemption). Auth on: the only vote or comment it can save is the facilitator's own, under their account; colleagues vote from their own devices; anything the room concludes is captured as a comment by the facilitator. Auth off: the same — the facilitator's browser votes under its own self-declared name, nothing more; no per-vote name entry for others. Votes and comments saved while presenter mode is open are marked as cast in a live session (K4, K7) (see: maintainer answer 2026-10-02; resolves Q4).
- [DD-21] **Readiness defaults: minimum Yes 3, veto off; minimum voters defaults to 3.** Minimum voters is kept as a separate setting (D16) but may never be set below minimum Yes — a lower value would be dead, because 3 Yes already means 3 voters. It only bites when raised above minimum Yes (e.g. 3 Yes out of at least 4 voters) (see: maintainer answer 2026-10-02; supersedes DD-8's defaults; resolves Q5).
- [DD-22] **Voting first after the walking skeleton**: order 01 → 02 → 10 → 11 → 13 → 03 → 04 → 05 → … . Slice 13 ships standalone — rows show Ready / n more Yes needed / Needs discussion and the heading counts vote-ready Work Items; when 03 lands, state-stage readiness joins the same count, and when 05 lands the verdict picks the count up. No slice in E3 waits for E2 (see: maintainer answer 2026-10-02).

---

## Wave: DISCUSS / [REF] Scope Assessment

**OVERSIZED — split proposed (maintainer to confirm in ADO; nothing changed there).** Signals: ~18 stories (>10);
≥4 areas with their own behaviour (Team settings, forecasting, a new vote log with identity, live sessions); effort
~16–18 days (>2 weeks); at least three independently shippable outcomes (see the list; know whether to refine; votes
make Work Items ready; run a session in the room). Four of five signals fire.

Split into **four Epics shipped in order E1 → (E2 ∥ E3) → E4**, plus one Premium Epic for later (E5). Each slice is
≤1 day **except slice 11 (~1.5 days)**: a new vote log, its migration, the vote endpoint with self-declared identity
and the vote control are the smallest thing a voter can use; cutting it further leaves a slice nobody can see. It is
called out here rather than disguised.

### Epic rewrite — ACCEPTED by the maintainer 2026-10-02 (DD-18); ADO not changed by this wave

| Epic | Title (term-neutral) | Goal | Slices | Tier | Depends on | ADO |
|---|---|---|---|---|---|---|
| E1 | **Refinement tab: see the Work Items in refinement** | Anyone opens a Team's Refinement tab and sees every Work Item in its refinement states, in backlog order | 01–02 | Community | — | **New Epic** |
| E2 | **Refinement need: refine enough, then stop** | Before the next Refinement, the tab says below / in / above range and marks the Work Items needed by then | 03–09 | Community | E1 | **#5881 kept, retitled** (was "Refinement Need Chart" — it is a verdict and a line, not a chart); ICE 60 stays; `ValueFlow` tag kept |
| E3 | **Sizing votes against the SLE** | Team members say Yes / Yes, but… / No per Work Item, any time, and enough votes make it Ready | 10–17b | Community (named votes come with Premium auth) | E1; 13 feeds E2's ready count | **#5510 kept, retitled** (was "Sizing Poker"); ICE 90, `Community` tag stay |
| E4 | **Live Refinement sessions** | The Team runs its Refinement on one shared screen, later remotely | 18–19 | Community | E3 | **New Epic**, Options, after E3 |
| E5 | **Sizing calibration** | "Of the Work Items you voted Yes, 82% finished within the SLE" | none yet | Premium | E3 + ≥ several weeks of votes | **New Epic**, Options, not sliced (D22) |

Alternative if the maintainer prefers fewer Epics: fold E1 into #5881 as its first two slices (#5510 then depends
on #5881's first two Stories). The proposal keeps E1 separate because both #5510 and #5881 stand on it and it is
the only part both need.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone (flow coach + voter)**: *set up refinement* → *see what is in refinement* → *know whether to refine more
or stop* → *give / collect sizing views* → *see what is ready* → *run the Refinement*.

| Set up | See | Know | Vote | Ready | Run |
|---|---|---|---|---|---|
| **01 refinement states** | **02 list in backlog order** | 05 need + verdict | 11 cast a vote | 13 readiness rule | 18 presenter mode |
| 03 stages | | 06 "enough for" line | 10 SLE yardstick | 08 stage rules | 19 remote spike |
| 04 cadence | | 09 CLI/MCP need | 12 "Yes, but…" + comments | 17a CLI/MCP read votes | |
| | | | 17b CLI/MCP cast a vote | | |
| 07 band setting | | | 14 hidden until cast | | |
| | | | 15 with an account · 16 revoke | | |

*Note 2026-10-04: 08 is folded into 03 and 14 is removed (maintainer); the map is kept as it was drawn.*

**Walking skeleton = 01 + 02** (brownfield: existing Team page, settings, tab pattern S1, demo states S8). It
crosses settings → persistence → tab enablement → list, which every later slice stands on. "Know", "Vote" and "Run"
are deliberately not on the skeleton line: the skeleton proves Teams can name their refinement states and that the
list is the right home, which is R1 (setup friction), the cheapest fatal assumption.

| # | Slice | Epic | Est. | Learning hypothesis — disproves … if it fails |
|---|---|---|---|---|
| 01 | Refinement states in Team Settings; tab disabled-with-tooltip → enabled | E1 | 1d | "Teams can name their refinement states from their mapped states" — if on the dev instance or demo Teams the states that mean refinement are unmapped or shared with delivery, rules (08) are needed from day one |
| 02 | Refinement tab lists the Work Items in backlog order; "Refinement" term | E1 | 1d | "A list in backlog order is the right home" — if a real Team's To Do state is the whole 300-item backlog, the list is noise and stages (03) must come before anything else |
| 03 | Tag each refinement state Waiting / Being refined / Ready; ready count | E2 | ½d | "States already make the split" (D15) — if the dogfood Team has no state meaning Ready, 08 becomes a prerequisite of 05 |
| 04 | Refinement cadence (weekdays, every N weeks, starting week) → "Next Refinement: Thu 8 Oct" | E2 | 1d | "Weekdays + every N weeks expresses real cadences" — if Teams refine "first Tuesday of the month" or ad hoc, D27's model fails |
| 05 | Need number, band (median–85%), verdict below / in / above incl. "stop" | E2 | 1d | "HowMany over total Throughput gives a band narrow enough to act on" — if a 1-week band on real data spans e.g. 2–14, the verdict is always "in range" and says nothing |
| 06 | Highlight the next N and draw "enough for ‹date›" in the list | E2 | ½d | "A line in the list is read as the answer" — if the dogfood coach still asks "so how many do we refine?", the verdict sentence must carry the number of Work Items to refine |
| 07 | Band percentiles as a Team setting | E2 | ½d | "50/85 suits most Teams" — if every dogfood Team changes it in week one, the defaults are wrong |
| 08 | Optional stage rules (reuse the rule editor); a Ready rule overrides votes | E2 | 1d | "The blocked-items rule editor is usable for stages" — if setting a Ready rule takes a config admin more than five minutes, it needs presets |
| 09 | CLI/MCP: need facts + list (Lighthouse-Clients minor bump) | E2 | 1d | "Facts on the wire let a client state the verdict honestly" — if the client must re-derive the band, the API shape is wrong |
| 10 | The SLE yardstick on the tab; fallback 85th pct of the default cycle time + hint | E3 | ½d | "Every Team has an SLE or accepts the fallback" — if dev-instance fallbacks are ≥ 30 days, the question "doable within 30 days?" is meaningless and the hint must be louder |
| 11 | Cast a vote (Yes / Yes, but… / No) from the list, auth off with a self-declared name | E3 | **1½d** | "A vote costs seconds from the list" — if voting on 5 Work Items takes a dogfood voter > 2 minutes, a focused voting mode is needed |
| 12 | Condition for "Yes, but…", comments, open questions; the log per Work Item | E3 | 1d | "Comments carry the conditions" (D5) — if "Yes, but…" votes arrive without a condition most of the time, the condition must be mandatory |
| 13 | Readiness setting (min Yes 3, min voters, veto); votes make Work Items Ready; standalone ready count, picked up by 03/05 when they land (DD-22) | E3 | 1d | "Votes can stand in for 'ready'" — if the vote-ready count and the state-ready count disagree wildly on the dogfood Team, DD-5 order is wrong |
| 14 | Others' votes hidden until you cast yours | E3 | ½d | "Anchoring matters async" — if no dogfood voter notices, the slice is cancellable |
| 15 | Votes under the signed-in account (auth on) | E3 | ½d | "Readers vote with their own account through RBAC" — if TeamRead cannot carry a write, RBAC needs a new requirement |
| 16 | Take back my vote | E3 | ½d | Lower priority; disproves "people need to revoke rather than re-vote" if no one uses it in a month |
| 17a | CLI/MCP: read tallies + readiness | E3 | ½d | Same as 09, for votes |
| 17b | CLI/MCP: cast a vote (and take it back once 16 exists), identity per DD-19 (Lighthouse-Clients minor bump) | E3 | 1d | "A vote cast from a terminal or assistant is still a person's judgement" — if dogfood client votes arrive that the named voter does not recognise, DD-19 (d) is not enough and client voting needs a confirmation step |
| 18 | Presenter mode: one shared screen, one Work Item at a time; only the facilitator's own vote/comment is saved (DD-20) | E4 | 1d | "A shared screen is enough to make the room discuss only the doubted" — if the dogfood session still walks every Work Item, the gateway story is wrong |
| 19 | Spike: remote facilitated session (push channel, auth-off subject) | E4 | ≤1d timebox | Learning only: can an auth-off browser join a hub session without weakening tenant security (R5, X4) |

*Note 2026-10-04: 08 is folded into 03 (#6146 Removed) and 14 is removed (#6152); the rows are kept as written.*

Briefs: `slices/slice-NN-*.md`.

### Prioritisation rationale

Order (maintainer, DD-22): **01 → 02 → 10 → 11 → 13 → 03 → 04 → 05 → 06 → 12 → 07 → 15 → 14 → 08 → 16 → 09 → 17a → 17b → 18 → 19.**

*Note 2026-10-04: 08 is folded into 03 and 14 is removed; each Epic now ships on its own, so its slices run in order
without another Epic's interleaved.*

- **01, 02 first**: the walking skeleton and R1 (setup friction, score 16) — fatal if Teams won't name states.
- **10, 11, 13 next — voting first**: R4 (async votes, score 17) is the riskiest bet and needs weeks of usage
  data, so its clock starts right after the skeleton — R4 is measured by K4; no push channel (D21). 10 is the
  question's yardstick (half a day); 13 turns votes into Ready and ships standalone (DD-22).
- **03–05 then**: the count and the "stop" verdict (O2, top outcome 14). 03 is the verdict's precondition; vote-ready
  Work Items from 13 are already there to be counted when 05 lands.
- **06, 12, 07** add precision once the core loop exists; **15** before **14** because auth-on readers must be
  able to vote at all before anchoring is polished; **14 and 16** are cancellable.
- **08** is late because D15 says most Teams' states already make the split — 03's hypothesis decides whether it moves up.
- **09, 17a, 17b** clients after the UI shapes settle; 17a and 17b can share one client minor release.
- **18, 19** last: live sessions are the gateway (D8) but async ships first.

### Slice taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice? | Pass — the largest (11) adds a vote control, a name prompt and a tally cell. |
| Every slice depends on a new abstraction? | Pass — the vote log is born in 11, its first consumer. |
| Disproves a pre-commitment? | Pass — 03 disproves D15's premise, 04 D27's, 05 D20's usefulness, 11 R4's cost side. |
| Synthetic-data-only slices? | Pass — each is dogfooded on the dev instance (`:5169`, real history) or Team Gravity's real-shaped demo states; demo data seeds settings, never accepts a slice on its own. |
| Two slices alike except scale? | Pass — 09/17a are one read slice per Epic by design; 17b is a write, not the same slice again. |

---

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-5510-5881-refinement.yaml` (refined this wave: two journeys, voter journey added,
jobs re-pointed). Emotional arc for the coach: **guessing → oriented → restrained** ("refine 2 to 5 more for Thu 8 Oct,
and nothing beyond the line"); for the voter: **interrupted → quick → heard** ("three clicks, my 'Yes, but…' says
why").

```
Team › Refinement                                   Next Refinement: Thu 8 Oct
┌──────────────────────────────────────────────────────────────────────────┐
│ 3 ready — below the range of 5–8 Work Items Team Gravity is likely to    │
│ pull before Thu 8 Oct.  Refine 2 to 5 more.                              │
│ SLE: 85% within 7 days                                                   │
├──────────────────────────────────────────────────────────────────────────┤
│ # │ Work Item                       │ State     │ Stage        │ Votes     │
│ 1 │ GR-058 User activity tracking   │ Next      │ Ready        │ ✓ ready   │
│ 2 │ GR-059 Advanced search filters  │ Next      │ Ready        │ ✓ ready   │
│ 3 │ GR-051 Advanced reporting module│ Analysing │ Being refined│ 1 more Yes│
│ … │                                 │           │              │           │
│ 8 │ GR-073 Configuration management │ Backlog   │ Waiting      │ Vote      │
│ ───────── enough for Thu 8 Oct (85%) ── not needed before then ───────── │
│ 9 │ GR-074 Load testing framework   │ Backlog   │ Waiting      │ Vote      │
└──────────────────────────────────────────────────────────────────────────┘
Above range: "11 ready — above the range of 5–8. Stop refining: nothing more is needed before Thu 8 Oct."
```

---

## Wave: DISCUSS / [REF] User Stories

<!-- markdownlint-disable MD024 -->

System constraints (all stories): Team level only (D2) · every user-facing word through Terminology (Work Item, Team,
SLE, Throughput, cycle time, Refinement) · no write-back (D9) · works hosted, standalone and auth off (C2, C3) · all
UI gating via `useRbac()`; nothing fetches `/api/latest/authorization/my-summary` directly · Community unless stated (D25)
· migrations expand-only, via `CreateMigration`.

### US-01 — Choose the refinement states (slice 01)

`job_id: job-flow-coach-refine-just-enough` · persona `config-admin` (Sofia Keller, Team admin of Team Gravity)

**Problem**: Sofia's Team refines Work Items in `Backlog`, `Analysing` and `Next`, but Lighthouse has no idea which
states mean refinement, and two of them are Doing states that already count in WIP.

#### Elevator Pitch
Before: no Team knows which of its states mean refinement; there is no Refinement tab.
After: Team Gravity → **Settings → Refinement** → ticks `Backlog` (To Do), `Analysing` (Doing), `Next` (Doing), saves → sees the **Refinement** tab next to Metrics turn from disabled to enabled.
Decision enabled: which of the Team's states the Refinement view (and later the need number) counts.

**Examples**: (1) Sofia ticks Backlog, Analysing, Next — saved, tab enabled. (2) Team Zenith has none ticked — tab
disabled; Sofia sees "Choose refinement states in Settings → Refinement", reader Jonas Weber sees "A Team admin needs
to choose refinement states first". (3) Sofia later unmaps `Analysing` from Doing — the Refinement section warns
"Analysing is no longer mapped; its Work Items cannot appear".

```gherkin
Scenario: A Team admin turns the Refinement tab on by naming refinement states
  Given Team Gravity maps To Do "Backlog" and Doing "Next", "Analysing"
  When Sofia selects "Backlog", "Analysing" and "Next" under Settings → Refinement and saves
  Then the Refinement tab of Team Gravity is enabled for every reader

Scenario: Only mapped states can be chosen, each labelled with its category
  Given Team Gravity maps "Done" as Done and has an unmapped state "Icebox"
  When Sofia opens Settings → Refinement
  Then she can choose from "Backlog (To Do)", "Next (Doing)", "Analysing (Doing)" only

Scenario: The disabled tab tells each reader what is missing
  Given Team Zenith has no refinement states
  When Jonas, a Viewer, hovers the Refinement tab
  Then it is disabled and reads "A Team admin needs to choose refinement states first"
```

**AC**: AC-1.1 Settings → Refinement offers only To Do and Doing mapped states, each with its category. AC-1.2 Saved
selection survives reload and a Team refresh. AC-1.3 Tab disabled with role-specific tooltip (DD-15) while none
selected; enabled otherwise. AC-1.4 A selected state that later becomes unmapped is flagged in the section, never
silently dropped. AC-1.5 A Doing state carries the note "already counts in {WIP} and {cycle time}". AC-1.6 Only Team
admins can save (server-guarded; UI via `useRbac()`).
**KPI**: K1. **Tech**: additive setting on the Team; DESIGN picks a storage shape that 03 (stage per state) extends without a destructive migration.

### US-02 — See every Work Item in refinement, in backlog order (slice 02)

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach` (Priya Raman, coach of Team Gravity)

**Problem**: Priya prepares the Refinement by scrolling the tracker's backlog; she cannot see in one place what is
in refinement and in what order it will be pulled.

#### Elevator Pitch
Before: the Refinement tab does not exist.
After: Team Gravity → **Refinement** tab → sees GR-058, GR-059, GR-051 … GR-073 in backlog order, each with state, category and age, headed "14 Work Items in Refinement".
Decision enabled: which Work Items to bring to the next Refinement.

**Examples**: (1) Gravity: 14 Work Items, GR-058 first. (2) A Team renamed "Refinement" to "Replenishment" sees that
word on the tab and heading. (3) A Team whose refinement states hold no Work Items sees "No Work Items in Refinement
states right now".

```gherkin
Scenario: The coach sees what is in refinement in pull order
  Given Team Gravity's refinement states hold 14 Work Items
  When Priya opens the Refinement tab
  Then she sees 14 Work Items in backlog order, GR-058 "User activity tracking" first, each with state and category

Scenario: The Team's own word for Refinement is used
  Given Terminology renames "Refinement" to "Replenishment"
  When Priya opens Team Gravity
  Then the tab and heading say "Replenishment"

Scenario: An empty refinement is stated, not shown as an error
  Given Team Zenith's refinement states hold no Work Items
  When Priya opens its Refinement tab
  Then she reads that no Work Items are in Refinement states right now
```

**AC**: AC-2.1 Lists exactly the Work Items in the Team's refinement states, in the same backlog order the Team's
forecasts use. AC-2.2 Each row: id + link to the work tracking system, name, state, category, age. AC-2.3 "Refinement"
is a Terminology key with seeded default "Refinement" (singular/plural), used for tab, heading, tooltip. AC-2.4
Readable by anyone with Team read. AC-2.5 Renders and stays responsive with 300 Work Items. AC-2.6 Demo data: Team Gravity
ships with refinement states `Backlog`, `Analysing`, `Next`; at least one other demo Team ships without any, so the
disabled tab can be shown and tested.
**KPI**: K1, K2. **Tech**: tab-open reuses `TeamTabOpened` with a new route key (S6).

### US-03 — Say which refinement states mean Ready (slice 03)

> **Superseded in part on 2026-10-04 (maintainer).** Refinement states no longer carry a stage. A Work Item's stage
> comes only from two optional rules, "Ready when" and "Being refined when"; anything they do not match is Waiting.
> Stage and votes are two separate signals: without rules the ready count is the votes', with rules it is the
> stages', never a sum, and a row is marked only where votes have been cast and disagree with the stage. US-08 is
> folded into this story (#6141 retitled "Optional stage rules: see what is refined, being refined or waiting"). See
> "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)" below and `distill/upstream-issues.md`.

`job_id: job-flow-coach-refine-just-enough` · persona `config-admin`

**Problem**: The need number has to know how many Work Items are already ready; Sofia's `Next` state means "refined,
waiting to be pulled" but Lighthouse cannot tell.

#### Elevator Pitch
Before: every refinement state looks alike.
After: Settings → Refinement → sets `Next` = Ready, `Analysing` = Being refined, `Backlog` = Waiting → the Refinement tab shows a Stage column and "2 ready".
Decision enabled: whether the Team's own states already say what is ready, or a rule is needed.

**Examples**: (1) Gravity: Next = Ready → 2 ready. (2) A Team with one state `Refinement` leaves it Waiting → 0 ready,
tab says "No state is marked Ready". (3) Sofia marks two states Ready → both counted.

```gherkin
Scenario: The ready count follows the Team's states
  Given Team Gravity's "Next" is marked Ready and holds GR-058 and GR-059
  When Priya opens the Refinement tab
  Then it says 2 Work Items are ready and shows each Work Item's stage

Scenario: New refinement states start as Waiting
  When Sofia adds "Backlog" as a refinement state
  Then its stage is Waiting until she changes it

Scenario: A Team without a Ready state is told so
  Given no refinement state of Team Pulsar is marked Ready and no rule exists
  When Priya opens its Refinement tab
  Then it reads that no state is marked Ready yet, with a link for Team admins
```

**AC**: AC-3.1 Each refinement state carries exactly one stage, default Waiting. AC-3.2 Ready count = Work Items whose
stage resolves to Ready, added to the vote-ready Work Items slice 13 already counts (DD-5, DD-22). AC-3.3 Stage shown per row. AC-3.4 Expand-only storage change.
**KPI**: K1. **Tech**: depends on 01.

### US-04 — Set the Refinement cadence (slice 04)

`job_id: job-flow-coach-refine-just-enough` · persona `config-admin`

**Problem**: "How many do we need" only has an answer relative to *when* the Team next refines.

#### Elevator Pitch
Before: Lighthouse does not know when the Team refines.
After: Settings → Refinement → Cadence: Thursday, every 1 week → Refinement tab header reads "Next Refinement: Thu 8 Oct".
Decision enabled: the horizon every count on the tab is measured against.

**Examples**: (1) Thursdays weekly, today Fri 2 Oct → Thu 8 Oct. (2) Tuesdays every 2 weeks from week of 6 Oct → Tue 6
Oct, then Tue 20 Oct. (3) Today is Thu 8 Oct (a Refinement day) → next is Thu 15 Oct (DD-6). (4) No cadence → no date,
hint "Set a Refinement cadence to see how many Work Items are needed".

```gherkin
Scenario: The next Refinement date follows the cadence
  Given Team Gravity refines on Thursdays every week and today is Friday 2 October 2026
  When Priya opens the Refinement tab
  Then it reads "Next Refinement: Thu 8 Oct"

Scenario: On a Refinement day the tab looks to the following one
  Given today is Thursday 8 October 2026
  Then the next Refinement shown is Thu 15 Oct

Scenario: Without a cadence the list stays and the number is replaced by a hint
  Given Team Pulsar has refinement states but no cadence
  When Priya opens its Refinement tab
  Then she sees the list and votes and a hint to set the Refinement cadence, but no need number
```

**AC**: AC-4.1 Cadence = one or more weekdays + every N weeks (N ≥ 1) + starting week when N > 1. AC-4.2 Next date per
DD-6 in the instance time zone. AC-4.3 No cadence → D29 behaviour. AC-4.4 Editors only.
**KPI**: K1. **Tech**: blackout days are not skipped in v1 (a Refinement on a holiday is the Team's call) — flagged for DESIGN.

### US-05 — Know whether to refine more or stop (slice 05)

> **Superseded in part on 2026-10-04 (maintainer): AC-5.7 only.** The ready count the verdict uses is the votes'
> on a Team without stage rules and the stages' on a Team with them — never the two added together. The rest of
> this story stands. See "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)" below and
> `distill/upstream-issues.md`.

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach`

**Problem**: Priya's Team "refines ten more, just in case"; nothing tells her when enough is enough.

#### Elevator Pitch
Before: the Refinement tab is a list.
After: Refinement tab → sees "3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull before Thu 8 Oct. Refine 2 to 5 more." — or, at 11 ready, "Stop refining: nothing more is needed before Thu 8 Oct."
Decision enabled: refine more, or stop and spend the meeting elsewhere.

**Examples**: (1) 3 ready, band 5–8 → below, "Refine 2 to 5 more". (2) 6 ready → in range, "Nothing more needs refining
before Thu 8 Oct". (3) 11 ready → above, "Stop refining" at the same weight. (4) Team with 9 days of Throughput history →
the existing minimum-data message instead of a number.

```gherkin
Scenario: Below range asks for a bounded number of Work Items
  Given Team Gravity has 3 ready Work Items and is likely to pull 5 to 8 before Thu 8 Oct
  When Priya opens the Refinement tab
  Then she reads that 3 are ready, below the range of 5–8, and that 2 to 5 more should be refined

Scenario: Above range says stop as loudly as refine more
  Given Team Gravity has 11 ready Work Items and a range of 5–8
  When Priya opens the Refinement tab
  Then she reads "Stop refining" in the same place and style as the below-range message

Scenario: Too little Throughput history gives the familiar guard, not a number
  Given Team Lightspeed has less Throughput history than forecasts require
  When Priya opens the Refinement tab
  Then she sees the minimum-data message forecasts already use, and the list
```

**AC**: AC-5.1 One `HowMany` run over the Team's Throughput (same history window and settings as its forecasts) for
the days until the next Refinement. AC-5.2 Low end = 50% value; high end = count exceeded with 15% likelihood (DD-2).
AC-5.3 Verdict per DD-3, boundaries inclusive in range. AC-5.4 Below/in/above share one component and size (C5). AC-5.5
The API returns facts (counts, band, date, verdict enum), never a sentence. AC-5.6 Minimum-data guard reused. AC-5.7 The ready count includes the vote-ready Work Items of
slice 13, which ships earlier (DD-22).
**KPI**: K3. **Tech**: careful with `HowManyForecast` semantics — its "85%" is the *conservative* (low) count.

### US-06 — See which Work Items are needed before the next Refinement (slice 06)

> **Superseded in part on 2026-10-04 (maintainer).** The "enough for" line follows the order the rows are shown
> in, not backlog order: a "#" column numbers the first N rows as shown (N = the high end) and the line sits after
> the N-th. Backlog order is only the default, and a sort is not kept. With fewer Work Items than N the line after
> the last row reads "All 6 Work Items in Refinement are needed before Thu 8 Oct." See "Maintainer decision — the E2
> UI, and slice 08 folded into 03 (2026-10-04)" below and `distill/upstream-issues.md`.

`job_id: job-flow-coach-refine-just-enough` · persona `product-owner` (Marco Bianchi)

#### Elevator Pitch
Before: the verdict says "refine 2 to 5 more" but not which ones.
After: Refinement tab → the first 8 Work Items in backlog order are highlighted and a line reads "enough for Thu 8 Oct (85%)"; below it "not needed before then".
Decision enabled: which Work Items to refine next, and which to leave alone.

**Examples**: (1) High end 8 → line after GR-073 (#8). (2) Only 5 Work Items in refinement, high end 8 → all highlighted,
line at the bottom reading "fewer Work Items in Refinement than may be needed". (3) No cadence → no line.

```gherkin
Scenario: The line marks the Work Items needed before the next Refinement
  Given the range before Thu 8 Oct is 5–8
  When Marco opens the Refinement tab
  Then the first 8 Work Items in backlog order are highlighted and a line reads "enough for Thu 8 Oct"

Scenario: Too few Work Items in refinement is said at the line
  Given only 5 Work Items are in refinement states and the high end is 8
  Then all 5 are highlighted and the line says fewer are in Refinement than may be needed
```

**AC**: AC-6.1 N = high end (DD-4), counted over all refinement Work Items. AC-6.2 Line absent when no number. AC-6.3
Highlight is not colour-only (accessible).
**KPI**: K3.

### US-07 — Tune the band to the Team's risk appetite (slice 07)

`job_id: job-flow-coach-refine-just-enough` · persona `config-admin`

#### Elevator Pitch
Before: the band is fixed at 50–85%.
After: Settings → Refinement → band 50% / 95% → the tab's range for Thu 8 Oct widens from 5–8 to 5–10.
Decision enabled: how much running-dry risk the Team accepts.

**Examples**: (1) 50/85 default. (2) 30/85 → low end drops. (3) Low ≥ high → refused with "the low end must be below
the high end".

```gherkin
Scenario: A wider band moves the stop line
  Given Team Gravity's band is changed to 50% and 95%
  When Priya opens the Refinement tab
  Then the range and the "enough for" line use the new percentiles

Scenario: An inverted band is refused
  When Sofia sets the low end to 90% and the high end to 85%
  Then saving is refused with a message naming both values
```

**AC**: AC-7.1 Percentiles 1–99, low < high. AC-7.2 Defaults 50/85. AC-7.3 Editors only.

### US-08 — Split refinement stages by rule (slice 08)

> **Folded into US-03 on 2026-10-04 (maintainer); ADO #6146 Removed.** Stage rules are now slice 03's whole story,
> and a Ready rule no longer overrides the votes: stage and votes are two separate signals. Pulsar's example below is
> a slice-03 scenario. See "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)" below and
> `distill/upstream-issues.md`.

`job_id: job-flow-coach-refine-just-enough` · persona `config-admin`

**Problem**: Team Pulsar has one state, `Refinement`, and marks ready Work Items with the tag `ready`.

#### Elevator Pitch
Before: Pulsar cannot show anything as Ready.
After: Settings → Refinement → Ready rule "Tags contains ready" → the tab shows 4 Ready and the verdict uses them.
Decision enabled: use the Team's own markers without changing its workflow.

```gherkin
Scenario: A rule marks Work Items Ready without a dedicated state
  Given Team Pulsar's Ready rule is "Tags contains ready" and 4 Work Items carry it
  When Priya opens the Refinement tab
  Then 4 Work Items are Ready

Scenario: A Ready rule overrides the vote outcome for the Work Items it matches
  Given PU-112 carries the tag "ready" and has one No vote under a veto of 1 No
  Then PU-112 is Ready
```

**AC**: AC-8.1 One optional rule per stage, same `WorkItemRuleSet` limits and editor as blocked rules. AC-8.2 DD-5
precedence. AC-8.3 Rules only ever narrow to Work Items already in refinement states.

### US-09 — Ask the refinement need from the CLI or an assistant (slice 09)

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach`

#### Elevator Pitch
Before: the need is only on the web page.
After: `lighthouse teams refinement 3` (and the MCP tool `get_team_refinement`) → prints "Team Gravity · next Refinement Thu 8 Oct · 3 ready · range 5–8 · below — refine 2 to 5 more", then the list with the line.
Decision enabled: same as US-05, from the terminal or an assistant.

```gherkin
Scenario: The CLI states the verdict the web page states
  Given Team Gravity is below range with 3 ready and a range of 5–8
  When Priya runs the refinement command for Team Gravity
  Then it prints the next Refinement date, the ready count, the range and "refine 2 to 5 more"

Scenario: The CLI says why there is no number
  Given Team Pulsar has no cadence
  Then the command prints the list and that a Refinement cadence is needed for a number
```

**AC**: AC-9.1 Uses the same endpoint as the tab. AC-9.2 Client composes the sentence from facts, using the
instance's Terminology. AC-9.3 Lighthouse-Clients minor version + changeset. Command names to DESIGN.

### US-10 — See the yardstick votes are cast against (slice 10)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter` (Jonas Weber, developer, Team Gravity)

#### Elevator Pitch
Before: "fits our SLE" means whatever each voter remembers.
After: Refinement tab → header reads "Doable within 7 days? (our SLE, 85%)"; on a Team without an SLE: "Doable within 12 days? (85th percentile of {cycle time} — set an SLE to replace this)".
Decision enabled: the voter answers against the same number as everyone else.

```gherkin
Scenario: The Team's SLE is the yardstick
  Given Team Gravity's SLE is 85% within 7 days
  When Jonas opens the Refinement tab
  Then the question reads "Doable within 7 days?"

Scenario: Without an SLE the fallback is shown and named as a fallback
  Given Team Meridian has no SLE and its default cycle time's 85th percentile is 12 days
  Then the question reads "Doable within 12 days?" with a hint to set an SLE

Scenario: No SLE and no finished Work Items
  Given Team Equinox has no SLE and no finished Work Items
  Then the question asks without a number and the hint says why
```

**AC**: AC-10.1 Fallback = 85th percentile of the Team's **default** cycle time only (D24), over the Team's metrics
window. AC-10.2 Hint links Team admins to the SLE setting. AC-10.3 Terms via Terminology.

### US-11 — Cast a sizing vote from the list (slice 11)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter` · ~1½ days (called out)

**Problem**: Jonas has no doubt about most Work Items but sits through all of them in the meeting.

#### Elevator Pitch
Before: views only exist in the meeting.
After: Refinement tab → on GR-073 clicks **Yes**, first time types his name "Jonas Weber" → the row shows "1 vote", his own vote marked; the next vote needs no name.
Decision enabled: say "this fits" in seconds, so the meeting can skip GR-073.

**Examples**: (1) Jonas votes Yes on GR-073 on Tue 6 Oct, auth off, name once per browser. (2) Ana Lima votes No on
GR-051 from her own browser. (3) Jonas changes GR-073 to "Yes, but…" — a new log entry, latest counts (DD-9).
(4) A Work Item that left refinement keeps its votes in the log but is no longer listed.

```gherkin
Scenario: A reader votes in seconds without an account on a Community instance
  Given authentication is off and Jonas has never voted from this browser
  When Jonas chooses Yes on GR-073 and enters the name "Jonas Weber"
  Then GR-073 shows 1 vote and the name is remembered for his next vote

Scenario: Changing one's mind adds to the log
  Given Jonas voted Yes on GR-073
  When he chooses "Yes, but…"
  Then his current vote is "Yes, but…" and both entries remain in the log

Scenario: Votes are open whatever the verdict says
  Given Team Gravity is above range
  When Ana votes on GR-074, below the "enough for" line
  Then her vote is accepted
```

**AC**: AC-11.1 Three answers on every Work Item in a refinement state, any time (DD-1). AC-11.2 Auth off: name per
browser, required once, editable (DD-10). AC-11.3 Append-only log (DD-9). AC-11.4 Readers may vote (TeamRead); Team
admins too. AC-11.5 ≤ 2 interactions per vote once the name is set. AC-11.6 New vote log aggregate + migration
(expand-only).
**KPI**: K4, K5.

### US-12 — Say the condition behind "Yes, but…" (slice 12)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter`

#### Elevator Pitch
Before: a "Yes, but…" says nothing about the "but".
After: chooses "Yes, but…" on GR-051, types "only if the PDF export moves to its own Work Item" → the row shows a comment marker; opening it shows every vote and comment in order.
Decision enabled: the Team discusses the condition, not the whole Work Item.

```gherkin
Scenario: A "Yes, but…" carries its condition
  When Ana chooses "Yes, but…" on GR-051 with "only if the PDF export moves to its own Work Item"
  Then the condition is shown with her vote in GR-051's log

Scenario: A question without a vote flags the Work Item
  When Jonas comments "Which API version?" on GR-054 without voting
  Then GR-054 is marked as having an open question and his comment counts as no vote
```

**AC**: AC-12.1 Comment optional on every answer; prompted (not forced) for "Yes, but…". AC-12.2 Comment-only entry
allowed (DD-11). AC-12.3 Log ordered oldest first, with names and dates.

### US-13 — See which Work Items the votes make Ready (slice 13)

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach` (setting by `config-admin`)

#### Elevator Pitch
Before: votes are just counts.
After: Settings → Refinement → readiness at its default "3 Yes", plus a veto at 1 No → Refinement tab shows GR-073 "Ready", GR-051 "2 more Yes needed", GR-054 "Needs discussion (1 No)", and the heading reads "1 ready by votes".
Decision enabled: which Work Items the meeting must discuss (and, once slice 05 exists, whether the Team still needs to refine more).

```gherkin
Scenario: Enough Yes votes make a Work Item Ready
  Given readiness needs 3 Yes and GR-073 has Yes from Jonas and Mo and "Yes, but…" from Ana
  When Priya opens the Refinement tab
  Then GR-073 is Ready and counted in the ready total

Scenario: A veto sends a Work Item to discussion
  Given the veto is 1 No and Ana voted No on GR-054
  Then GR-054 reads "Needs discussion"

Scenario: Missing votes are named, to be raised in the Team's rituals
  Given GR-051 has 1 Yes
  Then it reads "2 more Yes needed"
```

**AC**: AC-13.1 Setting per DD-7/DD-21: defaults 3 Yes, min voters 3, veto off; min Yes ≥ 1; min voters never below
min Yes. AC-13.2 Ready resolution per DD-5 (before slice 03/08 exist, votes alone decide). AC-13.3 Status per row:
Ready / n more needed / Needs discussion. AC-13.4 Ships standalone: the heading counts vote-ready Work Items; once
slice 05 exists its verdict uses the same count (DD-22) — that AC belongs to 05, not here.
**KPI**: K5.

### US-14 — Vote without being anchored (slice 14)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter`

#### Elevator Pitch
Before: Jonas sees "2 No" before forming his own view.
After: Refinement tab → GR-051 shows "3 votes" until Jonas votes; after his vote the split appears.
Decision enabled: an independent view.

```gherkin
Scenario: The split is hidden until you vote
  Given GR-051 has 3 votes and Jonas has not voted on it
  Then he sees "3 votes" without the split
  When he votes Yes
  Then he sees the split and the comments
```

**AC**: AC-14.1 Per Work Item, per voter (account or browser). AC-14.2 Team admins see the same (no bypass) except in
presenter mode. AC-14.3 Readiness status stays visible (it reveals no split). Cancellable per hypothesis.

### US-15 — Vote under my own account (slice 15)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter` on a Premium instance with auth on

#### Elevator Pitch
Before: with auth on, a vote cannot carry who cast it.
After: signed in as Jonas (Viewer on Team Gravity) → votes Yes on GR-073 → the log shows "Jonas Weber" from his account; no name prompt.
Decision enabled: accountable votes the Team can follow up.

```gherkin
Scenario: A signed-in reader votes under their account
  Given authentication is on and Jonas holds Viewer on Team Gravity
  When he votes Yes on GR-073
  Then the vote is recorded under his account name without a prompt

Scenario: Someone without Team read cannot vote
  Given Lena holds no role on Team Gravity
  Then she cannot see Team Gravity's Refinement tab or vote through the API
```

**AC**: AC-15.1 Identity from the session only (D14, no guest links). AC-15.2 TeamRead suffices for the vote write —
a deliberate read-permission write, flagged for DESIGN. AC-15.3 Names visible to the Team (Premium by platform, P2).

### US-16 — Take back my vote (slice 16, lower priority)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter`

#### Elevator Pitch
Before: a mistaken vote can only be changed, not removed.
After: on GR-073 clicks **Take back** → his vote no longer counts; the log shows "Jonas Weber took back his vote, Tue 6 Oct".
Decision enabled: withdraw a view given too early.

```gherkin
Scenario: A taken-back vote stops counting and stays in the log
  Given Jonas voted Yes on GR-073
  When he takes it back
  Then GR-073 no longer counts his vote and the log records the revocation

Scenario: Only the voter can take a vote back
  Given Ana's vote on GR-073 came from another browser
  Then Jonas is offered no way to take it back
```

### US-17a — Read votes and readiness from the CLI or an assistant (slice 17a)

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach`

#### Elevator Pitch
Before: the refinement command (US-09) shows no votes.
After: `lighthouse teams refinement 3` → each line ends with "Ready" / "1 more Yes needed" / "Needs discussion"; the MCP tool returns the same.
Decision enabled: which Work Items to put on the meeting agenda, without opening the browser.

```gherkin
Scenario: The command lists what needs discussion
  Given GR-054 needs discussion and GR-073 is Ready
  When Priya runs the refinement command
  Then GR-054 is listed as needing discussion and GR-073 as Ready

Scenario: Hidden splits stay hidden in the client too
  Given the client's voter has not voted on GR-051
  Then it returns the count and readiness of GR-051, not the split
```

**AC**: AC-17a.1 Read path only; additive response fields. AC-17a.2 DD-12 applies to the client caller. AC-17a.3 May
share one minor client release with 17b.

### US-17b — Cast a sizing vote from the CLI or an assistant (slice 17b)

`job_id: job-team-member-give-sizing-view` · persona `team-member-voter`

#### Elevator Pitch
Before: a vote needs the browser.
After: `lighthouse teams refinement vote 3 GR-051 --yes-but "only if the PDF export moves to its own Work Item" --as "Ana Lima"` (auth off) → prints "Recorded: Ana Lima — Yes, but… on GR-051. GR-051: 2 more Yes needed"; the MCP tool `vote_on_work_item` does the same after the assistant confirms with Ana. With auth on, no `--as`: the vote carries the account behind the client's credential.
Decision enabled: give a sizing view from the terminal or the assistant already open, without switching to the browser.

```gherkin
Scenario: A voter casts a "Yes, but…" with its condition from the CLI
  Given authentication is off and Ana supplies the name "Ana Lima"
  When she casts "Yes, but…" on GR-051 with the condition "only if the PDF export moves to its own Work Item"
  Then GR-051's log shows Ana Lima's "Yes, but…" with the condition, marked as cast from a client

Scenario: Without a name the client refuses to vote on an auth-off instance
  Given authentication is off
  When a vote is sent without a voter name
  Then it is refused with a message asking for the name, and nothing is recorded

Scenario: A credential that belongs to no person cannot vote
  Given authentication is on and the client's credential is not tied to a user account
  When a vote is sent
  Then it is refused and the message says votes need a personal account

Scenario: A vote can be taken back from the client once taking back exists
  Given slice 16 has shipped and Ana's vote on GR-051 came from her client identity
  When she takes it back through the client
  Then GR-051 no longer counts it and the log records the revocation
```

**AC**: AC-17b.1 Yes / Yes, but… (condition) / No, optional comment, comment-only question (DD-11, DD-19); comments
need slice 12. AC-17b.2 Identity per DD-19: auth on → the person behind the credential, refused if none; auth off →
required self-declared name, never defaulted. AC-17b.3 Same endpoint, permission (Team read) and log as the UI.
AC-17b.4 Take back only if slice 16 exists. AC-17b.5 Lighthouse-Clients minor bump + changeset. Open for DESIGN: DD-19
(a)–(d).

### US-18 — Run the Refinement on one shared screen (slice 18)

`job_id: job-flow-coach-refine-just-enough` · persona `flow-coach`

**Problem**: Teams that do Planning Poker in a room today won't start with async; they need a way in.

#### Elevator Pitch
Before: the room scrolls the Refinement tab together and loses its place.
After: Refinement tab → **Present** → full-screen, one Work Item at a time, starting with the highlighted ones that are not Ready, splits and comments visible; Priya adds the room's conclusion as a comment ("Split: PDF export becomes its own Work Item") and moves on.
Decision enabled: the room discusses only the Work Items that are not Ready.

```gherkin
Scenario: The room walks only the Work Items that are not Ready
  Given GR-073 is Ready and GR-051 and GR-054 are not
  When Priya starts presenter mode for Team Gravity
  Then it shows GR-051, then GR-054, and skips GR-073

Scenario: The facilitator captures the room's conclusion as a comment, never as others' votes
  Given authentication is on and Priya is presenting
  When the room agrees to split GR-051
  Then Priya can save her own vote and a comment, and there is no way to record a vote for Jonas or Ana

Scenario: Presenter mode works standalone with authentication off
  Given a standalone instance with authentication off
  Then presenter mode works without anyone else connecting, and saves only the presenting browser's own vote or comment
```

**AC**: AC-18.1 Saves only the facilitator's own vote/comment — account (auth on) or the browser's self-declared name
(auth off); no on-behalf recording (DD-20). AC-18.2 Splits visible (DD-12 exemption). AC-18.3 Same log; entries saved
while presenting are marked live (K4, K7). AC-18.4 Keyboard-only operable.

### Spike S-19 — Remote facilitated session (slice 19, ≤1 day, timeboxed)

Learning objective: can an auth-off browser (shared subject) and an auth-on account join a live session hub without
weakening tenant security (R5, X4), and what identity each carries. Output: an ADR-ready note for E4's next DISCUSS.
Not DoR-gated (spike).

---

## Wave: DISCUSS / [REF] Outcome KPIs

Objective: Teams refine just enough — they stop when the range is reached, and doubted Work Items are the only ones
discussed. **North star: K3** (share of Refinements entered in range). **DEVOPS turns each "Measured by" into a
usage-data event** (name-only preferred, closed-enum properties only; append to `UsageDataEventName`, never renumber).

| # | Who | Does what | Target | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | Opted-in instances with ≥1 Team | configure refinement states for ≥1 Team (R1 setup) | ≥15% within 60 days of E1 | 0 | event "refinement states saved" (name-only) ÷ instances reporting | Leading |
| K2 | Teams with refinement states | open the Refinement tab in ≥3 of 4 consecutive weeks | ≥50% | 0 | `TeamTabOpened` + new Refinement route key | Leading (WAU) |
| K3 | Teams with a cadence and a number | view the tab with verdict *in range* on a Refinement day | from first-month baseline to ≥60% after 3 months | first month after E2 | verdict property `{below, in, above, none}` on a "need shown" event | Leading (north star) |
| K4 | Votes cast | are cast **outside** a live session and **not** on a Refinement day (R4 — measured by K4; no push channel (D21)) | ≥40% of votes | 0 (MVP: ~0%, D19) | "vote cast" with closed enums `{refinementDay, otherDay}` and, from E4, `{liveSession, async}` | Leading (riskiest) |
| K5 | Work Items that become Ready by votes | reach Ready **before** their Refinement day (no meeting time spent) | ≥50% | 0 | "readiness reached" with `{beforeRefinementDay, onRefinementDay}` (readable once a cadence exists, slice 04; slice 13 ships first, DD-22) | Leading |
| K6 | Teams that saw *above range* | do **not** see *above range* again at the next Refinement (they stopped) | ≥50% | first month | derived from K3's verdict series | Leading |
| K7 | Teams using presenter mode | also have votes outside sessions within 4 weeks (gateway works) | ≥30% | 0 | K4's `liveSession` property per instance | Leading (E4) |

Guardrails: tab renders ≤2 s at 300 Work Items; existing forecasts unchanged (no shared setting altered); *below range*
share does not rise above its first-month value (the stop signal must not starve Teams). Qualitative until data
arrives: dogfood on the dev instance + 5 Mom Test conversations (R3) — owed by the maintainer, not a build step.

---

## Wave: DISCUSS / [REF] Out of Scope (first versions)

Portfolio level (D2) · cross-Team overview (D23) · sizing calibration (E5, Premium, D22) · write-back (D9) · push,
reminders, inboxes, deadlines (D21) · guest links with auth on (D14) · story points / estimation numbers · a choice of
cycle-time definition for the fallback (D24) · recording votes on someone else's behalf, presenter mode included (DD-20) ·
client votes under a credential that belongs to no person (DD-19) · rounds or session entities for async
(DD-1) · blackout-aware cadence dates (US-04 note).

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

No silent N/A — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **New, and one of it is unusual.** Settings (states, stages, cadence, band, readiness, rules) = Team admin, server-guarded, UI via `useRbac().isTeamAdmin` like the Settings tab. Tab, list, need, comments and **votes = Team read**: the vote endpoint is a *write gated by a read permission* — a first; DESIGN must state it explicitly rather than reach for an edit requirement. Auth off: no RBAC, everyone votes, identity self-declared (DD-10). Auth on + RBAC off: every signed-in user. No component fetches `/api/latest/authorization/my-summary` directly. |
| **Lighthouse-Clients CLI/MCP** | **Owed, including a write (DD-19).** Slice 09 (read need, minor bump), 17a (read votes, minor bump) and **17b (cast and take back a vote — a write, minor bump)**; 17a + 17b may share one release. Changesets in `lighthouse-clients`; `pnpm release:version` before the release run. New endpoints and fields are additive (no existing client response shape changes). The client's identity handling (credential → person; required self-declared name with auth off) is a DESIGN decision listed under DD-19. |
| **Website / marketing surface** | **Owed at finalize of E2 and E3**: letpeople.work/sizing-poker today points at the standalone MVP — once E3 ships, decide with the maintainer whether that page points to Lighthouse. Feature list on the site: Community feature, not Premium. Website lives in a separate repo; not read this wave; confirm before editing copy. |
| **Docs + screenshots** | **Owed per Epic at finalize**, not batched: a new docs page "Refinement" (tab, need, votes), a Team Settings section, Terminology entry; `@screenshot` one per theme for the tab below range, above range, and the vote log. Docs wait for user confirmation. |
| **Demo data** | **Owed in slice 01/02**: Team Gravity (and one more) gets refinement states `Backlog`/`Analysing`/`Next` (S8) and, in 04, a Thursday cadence; one demo Team stays unconfigured for the disabled-tab E2E. **Slice 11/13**: seed a handful of votes with self-declared names (Jonas Weber, Ana Lima, Mo Okafor — three, so the default of 3 Yes can be met) so the screenshots and the E2E walking skeleton show readiness. No CSV state changes needed. |
| **Terminology** | **New term "Refinement"** (singular/plural, seeded default "Refinement"), D26. Every string uses Work Item, Team, SLE, Throughput, cycle time, WIP, Refinement via `getTerm`; never "Story", "Epic", "Sprint". Renaming is Premium automatically (Terminology editing is Premium). |
| **Usage-data event** | **Wanted (D18)**: tab opening = existing `TeamTabOpened` + new route key; DEVOPS designs the rest against K1, K3–K7 — at least "refinement states saved", "need shown" `{below,in,above,none}`, "vote cast" `{refinementDay,otherDay}` (+ `{liveSession,async}` from E4), "readiness reached" `{before…, on…}`. Each emitted in the DELIVER slice that first makes it usable and listed in `docs/settings/usagedata.md`. |
| **EF migrations** | **Owed**: 01 (refinement states), 11 (vote log — new table), 13 (readiness), 03 (stages, extend 01's shape), 04 (cadence), 07 (band), 08 (rules) — in delivery order (DD-22). All additive, via `CreateMigration`, expand-only. DESIGN may batch settings columns into fewer migrations. |
| **Premium gating** | None built (D25). Named votes = Premium through auth; calibration = E5. |
| **ADO** | Proposed split above; maintainer applies it. One Story per slice once confirmed. |

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem clear, domain language | PASS | US-01–05, 08, 11, 18 state the problem through Priya, Sofia or Jonas; for the half-day stories the Elevator Pitch's *Before* line carries it. No "implement X" titles. |
| 2 | Persona specific | PASS | `flow-coach`, `config-admin`, `product-owner`, **new** `team-member-voter` with Team Gravity context. |
| 3 | 3+ domain examples, real data | PASS (US-01–05, 11) / PARTIAL elsewhere | Demo Team Gravity Work Items (GR-051/058/073), dates from Fri 2 Oct 2026. Smaller stories (06–09, 12–17a) carry 2–3 scenarios as examples — acceptable for ½-day slices, flagged. |
| 4 | UAT G/W/T, 3–7 | PASS / PARTIAL | US-01–05, 10, 11, 13, 18: 3 each; US-17b: 4. US-06, 07, 08, 09, 12, 15, 16, 17a: 2 each (½-day slices); US-14: 1. Below the 3 minimum — **flagged**: DISTILL to add the error path per story. |
| 5 | AC from UAT | PASS | AC-n.m per story trace to scenarios. |
| 6 | Right-sized | PASS with one exception | All ≤1 day except slice 11 (1½d), called out in Scope Assessment. Slice 17 split into 17a (½d, read) and 17b (1d, cast/take back) to stay within the bar after DD-19. |
| 7 | Technical notes | PASS | Brownfield inventory S1–S9; HowMany semantics; read-permission write; storage shape. |
| 8 | Dependencies tracked | PASS | Epic order E1 → E3 first, E2 alongside → E4 (DD-22); 13 decoupled from 05; 17b's take-back depends on 16, its comments on 12; spike 19 for X4/R5. |
| 9 | Outcome KPIs | PASS | K1–K7 with targets, baselines, measurement; guardrails. |

**DoR status: PASSED with two flagged gaps** (thin UAT on ½-day stories, slice 11 size). Peer review not run
(coordinator's call).

---

## Wave: DISCUSS / [REF] Maintainer Questions

All five answered by the maintainer on 2026-10-02.

1. ~~**Q1 — "Can't tell yet"**~~ — **Resolved (DD-17)**: no fourth answer; DD-11 as proposed.
2. ~~**Q2 — Epic split**~~ — **Resolved (DD-18)**: E1 own Epic, #5881 → E2, #5510 → E3, E4 and E5 new.
3. ~~**Q3 — Voting from CLI/MCP**~~ — **Resolved (DD-19)**: clients may vote and take back; slice 17 split into 17a/17b.
4. ~~**Q4 — Presenter mode with auth on**~~ — **Resolved (DD-20)**: no on-behalf votes; the facilitator writes a comment.
5. ~~**Q5 — Readiness defaults**~~ — **Resolved (DD-21)**: 3 Yes, no veto (min voters 3).

Also applied: voting first after the walking skeleton (DD-22). Nothing open for the maintainer; DD-19 (a)–(d) are
DESIGN's to decide.

---

## Wave: DISCUSS / [REF] Handoff

To DESIGN (`nw-solution-architect`): this section + `slices/` + `discuss/wave-decisions.md` + journey SSOT. Open for
DESIGN: storage shape for refinement settings (one owner, expand-only), the vote log aggregate and its identity field
for auth-off, the read-permission write, `HowManyForecast` high-end mapping, where backlog order comes from for the
list (same comparer as forecasts), and client-vote identity (DD-19 a–d: credential → person or refuse; required
auth-off name on the client; marking client-cast votes; MCP tool wording). DEVOPS: K1–K7 events. R4 is measured by
K4; no push channel (D21).

---

## Wave: DISCUSS / [REF] ADO mapping (2026-10-02)

| Epic | ADO | Slices → User Stories |
|---|---|---|
| E1 Refinement tab: see the Work Items in refinement | #6136 | 01 #6139 · 02 #6140 |
| E2 Refinement need: refine enough, then stop | #5881 (retitled) | 03 #6141 · 04 #6142 · 05 #6143 · 06 #6144 · 07 #6145 · 08 #6146 · 09 #6147 |
| E3 Sizing votes against the SLE | #5510 (retitled) | 10 #6148 · 11 #6149 · 12 #6150 · 13 #6151 · 14 #6152 · 15 #6153 · 16 #6154 · 17a #6155 · 17b #6156 |
| E4 Live Refinement sessions | #6137 | 18 #6157 · 19 #6158 |
| E5 Sizing calibration (Premium) | #6138 | not sliced |

Predecessor links: #6136 → #5881, #6136 → #5510, #5510 → #6137, #5510 → #6138. All Epics New in Options;
all User Stories New.

---

## Wave: DESIGN / [REF] Prior-Wave Reading Confirmation

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-02 · **Mode**: PROPOSE (autonomous subagent) ·
**Scope** (maintainer, 2026-10-02): FULL for E1 (#6136, 01–02), E3 (#5510, 10–17b), E2 (#5881, 03–09); LIGHT for E4
(#6137, slice 18 only; spike 19 = questions only); E5 (#6138) out, but not precluded.

| Read | Status |
|---|---|
| This file: DISCOVER D1–D31, DIVERGE (D20/D21 overrides win), DISCUSS DD-1..DD-22, US-01..US-18, S-19, K1–K7, checklist, ADO mapping | ✓ (paged, all 1118 lines) |
| `discuss/wave-decisions.md`, `wave-decisions.md` (DIVERGE), all 20 `slices/slice-*.md` | ✓ |
| `recommendation.md` — maintainer-overrides block only (body's push trigger and "leaving refinement" count are withdrawn) | ✓ |
| `docs/product/architecture/brief.md` (map + tail sections), `c4-diagrams.md` (map + tail), ADR index | ✓ — highest ADR in this worktree is **212**; the main checkout holds an uncommitted **213** (Story 6131), so this wave numbers from **214** |
| ADRs read for this design: 027 (bus), 056 (state-list setting placement), 064 (JSON-valued settings), 060 (weekday + every-N-weeks), 136/165 (authorship, absent profile), 137 (embed viewer identity), 190/191 (browser-detected usage data, per-browser pseudonym), 210 (HowMany is descending) | ✓ |
| `docs/product/journeys/epic-5510-5881-refinement.yaml`, `docs/product/jobs.yaml` (both refinement jobs), `docs/product/personas/team-member-voter.yaml` | ✓ |
| `CLAUDE.md`, `docs/ci-learnings.md` (preflight rules: S107, S6964, CA1859, CA1869, NUnit2045, S1192) | ✓ |
| `ARCHITECTURE.md` §4–§7 (modules, ArchUnitNET rules, bus, CQRS-lite, concurrency) | ✓ |
| Code (targeted, cited below): `WorkTrackingSystemOptionsOwner`, `Team`, `LighthouseAppContext`, `TeamController`, `RbacGuardAttribute`, `RbacGuardRequirement`, `RbacAdministrationService`, `UserRole`, `ApiKey*`, `CurrentUserProfileService`, `DisabledAuthenticationHandler`, `AuthModeResolver`, `ForecastService`, `ForecastBase`, `HowManyForecast`, `ForecastController`, `FeatureComparer`, `WorkItemBase`, `RecurringBlackoutRule(+Extensions)`, `IRuleEvaluator`, `TerminologySeeder`, `UsageDataEventName/Shapes`, `IDomainEventDispatcher`, `useRbac.ts`, `TeamDetail.tsx`, `lighthouse-clients` CLI + MCP tool list | ✓ |

---

## Wave: DESIGN / [REF] Architecture summary

**Style unchanged**: modular monolith, ports-and-adapters, OOP (ADR-027). This feature adds **one new module**,
`Refinement` (namespace `Services.*.Refinement`, the eighth beside the seven in `ARCHITECTURE.md` §4), that depends
**down** on Forecasting, Metrics, WorkItems/Rules, RBAC/Identity and Platform, and that **nothing but `API` depends on**.
No new container, no new external system, no push channel, no SignalR (D21; presenter mode reads the same GET).

Three shapes carry the whole design:

1. **Settings are one JSON-valued property on the Team** (`Team.RefinementSettings`), ridden on the existing Team
   settings write (TeamWrite, autosave, concurrency token, 409). One additive column in slice 01; every later
   settings slice (03, 04, 07, 08, 13) extends the JSON shape with defaulted members — **no further migration**
   ([ADR-214](../../product/architecture/adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md)).
2. **Everything on the tab is derived on read, nothing is stored but settings and the log.** List, stage, readiness,
   the SLE yardstick, the next Refinement date and the need band are computed per request from the stored Work Items,
   the settings and the sizing log (the "derived on read and never stored" pattern of `ARCHITECTURE.md` §6). The need
   band **is** the manual forecast's How Many for target date = next Refinement, read at (100 − p)
   ([ADR-215](../../product/architecture/adr-215-the-need-band-is-the-manual-how-many-for-the-next-refinement-read-at-100-minus-p.md)).
3. **The sizing log is append-only and keyed by a voter key, never by a row** — account subject with auth on,
   a per-browser (or per-client) random key with auth off, and a refusal when a credential belongs to no person
   ([ADR-216](../../product/architecture/adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md)). Writing to
   it needs only Team read, through a **named** guard requirement `TeamContribute`
   ([ADR-217](../../product/architecture/adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md)).
   Stage, readiness and the hidden split are one pure resolution
   ([ADR-218](../../product/architecture/adr-218-stage-readiness-and-the-hidden-split-are-one-pure-resolution-on-read.md)).

---

## Wave: DESIGN / [REF] Decisions (DSN-n)

Numbered DSN to avoid clashing with DISCUSS DD-n. Options were weighed only where a real choice existed.

| # | Decision | Options weighed → verdict | ADR |
|---|---|---|---|
| DSN-1 | **Refinement is a new module** `Services.*.Refinement` + `API/Refinement*Controller`; depends down only; nothing outside `API` depends on it. | (a) spread over Forecasting/WorkItems/Metrics; (b) **new module** ✓ — the feature has its own vocabulary, one read composition and one log; spreading it would put a vote log in a forecasting namespace. | 214 |
| DSN-2 | **Settings = one value object `RefinementSettings` on `Team`** (not on `WorkTrackingSystemOptionsOwner` — Team level only, D2), persisted as one JSON text column through a `ValueConverter` + `ValueComparer`, exactly like `StateMappings`/`CycleTimeDefinitions` (`LighthouseAppContext.cs:583-629`). Null = "not configured". | (a) a column per setting — 6 migrations across 01/03/04/07/08/13; (b) a 1:1 `TeamRefinementSettings` table — a join on every Team load and a second concurrency root; (c) **one JSON column** ✓ — one migration, later slices add defaulted members, rides the Team's tokened write. | 214 |
| DSN-3 | **Settings ride the existing `PUT /teams/{teamId}`** (`TeamController.UpdateTeam`, TeamWrite, `TeamSettingDto` gains a nullable `Refinement` sub-DTO). **Null on the wire = leave unchanged**, never "clear". `WorkItemRelatedSettingsChanged` must **not** look at refinement members (it deletes the Team's Work Items when true, `TeamController.cs:181-185`). | (a) **existing settings write** ✓ — autosave (ADR-029), 409 conflict, TeamWrite guard all inherited; (b) a separate `PUT …/refinement/settings` — second token, second form save path, nothing gained. | 214 |
| DSN-4 | **Refinement states store the entries the admin picks from `ToDoStates ∪ DoingStates`** (mapped names or raw states, as `WaitStates` does, ADR-056) each with a `Stage` (default `Waiting`). Resolution to raw states through the existing `GetRawStatesForCategory`. An entry no longer in To Do ∪ Doing is **kept and flagged** (AC-1.4), never silently dropped. | — (precedent) | 214 |
| DSN-5 | **Tab enablement fact** `refinementConfigured` (≥1 refinement state stored) is an additive field on `TeamDto`, read by `TeamDetail.tsx` like `team.features.length` (S1). | (a) a second fetch on page load; (b) **additive DTO field** ✓. | — |
| DSN-6 | **Backlog order** = `WorkItem.Order` (the tracker's rank) compared by the existing `FeatureComparer.CompareOrderValues` ladder, tiebreak `ReferenceId` ordinal. **Correction to AC-2.1**: Team forecasts do not order Work Items; Feature manual ranking (ADR-132/187) does not apply to Work Items. See Changed Assumptions. | (a) a new comparer; (b) **the one existing order ladder** ✓. | 215 |
| DSN-7 | **Next Refinement date** (DD-6): pure `RefinementCadence.NextAfter(today)`; weekdays + `IntervalWeeks` + anchor week, **using the same week-modulo rule as recurring blackouts**, extracted from `RecurringBlackoutRuleExtensions.Matches` into a shared pure `WeeklyRecurrence` (behaviour-preserving refactor commit; blackout tests pin it). `today` from `ILighthouseClock` (instance zone). Not blackout-shifted (US-04 note). | (a) a second copy of the modulo rule; (b) **extract and share** ✓. | 215 |
| DSN-8 | **Need band** = one `IForecastService.HowMany` over `ITeamMetricsService.GetForecastThroughputStatus(team, RespectTeamSetting).Throughput` for the blackout-aware working days from today to the next Refinement — byte-for-byte the manual forecast's How Many path (`ForecastController.cs:104-139`). Value at user percentile *p* = `GetProbability(100 − p)` because `HowManyForecast` sorts descending (ADR-210): low = `GetProbability(50)`, high (85) = `GetProbability(15)`. Minimum-data guard = `status.HasSufficientData` (AC-5.6). | (a) a new simulation over "Work Items leaving refinement" — withdrawn (D20); (b) **manual-forecast How Many, unchanged** ✓. | 215 |
| DSN-9 | **Verdict** `Below` (< low) / `In` (low..high inclusive) / `Above` (> high), or **no verdict** with a closed reason `NoCadence` / `InsufficientData` / `NoRefinementStates`. The wire carries facts only: `readyCount`, `low`, `high`, `lowPercentile`, `highPercentile`, `nextRefinementDate`, `horizonWorkingDays`, `isRefinementDay`, `verdict?`, `unavailableReason?`, `lineAfterPosition` (= min(high, listed count)) and `fewerListedThanHigh`. Clients compose every sentence (DD-13 still holds for wording). | — | 215 |
| DSN-10 | **SLE yardstick**: `Sle` (range + probability) when both SLE fields > 0; else `CycleTimeFallback` = P85 of `GetCycleTimePercentilesForTeam` over the **Team's Throughput history window** (`Team.GetThroughputSettings(today)`); else `Unavailable`. **Correction to AC-10.1**: there is no server-side "metrics window" per Team (the Metrics tab window is a browser choice); the Throughput window is the one per-Team history window the server owns, and it is the window the need number already samples. | (a) a hard-coded 30/90 days; (b) **the Team's own Throughput window** ✓. | 215 |
| DSN-11 | **Sizing log** = new append-only table `SizingLogEntries` (entity `SizingLogEntry`): `Id`, `TeamId` (FK, cascade), `WorkItemReferenceId` (string — **not** a FK to the Work Item row, which a refresh can delete and re-create), `Kind` {Vote, Comment, Revocation}, `Answer?` {Yes, YesBut, No}, `Comment?` (≤ 2,000), `VoterKey`, `VoterProfileId?` (FK, SET NULL), `VoterDisplayName` (captured at write time, ADR-165), `RecordedAt` (UTC instant), `Channel` {Web, LiveSession, Cli, Assistant}, `YardstickDays?`, `YardstickSource` {Sle, CycleTimeFallback, Unavailable}, `YardstickProbability?`. Index `(TeamId, WorkItemReferenceId, Id)`. **All columns in slice 11's one migration**, including those first used by 12/16/17b/18. | (a) a mutable "current vote" row per voter + history table — read-modify-write and a CAS to get right; (b) **append-only log, current = latest per voter** ✓ — no in-place update exists, so no lost update and no CAS (DD-9). | 216 |
| DSN-12 | **Voter identity** resolved by one `VoterIdentityResolver` from `IAuthModeResolver.Resolve().Mode` — **never** from "profile is null" (auth off still carries the shared subject `lighthouse|auth-disabled`, `DisabledAuthenticationHandler.cs:14-22`). Auth **Enabled** → the person from `ICurrentUserProfileService` (`VoterKey = "account:" + subject`); no person (an API key whose owner is unlinked, `ApiKeyService.cs:173-185`) → **refused**, problem code `vote-needs-a-person`. Auth **Disabled** → required self-declared name (trimmed, 1–100 chars) + a client-held random **voter key** sent in header `X-Lighthouse-Voter-Key` (≥ 32 chars); stored only as `"self:" + SHA-256(key)`; missing either → **refused**, `voter-name-required` / `voter-key-required`. A voter key is never returned by any endpoint; responses carry `isMine`. | (a) name as identity — two people with one name collide and anyone typing "Ana Lima" owns Ana's votes; (b) server-minted key round-trip — an extra endpoint for no gain, the key protects nothing the auth-off instance doesn't already grant (D11); (c) **client-minted key, hashed at rest** ✓. | 216 |
| DSN-13 | **The vote write is gated by a new, named `RbacGuardRequirement.TeamContribute`** whose predicate **is** `CanReadTeamAsync` today (one call, no second rule), refusing with **404** like a read (non-disclosing) rather than 403. Team admins edit settings via the unchanged `TeamWrite`. UI: `useRbac().canContributeToTeam(teamId)`. Plus a rate-limit policy `RefinementContribution` on the write endpoints and bounded text. | (a) reuse `TeamRead` on a POST — works, but makes "this persists" invisible and couples a future restriction to reads; (b) `TeamWrite` — contradicts C4/AC-11.4; (c) a new role — no role differs from Viewer; (d) **named requirement mapped to the read predicate** ✓. | 217 |
| DSN-14 | **Stage, readiness, open question and the hidden split are one pure resolution** (`RefinementResolution`, static, no I/O) over facts: the row's state stage, the matching stage rule (if any), the voters' current entries and the readiness setting. Precedence DD-5: a matching **stage rule decides** the stage and the row's readiness (votes cannot lift or sink it); otherwise the **state's stage**; a row not Ready by its stage is **Ready by votes** when `yes + yesBut ≥ MinYes` and `voters ≥ MinVoters` and no veto trips; a veto (≥ Threshold of `No` or of `No ∪ YesBut`) → `NeedsDiscussion`. If several stage rules match: Ready > Being refined > Waiting. | — | 218 |
| DSN-15 | **Current vote** = latest `Vote`/`Revocation` entry per `VoterKey` per Work Item (ordered by `Id`); `Comment` entries never change it. **Open question** = some voter's latest entry is a `Comment` and that voter has no current vote (the asker clears it by voting). Changing your mind appends; taking back appends a `Revocation` and is an **idempotent no-op** (nothing written) when there is no current vote. | — | 216, 218 |
| DSN-16 | **Hidden split is enforced in the API**: for a caller without a current vote on a row, `split`, `myVote`'s neighbours and every comment are **omitted**; only `voteCount` and the readiness status remain (AC-14.3). The per-item log endpoint answers `{ hidden: true, voteCount }`. Presenter mode reveals splits **only to a caller who passes `TeamWrite`** (with auth off or RBAC off that is everyone). Readers can still present; they see splits where they have voted. | (a) a query flag anyone may set — makes AC-14's API guarantee false; (b) **reveal tied to TeamWrite** ✓ (maintainer to confirm, MQ-2). | 218 |
| DSN-17 | **Channel is declared by the caller and validated as a closed enum**, never inferred: web UI sends `Web`, presenter mode sends `LiveSession`, CLI sends `Cli`, MCP sends `Assistant`. It is analytics and display, not security, and is documented as declared. It is the fact DEVOPS needs for K4/K7 and for DD-19 (c). | (a) infer from auth scheme — cannot tell a browser on auth off from the CLI on auth off; (b) **declared closed enum** ✓. | 216 |
| DSN-18 | **No domain event is published in E1–E3.** Nothing reacts to a vote or a settings change server-side: readiness is derived on read, usage-data events are detected in the browser (ADR-190) from facts the responses already carry. The seam for E4/E5 is named: `SizingEntryRecorded` published after commit by the vote service, if and when a subscriber exists (spike 19's fan-out, E5's calibration cache). | (a) publish now with no subscriber — dead code; (b) **name the seam, don't build it** ✓ (ADR-027 bus still the default when a reaction appears). | 216 |
| DSN-19 | **Read/write driving ports split**: `IRefinementViewQuery` (read, no repository write member reachable) and `ISizingLogCommands` (append only). Two controllers: `RefinementController` (GETs) and `RefinementVotesController` (writes) — keeps both under S107 and keeps the read path structurally write-free. | — | 216, 217 |
| DSN-20 | **"Refinement"/"Refinements" are Terminology keys** seeded by `TerminologySeeder.AddOrUpdateTerminology` (add-or-update, no migration) and mirrored in `TerminologyKeys.ts`; introduced in slice 01 so the tab label never hard-codes the word. | — | — |
| DSN-21 | **Clients (DD-19)**: `lh refinement get --team-id <id>`, `lh refinement vote --team-id <id> --work-item <ref> --answer yes|yes-but|no [--comment <text>] [--as <name>]`, `lh refinement comment …`, `lh refinement take-back …`, `lh config voter set --name <name>`; MCP `lighthouse_team_refinement_get`, `lighthouse_team_refinement_vote`, `lighthouse_team_refinement_comment`, `lighthouse_team_refinement_voteTakeBack` (house naming, `mcp-core/src/index.ts:718-1219`). Verdicts (a)–(d) below. | — | 216 |
| DSN-22 | **Every vote entry captures the yardstick it was cast against** (days, source, probability) so E5 can compare "Yes" with the Work Item's eventual cycle time without re-deriving a yardstick that has since moved. Nothing else is built for E5. | — | 216 |

---

## Wave: DESIGN / [REF] DD-19 verdicts (client votes)

| | Verdict |
|---|---|
| **(a) Credential → person** | **Resolve or refuse.** Auth on: a cookie session or JWT bearer carries the person's `sub`; an API key resolves to its owner's profile when `OwnerResolutionState == Resolved` (`ApiKeyService.cs:148-201`, `ApiKeyPrincipalFactory.cs:51-82`). An **unlinked** key yields a principal with no `sub`, `ICurrentUserProfileService` returns `null`, and the vote is refused with `vote-needs-a-person` (HTTP 403 — reachable only where the key can read the Team, i.e. RBAC off; with RBAC on the guard already answers 404 because `CanReadTeamAsync` needs a profile, `RbacAdministrationService.cs:215-219`). Lighthouse API keys are always created by a person, so "instance/service keys" exist only as unlinked keys; there is no other credential kind to special-case. |
| **(b) Auth-off name on the client** | **Required, never defaulted.** CLI: `--as "<name>"`, else the value the user stored explicitly with `lh config voter set --name`; neither → the CLI refuses before calling the server. It never derives a name from the OS user, git config or hostname. MCP: a `voterName` tool argument; the tool description tells the assistant to **ask the user for their name and never infer it**. The server refuses a nameless auth-off vote anyway (`voter-name-required`), so a client bug cannot default it. Voter key: CLI and MCP-stdio mint one random key per Lighthouse URL on first vote and keep it in the client package's local config store (the client's "browser"); take-back works from the same client only. **mcp-http** (a shared hosted bridge with no per-person store) refuses votes on an auth-off instance (MQ-3). |
| **(c) Marked in the log** | **Yes**, as `Channel = Cli` or `Assistant` (DSN-17), shown in the log view ("via the CLI" / "via an assistant", wording through the client) and available to DEVOPS as a closed-enum property. |
| **(d) MCP wording** | The vote and comment tools' descriptions state: *"Records the USER's own sizing judgement under their name. Never call this on your own initiative or on someone else's behalf: show the user the Work Item, the answer and any comment you intend to send, and call only after they explicitly confirm."* The confirmation is the assistant's; the server has no way to verify it, which is why (a) refuses non-persons and (c) marks the channel. |

---

## Wave: DESIGN / [REF] Component decomposition

Paths relative to `Lighthouse.Backend/Lighthouse.Backend/`, `Lighthouse.Frontend/src/`, `lighthouse-clients/packages/`.

| Component | Path | Change | Slice |
|---|---|---|---|
| `RefinementSettings` (+ `RefinementStateSetting`, `RefinementCadence`, `RefinementBand`, `ReadinessSetting`, `VetoSetting`, `StageRules`) | `Models/Refinement/` | **NEW** value objects, defaults in initialisers | 01 (states), 03, 04, 07, 08, 13 extend |
| Enums `RefinementStage`, `SizingAnswer`, `SizingEntryKind`, `SizingChannel`, `YardstickSource`, `RefinementVerdict`, `NeedUnavailableReason`, `RowReadiness`, `ReadySource` | `Models/Refinement/` | **NEW** (append-only ordinals) | as used |
| `Team.RefinementSettings` | `Models/Team.cs` | **EXTEND** (nullable property) | 01 |
| `SizingLogEntry` | `Models/Refinement/SizingLogEntry.cs` | **NEW** entity, constructor-set, no setters on identity fields | 11 |
| `LighthouseAppContext` | `Data/` | **EXTEND**: JSON converter/comparer for `RefinementSettings`; `DbSet<SizingLogEntry>`, index, FKs | 01, 11 |
| `WeeklyRecurrence` (pure) | `Services/Implementation/` | **EXTRACT** from `RecurringBlackoutRuleExtensions.Matches` (refactor commit) | 04 |
| `RefinementCadenceCalendar` (pure: next date, is-refinement-day) | `Services/Implementation/Refinement/` | **NEW** | 04 |
| `RefinementList` (query: Work Items in refinement states, backlog order) | `Services/Implementation/Refinement/` | **NEW** over `IWorkItemRepository` | 02 |
| `RefinementResolution` (pure: stage, readiness, open question, split visibility) | `Services/Implementation/Refinement/` | **NEW** static | 13 (votes), 03 (stages), 08 (rules), 12, 14 |
| `StageRuleMatcher` | `Services/Implementation/Refinement/` | **NEW** thin adapter over existing `IRuleEvaluator<WorkItem>` + `WorkItemFieldProvider` | 08 |
| `RefinementNeedCalculator` | `Services/Implementation/Refinement/` | **NEW** over `IForecastService.HowMany`, `ITeamMetricsService.GetForecastThroughputStatus`, `IBlackoutPeriodService` | 05 |
| `NeedBand` (pure: value at p = `GetProbability(100 − p)`, verdict) | `Services/Implementation/Refinement/` | **NEW** static | 05 |
| `SleYardstickResolver` | `Services/Implementation/Refinement/` | **NEW** over `ITeamMetricsService.GetCycleTimePercentilesForTeam` | 10 |
| `VoterIdentityResolver` | `Services/Implementation/Refinement/` | **NEW** over `IAuthModeResolver` (reads and writes) and `ICurrentUserProfileService` (**writes only**, E6) | 11 (auth off), 15 (auth on), 17b |
| `IRefinementViewQuery` / `RefinementViewQuery` (driving, read) | `Services/{Interfaces,Implementation}/Refinement/` | **NEW** — composes the view | 02 → every slice extends |
| `ISizingLogCommands` / `SizingLogCommands` (driving, write) | same | **NEW** — vote, comment, take back | 11, 12, 16 |
| `ISizingLogRepository` / `SizingLogRepository` (driven) | `Services/{Interfaces,Implementation}/Repositories/` | **NEW** — `Append`, `ReadForTeam(teamId, refs)`; **no update/remove members** | 11 |
| `RbacGuardRequirement.TeamContribute` | `Models/Authorization/` | **EXTEND** (appended member) | 11 |
| `RbacAdministrationService.CanSatisfyRequirementAsync` | `Services/Implementation/Authorization/` | **EXTEND** — `TeamContribute => CanReadTeamAsync` | 11 |
| `RbacGuardAttribute` | same | **EXTEND** — add `TeamContribute` to both `RequiresScope()` and `IsReadRequirement()` (`RbacGuardAttribute.cs:79-91`), so a failed check answers `NotFoundResult` (404), not `ForbidResult` | 11 |
| `RateLimitingConfiguration.RefinementContributionPolicy` | composition root | **EXTEND** — partition by subject, else voter-key hash, else IP | 11 |
| `TeamSettingDto.Refinement` (+ `RefinementSettingsDto`) | `API/DTO/` | **EXTEND** (nullable; null = unchanged; value-type members nullable, S6964) | 01 |
| `RefinementSettingsValidator` (pure) | `API/Helpers/` | **NEW** — states ⊆ To Do ∪ Doing at save, stage enum, cadence N ≥ 1 + anchor when N > 1, 1 ≤ low < high ≤ 99, MinYes ≥ 1, MinVoters ≥ MinYes, veto ≥ 1, stage rules via existing `RuleSetValidation` | 01 → 13 |
| `TeamExtensions.SyncRefinement` | `API/Helpers/` | **EXTEND** | 01 |
| `TeamController.UpdateTeam` | `API/` | **EXTEND** (one validator call; ctor unchanged — 11 params already, no new dependency) | 01 |
| `TeamDto.RefinementConfigured` | `API/DTO/` | **EXTEND** | 01 |
| `RefinementController` (`GET …/refinement`, `GET …/work-items/{workItemId}/log`) | `API/` | **NEW** | 02, 12 |
| `RefinementVotesController` (`POST …/votes`, `POST …/comments`, `DELETE …/votes/mine`) | `API/` | **NEW** | 11, 12, 16 |
| `TerminologySeeder` | `Services/Implementation/Seeding/` | **EXTEND** (`refinement`, `refinements`) | 01 |
| `DemoDataFactory` (+ demo vote seeding) | demo seeding | **EXTEND** — Gravity states/stages/cadence; three named voters' entries | 01, 03, 04, 11, 13 |
| ArchUnitNET rules E1–E9 | `Lighthouse.Backend.Tests/Architecture/` | **EXTEND** | each slice that introduces the rule |
| `TeamDetail.tsx` | `pages/Teams/Detail/` | **EXTEND** — tab between Metrics and Settings, role-specific disabled tooltip | 01 |
| `RefinementView` (+ `RefinementHeader`, `SleQuestion`, `NeedVerdictBanner`, `RefinementList`, `RefinementRow`, `VoteControl`, `ReadinessCell`, `EnoughForLine`) | `pages/Teams/Detail/Refinement/` | **NEW** | 02, 10, 11, 13, 05, 06 |
| `SizingLogDialog` | same | **NEW** | 12 |
| `VoterNamePrompt` + `useVoterIdentity` (localStorage `lighthouse:refinement:voter` = `{ name, key }`, key from `crypto.randomUUID()` ×2) | `pages/…/Refinement/`, `hooks/` | **NEW** | 11 |
| `PresenterMode` | `pages/Teams/Detail/Refinement/` | **NEW** | 18 |
| `RefinementSettingsSection` (states with category labels, stage select, cadence, band, readiness, stage rules via existing `DeliveryRuleBuilder`) | `components/Common/Team/` | **NEW** section inside `ModifyTeamSettings` | 01 → 13 |
| `useRbac` | `hooks/useRbac.ts` | **EXTEND** — `canContributeToTeam(teamId)` | 11 |
| `RefinementService` + `models/Refinement/*.ts` (wire enums as **string unions**) | `services/Api/`, `models/` | **NEW** | 02 → |
| `TerminologyKeys.ts` | `models/` | **EXTEND** | 01 |
| Usage-data route key for the tab | frontend usage-data detection | **EXTEND** (`TeamTabOpened` + `refinement`, DD-16) | 02 |
| `@letpeoplework/lighthouse-client` (`getTeamRefinement`, `castSizingVote`, `addSizingComment`, `takeBackSizingVote`, voter store) | `client/src/` | **EXTEND** | 09, 17a, 17b |
| CLI group `refinement`, `config voter` | `cli/src/` | **EXTEND** | 09, 17a, 17b |
| MCP tools `lighthouse_team_refinement_*` | `mcp-core/src/` | **EXTEND** | 09, 17a, 17b |

---

## Wave: DESIGN / [REF] Driving ports

**HTTP** (all under `api/v1/teams/{teamId:int}/…` and `api/latest/teams/{teamId:int}/…`):

| Verb + path | Guard | Purpose | Slice |
|---|---|---|---|
| `PUT /teams/{teamId}` (existing) | `TeamWrite` | carries `refinement` settings | 01 → 13 |
| `GET /teams/{teamId}` (existing) | `TeamRead` | adds `refinementConfigured` | 01 |
| `GET /teams/{teamId}/settings` (existing) | `TeamRead` | adds `refinement` (for the settings form) | 01 |
| `GET /teams/{teamId}/refinement[?presenting=true]` | `TeamRead` | the whole tab: list, stages, readiness, tallies, yardstick, cadence facts, need | 02 → 18 |
| `GET /teams/{teamId}/refinement/work-items/{workItemId}/log` | `TeamRead` | the log for one listed Work Item, oldest first; `{hidden:true, voteCount}` until the caller votes (14) | 12 |
| `POST /teams/{teamId}/refinement/work-items/{workItemId}/votes` | `TeamContribute` + rate limit | `{ answer, comment?, channel, voterName? }`; returns the updated row | 11, 12, 17b, 18 |
| `POST /teams/{teamId}/refinement/work-items/{workItemId}/comments` | `TeamContribute` + rate limit | `{ comment, channel, voterName? }` | 12, 17b |
| `DELETE /teams/{teamId}/refinement/work-items/{workItemId}/votes/mine` | `TeamContribute` + rate limit | appends a `Revocation`; no-op if no current vote | 16, 17b |

Auth-off identity travels as header `X-Lighthouse-Voter-Key` on every refinement GET and write (it decides `isMine` and
the hidden split) plus `voterName` in write bodies. Auth on: both are ignored. A vote on a Work Item that is not
currently listed → **409** `work-item-not-in-refinement` (DD-1 limits votes to Work Items in refinement states).

**UI routes**: Team → `refinement` tab (`/teams/:id/refinement`); Settings → *Refinement* section; Present (full screen,
same route, state in the URL query so a reload keeps the position).

**CLI / MCP**: DSN-21.

## Wave: DESIGN / [REF] Driven ports and adapters

| Port | Adapter | New? |
|---|---|---|
| `ISizingLogRepository` (append, read by team + refs) | EF Core over `SizingLogEntries` (SQLite + Postgres) | **NEW** |
| `IRepository<Team>`, `IWorkItemRepository` | existing EF | reused |
| `IForecastService.HowMany` | existing Monte Carlo | reused, unchanged (D20) |
| `ITeamMetricsService.GetForecastThroughputStatus`, `GetCycleTimePercentilesForTeam` | existing (cached) | reused |
| `IBlackoutPeriodService` (working-day horizon) | existing | reused |
| `IRuleEvaluator<WorkItem>` + `WorkItemFieldProvider` | existing rule engine (ADR-012/013) | reused |
| `IAuthModeResolver`, `ICurrentUserProfileService` | existing | reused |
| `IRbacAdministrationService` | existing | extended (one requirement) |
| `ILighthouseClock` | existing | reused |

**No external integration is added** — no tracker is contacted (votes stay in Lighthouse, D9), so **contract testing (Pact):
N/A**. The Lighthouse-Clients ↔ Lighthouse API is an internal cross-repo contract; its additive shape is pinned by the
client's existing fixture tests, not by Pact.

**Earned Trust**: no new substrate dependency. The one adapter added is an EF table on the existing providers, already
probed by the startup migration path (ADR-077). The probe this feature does owe is on **its own invariant**: E2 below
proves at test time that nothing can update or delete a log entry, instead of trusting that nobody will.

---

## Wave: DESIGN / [REF] Technology choices

**None new.** .NET 10 / EF Core (both providers) / ASP.NET Core rate limiting (already in use) / React 18 + MUI / the
Lighthouse-Clients TypeScript packages. All OSS already in the tree; no licence change. Rejected on purpose: SignalR
for presenter mode (a shared screen needs no push; DD-20), a scheduler or queue (nothing is precomputed), a second
Monte Carlo engine.

---

## Wave: DESIGN / [REF] Reuse analysis (HARD GATE)

| Existing component (evidence) | Overlap | Verdict |
|---|---|---|
| `WorkTrackingSystemOptionsOwner` / `Team` (`Models/WorkTrackingSystemOptionsOwner.cs:6-58`, `Models/Team.cs:3-50`) | where settings live | **EXTEND** `Team` (not the owner base: Team level only, D2) |
| JSON-valued settings (`LighthouseAppContext.cs:583-629`, ADR-064) | storage shape | **EXTEND the pattern** — one more converted property |
| `ToDoStates` / `DoingStates` / `GetRawStatesForCategory` (`WorkTrackingSystemOptionsOwner.cs:27-36, 90-110`; ADR-056 `WaitStates`) | refinement-state selection and resolution | **REUSE** as-is |
| Team settings write (`TeamController.cs:120-202`, `TeamExtensions.cs:78-105`), autosave (ADR-029), concurrency token (ADR-027) | settings save | **EXTEND** (one nullable DTO member, one validator call) |
| `WorkItemRelatedSettingsChanged` (`WorkTrackingSystemOptionsOwnerExtensions.cs:15`) | would wipe Work Items | **REUSE, guarded** — must ignore refinement members (E7) |
| `WorkItemRuleSet` / `IRuleEvaluator<WorkItem>` / `RuleSetValidation` (`Services/Interfaces/WorkItemRules/IRuleEvaluator.cs`, `API/Helpers/RuleSetValidation.cs`) and `DeliveryRuleBuilder` (the editor `FlowMetricsConfigurationComponent.tsx:32` uses for blocked rules) | stage rules (08) | **REUSE** engine, validation and editor (C7) |
| `ForecastService.HowMany` (`ForecastService.cs:33-53`), `HowManyForecast` descending comparer (`HowManyForecast.cs:7`), manual-forecast horizon (`ForecastController.cs:104-139`) | need number | **REUSE** unchanged (D20) |
| `ITeamMetricsService.GetForecastThroughputStatus` (`ITeamMetricsService.cs:19`) incl. `HasSufficientData` | throughput + minimum-data guard | **REUSE** (AC-5.6) |
| `ITeamMetricsService.GetCycleTimePercentilesForTeam` (`ITeamMetricsService.cs:63`) | SLE fallback P85 | **REUSE** |
| SLE fields (`WorkTrackingSystemOptionsOwner.cs:38-40`) | yardstick | **REUSE** |
| `RecurringBlackoutRule` weekday + `IntervalWeeks` + anchor (`RecurringBlackoutRuleExtensions.cs:26-37`, ADR-060) | cadence | **EXTRACT** `WeeklyRecurrence`, share |
| `FeatureComparer.CompareOrderValues` (`Models/FeatureComparer.cs:28`) | backlog order | **REUSE** |
| `WorkItemBase.WorkItemAge` (`WorkItemBase.cs:98-113`) | age column | **REUSE** (Doing rows only) |
| `TerminologySeeder` add-or-update (`TerminologySeeder.cs:23-60`), `TerminologyKeys.ts` | "Refinement" term | **EXTEND** |
| Disabled-tab pattern (`TeamDetail.tsx:506-507`), Settings/Access gating (`TeamDetail.tsx:122-125`) | tab | **REUSE** pattern |
| `UsageDataEventName.TeamTabOpened` (`UsageDataEventName.cs:12`, `UsageDataEventShapes.cs:30`) | tab-open event | **REUSE** + route key (DD-16) |
| `RbacGuardRequirement` / `RbacGuardAttribute` / `CanSatisfyRequirementAsync` (`RbacGuardRequirement.cs:3-14`, `RbacGuardAttribute.cs:44-77`, `RbacAdministrationService.cs:341-367`) | read-permission write | **EXTEND** (one member, mapped to `CanReadTeamAsync`) |
| `useRbac` (`hooks/useRbac.ts:29-89`) | UI gating | **EXTEND** (`canContributeToTeam`) |
| `ApiKeyService` owner resolution (`ApiKeyService.cs:148-201`), `ApiKeyPrincipalFactory` (`:51-82`), `CurrentUserProfileService` (`:14-56`) | client credential → person | **REUSE** — the `Unlinked` state *is* "belongs to no person" |
| `IAuthModeResolver` (`AuthModeResolver.cs:9-71`) | auth-on vs auth-off | **REUSE** — the only trustworthy mode probe |
| `DeliveryNote` authorship (ADR-165: FK SET NULL + name captured at write) | voter attribution | **REUSE the pattern** |
| ADR-191 per-browser identity (localStorage token, digest at rest) | auth-off voter | **REUSE the pattern** (not the consent table — different purpose, different retention) |
| ASP.NET rate limiting named policies (`ApiKeyController.cs:25`, `UsageDataController.cs:45`) | abuse bound on a reader write | **EXTEND** (one policy) |
| `IDomainEventDispatcher` (ADR-027) | reactions to votes/settings | **NOT USED YET** — no subscriber exists (DSN-18) |
| SignalR `UpdateNotificationHub` (ADR-075) | presenter mode | **NOT NEEDED** — confirmed: one screen, one browser, the same GET |
| Lighthouse-Clients command/tool patterns (`cli/src/index.ts:1114-1167`, `mcp-core/src/index.ts:718-1219`), `config output set` precedent | CLI/MCP | **EXTEND** |
| `DemoDataFactory` (S8) | demo settings + votes | **EXTEND** |

**CREATE NEW, each challenged:**

| CREATE NEW | Why no existing component serves |
|---|---|
| `SizingLogEntry` + `SizingLogEntries` table + `ISizingLogRepository` | Nothing stores per-person judgements about a Work Item. `DeliveryNote` is free text against a Delivery, mutable (edit/withdraw) and Portfolio-scoped — the opposite of an append-only, voter-keyed log (C10). |
| `RefinementSettings` value objects | No Team setting expresses stages, cadence, band or readiness. It is a new property on an existing aggregate, not a new aggregate. |
| `RefinementViewQuery` (read composition) | No read composes list + stage + readiness + need for a Team; extending `ForecastController` or `TeamMetricsController` would put a vote log behind a forecasting or metrics route. |
| `SizingLogCommands`, `VoterIdentityResolver` | No write path has an identity that is "account or self-declared key or refuse"; ADR-165's two branches cover account vs nothing, not a self-declared voter. |
| `RefinementResolution`, `NeedBand`, `RefinementCadenceCalendar`, `RefinementSettingsValidator` (pure) | New rules (DD-3/5/6/7, DD-21); each is a static function with no state to share. `ReadinessPolicy`-like logic exists nowhere. |
| `RefinementController`, `RefinementVotesController` | Adding to `TeamController` (already 11 ctor params, `TeamController.cs:27-38`) would trip S107 and mix a reader write into an admin controller. |
| Frontend `Refinement/*`, `RefinementSettingsSection`, `useVoterIdentity`, `RefinementService` | No tab, list, vote control or voter identity exists; the rule editor and disabled-tab pattern are reused inside them. |
| `TeamContribute` requirement | Reusing `TeamRead` on a POST would work and was rejected only because it hides that the action persists (ADR-217). It adds a name, not a rule. |

---

## Wave: DESIGN / [REF] Data model and migration plan

```
Teams                                    (existing table)
  + RefinementSettings  TEXT NULL        -- JSON; null = not configured            [slice 01 migration]

RefinementSettings (JSON shape, all members defaulted on read)
  States[]     { State, Stage = Waiting }                                          [01; Stage from 03]
  Cadence?     { Weekdays[], IntervalWeeks = 1, AnchorWeek? (Monday) }             [04]
  Band         { LowPercentile = 50, HighPercentile = 85 }                         [05 uses defaults; 07 edits]
  Readiness    { MinYes = 3, MinVoters = 3, Veto? { Threshold >= 1, Counts = No | NoOrYesBut } }   [13]
  StageRules   { Waiting?, BeingRefined?, Ready? }  -- each a WorkItemRuleSet       [08]

SizingLogEntries                          (new table)                               [slice 11 migration]
  Id                    PK identity
  TeamId                FK -> Teams ON DELETE CASCADE
  WorkItemReferenceId   TEXT NOT NULL
  Kind                  INT  (Vote=0, Comment=1, Revocation=2)
  Answer                INT  NULL (Yes=0, YesBut=1, No=2)          -- required iff Kind=Vote
  Comment               TEXT NULL (<= 2000)
  VoterKey              TEXT NOT NULL ("account:<sub>" | "self:<sha256 hex>")
  VoterProfileId        INT  NULL FK -> UserProfiles ON DELETE SET NULL
  VoterDisplayName      TEXT NOT NULL (captured at write time)
  RecordedAt            timestamp (UTC instant)
  Channel               INT  (Web=0, LiveSession=1, Cli=2, Assistant=3)
  YardstickDays         INT  NULL
  YardstickSource       INT  (Sle=0, CycleTimeFallback=1, Unavailable=2)
  YardstickProbability  INT  NULL
  INDEX (TeamId, WorkItemReferenceId, Id)
```

| Slice | Migration | Notes |
|---|---|---|
| 01 | **M1** `AddRefinementSettingsToTeams` — one nullable column | via `CreateMigration` (SQLite + Postgres); build migration DLLs first (memory: HintPath trap) |
| 02, 03, 04, 05, 06, 07, 08, 10, 13 | **none** | JSON members added with defaults; a missing member deserialises to its initialiser |
| 11 | **M2** `AddSizingLogEntries` — the whole table, every column | 12/14/15/16/17b/18 then need no migration |
| 12, 14, 15, 16, 17a, 17b, 18 | **none** | |

**Two migrations, both additive** (expand-only). JSON evolution rule for DELIVER: members are only ever **added**
with a default; renaming or removing one is a contract change that needs its own ADR. The JSON is written by one
converter with fixed options and read back by the same, so case sensitivity is not a hazard; any code that parses it
elsewhere uses `PropertyNameCaseInsensitive = true` from a cached `static readonly` options field (CA1869).

---

## Wave: DESIGN / [REF] RBAC model — the read-permission write

| Who | Read tab / log | Vote / comment / take back own | Edit refinement settings | Presenter reveals splits |
|---|---|---|---|---|
| Auth off (every Community instance) | yes | yes — self-declared name + browser key | yes | yes |
| Auth on, RBAC off | every signed-in user | every signed-in user (own account) | every signed-in user | yes |
| Auth on, RBAC on — Team admin / System admin | yes | yes | yes (`TeamWrite`) | yes (`TeamWrite`) |
| Auth on, RBAC on — Viewer on the Team | yes | **yes (`TeamContribute`)** | no | no (sees splits where they voted) |
| Auth on, RBAC on — no role | **404** | **404** (non-disclosing) | 404 | — |
| Auth on, RBAC off — unlinked API key | yes | **403 `vote-needs-a-person`** | yes | — |
| Auth on, RBAC on — unlinked API key | 404 (`CanReadTeamAsync` returns false without a profile, `RbacAdministrationService.cs:215-219`) | 404 | 404 | — |

The write is safe because (1) it can only **append an entry attributed to the caller** — there is no endpoint that
names another voter, and the voter key is derived server-side from the session or the presented key, never accepted
as an identity field; (2) it changes **no Work Item, no setting and nothing in the tracker** (D9); (3) it is bounded —
rate-limited per caller, text-capped, and only on Work Items currently in a refinement state; (4) it is visible —
every entry carries who, when and through which channel. Embedded sessions (ADR-137) vote as the signed-in viewer;
the accepted embed-nonce risk (memory `project_embed_nonce_unbound_accepted_risk`) already grants a hijacker
everything the viewer can do, and voting adds no capability beyond that.

## Wave: DESIGN / [REF] Identity model — auth on / auth off / client

| Situation | Voter key | Display name | Take back from | `isMine` / hidden split keyed by |
|---|---|---|---|---|
| Browser, auth on | `account:<sub>` | profile display name at write time | any session of the same account | account |
| Browser, auth off | `self:SHA-256(browser key)` | self-declared, per browser, editable (later entries carry the new name; earlier keep theirs) | that browser | browser key |
| CLI / MCP-stdio, auth on | `account:<sub>` of the key owner or bearer subject | owner's display name | any channel of the same account | account |
| CLI / MCP-stdio, auth off | `self:SHA-256(client key)` | `--as` / stored name / `voterName` — required | that client config | client key |
| Unlinked API key, auth on | — | — | — | **vote refused** |
| mcp-http, auth off | — | — | — | **vote refused** (MQ-3) |

Switching an instance from auth off to auth on leaves earlier `self:` entries as they were; the same person voting
again under their account is a new voter. Recorded, not hidden: it can double-count a person on Work Items that sat
in refinement across the switch. Clearing browser storage likewise makes a new voter (ADR-191 point 4's reasoning).

---

## Wave: DESIGN / [REF] Per-slice design notes (delivery order)

| Order | Slice | Design notes |
|---|---|---|
| 1 | **01** states setting | M1. `RefinementSettings.States` + validator (⊆ To Do ∪ Doing at save; stale entries kept and flagged on read). `RefinementSettingsSection` with category labels and the Doing note. `TeamDto.RefinementConfigured`; tab + role tooltip (`useRbac().isTeamAdmin`). Terminology keys `refinement`/`refinements` land **here** (slice brief allowed it), so the tab label is never hard-coded. Guard tests: TeamWrite; `WorkItemRelatedSettingsChanged` ignores refinement (E7); null DTO member leaves settings unchanged. Demo: Gravity `Backlog`/`Analysing`/`Next`; one demo Team unconfigured. |
| 2 | **02** list | `RefinementController GET`, `RefinementViewQuery` (list only), `RefinementList` ordered by DSN-6. Row: id + url, name, state, category, Work Item Age for Doing rows. Empty state. `TeamTabOpened` route key. E2E walking skeleton through a POM on demo Gravity. 300-item render budget. |
| 3 | **10** yardstick | `SleYardstickResolver` (DSN-10); facts `{source, days?, probability?}`; client builds the question. |
| 4 | **11** cast a vote | M2. `RefinementVotesController POST votes`, `SizingLogCommands`, `VoterIdentityResolver` (auth-off path), `TeamContribute`, rate-limit policy, `useVoterIdentity` + `VoterNamePrompt`, `VoteControl` + tally cell (`voteCount`, own vote marked). Writes `Channel=Web` and the current yardstick. Unknown/unlisted Work Item → 409. Response = the updated row (feeds the browser-detected "vote cast" event). |
| 5 | **13** readiness | `Readiness` JSON member + validator (MinYes ≥ 1, MinVoters ≥ MinYes, veto ≥ 1). `RefinementResolution` vote path only (no stages yet: every row's stage counts as Waiting). Row status Ready / n more Yes / n more votes / Needs discussion; heading `readyByVotesCount`. Demo votes: Jonas Weber, Ana Lima, Mo Okafor. |
| 6 | **03** stages | `States[].Stage`; `RefinementResolution` adds the state-stage step; `readyCount` = stage-Ready ∪ vote-Ready (no double count); "no state is marked Ready" hint fact `noReadySource` when no Ready stage, no Ready rule. |
| 7 | **04** cadence | Extract `WeeklyRecurrence` (refactor commit first, blackout tests green), `RefinementCadenceCalendar`; facts `nextRefinementDate`, `isRefinementDay`; `NoCadence` reason. Fixed-date unit tests from the slice brief. |
| 8 | **05** need + verdict | `RefinementNeedCalculator` + `NeedBand` (DSN-8/9), defaults 50/85. Parity test against the manual forecast (E9). One banner component, three states, exhaustive `Record<RefinementVerdict, …>`. |
| 9 | **06** line | `lineAfterPosition`, `fewerListedThanHigh` already on the wire from 05 — this slice is UI only (non-colour highlight + line row). |
| 10 | **12** comments | `POST comments`, comment on votes, `GET log`, open-question flag (DSN-15), `SizingLogDialog`. No migration. |
| 11 | **07** band setting | `Band` editable; validator 1 ≤ low < high ≤ 99 naming both values. |
| 12 | **15** account votes | `VoterIdentityResolver` auth-on path + unlinked-key refusal; no name prompt when `authenticationEnabled`. RBAC matrix test (E4). |
| 13 | **14** hidden split | `RefinementResolution` split visibility + API omission (view and log); presenter reveal rule prepared (DSN-16) though presenter ships in 18. Cancellable: removing it is deleting one predicate. |
| 14 | **08** stage rules | `StageRules` JSON + `StageRuleMatcher` + `DeliveryRuleBuilder` reuse; DSN-14 precedence; rules only over listed Work Items (AC-8.3). |
| 15 | **16** take back | `DELETE votes/mine` → `Revocation`; idempotent no-op; only own (by voter key). |
| 16 | **09** clients: need | client `getTeamRefinement`; `lh refinement get`; `lighthouse_team_refinement_get`; client composes sentences with the instance's Terminology. Lighthouse-Clients minor + changeset; `pnpm release:version` before the release run. |
| 17 | **17a** clients: votes read | Additive fields only; client sends its voter key header (if it has one) so hiding applies to it. |
| 18 | **17b** clients: cast | `lh refinement vote/comment/take-back`, `lh config voter set`, MCP vote/comment/take-back tools with DD-19 (d) wording; voter key store; channel `Cli`/`Assistant`; mcp-http refuses auth-off votes. Minor bump (may share 17a's release). |
| 19 | **18** presenter (LIGHT) | `PresenterMode` over the same GET with `presenting=true`; walks highlighted not-Ready rows in list order; writes only the facilitator's own entries with `Channel=LiveSession`; no endpoint can name another voter (structural, DD-20); keyboard-operable. No SignalR, no session entity. |
| — | **19** spike (questions only) | See below. |

### What spike 19 must answer (not designed here)

1. **Identity on the wire**: can an auth-off browser join a hub at all — the shared `lighthouse|auth-disabled` subject
   passes `[Authorize]`, so per-participant identity must ride as the same `self:` voter key this design already uses;
   is that enough to keep one participant from acting as another inside a session?
2. **Session entity or not**: does a remote session need a stored session (facilitator, participants, current Work
   Item), or can it stay a broadcast of "facilitator moved to Work Item X" over the existing log (DD-1: no rounds)?
3. **Fan-out**: is a per-Team SignalR group on the existing hub + Redis backplane (ADR-075) sufficient on multi-replica
   hosted, and does it need the `SizingEntryRecorded` event (DSN-18) as its trigger?
4. **Attack surface**: with auth on, does a hub group per Team leak Team existence to non-readers (must be
   non-disclosing like the 404s here)? With auth off, nothing new is exposed (D11) — confirm.
5. **Go / no-go** and the ADR draft for E4's next DISCUSS.

---

## Wave: DESIGN / [REF] Architectural enforcement

ArchUnitNET (existing under `Lighthouse.Backend.Tests/Architecture/`), the TypeScript compiler, and plain NUnit/Vitest
pins where ArchUnitNET cannot express the rule.

- **E1** — Nothing outside `API` and the composition root depends on `Services.*.Refinement`; `Refinement` does not
  depend on WorkTracking-Integration or Portfolio/Delivery.
- **E2** — `ISizingLogRepository` declares no member whose name starts with `Update`, `Remove`, `Delete`; no type
  calls `ExecuteUpdate*`/`ExecuteDelete*` or `Remove` on `SizingLogEntries` (reflection + ArchUnitNET call check). The
  append-only invariant, proved rather than trusted.
- **E3** — `RefinementResolution`, `NeedBand`, `RefinementCadenceCalendar`, `WeeklyRecurrence`,
  `RefinementSettingsValidator` are `static` and reference nothing in `Services.Implementation` (pure contract shape).
- **E4** — `TeamContribute` evaluates identically to `TeamRead` for every role × RBAC on/off × auth mode (parameterised
  matrix); every controller action carrying `TeamContribute` lives in `RefinementVotesController` (allowlist test, so a
  second read-permission write cannot appear unnoticed).
- **E5** — no `DateTime.UtcNow`/`Today` in the feature; days from `ILighthouseClock` (existing
  `CalendarDayAnchorSeamArchUnitTest`).
- **E6** — `IRefinementViewQuery`'s implementation has no dependency on `ISizingLogCommands`,
  `ISizingLogRepository.Append` or any repository `Save` (read path write-free). On reads, `VoterIdentityResolver`
  derives the caller's key from the principal's subject claim (auth on) or the presented key (auth off) **without**
  calling `ICurrentUserProfileService`, whose get-or-create touches the profile row; only writes resolve the profile
  (for `VoterProfileId` and the display name).
- **E7** — changing any refinement member leaves `WorkItemRelatedSettingsChanged` false (unit test over every member).
- **E8** — `NeedBand.ValueAt(p) == forecast.GetProbability(100 − p)` and `low ≤ high` for any 1 ≤ low < high ≤ 99
  (property test on fixed distributions).
- **E9** — for a Team and a cadence, the band equals the manual forecast's How Many values for target date = next
  Refinement (parity test through both public paths).
- **Frontend** — exhaustive `Record<…>` maps for verdict, readiness, stage, channel, unavailable reason (no `default:`);
  wire enums modelled as string unions (memory: enums are strings out); no hard-coded renameable term; no component
  fetches `/api/latest/authorization/my-summary` (existing lint/test).

---

## Wave: DESIGN / [REF] Quality attributes

| Attribute | Strategy |
|---|---|
| Performance (≤ 2 s at 300 Work Items, K-guardrail) | One GET: Team, refinement Work Items (one query by Team + raw states), log entries for those refs (one indexed query), cached throughput and cycle-time percentiles, one How Many run (trials × ≤ ~30 days, milliseconds). No N+1: resolution runs in memory. DISTILL adds a 300-row timing check. |
| Reliability / consistency | Settings: tokened write, 409 on conflict. Log: append-only, latest-by-`Id` — two concurrent writes from one voter both land and the later `Id` wins deterministically; no read-modify-write anywhere, so no CAS is needed. |
| Security | Named reader-write requirement, non-disclosing 404, rate limit, text caps, identity never accepted from the body with auth on, voter keys hashed at rest and never returned, React-escaped comment rendering. |
| Maintainability / testability | Pure resolution, band, cadence and validator; thin services over existing ports; two controllers. |
| Compatibility | Additive DTO fields and routes only; old clients unaffected; client minor bumps (09, 17a/17b). |
| Usability | One verdict component, same size for all three states (C5); terminology via `getTerm`; keyboard-operable presenter mode. |
| Observability | Structured log on refused votes (reason code, never the key or name). Usage events are DEVOPS's, fed by response facts. |

---

## Wave: DESIGN / [REF] Changed Assumptions

| Was (source) | Now | Why |
|---|---|---|
| AC-2.1 "the same backlog order the Team's forecasts use" | `WorkItem.Order` via `FeatureComparer.CompareOrderValues`, tiebreak `ReferenceId` | Team forecasts order no Work Items; the comparer is the one order ladder that exists. Manual Feature ranking does not apply. |
| AC-2.2 "age" on every row | Work Item Age on Doing rows; To Do rows show none | `WorkItemAge` is defined for started work only (`WorkItemBase.cs:100`); inventing a "waiting age" is a new metric (MQ-4). |
| AC-10.1 "over the Team's metrics window" | over the Team's **Throughput history window** | No server-side metrics window exists; the Metrics tab window is a browser choice. |
| AC-5.1 "same history window and settings as its forecasts" vs D20 "total Throughput" | Same as forecasts **including** the Team's forecast filter when set (Premium) | "Total" in D20 contrasted with "leaving refinement", not with the forecast filter; the band then equals the manual forecast for that date (MQ-1). |
| AC-11.2 / DD-10 "name per browser" | Name **plus a random per-browser voter key**; "mine" and take-back bind to the key | A name alone lets anyone take back anyone's vote and merges two people with one name. |
| AC-14.2 "no admin bypass except presenter mode" | Presenter reveal requires `TeamWrite` (everyone when auth/RBAC off) | A presenter flag anyone may set would make the API guarantee of AC-14 false (MQ-2). |
| US-09 / US-17b working names (`lighthouse teams refinement 3`, `get_team_refinement`, `vote_on_work_item`, `--yes-but "…"`) | `lh refinement get --team-id 3`, `lighthouse_team_refinement_get`, `lighthouse_team_refinement_vote`, `--answer yes-but --comment "…"` | House naming of the shipped CLI/MCP. |
| US-15 hypothesis "if TeamRead cannot carry a write, RBAC needs a new requirement" | It can, and a **named** requirement `TeamContribute` is added anyway, mapped to the read predicate | Makes the persisting action visible and allowlisted (ADR-217). No new role. |
| DISCUSS checklist "EF migrations owed 01, 11, 13, 03, 04, 07, 08" | **Two** migrations: 01 and 11 | JSON-valued settings (ADR-214) and an all-columns log table. |
| DD-11 "comment-only marks open question" (no end condition) | Open while the asker has no current vote | Needed a rule that clears; voting is the natural answer to one's own question. |
| DD-5 (rule vs votes, overlapping rules) | A matching rule decides stage **and** readiness; overlapping rules: Ready > Being refined > Waiting | DD-5 left both unstated. |

Story/AC edits are listed in `design/upstream-changes.md` for DISTILL to apply.

---

## Wave: DESIGN / [REF] Open items for DISTILL / DELIVER

1. DISTILL adds the missing error paths flagged at DoR (½-day stories) — the refusal codes above
   (`vote-needs-a-person`, `voter-name-required`, `voter-key-required`, `work-item-not-in-refinement`) are the error
   paths for 11/15/17b.
2. DISTILL: the 300-row timing check and the band/manual-forecast parity check (E9) are acceptance-level, not unit.
3. DELIVER 17b: confirm the client package's local config store is shared by CLI and MCP-stdio before relying on it
   for the voter key; if not, each keeps its own (two voters for one person, documented).
4. DELIVER 17b: confirm mcp-http's auth model; if it can run against an auth-off instance, it refuses votes (MQ-3).
5. DELIVER 01: `ARCHITECTURE.md` gains the eighth module, the sizing log as a third persistence shape beside
   snapshots and closure pins ("an append-only log keyed by voter"), and the reader-write rule in §10 — owed in the
   same change, per the keep-it-current rule.
6. DEVOPS: usage events against K1–K7 from the facts exposed here (`verdict`, `isRefinementDay`, row readiness in the
   write response, `channel`).

## Wave: DESIGN / [REF] Maintainer questions (only the maintainer can decide)

- **MQ-1** Does the need number respect the Team's forecast filter (default here, = forecasts) or always use
  unfiltered Throughput?
- **MQ-2** Presenter mode reveals splits only to Team admins when RBAC is on (default here), or to any presenter?
- **MQ-3** Votes through the hosted MCP bridge (mcp-http) on an auth-off instance: refused (default here)?
- **MQ-4** To Do rows: no age (default here), or a "waiting since" day?

## Wave: DESIGN / [REF] ADRs

[ADR-214](../../product/architecture/adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md) ·
[ADR-215](../../product/architecture/adr-215-the-need-band-is-the-manual-how-many-for-the-next-refinement-read-at-100-minus-p.md) ·
[ADR-216](../../product/architecture/adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md) ·
[ADR-217](../../product/architecture/adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md) ·
[ADR-218](../../product/architecture/adr-218-stage-readiness-and-the-hidden-split-are-one-pure-resolution-on-read.md).
C4: `docs/product/architecture/c4-diagrams.md` → "C4 Architecture Diagrams — epic-5510-5881-refinement".

## Wave: DESIGN / [REF] Contract shapes (effect isolation)

Every NEW or EXTENDED component the crafter will touch, classified so the assertion each test must make is known in
advance.

| Component | Change | Contract shape | Universe and declared change | Assertion mechanism |
|---|---|---|---|---|
| `RefinementResolution`, `NeedBand`, `RefinementCadenceCalendar`, `WeeklyRecurrence`, `RefinementSettingsValidator` | NEW / EXTRACT | pure function | none (return only) | E3 (static, no `Services.Implementation` refs) + table/property tests on return values |
| `IRefinementViewQuery` / `RefinementList` / `RefinementNeedCalculator` / `SleYardstickResolver` / `StageRuleMatcher` | NEW | pure read over ports (no writes) | universe: Team, Work Items, log, metrics cache; Δ = ∅ | E6 (no write member reachable) + test asserts DB row counts unchanged after a GET |
| `VoterIdentityResolver` | NEW | pure on reads; bounded-change on writes | writes: at most the caller's `UserProfiles` row (existing get-or-create touch) | E6 + test: read path makes no `ICurrentUserProfileService` call |
| `ISizingLogCommands` / `ISizingLogRepository` | NEW | bounded-change, append-only | universe: `SizingLogEntries` of one Team; Δ = exactly one new row (zero on idempotent take-back); nothing else | E2 (no update/delete members or calls) + test: row count +1, every other table unchanged |
| `TeamController.UpdateTeam` + `TeamExtensions.SyncRefinement` | EXTEND | bounded-change | universe: the one `Teams` row; Δ = `RefinementSettings` column (plus existing settings); Work Items untouched when only refinement changed | E7 + test: Team's Work Item count unchanged after a refinement-only save |
| `RbacGuardRequirement.TeamContribute` / `RbacGuardAttribute` / `CanSatisfyRequirementAsync` | EXTEND | pure decision | none | E4 matrix |
| `LighthouseAppContext` (converter, `DbSet`) | EXTEND | configuration | schema Δ = M1, M2 only | migration review; JSON round-trip test |
| Frontend `useVoterIdentity` | NEW | bounded-change | universe: one `localStorage` key `lighthouse:refinement:voter` | Vitest: no other key written |
| Client voter store (CLI/MCP-stdio) | EXTEND | bounded-change | universe: the voter entry for one Lighthouse URL in the client config | client tests: other config entries unchanged |

## Wave: DESIGN / [REF] Peer review

**Reviewer**: `nw-solution-architect-reviewer`, iteration 1, 2026-10-02 · **Verdict**: **approved** · critical 0 ·
high 0 · medium 2 · low 2. Spot-checks it ran against code: `HowManyForecast` descending comparer,
`DisabledAuthenticationHandler` shared subject, `RbacGuardAttribute` 404/403 split, percentile mapping.

| Finding | Severity | Resolution |
|---|---|---|
| Contract shapes scattered across E1–E9 rather than one table | medium | Added "Contract shapes (effect isolation)" above |
| `RbacGuardAttribute` change for non-disclosure not explicit | medium | Component row now says: add `TeamContribute` to `RequiresScope()` and `IsReadRequirement()` |
| `VoterIdentityResolver` dependencies not split by read/write | low | Component row now says `ICurrentUserProfileService` is used by writes only |
| ADR-214 names a migration build-order trap without stating it | low | ADR-214 Context now states it (HintPath DLL, build the backend first) |

Iteration 2 not needed: no critical or high findings. Priority validation recorded by the reviewer: Q1 *unclear*
(problem evidence is community desk signals, not Lighthouse-user interviews — DISCOVER G1 partial, carried, not a
DESIGN defect), Q2 adequate, Q3 correct, Q4 partial (performance guardrail to be measured in DISTILL).

---

## Wave: DEVOPS / [REF] Scope and Prior-Wave Reading

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent, documents only.

**The nine decisions were not asked; this is brownfield and each one is already settled project-wide.** Re-asking
would invite an answer that contradicts the project. Each is read from its source:

| # | Decision | Answer for this feature | Source |
|---|---|---|---|
| 1 | Deployment target | What Lighthouse already ships: standalone packages (Linux / Windows / macOS, Tauri desktop), the Docker image, the Helm chart for hosted. **The hosted platform is torn down (2026-09-26); nothing is deployed there now** | `ci.yml` package/docker/chart jobs; memory *Platform torn down on Infomaniak* |
| 2 | Container orchestration | Unchanged: single container standalone; Kubernetes via the chart when the platform is respun. No chart change | `ci_chart.yml` |
| 3 | CI/CD platform | GitHub Actions, the existing workflows; **extend, never add** | `.github/workflows/` (29 files); maintainer rule *consolidate CI* |
| 4 | Existing infrastructure | Yes — all of it reused; no new component | DESIGN "Technology choices: none new" |
| 5 | Observability | Structured logging + the in-process warning sink (*Recent problems*) + the opt-in usage-data pipe (browser detects, backend forwards to PostHog EU) | ADR-185, ADR-190/191 |
| 6 | Deployment strategy | The existing calver release (`/release`; a `waiting` run on main is the deploy approval); Recreate on hosted; startup migrations | memory *How to cut a release*; ADR-077 |
| 7 | Continuous learning | **The usage-data catalogue**, extended one feature at a time. Progressive exposure is per Team, built in: the tab stays disabled and nothing new is written until a Team admin chooses refinement states. No feature flag, no A/B | `CLAUDE.md` § DEVOPS usage-data rule |
| 8 | Branching | Trunk-based on `main`, no branches or PRs | `CLAUDE.md`; memory *trunk-based on main* |
| 9 | Mutation testing | `per-feature`, kill rate ≥ 80% — unchanged, not rewritten | `CLAUDE.md` § Mutation Testing Strategy |

**Prior-wave reading**

✓ This file: DISCUSS (DD-1..DD-22, K1–K7 + guardrails, checklist, ADO mapping), DESIGN (DSN-1..22, data model,
migration plan M1/M2, RBAC, identity, per-slice notes, quality attributes, open items, MQ-1..4)
✓ `design/wave-decisions.md`, `design/upstream-changes.md`, `discuss/wave-decisions.md`
✓ ADR-214..218 (217 §5 for the rate-limit policy, 216 for `Channel`)
✓ `Models/UsageData/UsageDataEventName.cs` (last member `TeamForecastRealityCheckRun = 11`), `UsageDataEventShapes.cs`,
`UsageDataRouteKey.cs` (last `PortfolioDetail_Access = 9`), `UsageDataRoutePatterns.cs`, `UsageDataEventReported.cs`,
`PostHogUsageDataPublisher.cs` (wire property names), frontend `models/UsageData/UsageData.ts`,
`services/UsageData/usageDataRouteKeys.ts`, `SystemSettingsTab.tsx` (`OptionalFeatureToggled` emission)
✓ `docs/settings/usagedata.md`; `UsageDataDisclosureTest` (counts the page's rows against the enum)
✓ `CLAUDE.md`, `docs/ci-learnings.md` (CA1869, S6964, S107, Program.cs force-full, migration DLL build order,
Stryker config traps, demo-data global effect, Europe/Zurich pin)
✓ House style: `story-6055-activity-names-the-work` DEVOPS sections + its `environments.yaml`
✓ `docs/product/kpi-contracts.yaml` (exists; append-only; `measurement_scope` vocabulary)
✓ `Program.cs` rate limiter (`ConfigureRateLimiting`, `WhatOneAddressMayHandIn`, middleware order), `RateLimitingConfiguration.cs`,
`appsettings.json` `RateLimits`; `Create-Migration.ps1`; `HistoricalSchemaPatch.cs`; `ExpandOnlyMigrationGuard.cs`;
`ci.yml` job graph incl. `ci_verifyauth.yml`

**Contradictions with DESIGN: one** — ADR-217's rate-limit partition "by subject" cannot work where the limiter
sits in the pipeline. See *Rate limiting* below and `devops/upstream-changes.md`.

---

## Wave: DEVOPS / [REF] Environment Matrix

Machine artifact: `docs/feature/epic-5510-5881-refinement/environments.yaml` (environments, `scenario_axes` per slice,
coexistence matrix, deployment assumptions).

| Environment | Why it exists here |
|---|---|
| `clean` | Baseline: SQLite, auth off, demo data (Gravity configured, one Team unconfigured) |
| `sqlite` / `postgres` | M1 and M2 apply at startup on both providers; JSON settings round-trip; DB-level cascade / SET NULL |
| `auth-off` | Every Community instance: self-declared name + client-minted voter key; `voter-name-required`, `voter-key-required` |
| `auth-on-rbac-off` | Every signed-in user votes; unlinked API key → 403 `vote-needs-a-person` |
| `auth-on-rbac-on` | The RBAC matrix: Viewer votes via `TeamContribute`; no role → 404 on read and write |
| `renamed-terminology` | New term "Refinement"; catches a hard-coded label |
| `refinement-day-boundary` | Instance-zone clock on/just before a cadence date: next date, verdict event, `sizingMoment` |
| `usage-data-consented` | The only environment in which the new events leave the browser; also asserts none without consent |
| `client-cli-mcp` | CLI / MCP-stdio voter store and channel; mcp-http refuses auth-off votes |
| `screenshot-capture` | Finalize screenshots; premium licence fixture, delete-before-regenerate |

Deliberately excluded: operating system (nothing platform-sensitive), licence tier (all Community; Premium enters only
via auth and Terminology, which are axes), hosted tenant (platform torn down; Postgres covers the provider).

---

## Wave: DEVOPS / [REF] CI/CD Pipeline Outline

**No workflow change. No new job, runner, secret or workflow.** The feature passes through what exists:

| Stage | Workflow | What it does for this feature |
|---|---|---|
| Change detection | `ci_changes.yml` (`path-classifier.sh`) | Flags backend and frontend. **Slice 11 edits `Program.cs`** (rate-limit policy, DI) → `connector_shared=true` → the full live-connector `Integration` category runs (~258 extra tests, shared Linear key, unauthenticated GitHub pair can flake). A red backend skips `sonar-gates`. Expect it; do not debug it as a regression |
| Backend | `ci_backend.yml` | `dotnet build` zero warnings; NUnit unit + WebApplicationFactory acceptance tests (RBAC matrix E4, append-only E2, settings write E7, band parity E9, migration fixtures incl. `HistoricalSchemaPatch`); ArchUnitNET rules E1–E9; `ExpandOnlyMigrationGuard` over M1/M2 |
| Frontend | `ci_frontend.yml` | Vitest (RefinementView, VoteControl, useVoterIdentity, usage-data emission per event, route key); `pnpm build` (`tsc -b`, Biome `--write` in prebuild) |
| E2E (compile) | `ci_e2e.yml` | Compiles the suite only |
| Verify SQLite / Postgres | `ci_verifysqlite.yml`, `ci_verifypostgres.yml` | **Where E2E runs, twice.** Two walking skeletons only (thin-sanity rule): **E1** Gravity's Refinement tab lists its Work Items in backlog order, the unconfigured Team's tab is disabled with the role tooltip; **E3** enter a self-declared name, cast a vote, see the tally, and Ready once the seeded voters plus this one meet MinYes. Verdict, cadence, stages, rules, hidden split, presenter: below E2E (Vitest + backend acceptance) |
| Verify auth | `ci_verifyauth.yml` | **No new `@auth` spec.** Auth-on identity (slice 15) is pinned by backend acceptance tests over the RBAC matrix; a browser leg would add a Keycloak run for a resolver already proven below it |
| Quality gate | `ci_sonar_gates.yml` | No new issue of any severity. Pre-apply: S6964 (nullable value types on `RefinementSettingsDto`/vote body), S107 (controllers split per DSN-19; `TeamController` ctor unchanged), CA1869 (JSON options for the settings converter cached `static readonly`), CA1859, CA1861/NUnit2045 in new tests, S1192 in DISTILL scaffolds |
| Package / Docker / Chart | existing jobs | Unchanged. No chart value added |
| Release | `ci_release.yml` via `/release` | Calver; `waiting` on main = deploy approval |

**Local gates** (CI parity, `CLAUDE.md` § Quality Gates): `pnpm test`, `pnpm build`, backend build + filtered
`dotnet test` (connector categories excluded). In a worktree, copy the premium licence fixture first (2 Licensing
failures otherwise). Generate migrations with `Create-Migration.ps1`, then rebuild each migrations csproj
`--no-incremental` and the solution `--no-incremental` before verifying (`PendingModelChangesWarning` trap).

**Lighthouse-Clients** (separate repo, own CI): slice 09 → **minor**; 17a + 17b → **minor** (may share one release).
Changeset per slice; **`pnpm release:version` + commit + push before the release workflow** (manual bump, memory
*Clients release manual version bump*). New endpoints and fields are additive; existing client response shapes do not
change, so older clients keep working against a newer server, and a newer client against an older server gets 404
on the refinement routes (the client says the instance is too old rather than crashing — DELIVER 09 asserts it).

---

## Wave: DEVOPS / [REF] Monitoring Contracts (KPI → instrument)

**Rule applied** (`CLAUDE.md`): name-only first; a property only when a KPI cannot be counted without it, and only a
closed enum. **What the pipe can and cannot see decides the honest answer per KPI**: every event reaches PostHog
under a **per-browser** pseudonym (ADR-191) with **no instance id and no Team id** — permanently, by design
(`docs/settings/usagedata.md` § *Counting browsers, not installations*). So any KPI phrased per *Team* or per
*instance* is measured per *browser* or not at all. And **CLI / MCP votes are invisible**: the pipe starts in a
consenting browser; a client has no consent row, so nothing a client does is ever reported.

### New vocabulary (appended, never renumbered)

**Integers are assigned in delivery order (DD-22), so each slice appends the next number when it lands.** The integers
below assume no other feature appends first; if one does, DELIVER takes the next free integer — the *name* is the
contract, the number is not.

| Event (`UsageDataEventName`) | Int | Carries | Emitting slice (DELIVER step) | Fires (browser, after the server accepted) |
|---|---|---|---|---|
| `TeamTabOpened` + route key **`TeamDetail_Refinement = 10`** (`/teams/:id/refinement`) | 0 (existing) | route (existing) | **02** (DESIGN; the first slice whose tab shows anything) | Existing 5-second dwell rule; `usageDataRouteKeys.ts` gains `["refinement", …]`, `UsageDataRoutePatterns` gains the address |
| **`TeamRefinementConfigured`** | **12** | nothing | **01** | The Team settings save the server accepted turned `refinementConfigured` from false to true. Not on every autosave |
| **`TeamSizingVoteCast`** | **13** | `sizingMoment` | **11** | The vote POST returned 2xx and the entry was a **vote** (not a comment, not a take-back). Re-votes count |
| **`TeamSizingReadinessReached`** | **14** | `sizingMoment` | **13** | The vote response's row turned Ready **by votes** where the tab's last read had it not Ready (the tipping vote, in the tipping browser only) |
| **`TeamRefinementDayVerdictShown`** | **15** | `refinementVerdict` | **05** | The tab's GET answered with `isRefinementDay = true`; at most once per tab mount. **Never on other days** — K3 and K6 ask only about Refinement days, so the day itself needs no property |
| **`TeamRefinementPresented`** | **16** | nothing | **18** | Presenter mode entered (dwell 5 s, same rule as tab openings) |

**Two new closed enums** (each a new nullable part on `UsageDataEventReported`, declared in `UsageDataEventShapes`
with *IsCarriedExactlyWhenDeclared*, a new `snake_case` property in `PostHogUsageDataPublisher.WhatEachMessageCarries`,
a string-union mirror in `UsageData.ts`, and a row in the disclosure page's field table):

| Enum | Values (ordinal = slice that first can produce it) | Wire property | Carried by |
|---|---|---|---|
| `UsageDataSizingMoment` | `NoCadence = 0` (11) · `OnRefinementDay = 1`, `OnOtherDay = 2` (04) · `InLiveSession = 3` (18) | `sizing_moment` | `TeamSizingVoteCast`, `TeamSizingReadinessReached` |
| `UsageDataRefinementVerdict` | `Below = 0`, `In = 1`, `Above = 2`, `None = 3` (05; `None` = no verdict: insufficient data) | `refinement_verdict` | `TeamRefinementDayVerdictShown` |

`sizingMoment` is derived in the browser from facts already on the wire: `channel == LiveSession` → `InLiveSession`;
else the tab's last `isRefinementDay` (`true` → `OnRefinementDay`, `false` → `OnOtherDay`); no cadence → `NoCadence`.
**One property instead of two** (`{refinementDay, otherDay}` × `{liveSession, async}` as DISCUSS sketched): a live
session is by definition not async, so the four values cover every cell K4/K5/K7 read, with one closed enum.
`Cli` / `Assistant` never appear — they never reach a browser.

### KPI → instrument

| KPI | Instrument | Computed in PostHog | As specified? |
|---|---|---|---|
| **K1** setup (≥15% within 60 d of E1) | `TeamRefinementConfigured` | distinct browsers sending it ÷ distinct browsers sending `TeamTabOpened` route `TeamDetail_Settings`, 60 days from the E1 release | **Not as specified** — "instances with ≥1 Team" has no identity in the pipe. **Proxy**: browsers that could configure (opened a Team's Settings) vs browsers that did. Target kept as a hypothesis; re-read against the first month |
| **K2** WAU (≥50% of Teams in ≥3 of 4 weeks) | `TeamTabOpened` / `TeamDetail_Refinement` | stickiness: browsers with the opening in ≥3 of 4 consecutive ISO weeks ÷ browsers with ≥1 opening | **Not per Team**. **Proxy**: per browser. The tab is disabled until configured, so every opening is of a configured Team |
| **K3** north star (in range on a Refinement day, → ≥60%) | `TeamRefinementDayVerdictShown.refinement_verdict` | per (browser, day): last verdict; share `In` ÷ (`Below`+`In`+`Above`); `None` reported apart. Baseline = first month after E2 | **Per browser-day, not per Team.** A browser that looks at two Teams on one day mixes them; accepted noise |
| **K4** riskiest (≥40% of votes async and off-day) | `TeamSizingVoteCast.sizing_moment` | `OnOtherDay` ÷ (`OnRefinementDay` + `OnOtherDay` + `InLiveSession`); `NoCadence` excluded and shown apart | **Yes, for browser-cast votes.** Unreadable before slice 04 (every vote is `NoCadence` while 11/13 precede 04 per DD-22) — the volume clock still starts at 11. CLI/MCP votes uncounted |
| **K5** (≥50% Ready before the day) | `TeamSizingReadinessReached.sizing_moment` | `OnOtherDay` ÷ (all minus `NoCadence`) | **Yes, for readiness tipped by a browser vote.** Readiness tipped by a client vote, or reached through a state/rule change, is not this KPI and not counted. Readable from slice 04 |
| **K6** (≥50% stop after *above*) | derived from K3's series | per browser: each Refinement day whose last verdict was `Above`; success if the browser's next Refinement-day verdict is not `Above` | **Not as specified** — "same Team at its next Refinement" needs Team identity. **Proxy**: same browser, next Refinement day it reported. Noisy for browsers that look at several Teams; read as directional |
| **K7** gateway (≥30% of presenting Teams get async votes in 4 weeks) | `TeamRefinementPresented` + `TeamSizingVoteCast.sizing_moment` | browsers that presented ÷ of those, browsers that cast an `OnOtherDay`/`OnRefinementDay` vote within 28 days | **Not as specified** — colleagues vote from *their* browsers, which nothing links to the facilitator's. **Proxy**: does the facilitator's own browser go on to vote async. Lower bound only |
| Guardrail: *below range* share not above first month | K3 series, `Below` share | same as K3 | Yes (per browser-day) |
| Guardrail: ≤2 s at 300 Work Items | DISTILL 300-row timing check | CI | Not telemetry |
| Guardrail: existing forecasts unchanged | band/manual-forecast parity test (E9) + no shared setting touched (E7) | CI | Not telemetry |

**Smuggling refused, explicitly**: no Team id, Work Item id, vote count, voter count, band number, percentile, cadence or
channel string travels. Counts are derived in PostHog from event *occurrences*, never sent.

**Per-slice disclosure duty** (`docs/settings/usagedata.md`, same DELIVER step that emits): 01 adds the
*refinement set up* row (and the "eight of twelve carry nothing" sentence becomes nine of thirteen); 02 adds the
sixth Team tab and the eleventh address; 11 adds the vote row and the `sizing_moment` field row (value `NoCadence`
only); 04 widens `sizing_moment`'s value list; 13 adds the readiness row; 05 adds the verdict row and the
`refinement_verdict` field row; 18 adds the presenter row and `InLiveSession`. `UsageDataDisclosureTest` fails the
build if a row is missing. `kpi-contracts.yaml` updated (seven `OUT-5510-*` entries + guardrail).

---

## Wave: DEVOPS / [REF] Deployment Strategy and Rollback

**Rollback first.** Every slice is additive; the rollback contract is the same for all of them:

| What | Rollback | Why it is safe |
|---|---|---|
| Code | `git revert` of the slice's commits, then the ordinary release path; or install the previous release | One artifact carries backend + frontend; no mixed-version window inside an instance |
| **M1** `AddRefinementSettingsToTeams` (slice 01) — one nullable `TEXT` column on `Teams` | **None.** Leave it. An older binary does not map the column; EF updates only changed columns, so its Team saves never touch it | Expand-only; values reappear intact on re-upgrade |
| **M2** `AddSizingLogEntries` (slice 11) — new table, every column, FK `TeamId` → `Teams` **ON DELETE CASCADE**, `VoterProfileId` → `UserProfiles` **ON DELETE SET NULL**, index `(TeamId, WorkItemReferenceId, Id)` | **None.** Leave it | The FK actions are **database-level** constraints (EF emits them into the migration), so an older binary deleting a Team or a profile neither fails on the FK nor orphans rows. DELIVER 11 asserts the constraint exists on both providers, not only the EF model's `OnDelete` |
| JSON settings shape | Members only ever **added** with defaults (DESIGN); a missing member reads as its initialiser, an unknown member is ignored by an older reader | No rename/remove without its own ADR |
| `Down()` | Never run in production | `ExpandOnlyMigrationGuard` checks `Up` for Drop/Rename |

**Rollback rehearsal (owed, DELIVER 01 and 11)**: start the previous published release's Docker image against a
database migrated by the slice (SQLite file + Postgres), open a Team, save its settings, delete a Team carrying log
rows. Expected: starts, healthy (`MigrationsAppliedHealthCheck` checks *pending*, and the older binary has none), no
error. One manual check per migration, recorded in the slice's deliver notes. It is the only part of this plan that is
otherwise an assumption.

**Migration generation (DELIVER 01, 11)**: `Lighthouse.Backend/Create-Migration.ps1 -MigrationName …` (SQLite +
Postgres); then the ordered `--no-incremental` rebuild (migration DLLs are HintPath refs). **`HistoricalSchemaPatch`**:
slice 01 adds `new("Teams", "RefinementSettings", "TEXT", "AddRefinementSettingsToTeams")` — every migration fixture
that seeds Teams through the current model otherwise dies with *no column named RefinementSettings*. M2 needs no entry
(no fixture seeds the new table). Postgres concurrent startup is already covered by `ConcurrentStartupMigrationTests`.

**Rollout**: the existing release path. Exposure is progressive by construction — per Team, the tab is disabled and
nothing is written until a Team admin chooses refinement states, and votes need a listed Work Item. **Hosted**: none
now (platform torn down); when respun, Recreate picks up the image, no chart change.

---

## Wave: DEVOPS / [REF] Rate Limiting (vote, comment, take-back)

**Extend the existing limiter; no new infrastructure.** One named policy beside the six that exist:

- `RateLimitingConfiguration.RefinementContributionPolicy = "RefinementContribution"`, on `POST …/votes`,
  `POST …/comments`, `DELETE …/votes/mine` via `[EnableRateLimiting]`.
- `appsettings.json` → `RateLimits:Policies:RefinementContribution: { PermitLimit: 30, WindowSeconds: 60, QueueLimit: 0 }`.
  30/min per voter is ~10× the DISCUSS pace hypothesis (5 votes in < 2 min) and still bounds a script.
  **An unconfigured policy silently applies no limit** (`AddFixedWindowPolicy` → `GetNoLimiter("unconfigured")`), so
  DELIVER 11 adds a test that the shipped `appsettings.json` configures it, beside the 429 test in `S6_RateLimitingTests`.
- **Partition key — corrected from ADR-217**: `app.UseRateLimiter()` runs **before** `UseAuthentication()`
  (`Program.cs:225-228`), so `HttpContext.User` carries no subject when the partition is chosen; "by subject" would
  fall through to the address for every caller. Use the existing presented-handle pattern of `UsageDataIngest`
  instead: SHA-256 of the first present of `X-Lighthouse-Voter-Key` (auth off), the `X-Api-Key` header, the
  `Authorization` header, the authentication cookie value; else the remote address. A handle is presented, not
  proved — so **extend `WhatOneAddressMayHandIn`** to apply the address ceiling (`PermitLimit × BrowsersOneAddressMaySpeakFor`
  = 600/min) to this policy as well, which bounds a caller minting a new voter key per request. Moving
  `UseRateLimiter` after authentication would also work but changes every existing policy; not proposed.
- `RateLimits:Enabled = false` switches it off with everything else — unchanged semantics.

Accepted residual: on an auth-off instance a caller minting voter keys can add up to the address ceiling of
distinct "voters" per minute. Auth off already grants every reader everything (D11); entries are visible and attributed
to their declared name.

---

## Wave: DEVOPS / [REF] Observability Stack

**Unchanged stack; four log lines added, no metric, no dashboard.**

| Event | Level | Structured fields | Never logged |
|---|---|---|---|
| Vote/comment/take-back refused `vote-needs-a-person` (unlinked API key, RBAC off) | **Warning** — operator-actionable (link the key's owner); surfaces in *Recent problems*; bounded by the rate limit | `Reason`, `TeamId`, `Channel` | key id, key name |
| Refused `voter-name-required` / `voter-key-required` | Information (a client bug or a cleared browser; not operator-actionable) | `Reason`, `TeamId`, `Channel` | — |
| Refused `work-item-not-in-refinement` (409) | Information (the Work Item left refinement between read and vote; normal) | `Reason`, `TeamId`, `Channel` | Work Item reference |
| Entry appended | Debug | `TeamId`, `Kind`, `Channel` | — |

**Never in any log line**: the self-declared name, the voter key or its hash, the account subject/email, the comment
text, the answer. Message templates with named placeholders (no interpolation); a catch that logs passes the exception
first (S6667). Rate-limit rejections stay unlogged, as for every existing policy (429 + `Retry-After`).
No log on the GET path (it runs on every tab view). The tab itself is the observability surface for the derived
read; DISTILL's 300-row timing check guards its latency.

---

## Wave: DEVOPS / [REF] Mutation Testing Strategy

**`per-feature`, ≥ 80%** — the project setting, not re-decided. Run **once per Epic at finalize, last, on frozen
code** (E1, E3, E2, E4 in delivery order), recorded under `docs/feature/epic-5510-5881-refinement/mutation/`.

- **Backend (Stryker.NET)**, whole files only (.NET Stryker ignores line spans): `Models/Refinement/*`,
  `Services/Implementation/Refinement/*` (`RefinementResolution`, `NeedBand`, `RefinementCadenceCalendar`,
  `VoterIdentityResolver`, `SizingLogCommands`, …), `WeeklyRecurrence`, `RefinementSettingsValidator`,
  `TeamExtensions` refinement sync if split into its own file. **Not** `RbacAdministrationService` / `RbacGuardAttribute`
  / `Program.cs` whole-file (shared, large; the `TeamContribute` arm is pinned by the E4 RBAC matrix instead — stated
  in `results.md`, not hidden in the number).
- **Exclude the acceptance suite** (WebApplicationFactory hosts: 80 min vs 3) — unit tests only.
- **Frontend (StrykerJS)**: `pages/Teams/Detail/Refinement/*`, `useVoterIdentity`, `useRbac` (`canContributeToTeam`),
  the usage-data detection additions (moment/verdict mapping, tipping detection). Copy literals: pin against the literal
  (StrykerJS does not mutate JSX text).
- Config files: `stryker-<id>.*.json` matches the report ignore pattern — **force-add** and say so in the commit; copy
  the vitest runner config to `Lighthouse.Frontend/` to run.

---

## Wave: DEVOPS / [REF] Branching Strategy

**Trunk-based on `main`**, unchanged: every commit runs the full workflow set. Slice boundary ritual: a focused commit
per step, push at slice end only when green, wait for CI, then ADO Active → Resolved. Never push red (skip a
not-yet-passing acceptance test). This worktree branch (`worktree-declarative-crafting-beacon`) lands by push to `main`
when the maintainer says so; no autonomous rebase.

---

## Wave: DEVOPS / [REF] Coexistence Matrix

Full table in `environments.yaml`. What must keep working while this ships:

| Must not break | Why it is at risk |
|---|---|
| Team settings autosave, 409 on conflict | Refinement settings ride the same write; null = unchanged |
| `WorkItemRelatedSettingsChanged` | A refinement-only save must not delete and re-fetch the Team's Work Items (E7) |
| Manual forecast How Many | Called unchanged by the need band; parity test |
| Recurring blackout rules | `WeeklyRecurrence` extracted from them (slice 04, refactor commit first) |
| Existing usage-data events, route keys, the disclosure test | Appended to, never renumbered |
| Existing rate-limit policies + the UsageDataIngest address ceiling | The ceiling's applicability widens to one more policy |
| Migration fixtures | `HistoricalSchemaPatch` entry for `Teams.RefinementSettings` |
| Demo data consumers (every E2E) | Gravity gains states, cadence, three named voters — grep E2E for Gravity first |
| Team tab order and the Settings tab | Refinement inserted between Metrics and Settings |
| Lighthouse-Clients commands / MCP tools | Additive only |

---

## Wave: DEVOPS / [REF] Pre-requisites from DESIGN

| DESIGN constraint | Platform answer |
|---|---|
| DSN-2 / ADR-214 one JSON column, M1 | Expand-only; `HistoricalSchemaPatch` entry; rollback = leave the column |
| DSN-11 / ADR-216 append-only table, M2 | Expand-only; DB-level cascade / SET NULL asserted; rollback = leave the table |
| DSN-13 / ADR-217 `TeamContribute` + rate limit | Policy `RefinementContribution` 30/60 s; partition by presented handle (correction) + address ceiling |
| DSN-17 channel enum | Mapped to `sizingMoment` in the browser; `Cli`/`Assistant` unreachable by usage data |
| DSN-18 no domain event | Usage events are browser-detected from response facts — consistent |
| Quality attributes: "structured log on refused votes (reason code, never key or name)" | Four lines, levels above |
| MQ-1..MQ-4 | No platform consequence either way |

---

## Wave: DEVOPS / [REF] Handoff

**To** `nw-acceptance-designer` (DISTILL): `environments.yaml` (`scenario_axes` per slice); the event table above
(each emitting slice gets two Vitest scenarios — consented: exact name + exact property; not consented: nothing — plus
a backend `Fits` shape test per new part); the refusal log assertions (level + no name/key in the rendered message);
the rate-limit configured-and-429 test; the M2 FK-constraint assertion; `HistoricalSchemaPatch` entry in slice 01.
Tags: `@kpi-OUT-5510-*` per `kpi-contracts.yaml`.

**Per-wave peer review: not run** (documents-only subagent; coordinator's call). No novel deployment target, no new
CI framework, no observability rewrite.

## Wave: DEVOPS / [REF] Changed Assumptions

| Was | Now | Why |
|---|---|---|
| ADR-217 / DSN-13 "partitioned by subject, else voter-key hash, else IP" | By presented handle (voter key, API key, bearer, cookie), else IP; plus the address ceiling | The limiter runs before authentication (`Program.cs:225-228`) — no subject exists yet. `devops/upstream-changes.md` |
| DISCUSS K4 "vote cast with `{refinementDay, otherDay}` and, from E4, `{liveSession, async}`" | One closed enum `sizingMoment` {NoCadence, OnRefinementDay, OnOtherDay, InLiveSession} | Live is never async; one property covers every cell |
| DISCUSS K3 "verdict property `{below, in, above, none}` on a 'need shown' event" | `TeamRefinementDayVerdictShown` fires on Refinement days only | K3 and K6 read Refinement days only; fewer events, no day property |
| K1, K2, K3, K6, K7 phrased per Team / instance | Per browser (proxies above) | The pipe has no Team or instance identity, by design |
| K4/K5 count all votes | Browser-cast votes only | Clients have no consent; nothing they do is reported |


---

## Wave: DISTILL / [REF] Scope and Reconciliation

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, maintainer AFK.
**Scope**: **E1 only — Epic #6136**: slice 01 (US-01, #6139) and slice 02 (US-02, #6140). E2–E5 are not distilled.

**Reconciliation passed — 0 contradictions** across DISCUSS, DESIGN and DEVOPS. DESIGN's corrections to AC-2.1
(backlog order = tracker rank, DSN-6), AC-2.2 (age on Doing rows only, MQ-4 default) and the slice-01 OUT list
(Terminology keys land in 01, DSN-20) are applied as written in `design/upstream-changes.md`. DEVOPS's events for
these slices (`TeamRefinementConfigured`, slice 01; route key `TeamDetail_Refinement` on `TeamTabOpened`, slice 02)
each have scenarios. Settled calls re-checked: the term is **Refinement**; slices 01–02 are **Community** (no
licence gate anywhere); nothing here touches votes or the need number. Decisions DST-1..DST-13:
`distill/wave-decisions.md`.

> **Note added at DELIVER finalize (2026-10-03), DISTILL text left as written.** DST-2 and DST-3 were **reversed
> by the maintainer on 2026-10-03** after the slice-01 review: a refinement state that stops being To Do or Doing is
> now removed in the save that takes it out (auto-remove, step 01-13), so nothing is kept and flagged, `isMapped`
> left the wire and "Work Items in a flagged state are not listed" became moot. After the slice-02 review **Work Item
> Age and the state category left the wire** as well as the table (step 02-10); rows carry id, name, link, state and
> parent. The scenario tables below describe the suite as DISTILL wrote it; see `## Wave: DELIVER / …` for what runs.

## Wave: DISTILL / [REF] Scenario list with tags

All non-skeleton scenarios are pending (`[Ignore(PendingSlice0n)]`, `it.skip`); the E2E skeleton is `test.fixme`.
Error/boundary share: **39 of 66** runnable cases (59%).

**Backend — `Slice01RefinementStatesTest`** (13 scenarios, 14 cases; `@driving_port @real-io @slice-01`)

| Scenario | Tags |
|---|---|
| A Team admin names the refinement states and the Team says it has them | `@us-01 @kpi-OUT-5510-K1-refinement-set-up @contract-shape:bounded-change` |
| A Team nobody has set up says it has no refinement states | `@us-01 @boundary @contract-shape:pure-function` |
| Every reader of the Team learns that it has refinement states | `@us-01 @contract-shape:pure-function` |
| A state the Team maps under a name of its own can be chosen by that name | `@us-01 @contract-shape:bounded-change` |
| A state that is neither To Do nor Doing is refused and nothing is saved (Done, Icebox) | `@us-01 @error @contract-shape:unbounded-preservation` |
| Only a Team admin can change the refinement states | `@us-01 @error @contract-shape:unbounded-preservation` |
| A save that says nothing about refinement leaves the chosen states as they were | `@us-01 @boundary @contract-shape:unbounded-preservation` |
| Clearing every refinement state turns the Team back to having none | `@us-01 @boundary @contract-shape:bounded-change` |
| A chosen state that stops being mapped is kept and flagged, never dropped | `@us-01 @error @contract-shape:bounded-change` |
| Saving again with the flagged state still chosen keeps it flagged instead of refusing the save | `@us-01 @error @contract-shape:bounded-change` |
| A newly chosen state that is no longer mapped is refused | `@us-01 @error @contract-shape:unbounded-preservation` |
| Choosing refinement states keeps every Work Item the Team already holds | `@us-01 @contract-shape:bounded-change` |
| Refinement is a word every instance can rename | `@us-02 @contract-shape:pure-function` |

**Backend — `Slice02RefinementListTest`** (14; `@driving_port @real-io @slice-02 @us-02`)

| Scenario | Tags |
|---|---|
| The coach sees every Work Item in refinement in backlog order | `@kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function` |
| Each row names the Work Item, links to the tracker and gives its state and category | `@contract-shape:pure-function` |
| A Doing row carries its Work Item Age and a To Do row carries none | `@boundary` |
| Work Items the tracker ranks equally are listed by id | `@boundary` |
| Numeric ranks are compared as numbers, not as text | `@boundary` |
| A state chosen by its mapped name lists the Work Items held under that name | — |
| An empty refinement is stated, not answered as an error | `@error` |
| A Team nobody set up answers that it has no refinement states and lists nothing | `@error` |
| Another Team's Work Items in the same state are not listed | `@boundary` |
| Work Items in a chosen state that is no longer mapped are not listed | `@us-01 @error` |
| Somebody without a role on the Team is told the tab does not exist | `@error @contract-shape:unbounded-preservation` |
| Asking for the Refinement tab of a Team that does not exist is not found | `@error @contract-shape:unbounded-preservation` |
| Opening the tab changes nothing about the Team or its Work Items | `@contract-shape:pure-function` |
| Three hundred Work Items in refinement come back in backlog order within two seconds | `@boundary @kpi-OUT-5510-K2-refinement-tab-weekly` |

**Backend — `TeamRefinementUsageEventsTests`** (5 scenarios, 6 cases; `@driving_port @real-io`)

| Scenario | Tags |
|---|---|
| A browser that agreed reports refinement being set up as one event carrying only its name | `@us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up` |
| A refinement set-up event carrying anything but its name is refused (route, work tracking system) | `@us-01 @slice-01 @error` |
| Refinement being set up is appended to the list of names, never inserted | `@us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up` |
| A browser that agreed reports opening the Refinement tab as a Team tab opening naming that tab | `@us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly` |
| The usage data page lists the Refinement tab among the addresses it publishes | `@us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly` |

**Frontend — Vitest + RTL** (32 cases)

| File | Scenarios | Tags |
|---|---|---|
| `TeamDetail.refinementTab.test.tsx` | sits between Metrics and Settings · switched off, points its admin to Settings · tells a reader a Team admin has to choose · points everybody to Settings when roles are not enforced · switched on for a reader once configured · the Team's own word on tab and tooltip · opens the view and puts the tab in the address · opens straight from an address · lands on Forecasts from an address naming the tab of an unconfigured Team · switches on as soon as the save is accepted · reports set-up once · reports nothing on a later save · reports nothing and stays off when the save is refused | `@us-01 @us-02 @slice-01 @slice-02`, 6 `@error`, 1 `@boundary`, `@kpi-OUT-5510-K1-refinement-set-up` ×3 |
| `Refinement/RefinementView.test.tsx` | asks for this Team's refinement and counts it in the heading · keeps the server's (backlog) order · links, names, state, category · age on Doing only · singular count · empty state, no error · the Team's own words (list, empty) · 300 Work Items | `@us-02 @slice-02`, 2 `@error`, 4 `@boundary`, `@kpi-OUT-5510-K2-refinement-tab-weekly` |
| `ModifyTeamSettings.refinement.test.tsx` | offers only To Do and Doing states with their category · Doing note · saves the ticked states · shows chosen states ticked · saves none once the last is unticked · flags an unmapped chosen state · keeps a flagged state on the next edit · the Team's own word in title and flag | `@us-01 @slice-01`, 2 `@error`, 2 `@boundary` |
| `usageDataRouteKeys.refinement.test.ts` | the Refinement tab is a Team tab opening naming that tab · names the same tab for every Team | `@us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly`, 1 `@error` |

**E2E — `specs/teams/Refinement.spec.ts`** (1, `test.fixme`): *a coach opens a Team's Refinement tab and sees its
Work Items in backlog order, while a Team without refinement states keeps the tab switched off* —
`@walking_skeleton @driving_port @us-01 @us-02 @slice-01 @slice-02`. Given: demo scenario 12 with Team Gravity's
refinement states Backlog, Analysing, Next and Team Zenith left unconfigured (demo seeding is DELIVER's, slice 01).

## Wave: DISTILL / [REF] WS strategy

Architecture of Reference + project policy (inherited, nothing appended). One walking skeleton for E1, the E2E above,
as DVO-7 specifies: real browser → real app → seeded demo data, through POMs. Backend and frontend acceptance
scenarios exercise the production composition root (`WebApplicationFactory<Program>` over real EF; the real component
tree) with only the licence and the instance clock faked. Tier B: not applicable (DST-12).

## Wave: DISTILL / [REF] Test placement

| Where | Why (precedent) |
|---|---|
| `Lighthouse.Backend.Tests/API/Integration/Refinement/` — `RefinementAcceptanceTest` (harness) + `Slice01RefinementStates{Scenarios,Specifications}.cs`, `Slice02RefinementList{Scenarios,Specifications}.cs` | Per-feature folder, partial-class Scenarios/Specifications split (BlockedItems, ForecastRealityCheck, story 6083) |
| `Lighthouse.Backend.Tests/Integration/UsageData/TeamRefinementUsageEventsTests.cs` | Beside `TeamForecastRealityCheckRunEventTests` on `UsageDataCollectorObservationTest` |
| `Lighthouse.Frontend/src/pages/Teams/Detail/TeamDetail.refinementTab.test.tsx`, `…/Refinement/RefinementView.test.tsx`, `src/components/Common/Team/ModifyTeamSettings.refinement.test.tsx`, `src/services/UsageData/usageDataRouteKeys.refinement.test.ts` | Colocated `<component>.<concern>.test.tsx` (story 6094's `TeamForecastView.realityCheck.*`) |
| `Lighthouse.EndToEndTests/tests/specs/teams/Refinement.spec.ts` + POM `tests/models/teams/TeamRefinementPage.ts`, `TeamDetailPage` gains `refinementTab`, `refinementTabTooltip()`, `goToRefinement()` | Specs through POMs only, demo data (`testWithDemoData`) |

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving adapter (DESIGN) | Covered by |
|---|---|
| `PUT /teams/{teamId}` carrying `refinement` | every slice-01 backend scenario; frontend save scenarios |
| `GET /teams/{teamId}` → `refinementConfigured` | slice-01 configured / not configured / reader; frontend tab enablement |
| `GET /teams/{teamId}/settings` → `refinement` | slice-01 read-backs; settings form scenarios |
| `GET /teams/{teamId}/refinement` | all slice-02 backend scenarios; `RefinementView` via `RefinementService`; E2E |
| `GET /terminology/all` → `refinement`, `refinements` | `Refinement_is_a_word_every_instance_can_rename` |
| `POST /usagedata/events` (`TeamRefinementConfigured`, `TeamTabOpened` + `TeamDetail_Refinement`) | `TeamRefinementUsageEventsTests` |
| UI route `/teams/:id/refinement`, Settings → Refinement section | `TeamDetail.refinementTab.test.tsx`, `ModifyTeamSettings.refinement.test.tsx`, E2E |

## Wave: DISTILL / [REF] Adapter coverage

| Driven adapter | Real I/O scenario |
|---|---|
| EF `Teams.RefinementSettings` JSON column (M1, via `IRepository<Team>`) | every slice-01 save/read-back (real EF, provider per CI leg) |
| EF Work Items (`IWorkItemRepository`) | every slice-02 list scenario; Work Item count kept (slice 01) |
| `TerminologySeeder` | terminology scenario (seeders run in the harness) |
| Usage-data forwarding (`IUsageDataPublisher` fake, outbound recorder) | `TeamRefinementUsageEventsTests` |
| `ILighthouseClock` | faked (`FakeLighthouseClock`, policy row) — the Work Item Age scenario |

No new external adapter in E1 (contract testing N/A, as DESIGN states). The M1 migration itself is DELIVER's
(`CreateMigration`), with its `HistoricalSchemaPatch` entry (DEVOPS).

## Wave: DISTILL / [REF] Scaffolds

| File | Marker | Behaviour until DELIVER |
|---|---|---|
| `Lighthouse.Frontend/src/services/Api/RefinementService.ts` | `__SCAFFOLD__` | `getRefinement` throws `Not yet implemented -- RED scaffold` |
| `Lighthouse.Frontend/src/pages/Teams/Detail/Refinement/RefinementView.tsx` | `__SCAFFOLD__` | throws on render |
| `Lighthouse.Frontend/src/models/Refinement/Refinement.ts` | types only | — |
| `ITeamSettings.refinement?`, `IApiServiceContext.refinementService` (+ default and mock entries) | additive | not read by any production code yet |

Backend: none — the scenarios are black-box over HTTP/JSON (precedent 22e43e1f9). No EF migration. The Terminology
keys are deliberately **not** added to `TERMINOLOGY_KEYS` (DST-7).

## Wave: DISTILL / [REF] Pre-requisites

DESIGN: driving ports and wire members above (DSN-3/5, DSN-6, DSN-20; ADR-214), RBAC `TeamWrite` / `TeamRead`
(non-disclosing 404). DEVOPS: environments `clean`, `sqlite`/`postgres` (settings round-trip on both CI legs),
`renamed-terminology`, `usage-data-consented`; E2E in `ci_verifysqlite`/`ci_verifypostgres` with the premium
licence CI already uploads. DELIVER owns: M1 via `CreateMigration` + `HistoricalSchemaPatch` row, demo data (Gravity
Backlog/Analysing/Next; Zenith unconfigured), seeder + `TERMINOLOGY_KEYS` + fallback words, `docs/settings/usagedata.md`
rows, `ARCHITECTURE.md` eighth module.

## Wave: DISTILL / [REF] RED classification

66 of 66 runnable cases fail for `MISSING_FUNCTIONALITY` when un-skipped; none broken. Two backend refusal
scenarios that were vacuously green (a missing route also answers 404) now first prove the tab opens for a permitted
caller. E2E type-checked, not run live. Detail: `distill/red-classification.md`.

## Wave: DISTILL / [REF] Review

Final-gate review for this scope: `nw-acceptance-designer-reviewer` (Sentinel), iteration 1 — **conditionally
approved**, 0 blockers, 3 high, 6 low. The DISCUSS, DESIGN and DEVOPS reviewers were not re-dispatched: those
waves were reviewed and pushed before this run and are unchanged by it.

| Finding | Resolution |
|---|---|
| (high) Frontend and E2E cases lacked `@contract-shape:` tags | Added to every case |
| (high) AC-2.6 demo seeding guarded only by the un-run E2E; premium demo scenario for a Community feature | Accepted, DELIVER condition: slice 01 adds a backend demo-data scenario (scenario 12 → Gravity configured, Zenith not). The premium scenario is only where Gravity lives; no scenario gates on a licence (DST-14) |
| (high) AC-1.6's UI half not shown in the new files | Already pinned: the Settings tab, and with it the Refinement section, is hidden from readers by the existing `TeamDetail.test.tsx` RBAC cases; the server half is `Only_a_Team_admin_can_change_the_refinement_states`. Done is the non-candidate the form test proves absent |
| (low) DST-9, DST-10, DST-13 are product-facing | Listed for maintainer confirmation in `distill/wave-decisions.md` |
| (low) Age 4 for a start three days ago unexplained | Comment added |
| (low) Two preservation/isolation cases tagged `@error` | Retagged `@boundary`; error + boundary share unchanged at 39 / 66 |
| (low) Wall-clock budget, `toHaveLength(2)` on the Doing note, enum-order reflection | Kept as written; noted for DELIVER |

## Wave: DISTILL / [REF] Scope and Reconciliation — E3 (#5510)

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, maintainer AFK.
**Scope**: **E3 only — Epic #5510 Sizing votes**: slices 10 (US-10, #6148), 11 (US-11, #6149), 12 (US-12, #6150),
13 (US-13, #6151), 14 (US-14, #6152), 15 (US-15, #6153), 16 (US-16, #6154), 17a (US-17a, #6155), 17b (US-17b, #6156).
E1's DISTILL sections above are left as written; E2, E4, E5 are not distilled.

**Reconciliation passed — 0 contradictions** across DISCUSS, DESIGN and DEVOPS for E3. DESIGN's corrections to
AC-10.1 (Throughput window), AC-11.2 (name plus voter key), AC-11.4/AC-15.2 (`TeamContribute`, 404 for non-readers),
AC-12.2 (open question until the asker votes), AC-13.2 (votes alone until 03/08) and the client command names, and
DEVOPS's `sizingMoment` enum and rate-limit partition, are applied as written in the two `upstream-changes.md` files.
Settled maintainer calls re-checked against every scenario: votes always open; async is pull; all Community, named
votes only with sign-in; minimum Yes ≥ 1; the SLE fallback is the default cycle time's 85th percentile over the
Throughput window; every word through Terminology; votes join the shared grid as columns. Decisions DST-15..DST-35:
`distill/wave-decisions.md` → "E3 Sizing votes".

## Wave: DISTILL / [REF] Maintainer decision — the slice-10 question (2026-10-03)

Approved by the maintainer on 2026-10-03; **supersedes the US-10 elevator-pitch copy and AC-10.2** (DST-15). The tab
shows the existing heading ("{count} {Work Items} in {Refinement}") and **exactly one more line, never more**: the
question followed by an info icon (ⓘ) whose detail appears on hover. No visible hint line, no settings link.

| Team | Line | Tooltip |
|---|---|---|
| SLE 75% / 7 days | Doable within 7 days? ⓘ | SLE 75% of work items in 7 days or less |
| No SLE, fallback P85 = 12 days | Doable within 12 days? ⓘ | No SLE set, based off 85% of historical cycle time |
| No SLE, nothing finished | Doable within our SLE? ⓘ | No SLE is set and no Work Items have finished yet |

SLE, Cycle Time, Work Items and Refinement come from Terminology (seeded defaults shown). The server answers facts
`{source: Sle | CycleTimeFallback | Unavailable, days?, probability?}`; the browser composes sentence and tooltip.
The frontend scenarios assert the question, the info icon's accessible tooltip text, and that nothing else sits
between the heading and the list.

**Amended by the maintainer, 2026-10-03 (later the same day):** the SLE tooltip uses the Work Items term, not the
literal "items" (lower-cased mid-sentence, like Cycle Time in the fallback tooltip). And for slices 11–12: a plain Yes
or No may also carry an optional comment explaining why, because voting is asynchronous and the reason helps answer
points raised earlier. Yes and No still record in one click; adding a comment is optional, never a prompt. This
replaces DST-24 ("the UI prompts for a comment only on Yes, but…"); "Yes, but…" keeps its prompt. The exact control is
sketched before slice 11 is built.

The conditional answer is labelled **"Yes, if…"** (maintainer, 2026-10-03), replacing "Yes, but…" everywhere a user
reads it; the three answers read Yes · Yes, if… · No. Only the label changes; the stored answer value stays as it is.

## Wave: DISTILL / [REF] Maintainer decision — the voting UI, and slice 14 dropped (2026-10-04)

Approved by the maintainer on 2026-10-04 from sketches, before slices 11–16 were built. **Supersedes DST-22's
provisional copy where they differ, and drops slice 14 (US-14, #6152 Removed).**

**Slice 14 is dropped: every vote and every comment is visible to everyone, voted or not.** A Product Owner who does not
vote still needs to read how the Team voted. "Not at first glance" is enough: the grid shows only the count, and the
split and the comments sit one click away in the Votes dialog. The API never hides the split or the comments; the
`split` on a row and the log entries are always returned. The slice-14 scenarios are deleted, and every other scenario
that read "hidden until you vote" now reads "visible".

The grid (the shared `DataGridBase`, after Work Item / Parent / State):

```
3 Work Items in Refinement · 1 ready by votes
Doable within 7 days? ⓘ

Work Item           Parent  State  Your vote                 Votes          Readiness
GR-051 PDF export   GR-010  Next   [Yes] [Yes, if…] [No]     3 votes 💬 ❓   2 more Yes needed
GR-054 Bulk import  GR-010  Next   [Yes] [Yes, if…] [■No]    2 votes        Needs discussion
GR-073 SSO login    GR-012  Ready  [■Yes] [Yes, if…] [No]    3 votes        Ready
```

- **Your vote** (slice 11): three one-click buttons, the voter's own answer `aria-pressed`; clicking another changes it.
  Yes and No record in one click. **Yes, if…** (slice 12) opens a dialog with an optional "Condition" textbox, Cancel / Vote.
- **Votes** (slice 11, markers in 12): "No votes" / "1 vote" / "3 votes", with a comments marker and an open-question
  marker. Clicking the cell opens **Votes and comments**: the split "3 Yes · 0 Yes, if… · 1 No", the log oldest first
  ("Wed 7 Oct", "via the command line" / "via an assistant", "Jonas Weber took back their vote"), "Ask a question",
  a footer "Voting as Ana Lima · Change your name" (auth off), "Take back my vote" (slice 16), Close. A comment added
  there by someone who has a current vote is a plain comment, which is how a Yes or No carries its optional reason
  from the UI.
- **Who is voting?** (slice 11, auth off): the first vote opens a dialog with "Your name", "Kept in this browser only.",
  Cancel / Vote. Name and a random voter key are kept in localStorage.
- **Readiness** (slice 13): its own column after Votes: "Ready" / "2 more Yes needed" / "1 more voter needed" /
  "Needs discussion". Settings → Refinement gains "Readiness by votes": Yes votes needed [3], Voters needed [3],
  a "Send to discussion at [1] or more" checkbox with Counting (•) No ( ) No or Yes, if…, and the two errors of DST-22.

DEVOPS points settled the same day: the per-browser KPI proxies (DVO-3) and K4/K5 leaving out client votes are
accepted; the vote-write rate limit stays as DVO-5 partitions it. **No usage-data event ever carries personal data**:
no IP, no voter name, no voter key, no account id, no free text. Each event emitted by these slices has a test that
reads the forwarded payload and finds only the event name and its closed-enum property.

## Wave: DISTILL / [REF] Scenario list with tags — E3 (#5510)

All cases are pending (`[Ignore(PendingSlice1n)]` / `IgnoreReason = PendingSlice1n`, `it.skip`); the E2E skeleton is
`fixme`. **196 runnable cases, 122 error/boundary (62%)**: backend 132, frontend 64.

| Slice | Cases (backend + frontend) | Error / boundary |
|---|---|---|
| 10 yardstick | 20 (9 + 11) | 15 |
| 11 cast a vote | 51 (35 + 16) | 37 |
| 12 comments and the log | 30 (19 + 11) | 16 |
| 13 readiness | 41 (26 + 15) | 25 |
| 14 hidden split | 13 (10 + 3) | 6 |
| 15 votes with an account | 12 (9 + 3) | 7 |
| 16 take back | 16 (11 + 5) | 9 |
| 17a clients read (Lighthouse half) | 3 (3 + 0) | 2 |
| 17b clients cast (Lighthouse half) | 10 (10 + 0) | 5 |

Every backend case is `@driving_port @real-io` with a `@contract-shape:` tag in the source; the tables below drop
those three for width.

**Backend — `Slice10SleYardstickTest`**

| Scenario | Tags |
|---|---|
| The Team's SLE is the yardstick every voter answers against | `@us-10 @slice-10` |
| Without an SLE the yardstick is the 85th percentile of the Team's cycle time | `@us-10 @slice-10` |
| Work Items finished before the Throughput window do not move the fallback | `@us-10 @slice-10 @boundary` |
| No SLE and no finished Work Items leaves the question without a number | `@us-10 @slice-10 @error` |
| Work finished only before the Throughput window counts as nothing finished | `@us-10 @slice-10 @boundary` |
| Half an SLE is no SLE and the fallback is used | `@us-10 @slice-10 @boundary` |
| Setting an SLE replaces the fallback on the next read | `@us-10 @slice-10` |
| Every voter is shown the same yardstick | `@us-10 @slice-10 @boundary` |

**Backend — `Slice11CastAVoteTest`**

| Scenario | Tags |
|---|---|
| A reader votes in seconds without an account | `@us-11 @slice-11 @kpi-OUT-5510-K4-votes-outside-the-meeting` |
| Changing one's mind replaces the current vote and still counts once | `@us-11 @slice-11` |
| Every voter counts once and sees only their own answer as theirs | `@us-11 @slice-11` |
| Two people who declare the same name are two voters | `@us-11 @slice-11 @boundary` |
| Votes are open on every Work Item in refinement | `@us-11 @slice-11 @boundary` |
| A Work Item that is not in refinement cannot be voted on | `@us-11 @slice-11 @error` |
| A vote without a name is refused and nothing is counted | `@us-11 @slice-11 @error` |
| A vote without a voter key is refused and nothing is counted | `@us-11 @slice-11 @error` |
| A declared name of one hundred characters is accepted | `@us-11 @slice-11 @boundary` |
| A declared name longer than one hundred characters is refused | `@us-11 @slice-11 @error` |
| An answer other than Yes Yes, but or No is refused | `@us-11 @slice-11 @error` |
| A vote that does not say where it was cast from is refused | `@us-11 @slice-11 @error` |
| The voter key never comes back in any answer | `@us-11 @slice-11 @boundary` |
| A vote for a Team that does not exist is not found | `@us-11 @slice-11 @error` |
| Votes outlive a Work Item leaving refinement and count again when it returns | `@us-11 @slice-11 @boundary` |
| Voting changes nothing about the Team or its Work Items | `@us-11 @slice-11` |
| A voter sending more than thirty entries a minute is told to slow down | `@us-11 @slice-11 @error` |
| Without sign in the tab says a voter declares their name | `@us-11 @slice-11` |

**Backend — `Slice12CommentsTest`**

| Scenario | Tags |
|---|---|
| A Yes, but carries its condition into the Work Item's log | `@us-12 @slice-12` |
| Any answer may carry a comment | `@us-12 @slice-12` |
| A vote without a comment carries none | `@us-12 @slice-12 @boundary` |
| A changed mind keeps both votes in the log oldest first | `@us-11 @us-12 @slice-12` |
| The log says who said what when and from where oldest first | `@us-12 @slice-12` |
| A question without a vote flags the Work Item and counts as no vote | `@us-12 @slice-12` |
| The asker voting closes their own question | `@us-12 @slice-12` |
| Somebody else's vote does not close the question | `@us-12 @slice-12 @boundary` |
| A comment from somebody who has voted is no open question and keeps their vote | `@us-12 @slice-12 @boundary` |
| A question counts for nothing towards readiness | `@us-12 @us-13 @slice-12 @boundary` |
| An empty question is refused and nothing is recorded | `@us-12 @slice-12 @error` |
| A comment of two thousand characters is kept whole | `@us-12 @slice-12 @boundary` |
| A comment longer than two thousand characters is refused | `@us-12 @slice-12 @error` |
| A question without a name is refused | `@us-12 @slice-12 @error` |
| A question on a Work Item that is not in refinement is refused | `@us-12 @slice-12 @error` |
| The log of a Work Item that is not in refinement is not found | `@us-12 @slice-12 @error` |
| A comment comes back exactly as it was written | `@us-12 @slice-12 @boundary` |

**Backend — `Slice13ReadinessTest`**

| Scenario | Tags |
|---|---|
| A Team that never chose readiness needs three Yes from three voters and has no veto | `@us-13 @slice-13 @boundary` |
| A Team admin sets readiness and it reads back | `@us-13 @slice-13` |
| Fewer than one Yes is refused and nothing is saved | `@us-13 @slice-13 @error` |
| Fewer voters than Yes votes is refused and nothing is saved | `@us-13 @slice-13 @error` |
| As many voters as Yes votes is accepted | `@us-13 @slice-13 @boundary` |
| A veto of zero votes is refused and nothing is saved | `@us-13 @slice-13 @error` |
| A save that says nothing about readiness leaves it as it was | `@us-13 @slice-13 @boundary` |
| Changing readiness keeps every Work Item the Team holds | `@us-13 @slice-13 @boundary` |
| Enough Yes votes make a Work Item Ready and the tab counts it | `@us-13 @slice-13 @kpi-OUT-5510-K5-ready-before-the-day` |
| The missing Yes votes are named | `@us-13 @slice-13` |
| A Work Item nobody has voted on needs every Yes | `@us-13 @slice-13 @boundary` |
| A No does not count towards the Yes votes | `@us-13 @slice-13 @boundary` |
| Enough Yes votes from too few voters name the missing voters | `@us-13 @slice-13 @boundary` |
| Any answer from the missing voter makes the Work Item Ready | `@us-13 @slice-13` |
| A veto sends a Work Item to discussion however many say Yes | `@us-13 @slice-13 @error` |
| A veto counts Yes, but only when the Team says so | `@us-13 @slice-13 @boundary` |
| A veto of two is not tripped by one No | `@us-13 @slice-13 @boundary` |
| A No changed to Yes lifts the veto | `@us-13 @slice-13` |
| Lowering readiness makes a Work Item Ready on the next read | `@us-13 @slice-13` |
| The tab counts only the Work Items the votes made Ready | `@us-13 @slice-13` |

**Backend — `Slice14HiddenSplitTest`**

| Scenario | Tags |
|---|---|
| The split is hidden from somebody who has not voted | `@us-14 @slice-14` |
| Voting reveals the split | `@us-14 @slice-14` |
| The log stays closed to somebody who has not voted | `@us-14 @slice-14` |
| Voting opens the log | `@us-14 @slice-14` |
| Asking a question does not reveal the split | `@us-14 @slice-14 @boundary` |
| Having voted on one Work Item reveals nothing about another | `@us-14 @slice-14 @boundary` |
| Each voter sees the split only where they have voted themselves | `@us-14 @slice-14 @boundary` |
| A Ready Work Item shows Ready to somebody who has not voted | `@us-13 @us-14 @slice-14 @boundary` |
| A reader who brings no voter key sees counts and readiness only | `@us-14 @us-17a @slice-14 @boundary` |

**Backend — `Slice14HiddenFromTeamAdminsTest`**

| Scenario | Tags |
|---|---|
| A Team admin who has not voted sees no split and no log either | `@us-14 @slice-14 @error` |

**Backend — `Slice15VotesWithAnAccountTest`**

| Scenario | Tags |
|---|---|
| A signed in reader votes under their account without giving a name | `@us-15 @slice-15` |
| A name sent with a signed in vote is ignored | `@us-15 @slice-15 @error` |
| One account is one voter whichever browser it votes from | `@us-15 @slice-15 @boundary` |
| A Team admin votes like any reader | `@us-11 @us-15 @slice-15` |
| Two accounts with the same name are two voters | `@us-15 @slice-15 @boundary` |
| Somebody without a role on the Team can neither open the tab nor vote | `@us-15 @slice-15 @error` |
| Somebody without a role on the Team can neither read the log nor comment | `@us-12 @us-15 @slice-15 @error` |
| With sign in the tab says a voter is known by their account | `@us-15 @slice-15` |

**Backend — `Slice15VotesWithoutRolesTest`**

| Scenario | Tags |
|---|---|
| Every signed in person votes when roles are not enforced | `@us-15 @slice-15` |

**Backend — `Slice16TakeBackTest`**

| Scenario | Tags |
|---|---|
| A taken back vote stops counting and the log keeps both | `@us-16 @slice-16` |
| Taking back a vote you do not have records nothing | `@us-16 @slice-16 @boundary` |
| Taking back twice records one take back | `@us-16 @slice-16 @boundary` |
| Nobody can take back somebody else's vote even under their name | `@us-16 @slice-16 @error` |
| Taking back without a voter key is refused | `@us-16 @slice-16 @error` |
| Taking back on a Work Item that is not in refinement is refused | `@us-16 @slice-16 @error` |
| Taking back a Yes can cost a Work Item its Ready | `@us-13 @us-16 @slice-16 @boundary` |
| Voting again after taking back counts again | `@us-16 @slice-16` |
| Taking back hides the split again | `@us-14 @us-16 @slice-16 @boundary` |

**Backend — `Slice16TakeBackWithAnAccountTest`**

| Scenario | Tags |
|---|---|
| Any session of the account that voted can take the vote back | `@us-15 @us-16 @slice-16` |
| Somebody without a role on the Team cannot take anything back | `@us-15 @us-16 @slice-16 @error` |

**Backend — `Slice17ClientVotesTest`**

| Scenario | Tags |
|---|---|
| A client is told which Work Items need discussion and which are Ready | `@us-17a @slice-17a` |
| A client whose user has not voted is not told the split | `@us-14 @us-17a @slice-17a @error` |
| A client whose user voted through it is told the split | `@us-14 @us-17a @slice-17a @boundary` |
| A voter casts a Yes, but with its condition from the command line | `@us-17b @slice-17b` |
| A vote cast through an assistant is marked as cast through an assistant | `@us-17b @slice-17b` |
| A client vote without a name is refused and nothing is recorded | `@us-17b @slice-17b @error` |
| A question asked through an assistant flags the Work Item | `@us-12 @us-17b @slice-17b` |
| A client takes back the vote it cast | `@us-16 @us-17b @slice-17b` |
| A client cannot take back a vote its user cast from a browser | `@us-16 @us-17b @slice-17b @error` |

**Backend — `Slice17ClientVotesWithAnApiKeyTest`**

| Scenario | Tags |
|---|---|
| A personal API key votes under its owner's name | `@us-15 @us-17b @slice-17b` |
| A credential that belongs to no person cannot add to the log | `@us-15 @us-17b @slice-17b @error` |

**Backend — `TeamSizingUsageEventsTests`**

| Scenario | Tags |
|---|---|
| A browser that agreed reports the event with its name and when it happened and nothing else | `@us-11 @slice-11 @kpi-OUT-5510-K4-votes-outside-the-meeting` |
| The event without when it happened is refused | `@us-11 @us-13 @error` |
| The event carrying anything more is refused | `@us-11 @us-13 @error` |
| A vote cast at a moment not on the list is refused | `@us-11 @slice-11 @error` |
| Another event carrying a sizing moment is refused | `@us-11 @slice-11 @error` |
| The sizing events are appended to the list of names never inserted | `@us-11 @us-13` |

**Frontend — Vitest + RTL** (64 cases)

| File | Scenarios | Tags |
|---|---|---|
| `Refinement/RefinementView.yardstick.test.tsx` | asks against the SLE, tooltip says the SLE · fallback, tooltip says it is one · no number, tooltip says why · exactly one line between heading and list, no link (×3) · one day singular · asks nothing once nothing is in refinement · the Team's own words in question and tooltip (×3) | `@us-10 @slice-10`, 1 `@error`, 8 `@boundary` |
| `Refinement/RefinementView.votes.test.tsx` | three answers and a count on every row · first vote asks the name, vote under it, marked as own · name remembered, one click · only name and key kept in the browser · closing the prompt casts nothing · no blank name · change the name later votes carry · refused vote leaves the row and says why · 1 vote / 3 votes · each accepted vote reported (`TeamSizingVoteCast`, `NoCadence`) · refused vote reports nothing · signed in: no prompt, nothing stored · signed in: no name to change · count only before voting · split after voting · take back offered only on own vote · take back from this browser · failed take back keeps the vote | `@us-11 @us-14 @us-15 @us-16`, 5 `@error`, 3 `@boundary`, `@kpi-OUT-5510-K4-votes-outside-the-meeting` ×3 |
| `Refinement/RefinementView.comments.test.tsx` | "Yes, but…" asks for the condition · empty condition still votes · ask a question without voting · no empty question · open question marked · log oldest first with names and days · comment shown as text, never markup · via the command line / an assistant · taken back shown · hidden log says only how many · unreadable log says why | `@us-12 @us-14 @us-16 @us-17b`, 3 `@error`, 1 `@boundary` |
| `Refinement/RefinementView.readiness.test.tsx` | Ready / n more Yes / n more voters / Needs discussion · singular and plural · readiness shown without the split · heading counts ready by votes on its own line · 0 ready by votes · tipping vote reports `TeamSizingReadinessReached` · already Ready reports nothing · still short reports nothing | `@us-13 @slice-13`, 5 `@boundary`, `@kpi-OUT-5510-K5-ready-before-the-day` ×4 |
| `components/Common/Team/ModifyTeamSettings.readiness.test.tsx` | defaults 3 / 3 / no veto · changed Yes saved with the rest · fewer than one Yes refused · fewer voters than Yes refused · veto after one No · veto counting "Yes, but…" · opening saves nothing | `@us-13 @slice-13`, 2 `@error`, 1 `@boundary` |
| `services/Api/SizingLogService.test.ts` | vote: address, body, key header, the row back · no header without a key · reference escaped · refused vote passed on · question to comments · log read with the key · take back with the key | `@us-11 @us-12 @us-15 @us-16`, 1 `@error`, 2 `@boundary` |
| `services/Api/RefinementService.voterKey.test.ts` | tab read with the stored key, none before · unreadable store sends none, then the key | `@us-11 @us-14 @slice-11`, 1 `@error` |

**E2E — `specs/teams/Refinement.spec.ts`** (second skeleton, `fixme`): *a voter gives a name, says Yes on a Work Item
in refinement and the votes make it Ready* — `@walking_skeleton @driving_port @us-11 @us-13 @slice-11 @slice-13
@kpi-OUT-5510-K4-votes-outside-the-meeting`. Given: demo scenario 12 with GR-059 holding Yes from Jonas Weber and Mo
Okafor (demo seeding is DELIVER's, slices 11/13). POM `TeamRefinementPage` gains `answerButton()` and `vote()`.

## Wave: DISTILL / [REF] WS strategy — E3 (#5510)

Architecture of Reference + project policy (inherited, nothing appended). One more walking skeleton, the E2E above,
as DVO-7 specifies — real browser → real app → seeded demo data, through POMs. Backend scenarios run the production
composition root (`WebApplicationFactory<Program>` over real EF) on three instances: without sign-in (every Community
instance), sign-in with roles enforced, sign-in without roles (with the product's own API-key handler). Only the
licence and the instance clock are faked; the usage-data collector is captured. Tier B: not declared (DST-31).

## Wave: DISTILL / [REF] Test placement — E3 (#5510)

| Where | Why (precedent) |
|---|---|
| `Lighthouse.Backend.Tests/API/Integration/Refinement/` — harness `SizingVotesAcceptanceTest` + `Slice10SleYardstick…`, `Slice11CastAVote…`, `Slice12Comments…`, `Slice13Readiness…`, `Slice14HiddenSplit…`, `Slice15VotesWithAnAccount…`, `Slice16TakeBack…`, `Slice17ClientVotes…` (`Scenarios.cs` + `Specifications.cs` each) | E1's folder and partial-class split |
| `Lighthouse.Backend.Tests/Integration/UsageData/TeamSizingUsageEventsTests.cs` | Beside E1's `TeamRefinementUsageEventsTests` |
| `Lighthouse.Frontend/src/pages/Teams/Detail/Refinement/RefinementView.{yardstick,votes,comments,readiness}.test.tsx`, `src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx`, `src/services/Api/SizingLogService.test.ts`, `src/services/Api/RefinementService.voterKey.test.ts`, shared kit `src/tests/RefinementTabTestKit.tsx` | E1's colocated `<component>.<concern>.test.tsx`; kit beside `RealityCheckFixture.tsx` |
| `Lighthouse.EndToEndTests/tests/specs/teams/Refinement.spec.ts` + POM `models/teams/TeamRefinementPage.ts` | E1's spec and POM |

## Wave: DISTILL / [REF] Driving adapter coverage — E3 (#5510)

| Driving adapter (DESIGN) | Covered by |
|---|---|
| `GET /teams/{teamId}/refinement` → `yardstick`, `voterIdentity`, `readyByVotesCount`, row vote facts | slices 10, 11, 13, 14, 15, 17a; frontend via `RefinementService`; E2E |
| `POST …/work-items/{workItemId}/votes` | slices 11–17b; `SizingLogService.castVote`; E2E |
| `POST …/work-items/{workItemId}/comments` | slices 12, 15, 17b; `SizingLogService.addComment` |
| `DELETE …/work-items/{workItemId}/votes/mine` | slices 16, 17b; `SizingLogService.takeBackMyVote` |
| `GET …/work-items/{workItemId}/log` | slices 12, 14, 15, 16, 17b; `SizingLogService.getLog` |
| `PUT /teams/{teamId}` carrying `refinement.readiness`; `GET …/settings` | slice 13; `ModifyTeamSettings.readiness` |
| `POST /usagedata/events` (`TeamSizingVoteCast`, `TeamSizingReadinessReached`, `sizingMoment`) | `TeamSizingUsageEventsTests`; frontend emission in `votes` / `readiness` |
| CLI `lh refinement …`, MCP `lighthouse_team_refinement_*` | **not here** — `lighthouse-clients`, with slice 09 (DST-25) |

## Wave: DISTILL / [REF] Adapter coverage — E3 (#5510)

| Driven adapter | Real I/O scenario |
|---|---|
| EF `SizingLogEntries` (M2, via `ISizingLogRepository`) | every vote/comment/take-back scenario reads its effect back through the tab or the log (real EF, provider per CI leg) |
| EF `Teams.RefinementSettings` readiness member | slice 13 save/read-back |
| `ITeamMetricsService.GetCycleTimePercentilesForTeam` | slice 10 fallback scenarios over seeded finished Work Items |
| `IAuthModeResolver` / `ICurrentUserProfileService` / `ApiKeyAuthenticationHandler` | slices 11 (off), 15 and 17b (on, with and without roles, personal and unowned keys) |
| Rate limiter (`RefinementContribution`, shipped `appsettings.json`) | slice 11, 31st entry → 429 |
| Usage-data forwarding (collector captured) | `TeamSizingUsageEventsTests` |

No external adapter is added (DESIGN: contract testing N/A). M2 itself is DELIVER's (`CreateMigration`); its
database-level cascade / SET NULL assertion is DELIVER 11's (DEVOPS).

## Wave: DISTILL / [REF] Scaffolds — E3 (#5510)

| File | Marker | Behaviour until DELIVER |
|---|---|---|
| `Lighthouse.Frontend/src/services/Api/SizingLogService.ts` | `__SCAFFOLD__` | every method throws `Not yet implemented -- RED scaffold` |
| `Lighthouse.Frontend/src/models/Refinement/Refinement.ts` | types only | new members optional, so E1 code and tests compile unchanged |
| `IApiServiceContext.sizingLogService` (+ default and mock entries) | additive | read by no production code yet |

Backend: none — the scenarios are black-box over HTTP/JSON (as E1). No EF migration (M2 is DELIVER 11's).

## Wave: DISTILL / [REF] Pre-requisites — E3 (#5510)

DESIGN: the driving ports above, `TeamContribute` (DSN-13), voter identity (DSN-12), the log shape (DSN-11, M2),
resolution precedence (DSN-14/15/16). DEVOPS: environments `auth-off`, `auth-on-rbac-off`, `auth-on-rbac-on`,
`renamed-terminology`, `usage-data-consented`; E2E in `ci_verifysqlite` / `ci_verifypostgres`. DELIVER owns: M2 via
`CreateMigration` and its FK assertion, demo votes (DST-33), `docs/settings/usagedata.md` rows for both events and the
`sizing_moment` field, `ARCHITECTURE.md` (sizing log, reader write), the UI sketch shown to the maintainer before any
vote control is built (DST-22).

## Wave: DISTILL / [REF] RED classification — E3 (#5510)

196 of 196 runnable cases fail for `MISSING_FUNCTIONALITY` when un-skipped (backend 132, frontend 64); none broken.
Three cases that came back green on the first run were rewritten to prove something first; refusal cases a missing
route would also answer start from a permitted call that succeeds. E2E type-checked, not run live. Detail:
`distill/red-classification.md` → "Epic #5510 (E3)".

## Wave: DISTILL / [REF] Delivery order — E3 (#5510)

DD-22's sequence, E3 slices only: **10 → 11 → 13**, then (E2's 03–06) **12**, (07) **15 → 14**, (08) **16**, (09)
**17a → 17b**. Slice 10 un-skips `Slice10SleYardstickTest` and `RefinementView.yardstick`; 11 its backend fixture,
the slice-11 usage cases, the first block of `RefinementView.votes`, the slice-11 `SizingLogService` cases and
`RefinementService.voterKey`; 13 its backend fixture, the slice-13 usage cases, `RefinementView.readiness`,
`ModifyTeamSettings.readiness` and then the E2E skeleton (run live first); and so on. One scenario at a time.

## Wave: DELIVER / [REF] Implementation summary — E1 (#6136)

**Scope**: E1 only — Epic #6136, slice 01 (US-01, #6139) and slice 02 (US-02, #6140). E2–E5 are not delivered; this
workspace stays in place for them. **Delivered** 2026-10-03, 26 roadmap steps (01-01 … 01-14, 02-01 … 02-12, of which
01-12, 01-13, 01-14, 02-10, 02-11 and 02-12 came from the maintainer's slice reviews), every step RED → GREEN →
COMMIT in the DES step log under `deliver/` (02-12's RED skipped as not applicable: the page objects already read
the grid). Then refactor (4 commits), adversarial review revision (7 commits), mutation (1 commit). On `main` as
`fd9df2bca..c5cb52587`. Evolution record: `docs/evolution/2026-10-03-epic-6136-refinement-tab.md`.

A Team admin picks refinement states as chips (To Do and Doing states only) under Settings → Refinement; the
Refinement tab between Metrics and Settings is switched off with a role-specific tooltip until a Team has some, and
then lists the Work Items in those states in the shared Work Item grid (Name, Parent, State), backlog order by
default, sortable. A state that stops being To Do or Doing leaves the refinement states in the same save.

## Wave: DELIVER / [REF] Files modified — E1 (#6136)

| Area | Files |
|---|---|
| Backend — model and storage | `Models/Refinement/RefinementSettings.cs` (new), `Models/Team.cs`, `Data/LighthouseAppContext.cs`; migrations `AddRefinementSettingsToTeams` (SQLite `20261003114145`, Postgres `20261003114155`) + snapshots |
| Backend — settings write | `API/Helpers/RefinementSettingsValidator.cs` (new), `API/Helpers/TeamExtensions.cs` (`SyncRefinement`, save-time pruning), `API/DTO/RefinementSettingsDto.cs` (new), `API/DTO/TeamSettingDto.cs`, `API/DTO/TeamDto.cs` (`refinementConfigured`), `API/TeamController.cs`, `API/TeamsController.cs` (validate on create) |
| Backend — Refinement module (read) | `API/RefinementController.cs`, `API/DTO/RefinementViewDto.cs`, `Services/Interfaces/Refinement/IRefinementViewQuery.cs`, `Services/Implementation/Refinement/RefinementViewQuery.cs`, `Services/Implementation/Refinement/RefinementList.cs` (all new), `Program.cs` (registration) |
| Backend — cross-cutting | `Models/UsageData/UsageDataEventName.cs` (`TeamRefinementConfigured = 12`), `UsageDataRouteKey.cs` (`TeamDetail_Refinement = 10`), `UsageDataRoutePatterns.cs`, `Services/Implementation/Seeding/TerminologySeeder.cs` (`refinement`, `refinements`), `Factories/DemoDataFactory.cs` (Team Gravity) |
| Backend — tests | `API/Integration/Refinement/` (harness + Slice01/Slice02 scenarios and specifications), `Integration/UsageData/TeamRefinementUsageEventsTests.cs`, `Services/Implementation/Refinement/RefinementListTest.cs`, `API/Helpers/RefinementSettingsValidatorTest.cs`, `Architecture/RefinementModuleArchUnitTest.cs` (new); `FeatureOrderingSingleSourceArchUnitTest`, `ModuleBoundariesArchUnitTest`, `FetchShapingPropertyGuardTest`, `StateMappingSyncTest`, `DemoDataFactoryTest`, `TerminologySeederTests`, `UsageDataRoutePatternsTests`, `Slice04ProductEventsTests`, `HistoricalSchemaPatch` (extended) |
| Frontend — settings | `components/Common/Team/RefinementSettingsSection.tsx` (new), `ModifyTeamSettings.tsx`, `pages/Teams/Edit/EditTeam.tsx`, `hooks/useRefinementSetUpReporter.ts` (new), `models/Team/TeamSettings.ts` |
| Frontend — tab and list | `pages/Teams/Detail/TeamDetail.tsx`, `pages/Teams/Detail/Refinement/RefinementView.tsx` (new), `services/Api/RefinementService.ts` (new), `models/Refinement/Refinement.ts` (new), `models/Team/Team.ts`, `services/Api/ApiServiceContext.ts`, `components/Common/FeatureListDataGrid/columns.tsx` and `hooks/useParentWorkItems.ts` (widened to Work Items) |
| Frontend — cross-cutting | `models/TerminologyKeys.ts`, `services/TerminologyContext.tsx`, `models/UsageData/UsageData.ts`, `services/Api/UsageDataService.ts`, `services/UsageData/usageDataRouteKeys.ts`, `tests/MockApiServiceProvider.ts` |
| Frontend — tests | `TeamDetail.refinementTab.test.tsx`, `RefinementView.test.tsx`, `ModifyTeamSettings.refinement.test.tsx`, `usageDataRouteKeys.refinement.test.ts`, `RefinementService.test.ts`, `useRefinementSetUpReporter.test.ts` (new); `EditTeam.test.tsx` and three existing specs extended |
| E2E | `specs/teams/Refinement.spec.ts`, POM `models/teams/TeamRefinementPage.ts` (new), `TeamDetailPage.ts` |
| Docs | `docs/settings/usagedata.md`, `ARCHITECTURE.md` (eighth module), `CLAUDE.md` (sketch-first rule), this workspace's `deliver/`, `distill/`, `mutation/` |

## Wave: DELIVER / [REF] Scenarios green — E1 (#6136)

Nothing pending remains: no `[Ignore(PendingSlice0n)]`, no `it.skip`, no `test.fixme` in the E1 files.

| Suite | Count |
|---|---|
| Backend acceptance — `Slice01RefinementStatesScenarios` | 16 tests + 2 parameterised cases (rewritten for auto-remove; review additions: case-insensitive duplicates, refused on create) |
| Backend acceptance — `Slice02RefinementListScenarios` | 15 tests (rows now name, link, state, parent; case-insensitive state match added by the review) |
| Backend — `TeamRefinementUsageEventsTests` | 4 tests + 2 cases |
| Backend unit / architecture | `RefinementListTest` 2 + 9 cases, `RefinementSettingsValidatorTest` 5 + 11 cases, `RefinementModuleArchUnitTest` 5 |
| Frontend | `TeamDetail.refinementTab` 15, `RefinementView` 13, `ModifyTeamSettings.refinement` 11, `usageDataRouteKeys.refinement` 2, `RefinementService` 2, `useRefinementSetUpReporter` 1, plus the refinement block of `EditTeam.test.tsx` |
| E2E | 1 walking skeleton, green live three times |

Whole suites after the review revision (`1e4cb29e3`): backend 7780 passed / 0 failed / 1 skipped (connector
categories excluded), frontend 6039 passed / 0 failed.

## Wave: DELIVER / [REF] DoD check against US-01 / US-02 — E1 (#6136)

| AC | Status |
|---|---|
| AC-1.1 only To Do and Doing states offered, each with its category | **Met, changed by the maintainer**: offered as chip suggestions (To Do and Doing only, server-enforced on update and create); the "(To Do)" / "(Doing)" labels went with the checkboxes |
| AC-1.2 saved selection survives reload and a Team refresh | **Met**: settings round-trip on both providers; a refresh never writes Team settings |
| AC-1.3 tab disabled with role-specific tooltip until configured | **Met** |
| AC-1.4 a state that becomes unmapped is flagged, never dropped | **Changed by the maintainer (2026-10-03)**: the state is removed in the save that takes it out of To Do / Doing (reverses DST-2, DST-3) |
| AC-1.5 Doing note "already counts in WIP and Cycle Time" | **Removed by the maintainer** (step 01-14) |
| AC-1.6 only Team admins can save | **Met**: server `TeamWrite`; the Settings tab is hidden from readers through `useRbac()` |
| AC-2.1 exactly the Work Items in the refinement states, backlog order | **Met**, backlog order = the tracker's rank ladder, ties by id (DSN-6); sorting by column is allowed with this as the default (maintainer) |
| AC-2.2 each row: id + link, name, state, category, age | **Changed by the maintainer**: Name (id + name, linked), Parent, State; category and age removed from the table and the API |
| AC-2.3 "Refinement" is a Terminology key used on tab, heading, tooltip | **Met** (also the settings section) |
| AC-2.4 readable by anyone with Team read | **Met**, non-disclosing 404 otherwise |
| AC-2.5 renders and stays responsive with 300 Work Items | **Met**: backend under 2 s at 300 rows; the grid is the virtualised shared grid |
| AC-2.6 demo Gravity configured, one other demo Team not | **Met**: Gravity `Backlog`, `Analysing`, `Next`; Zenith and the rest unconfigured; `DemoDataFactoryTest` |

## Wave: DELIVER / [REF] Quality gates — E1 (#6136)

- Backend build zero warnings; backend and frontend suites green (numbers above); `pnpm build` and Biome clean.
- E2E walking skeleton green live (×3).
- Refactor pass: redundant list filter removed, test helpers shared, `useErrorSnackbar`, naming; Sonar S3776 max 12.
- Adversarial review: **needs_revision**, 1 high / 3 medium / 4 low; all 8 fixed with a failing-first test each.
- Mutation (gate 80 %): backend **90.00 %** on the feature's code, frontend **91.41 %** — `mutation/results.md`.
- CI on `c5cb52587`: running at finalize; `/clean-ci` owned by the orchestrator.
- DELIVER checklist: docs prose **done** (`9b2602cf0`: Team edit and detail pages, concepts, Terminology);
  screenshots **deferred by the maintainer** until the next Epic has changed the tab; demo data **done**; website assets
  **deferred, and N/A for E1** (DISCUSS placed the website change at E2/E3 finalize); usage-data event in
  `docs/settings/usagedata.md` **done**; Lighthouse-Clients **N/A for E1** (first client surface is slice 09).

## Wave: DELIVER / [REF] Pre-requisites for what follows — E1 (#6136)

- **E2 (#5881) and later build on**: `Team.RefinementSettings` (members are added with defaults, never renamed —
  ADR-214); the `Refinement` module and its ArchUnit rules; `RefinementList` as the one source of "what is in
  refinement, in backlog order"; the grid in `RefinementView` (vote columns join it in E3).
- **ADO**: #6139 and #6140 Resolved; Epic #6136 stays open until the release carrying it.
- **Owed**: screenshots for the tab and the settings section (maintainer deferred); KPI baselines for
  `OUT-5510-K1` and `OUT-5510-K2` after the first release carrying E1.

## Wave: DELIVER / [REF] Implementation summary — E3 slice 10 (#6148)

The Refinement tab's read carries the yardstick as facts `{ source, days, probability }`: the Team's SLE
when both fields are set, else the 85th percentile of the Team's default cycle time over its Throughput
window, else no number (also when the Team does not refine, without asking the metrics service). The
tab shows the heading plus one line, "Doable within N days?" ("1 day" in the singular, "our SLE" with no
number), and an info icon whose tooltip and accessible name say where N comes from, in the Team's
Terminology. Steps 10-01..10-06, then refactor, an adversarial review (0 blocker / 0 high; 2 medium and
7 low fixed, the "no Work Items have finished yet" wording kept by the maintainer, acronym lower-casing
left as the existing project pattern), and mutation testing (backend 100 %, frontend 94.23 %,
`mutation/results.md`). The maintainer tested it by hand on 2026-10-03.

Open: the dev-instance check of how large real fallback values get (the slice's learning hypothesis)
was not run — the dev instance was down.

## Wave: DELIVER / [REF] Maintainer review of slices 11 and 13 (2026-10-04)

Reviewed by hand on the demo instance after slices 11 and 13 were delivered locally.

**The veto becomes two independent discussion rules, both on by default.** A Work Item goes to discussion when it
has **1 or more No votes**, or **2 or more "Yes, if…" votes**. Each rule has its own checkbox and threshold and either
one is enough; the old single threshold with a "Counting" choice is gone. A Team that never set readiness gets both
rules on with those thresholds. "Yes, if…" still counts as a Yes towards Ready. Wire shape:
`readiness.discussWhen { no: number | null, yesIf: number | null }`, where `null` means that rule is off; a
threshold below 1 is refused ("A discussion rule needs at least 1 vote"). Settings → Refinement:

```
Readiness by votes
  Yes votes needed  [ 3 ]
  Voters needed     [ 3 ]

  Send to discussion when
  ☑ [ 1 ] or more No votes
  ☑ [ 2 ] or more "Yes, if…" votes
```

A save that changes only some readiness fields is checked against the stored values for the rest, so the stored
readiness can never end up breaking a rule.

**The Votes and comments dialog stays as it is for now**; it gets its richer look with the comments (slice 12).
**Icons in the Readiness column** (a check for Ready, a stop for a discussion) are an idea to revisit at the end of
the Epic, once every readiness source exists.

## Wave: DISTILL / [REF] Scope and Reconciliation — E2 (#5881)

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-04 · **Mode**: autonomous subagent, maintainer reachable
through the orchestrator.
**Scope**: **E2 only — Epic #5881 "Refinement need: refine enough, then stop"**: slices 03 (US-03, #6141), 04 (US-04,
#6142), 05 (US-05, #6143), 06 (US-06, #6144), 07 (US-07, #6145) and 09 (US-09, #6147, the Lighthouse/API half only; the
CLI and MCP half belongs to `lighthouse-clients`, as E3's DST-25). Slice 08 (US-08, #6146) is folded into 03 by the
maintainer (below). E1 and E3 sections above are left as written; E4 and E5 are not distilled.

**Reconciliation passed — 0 contradictions** across DISCUSS, DESIGN and DEVOPS for E2. The places where the maintainer's
2026-10-04 sketch decisions replace DISCUSS/DESIGN wording (per-state stage, DD-5's rule-overrides-votes, AC-6.1's
backlog-order line) are maintainer decisions taken in this wave, not contradictions between waves; they are recorded
below and back-propagated in `distill/upstream-issues.md`. Settled calls re-checked against every scenario: the need
is HowMany over the Team's total Throughput; async is pull; votes stay open on every Work Item in refinement; band
defaults 50/85, a Team setting from slice 07; cadence = weekdays + every N weeks + starting week, next Refinement the
first cadence day strictly after today in the instance time zone; date ranges inclusive; all Community; every word
through Terminology; settings reuse existing controls. Decisions DST-36..DST-56: `distill/wave-decisions.md` → "E2
Refinement need".

## Wave: DISTILL / [REF] Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)

Approved by the maintainer on 2026-10-04 from sketches, before any E2 slice was built. **Supersedes DD-5, DSN-14's
rule-decides-readiness half, AC-3.1–AC-3.2, US-08 and AC-6.1's counting order, and every provisional E2 copy where
they differ. Slice 08 (US-08, #6146) is folded into slice 03; the orchestrator marks #6146 Removed once the
maintainer confirms.**

**Stage and votes are two independent signals.**

- A Work Item's **stage** comes only from two optional rules on the Team — "Ready when" and "Being refined when" —
  built with the rule editor the Team's other rules use. Anything neither rule matches is Waiting. There is no
  stage per refinement state; the stored per-state stage stays unused (expand-only).
- When both rules match a Work Item, **Ready wins**.
- **Without rules**: no Stage column, no breakdown; the ready count is the votes' (slice 13, unchanged).
- **With rules**: the ready count is the Work Items whose stage is Ready. Votes never make a Work Item Ready and
  never block a stage-Ready one; they are still taken on every Work Item.
- A row is **flagged when votes have been cast and they disagree with the stage**: stage Ready but the votes cast
  fall short of Yes or need discussion, or the votes say Ready on a row whose stage is Waiting or Being refined.
  **No votes means no opinion, so no marker** (maintainer, 2026-10-04, answering DISTILL's question).
- No "no state is marked Ready" hint.

```
9 Work Items in Refinement · 2 ready                      Next Refinement: Thu 8 Oct · in 4 days
2 Ready · 3 Being refined · 4 Waiting
[⚠] 3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull before Thu 8 Oct. Refine 2 to 5 more.

#  Work Item              Parent  State      Stage           Your vote            Votes     Votes say
1  GR-058 User activity   GR-010  Next       Ready           [Yes][Yes, if…][No]  3 votes   Ready
2  GR-059 Advanced search GR-010  Next       Ready  ⚠        [Yes][Yes, if…][No]  1 vote    2 more Yes needed
3  GR-051 Reporting       GR-010  Analysing  Being refined   …
── enough for Thu 8 Oct (85%) · not needed before then ─────────────────────────────────────
   GR-054 …   (muted, unnumbered)
```

**Slice 03 — stages.** Settings → Refinement → "Stages (optional)" with "Ready when [+ Add rule]" and "Being refined
when [+ Add rule]". Tab with rules: heading "N Work Items in Refinement · R ready", a breakdown line "2 Ready · 3 Being
refined · 4 Waiting", a "Stage" column straight after State, the readiness column headed "Votes say", and a ⚠ marker
with a tooltip on a disagreeing row. Without rules the tab is as slice 13 left it ("· R ready by votes", "Readiness").

**Slice 04 — cadence.** Settings → Refinement → "Refinement cadence", copying the recurring-blackout form: weekday
checkboxes Monday…Sunday, "Repeat every (weeks)" [1], "Starting week" date shown only when the repeat is above 1
(required then). Tab: on the heading's row, right-aligned, "Next Refinement: Thu 8 Oct · in 4 days" — "tomorrow" for
one day, never "today" (the date is always after today); calendar days, from the ISO date the API returns. Without a
cadence that spot holds a role-aware hint: editors "Set a Refinement cadence in Settings to see how many Work Items are
needed"; readers "A Team admin can set a Refinement cadence to see how many Work Items are needed" (the tab tooltip's
role split).

**Slice 05 — verdict.** One MUI Alert under the heading, same size and place in all three states, icon plus colour:
below = warning, in = success, **above = warning too** ("stop" as loud as "refine more"). Copy: below "3 ready — below
the range of 5–8 Work Items Team Gravity is likely to pull before Thu 8 Oct. Refine 2 to 5 more." · in "6 ready — in
the range of 5–8. Nothing more needs refining before Thu 8 Oct." · above "11 ready — above the range of 5–8. Stop
refining: nothing more is needed before Thu 8 Oct." Too little history: the alert shows the forecasts' minimum-data
message. No cadence: no alert (slice 04's hint is enough). The ready count is the votes' without rules, the stages'
with rules.

**Slice 06 — needed rows.** A leading "#" column numbers rows 1..N (N = the band's high end), then a full-width divider
"enough for Thu 8 Oct (85%) · not needed before then"; the rows below are unnumbered and muted. **The numbering follows
the order the rows are shown in, not backlog order** (a Team may be unable to fix its backlog order): sort by another
column and the first N rows as shown are numbered, the line after the N-th. Backlog order is the default; the sort is
not kept. So the API returns N (the high end) with the date and percentile, and the browser places the line. Fewer in
refinement than N: the line after the last row reads "All 6 Work Items in Refinement are needed before Thu 8 Oct." No
number: no "#" column, no line. Whether `DataGridBase` can hold a divider row is DELIVER's to find out; the scenarios
assert the line's text and its place among the rows, not grid internals.

Still provisional (DST-48): the ⚠ marker's tooltip words, the band's field labels and its error wording, the cadence
validation messages.

**Amended during DELIVER (2026-10-04): a Refinement on a blackout day is skipped.** The maintainer reversed the
"not blackout-shifted" rule (DSN-7, the slice-04 scenario "a Refinement on a blackout day keeps its date"): a cadence
day that is blacked out is no Refinement, so the next Refinement is the first cadence day after today that is not a
blackout day, and a blacked-out cadence day is not a Refinement day. The slice-04 scenario is renamed "a Refinement on a
blackout day is skipped". The need's horizon (slice 05) still counts working days to that date.

**Amended after the slice 03–04 manual review (2026-10-04).** The maintainer reviewed the tab in the browser:

- The yardstick question moves onto the vote: the "Your vote" column header reads "Doable within 2 days? ⓘ" (same
  tooltip as before), and the separate line above the grid goes.
- With stage rules the heading is the breakdown itself, "2 Ready · 2 Being refined · 6 Waiting"; the separate
  "N Work Items in Refinement · R ready" line goes. Without rules there is no breakdown and the heading stays
  "N Work Items in Refinement · R ready by votes". Slice 05's verdict alert sits under the heading and carries the
  ready number with the range.
- Without a cadence the spot on the heading's row reads "No Refinement cadence ⓘ"; the role-aware hint ("Set a
  Refinement cadence in Settings…" / "A Team admin can set…") moves into the icon's tooltip.

## Wave: DISTILL / [REF] Scenario list with tags — E2 (#5881)

All cases are pending (`[Ignore(PendingSliceNN)]`, `it.skip`); the E2E skeleton is `fixme`. **162 runnable cases**:
backend 105, of which 75 error/boundary (71%); frontend 58 cases from 51 scenario templates, of which 30 templates are
error/boundary (59%). Every backend case is `@driving_port @real-io` with a `@contract-shape:` tag in the source.

| Slice | Backend cases | Frontend cases | Error / boundary (backend) |
|---|---|---|---|
| 03 stages (08 folded in) | 24 `Slice03StageRulesTest` | 9 `RefinementView.stages` + 5 settings | 20 |
| 04 cadence | 26 `Slice04RefinementCadenceTest` + 6 usage | 10 `RefinementView.cadence` + 6 settings | 21 + 0 |
| 05 need and verdict | 18 `Slice05NeedAndVerdictTest` + 11 usage | 13 `RefinementView.need` | 15 + 6 |
| 06 needed rows | — (facts pinned in 05) | 9 `RefinementView.enoughFor` | — |
| 07 band | 16 `Slice07BandPercentilesTest` | 6 settings | 11 |
| 09 clients (Lighthouse half) | 4 `Slice09ClientNeedTest` + `…WithAnApiKeyTest` | — | 2 |

**Backend — `Slice03StageRulesTest`**: rules set and read back · no rules, no stages · without rules the count follows
the votes · with rules it follows the stages · unmatched is Waiting · both match, Ready wins `@boundary` · votes never
make Ready `@boundary` · votes never block Ready `@boundary` · stage-Ready not backed by the votes cast disagrees · stage-Ready
nobody voted on shows no disagreement `@boundary` · both Ready agree · Waiting and not voted Ready agree · votes stay open on a Ready row · no Ready match → 0 `@boundary` · Being
refined rule alone still counts by stage `@boundary` · rules judge only Work Items in refinement `@boundary` · Pulsar's
one state split by rule (US-08's case) · unknown field refused `@error` · 21 conditions refused `@error` · 20 accepted
`@boundary` · a save silent on rules keeps them (×2) `@boundary` · turning rules off hands back to votes `@boundary` ·
Work Items kept `@boundary`.

**Backend — `Slice04RefinementCadenceTest`**: set and read back · starting week kept as its Monday `@boundary` · same
weekday twice is one `@boundary` · every-N without a starting week refused `@error` · fewer than one week refused (×2)
`@error` · not a weekday refused (×2) `@error` · silent save keeps it (×2) `@boundary` · clearing every day leaves no
cadence `@boundary` · Work Items kept `@boundary` · next Refinement strictly after today (×4) · every second week from
the starting week (×3) `@boundary` · none before the starting week `@boundary` · two weekdays, whichever is first
`@boundary` · the instance's time zone decides today (×2) `@boundary` · a Refinement on a blackout day keeps its date
`@boundary` · without a cadence the list stays and no date is named `@error` · without a cadence votes are taken
`@error`.

**Backend — `Slice05NeedAndVerdictTest`**: below range with the full facts `@kpi-OUT-5510-K3` · verdict against both
inclusive ends (×4) `@boundary` · without stages a Ready-making vote moves the verdict · with stages votes do not
`@boundary` · range = the Team's own manual How Many forecast for the date (shipped engine) · blackout days not counted
`@boundary` · on a Refinement day the number is for the following one `@boundary` · too little history `@error` · no
cadence `@error` · no refinement states `@error` · the high end stated as forecast, never cut to the listed Work Items
(×3) `@boundary` · a range of nothing says stop `@boundary` · 300 Work Items answer within two seconds `@boundary`.

**Backend — `Slice07BandPercentilesTest`**: defaults 50/85 · set and read back · the band decides where the range is
read (×3) · low not below high refused, naming both (×2) `@error` · outside 1–99 refused (×3) `@error @boundary` · 1/99
accepted `@boundary` · one end alone judged against the stored other `@error` · low end alone keeps the stored high
`@boundary` · silent save keeps it (×2) `@boundary` · Work Items kept `@boundary`.

**Backend — `Slice09ClientNeedTest` / `Slice09ClientNeedWithAnApiKeyTest`**: a client is told refine more or stop · told
why there is no number `@error` · the need joins the answer without removing what older clients read `@boundary` · a
personal API key is told the same need.

**Backend — `TeamRefinementNeedUsageEventsTests`**: a sizing event carries `OnRefinementDay` / `OnOtherDay` and nothing
else (×4) · the moments are appended after `NoCadence` (×2) · `TeamRefinementDayVerdictShown` carries one verdict and
nothing else (×4) `@kpi-OUT-5510-K3` · refused without a verdict from the list or with anything more (×5) `@error` · a
sizing event carrying a verdict refused `@error` · appended after `TeamSizingReadinessReached`.

**Frontend**

| File | Cases | Tags |
|---|---|---|
| `pages/Teams/Detail/Refinement/RefinementView.stages.test.tsx` | heading "· 2 ready" + breakdown · Stage after State, "Votes say" · stage in words · ⚠ marker by label · only the disagreeing row marked · no marker on a Ready row nobody voted on · 0 ready with rules · no rules = slice 13's tab · the Team's own words | `@us-03 @slice-03`, 5 `@boundary` |
| `pages/Teams/Detail/Refinement/RefinementView.cadence.test.tsx` | "Next Refinement: Thu 8 Oct · in 4 days" · "tomorrow" · a Refinement day counts a week ahead, never "today" · on the heading's row · editor hint · reader hint · the Team's own words · sizing moment per day (×3) | `@us-04 @slice-04`, 2 `@error`, 4 `@boundary`, `@kpi-OUT-5510-K4` |
| `pages/Teams/Detail/Refinement/RefinementView.need.test.tsx` | below / in / above copy with act vs settled icon · inclusive low end · the Team's own words · minimum-data message · no alert without a cadence · a Ready-making vote moves the alert (no rules) · verdict reported once on a Refinement day (×3) · `None` without a number · nothing reported on other days | `@us-05 @slice-05`, 2 `@error`, 2 `@boundary`, `@kpi-OUT-5510-K3` |
| `pages/Teams/Detail/Refinement/RefinementView.enoughFor.test.tsx` | "#" 1..N and the line after N · numbering follows a sort · backlog order again next visit · all needed when fewer · exactly as many · nothing needed, line first · no number, no "#" and no line (×2) · the Team's own words | `@us-06 @slice-06`, 1 `@error`, 5 `@boundary` |
| `components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx` | stages: offered empty · stored rule shown · changed Ready rule saved · last condition removed → null · incomplete rule saves nothing; cadence: Monday…Sunday + every 1 week · Thursdays saved · starting week asked for and saved · every 2 weeks without one saves nothing · under one week saves nothing · last weekday off → no cadence; band: 50/85 · high end saved · inverted refused naming both (×2) · outside 1–99 (×2) | `@us-03 @us-04 @us-07`, 6 `@error`, 2 `@boundary` |

**E2E — `specs/teams/Refinement.spec.ts`** (third skeleton, `fixme`): *a Team admin sets the Refinement cadence and the
tab says how many Work Items to refine before the next Refinement* — `@walking_skeleton @driving_port @us-04 @us-05
@us-06 @kpi-OUT-5510-K3-in-range-on-refinement-day`. Demo scenario 12, Team Gravity: the admin ticks Thursday; the tab
names the next Thursday, shows a verdict and the "enough for" line. POM: `TeamEditPage.refineEveryWeekOn()`,
`TeamRefinementPage.nextRefinement`, `.verdict`, `.enoughForLine`.

## Wave: DISTILL / [REF] WS strategy — E2 (#5881)

Architecture of Reference + project policy (inherited; nothing appended). One more walking skeleton, the E2E above
(DST-49). Backend scenarios run the production composition root (`WebApplicationFactory<Program>` over real EF) without
sign-in, and one fixture with sign-in without roles. Faked: the licence, the instance clock (`FakeLighthouseClock`, now
also on E1's harness), and — per horizon a scenario scripts — the How Many forecast (`ForecastWithScriptedHorizons`
around the shipped `ForecastService`; unscripted horizons run the shipped engine, which the parity and the 300-row
scenarios use over a Team with constant Throughput). The usage-data collector is captured. Tier B: not declared
(DST-53).

## Wave: DISTILL / [REF] Test placement — E2 (#5881)

| Where | Why (precedent) |
|---|---|
| `Lighthouse.Backend.Tests/API/Integration/Refinement/` — harness `RefinementNeedAcceptanceTest` (extends `SizingVotesAcceptanceTest`) + `Slice03StageRules…`, `Slice04RefinementCadence…`, `Slice05NeedAndVerdict…`, `Slice07BandPercentiles…`, `Slice09ClientNeed…` (`Scenarios.cs` + `Specifications.cs` each) | E1/E3 folder and partial-class split |
| `Lighthouse.Backend.Tests/Integration/UsageData/TeamRefinementNeedUsageEventsTests.cs` | Beside `TeamSizingUsageEventsTests` |
| `Lighthouse.Frontend/src/pages/Teams/Detail/Refinement/RefinementView.{stages,cadence,need,enoughFor}.test.tsx`, `src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx`, shared kit `src/tests/RefinementTabTestKit.tsx` (gains `gravitysSixWorkItems`, `aNeedOfFiveToEight`, `noNeedBecause`, an onlooker role) | E3's colocated `<component>.<concern>.test.tsx` and kit |
| `Lighthouse.EndToEndTests/tests/specs/teams/Refinement.spec.ts` + POMs `TeamRefinementPage.ts`, `TeamEditPage.ts` | E1/E3 spec and POM |

## Wave: DISTILL / [REF] Driving adapter coverage — E2 (#5881)

| Driving adapter (DESIGN) | Covered by |
|---|---|
| `PUT /teams/{teamId}` carrying `refinement.stageRules`, `.cadence`, `.band`; `GET …/settings` | slices 03, 04, 07; `ModifyTeamSettings.refinementNeed`; E2E |
| `GET /teams/{teamId}/refinement` → `stagesConfigured`, `readyCount`, `readySource`, row `stage` / `signalsDisagree`, `nextRefinementDate`, `isRefinementDay`, `need` | slices 03, 04, 05, 07, 09; frontend via `RefinementService`; E2E |
| `POST …/work-items/{id}/votes` (moves the verdict on a Team without rules) | slices 03, 05, 09 |
| `POST /api/latest/forecast/manual/{teamId}` (parity reference) | slice 05 |
| `POST /api/latest/blackout-periods` (calendar precondition) | slices 04, 05 |
| `POST /usagedata/events` (`TeamRefinementDayVerdictShown`, `refinementVerdict`, the two new `sizingMoment` values) | `TeamRefinementNeedUsageEventsTests`; frontend emission in `cadence` / `need` |
| CLI / MCP need commands | **not here** — `lighthouse-clients`, slice 09's other half |

## Wave: DISTILL / [REF] Adapter coverage — E2 (#5881)

| Driven adapter | Real I/O scenario |
|---|---|
| EF `Teams.RefinementSettings` (stage rules, cadence, band in the one JSON value) | slices 03, 04, 07 save / read-back / silent-save-keeps |
| `IForecastService.HowMany` over `IThroughputService` | slice 05 parity (shipped engine) and every scripted-horizon case (the horizon asked is checked) |
| `ForecastDataSufficiencyPolicy` | slice 05 too-little-history |
| Blackout periods (`IBlackoutPeriodService`) | slices 04 (date kept), 05 (horizon shortened) |
| `ILighthouseClock` + instance time zone | slice 04 time-zone cases |
| Usage-data forwarding (collector captured) | `TeamRefinementNeedUsageEventsTests` |

No external adapter is added. No EF migration: the settings are members of the existing JSON value (ADR-214).

## Wave: DISTILL / [REF] Scaffolds — E2 (#5881)

| File | Marker | Behaviour until DELIVER |
|---|---|---|
| `Lighthouse.Frontend/src/models/Refinement/Refinement.ts` | types only | new members optional (`stageRules`, `cadence`, `band`, row `stage` / `signalsDisagree`, tab `stagesConfigured`, `readyCount`, `readySource`, `nextRefinementDate`, `isRefinementDay`, `need`), so E1/E3 code compiles unchanged |
| `RefinementAcceptanceTest` (E1 harness) | additive | a `FakeLighthouseClock` registered as the clock, and a `ConfigureAdditionalServices` hook |

Backend production code: none. Frontend production code: none beyond the optional model members.

## Wave: DISTILL / [REF] Pre-requisites — E2 (#5881)

DESIGN: the settings JSON members, the tab facts, ADR-214's "absent means unchanged", the forecast port. DEVOPS:
`renamed-terminology`, `usage-data-consented`, `auth-on-rbac-off`; E2E in `ci_verifysqlite` / `ci_verifypostgres`.
DELIVER owns: the UI sketches still open (DST-48); `UsageDataEventName.TeamRefinementDayVerdictShown = 15`,
`UsageDataRefinementVerdict`, `UsageDataSizingMoment.OnRefinementDay = 1` / `OnOtherDay = 2`, and their rows in
`docs/settings/usagedata.md`; `ARCHITECTURE.md` (stages as rules, the need); demo data if the E2E needs a cadence
seeded rather than set; updating the E2E heading regex when demo Gravity gets stage rules (it has none today).

## Wave: DISTILL / [REF] RED classification — E2 (#5881)

162 of 162 runnable cases were un-skipped once and run: **backend 105 fail, 0 pass; frontend 56 fail, 2 pass by
design** (preservation guards: without stage rules the tab is slice 13's, and a vote on a Team without a cadence still
reports `NoCadence`). Every failure is on missing behaviour (`MISSING_FUNCTIONALITY`); none broken. Five cases that
first passed vacuously were rewritten to prove something first. E2E type-checked and linted, not run live. Detail:
`distill/red-classification.md` → "Epic #5881 (E2)".

## Wave: DISTILL / [REF] Delivery order — E2 (#5881)

The maintainer kept DD-22's interleaving for this split (the "each Epic ships on its own" rule applies to future
splits only), with 08 folded into 03 and 14 removed: **03 → 04 → 05 → 06 → (E3 12) → 07 → (E3 15) → (E3 16) → 09
→ (E3 17a → 17b)**.
Slice 03 un-skips `Slice03StageRulesTest`, `RefinementView.stages` and the stage block of
`ModifyTeamSettings.refinementNeed`; 04 `Slice04RefinementCadenceTest`, the slice-04 usage cases,
`RefinementView.cadence` and the cadence block; 05 `Slice05NeedAndVerdictTest`, the slice-05 usage cases and
`RefinementView.need`; 06 `RefinementView.enoughFor` and then the E2E skeleton (run live first); 07
`Slice07BandPercentilesTest` and the band block; 09 the two slice-09 fixtures. One scenario at a time.

## Wave: DELIVER / [REF] Maintainer decision — the verdict alert's open points (2026-10-05)

Sketched before 05-08 and answered by the maintainer on 2026-10-05. These close what the 2026-10-04 E2 UI review left
open for slice 05; the three sentences, the single MUI Alert and "above as loud as below" stand as approved then.

- **The ready number appears twice on a Team without stage rules, and that stays.** The heading keeps
  "N Work Items in Refinement · R ready by votes" (slice 13's) and the alert opens with "R ready —". The heading
  still carries the number when there is no alert (no cadence, too little history).
- **Equal ends collapse to one number.** When low = high the range reads as one number and "Refine X to Y more"
  drops "to Y": "3 ready — below the 5 Work Items Team Gravity is likely to pull by then. Refine 2 more."
  · above: "2 ready — above the 0 likely to be pulled. Stop refining: nothing more is needed by then."
  · in range: "5 ready — exactly the 5 likely to be pulled. Nothing more needs refining
  by then." (the in-range and above wordings were the orchestrator's, written to the rule; open to change at
  review). The browser decides this from the facts; the wire is unchanged.
- **Too little history is `info`, not `warning`.** The alert shows the forecasts' minimum-data message with the
  neutral info severity and icon: there is nothing for the Team to act on, it only has no number yet.
- **An ⓘ at the end of the alert says where the range comes from** (proposed during DELIVER, accepted "ok fair"),
  reusing the tab's `InfoTooltip`: "Based on Team Gravity's Throughput: a How Many forecast for the 6 working days
  until Thu 8 Oct. The Team pulls at least the low end with 50% likelihood, and more than the high end with only
  15% likelihood. Same forecast as on the Forecasts page." (corrected 2026-10-05 after review: the low end is a
  floor reached with (100 − low)% likelihood; the default reads the same) Built from facts already on the wire (percentiles,
  horizon, date); Team and Work Item words through Terminology. Only on the three verdict states, not on the
  minimum-data message.
- **Next Refinement titles the message** (manual review, 2026-10-05): when the need message shows, its title is
  'Next Refinement: Fri 9 Oct · in 4 days' and the date leaves the heading row; the sentences say 'by then' instead
  of repeating the date. Without a cadence there is no message and 'No Refinement cadence ⓘ' stays on the heading
  row.

## Wave: DELIVER / [REF] Maintainer decision — the "enough for" line names the next Refinement (2026-10-05)

Asked before slice 06 (#6144) was built, after the need message took the date as its title. **The line and the
all-needed sentence say "the next Refinement" instead of repeating the date**: "enough for the next Refinement (85%) ·
not needed before then" and "All 6 Work Items in Refinement are needed before the next Refinement." This supersedes
the wording in "Maintainer decision — the E2 UI, and slice 08 folded into 03 (2026-10-04)" for slice 06; the
"Refinement" word goes through Terminology like every other renameable term.

## Wave: DELIVER / [REF] Maintainer decision — comments instead of a condition prompt (2026-10-05)

Sketched before slice 12 (#6150) was built and answered by the maintainer on 2026-10-05. **Supersedes the "Yes, if…
opens a dialog with an optional Condition textbox" and the "Ask a question" wording of "Maintainer decision — the
voting UI, and slice 14 dropped (2026-10-04)", and the channel wording ("via the command line" / "via an assistant")
in the log.**

- **"Yes, if…" records in one click, like Yes and No.** There is no Condition dialog. A condition, a reason or a
  question is a comment, and every comment is treated alike: "a condition is just a comment".
- **"Votes and comments" has one "Add a comment" button** that opens a "Comment" box with Send, for everybody. A
  comment from somebody who has no current vote is an **open question** (❓ in the grid) until they vote; from
  somebody who has voted it is a plain comment.
- **The log lists votes and comments in time order** — "Jonas Weber voted Yes · Wed 7 Oct", "Ana Lima · Wed 7 Oct"
  with her comment beneath as plain text, an open question marked as such. A vote that arrives with a comment (the
  command line and the assistant can still send one) shows as the vote with its comment beneath.
- **The log does not say where an entry came from.** The channel stays on the wire and in the usage counts; the dialog
  never shows it.
- **Approved from the same sketches:** the Votes cell keeps "3 votes" and adds two small icons, a comment icon
  ("Comments") and a warning-coloured question icon ("Open question"), both in the button's accessible name; the
  column stays 120 px. The dialog shows the split on top, the log beneath, "Add a comment" under the log, and the
  footer "Voting as … · Change your name" with Close; an empty log reads "No votes or comments yet."
- The backend is unchanged by this: a vote may still carry a comment (the API, CLI and assistant use it), the open
  question is still "the latest entry is a comment and that voter has no current vote".

## Wave: DELIVER / [REF] Maintainer decision — the band's fields and its range (2026-10-05)

Sketched before slice 07 (#6145) was built and answered by the maintainer on 2026-10-05. **Supersedes AC-7.1's
"Percentiles 1–99"**: each end of the band is a likelihood **between 50% and 95%**, both included, and the low end stays
strictly below the high end. Below 50% is not a sensible floor to plan by, and above 95% is not realistic. Defaults stay
50/85. The rule holds on the server and in the form alike; no band was ever stored outside the defaults (the setting is
exposed for the first time in this slice), so there is nothing to migrate.

- **Layout:** a block after the cadence in Settings → Refinement, styled like "Readiness by votes", headed
  "{Work Items} needed before the next {Refinement}" with an ⓘ (the tab's `InfoTooltip`) that says where the range comes
  from, in the same words as the need message's ⓘ: the Team's How Many forecast up to the next Refinement; the Team pulls
  at least the low end with the low likelihood, and more than the high end with only (100 − high)% likelihood. No helper
  line under the fields.
- **Fields:** "Low end likelihood" and "High end likelihood", small number fields with a % adornment, prefilled 50 and 85.
- **Errors, under the field, nothing saves while one shows:** inverted or equal — "The low end (90%) must be below the
  high end (85%)."; out of range — "Between 50% and 95%."

## Wave: DELIVER / [REF] Maintainer review of slice 12 (2026-10-05)

Reviewed in the browser on 2026-10-05, before slice 12 (#6150) was pushed. **Supersedes, for slice 12, the one-click
"Yes, if…", the open-question icon in the Votes cell, the stage ⚠ in the Stage cell and the log list of "Maintainer
decision — comments instead of a condition prompt (2026-10-05)".**

- **A Warnings column**, last in the grid, reusing the Features list's one-icon idea: a single ⚠ when the row has
  anything to warn about, every reason in the tooltip (a list when several), **nothing at all on a clean row**, sortable
  so the rows with warnings come together. Its reasons today: the stage and the votes disagree (the ⚠ leaves the Stage
  cell), and somebody asked a question and has not voted yet (the question icon leaves the Votes cell). The Votes cell
  keeps "3 votes" and the comment icon only.
- **"Votes and comments" shows no vote trail.** Hovering a count in the split ("2 Yes · 1 Yes, if… · 0 No") lists the
  people whose current vote it is; the server sends those names, keyed per voter like the counts, so the two never
  disagree. Beneath the split only what people **wrote**: comments, and a vote's condition shown as "Ana Lima · Yes, if…"
  with its text; an open question is marked as one. An empty list reads "No comments yet." The log in the API keeps
  every entry.
- **"Yes, if…" asks for its condition, and the UI requires one.** Clicking it opens a small "Yes, if…" dialog with the
  Work Item, a "Condition" box with the placeholder "What has to be true for a Yes?", Cancel and Vote; Vote stays
  disabled while the box is blank; the condition travels with the vote in one request. The server keeps the comment on
  a vote optional (the command line and assistants may omit it). Yes and No stay one click.
- **Comments cannot be deleted, and none is planned.** The log is append-only on purpose; a withdrawal would need its
  own decision about who may withdraw what with sign-in off. No Story is raised.

## Wave: DELIVER / [REF] Maintainer decision — taking a vote back by clicking it again (2026-10-05)

Sketched before slice 16 (#6154) was built and answered by the maintainer on 2026-10-05, after the Votes dialog lost its
vote trail. **Supersedes the "Take back my vote" button in the Votes and comments dialog (2026-10-04) and the log line
"<name> took back their vote".**

- **Clicking your own pressed answer in the grid takes the vote back**, like un-toggling: the answer is no longer
  pressed, the count, the split and its hover names and the readiness move to how the row now stands. A pressed answer
  carries the tooltip "Click again to take back your vote". There is no button in the dialog.
- Nothing is added to the dialog's list of what people wrote; the API log still records the take-back.
- A browser without a name has no vote to take back, so it never meets "Who is voting?" for this.

## Wave: DELIVER / [REF] Maintainer decision — the need covers one Refinement cycle (2026-10-06)

Answered by the maintainer on 2026-10-06, after slices 05 and 06 shipped. Raised as Story #6204 (under #5881, state
Next), to be built after slices 15 and 16. **Supersedes the need's forecast window "from today to the next
Refinement".**

- **The need is a replenishment target.** A Refinement must put enough on the shelf to last until the following one,
  so the How Many forecast runs over **one Refinement cycle**: the working days after the next Refinement up to and
  including the one after it. Weekly on Wednesdays gives Thursday to the following Wednesday, 5 working days. Which
  day of the week it is does not change the number; Monday and Tuesday read the same.
- **Several refinement weekdays:** the cycle is the gap between the next two Refinements, so the number follows the
  gap sizes (Monday and Thursday alternate between 3 and 4 days).
- **When today is a Refinement day,** the cycle runs from today to the next Refinement.
- **Blackout days** are not working days in the cycle. A Refinement on a blackout day is skipped (unchanged), so the
  cycle runs to the next Refinement that happens.
- **Work Items pulled before the Refinement are fine.** The ready count drops, and the number still to refine goes up
  on its own. No forecast of what gets pulled before then.
- **The "Next Refinement" heading keeps saying when.** The ⓘ text changes to say which cycle the number covers; its
  copy is sketched before it is built.

## Wave: DELIVER / [REF] Maintainer decision — voting with sign-in asks for nothing (2026-10-06)

Sketched before slice 15's frontend step (#6153) and approved by the maintainer on 2026-10-06. With sign-in on, the
first click on an answer records the vote under the account: no "Who is voting?" dialog, nothing kept in the browser,
and the Votes and comments dialog shows neither a "Voting as" line nor "Change your name". With sign-in off nothing
changes.

## Wave: DELIVER / [REF] Maintainer decision — the cycle's copy (2026-10-06)

Sketched for Story #6204 and approved by the maintainer on 2026-10-06, so the Story can run without them.

- **The verdict below the range** reads "3 ready — below the range of 5–8 {Work Items} Team Gravity is likely to pull
  until the {Refinement} after. Refine 2 to 5 more." The alert's title still names the next Refinement. "Nothing more
  needs refining by then." and "Stop refining: nothing more is needed by then." are unchanged.
- **The ⓘ text** reads "Based on Team Gravity's {Throughput}: a How Many forecast for the 5 working days between the
  {Refinements} on Wed 8 Oct and Wed 15 Oct." followed by the likelihood sentence and "Same forecast as on the
  Forecasts page." When today is a Refinement day, the first date is today. One working day reads "working day".
- Every term in braces is the Team's configured Terminology.
- **DEVOPS:** no new usage-data event; the need is already counted by the existing refinement events.

## Wave: DESIGN / [REF] Story #6204 — the need covers one Refinement cycle (2026-10-06)

DESIGN for Story #6204 only, PROPOSE, maintainer AFK; behaviour and copy are the two maintainer decisions above and
are not re-decided here. Scope: application/components. **No container or component topology change**: no new
module, port, controller, route, table, migration, RBAC requirement, usage-data event or external integration, so the
C4 L1/L2/L3 diagrams for this feature in `c4-diagrams.md` stay valid. **Contract testing (Pact): N/A**, nothing
external is touched. Style unchanged (modular monolith, ports-and-adapters, OOP).

Facts read in the code that shape the design:

- `IForecastService.HowMany(RunChartData throughput, int days)` takes **a day count only**: no start date enters the
  simulation. Moving the window from "today → next" to "next → the one after" changes only the count passed in.
- `CountWorkingDays(start, target)` counts calendar days **after** `start` up to and including `target`, minus
  blackout days in that span. A working day is a day that is not a blackout day; weekends are excluded only where
  the instance blacks them out. The approved example's "5 working days" from Wed to Wed assumes weekend blackouts;
  with none it reads 7. This is the manual forecast's rule and stays, because the copy promises "Same forecast as on
  the Forecasts page".
- `RefinementCalendarFacts.IsRefinementDay` already uses `IsCadenceDay` **with** the blackout predicate, so a
  blacked-out Refinement day is not a Refinement day. That is exactly the approved rule.
- On a Refinement day the shipped window (today → next) **is** the new cycle, so that case keeps its number.
- `RefinementCalendar` fetches blackout days only for today and for `DaysSearched(cadence, today)` (a 366-day window
  that starts at tomorrow, or at a far starting week), so it never expands recurring blackouts across the gap to a
  starting week years ahead. Its predicate answers "not blacked out" for any day it did not fetch.
- Terminology keys `refinement` and `refinements` both exist (`TerminologyKeys.ts:27-28`, DSN-20), so the ⓘ's
  "{Refinements}" needs nothing new.
- No Lighthouse-Clients package reads the need yet (zero references to `horizonWorkingDays`, `nextRefinementDate` or
  `daysUntilNextRefinement` in `lighthouse-clients`); slice 09 will be the first reader.

## Wave: DESIGN / [REF] Story #6204 — decisions (DSN-23..DSN-30)

| # | Decision | Options weighed → verdict | ADR |
|---|---|---|---|
| DSN-23 | **The cycle is a calendar fact.** `RefinementCalendarFacts` gains `Cycle` (start, end; null without a cadence or when no Refinement follows the start within the search horizon). Start = today when `IsRefinementDay`, else `NextRefinementDate`; end = `NextAfter(cadence, start, isBlackedOut)`. The derivation is one pure member of the static `RefinementCadenceCalendar` built from `IsCadenceDay` and `NextAfter`, so the cadence rule stays in one place and E3 purity holds. `NextRefinementDate`, `IsRefinementDay` and `DaysUntilNextRefinement` keep their meaning. | (a) the need calculator calls `NextAfter` a second time itself: it would then need the blackout predicate and the "today is a Refinement day" rule, spreading calendar knowledge into the forecast composer; (b) the browser derives the cycle from the next date: the server owns the instance's today and the blackout days, and every client would repeat the rule; (c) **calendar fact, pure derivation** ✓. | 215 (amended) |
| DSN-24 | **The blackout lookup reaches the end of the cycle.** Invariant: the blackout predicate is never asked about a day that was not fetched. The second search (from start + 1) may run past the first fetched window, so `RefinementCalendar` also fetches **only the tail** beyond it, up to the last day the second search looks at. On a Refinement day the second search is the first search, so nothing more is fetched; otherwise the tail is `start − firstSearched + 1` days, a few days for any real cadence. The far-starting-week optimisation is kept: the gap between today and a later starting week is still never fetched. | (a) one fixed window of two years: doubles the recurring-blackout expansion on every tab read; (b) leave the predicate as it is: a blackout on the Refinement after next that falls past the first window reads as a Refinement, which is a silently wrong number; (c) **fetch the tail only** ✓. | — |
| DSN-25 | **The need's horizon is the cycle's working days**: `CountWorkingDays(cycle.Start, cycle.End)` over the blackout days fetched for that span (the call shape the calculator already uses, new bounds). Start is excluded and end included, so a weekly Wednesday cadence counts Thursday through the following Wednesday. Throughput call (`RespectTeamSetting`), `HowMany`, reading at `GetProbability(100 − p)`, verdict and the `HasSufficientData` guard are unchanged. **The ready count is unchanged** (pulled Work Items leave the list and lower it on their own). | — (one way follows from DSN-23 and the approved rule) | 215 (amended) |
| DSN-26 | **No cycle → the existing `NoCadence` reason.** This happens only when a cadence has a next Refinement but no Refinement in the following 366 days that is not blacked out (the existing "a year of blacked-out Refinement days is no Refinement worth naming" rule, applied once more). The reason order is unchanged: `NoRefinementStates` → `NoCadence` → `InsufficientData`; `UnavailableReasonFor`'s cadence test becomes "has a cycle". | (a) a new `NoCycle` member: new copy for a case nobody can meet in practice, and a wire enum change for clients; (b) **reuse `NoCadence`** ✓. As today for `NoCadence`, the tab shows no need message, and the heading still names the next Refinement. | — |
| DSN-27 | **Wire delta is additive and lives on the need.** `NeedRange` carries the cycle; `RefinementNeedDto` gains `cycleStart` and `cycleEnd` (`"yyyy-MM-dd"`, invariant culture like `nextRefinementDate`; null exactly when `low`/`high` are null). **`horizonWorkingDays` keeps its name** and now counts the cycle's working days: it was always "the working days the How Many forecast runs over", and no client reads it yet. View-level `nextRefinementDate`, `isRefinementDay`, `daysUntilNextRefinement` are unchanged. | (a) rename to `cycleWorkingDays`: breaks a shipped field for a name; (b) keep `horizonWorkingDays` on its old meaning and add a second count: computes a window nothing shows; (c) send only `refinementAfterNext` and let clients choose the start from `isRefinementDay`: every client repeats the Refinement-day rule; (d) **cycle dates on the need, horizon renamed in meaning only** ✓. | — |
| DSN-28 | **The browser composes the approved copy from these facts.** `NeedFacts` gains the `{Refinement}` term and `describeBelow` ends "is likely to pull until the {Refinement} after. Refine … more."; In and Above keep "by then". `NeedOriginFacts` swaps the single `refinementDay` for the cycle's start and end and gains the `{Refinements}` term: "a How Many forecast for the N working day(s) between the {Refinements} on {start} and {end}." (dates through the existing `formatDayAndDate`, parsed with `parseLocalDate`), then the likelihood sentence and "Same forecast as on the Forecasts page." unchanged. `NeedVerdictTerms` gains `refinement` and `refinements`, passed from `RefinementView` via `getTerm(TERMINOLOGY_KEYS.REFINEMENT / REFINEMENTS)`. `isJudged` also requires the two dates. The alert title (the next Refinement) is unchanged. | — (copy approved) | — |
| DSN-29 | **The "enough for" line and the "#" column keep using `high`**, which now is the cycle's high end. No code change there. | — | — |
| DSN-30 | **ADR-215 is amended, not replaced.** Decision 1's window ("target date = next Refinement", "from now until the next Refinement") is superseded by the cycle; decisions 2–6 stand. E9 (band parity) is restated: the band equals the manual How Many over the same throughput for the cycle's working-day count (equivalently, the manual forecast for a target date that many working days ahead). | (a) a new ADR for one window change; (b) **an amendment note on ADR-215**, as the 2026-10-04 amendment did ✓. | 215 |

## Wave: DESIGN / [REF] Story #6204 — component decomposition

Paths relative to `Lighthouse.Backend/Lighthouse.Backend/` and `Lighthouse.Frontend/src/`.

| Component | Path | Change |
|---|---|---|
| `RefinementCadenceCalendar` (pure, static) | `Services/Implementation/Refinement/` | **EXTEND**: the cycle (start, end) from cadence, today and the blackout predicate (DSN-23) |
| `RefinementCalendarFacts` (+ the cycle value; a record or named tuple, crafter's choice) | `Services/Interfaces/Refinement/IRefinementCalendar.cs` | **EXTEND**: a `Cycle` member, null in `None` |
| `RefinementCalendar` | `Services/Implementation/Refinement/` | **EXTEND**: computes the cycle; fetches the blackout tail beyond the first window (DSN-24) |
| `RefinementNeedCalculator` | `Services/Implementation/Refinement/` | **EXTEND**: horizon over the cycle; "has a cycle" for the reason (DSN-25, DSN-26) |
| `NeedRange` | `Services/Interfaces/Refinement/IRefinementViewQuery.cs` | **EXTEND**: carries the cycle |
| `RefinementNeedDto` | `API/DTO/RefinementViewDto.cs` | **EXTEND**: `CycleStart`, `CycleEnd` (DSN-27) |
| `IRefinementNeed` | `models/Refinement/Refinement.ts` | **EXTEND**: `cycleStart`, `cycleEnd` (`string \| null`) |
| `needWording.ts` | `pages/Teams/Detail/Refinement/` | **EXTEND**: Below sentence and origin sentence (DSN-28) |
| `NeedVerdict.tsx` | same | **EXTEND**: judged need needs the cycle dates; terms gain `refinement`, `refinements` |
| `RefinementView.tsx` | same | **EXTEND**: passes the two terms |
| `RefinementTabTestKit.tsx` | `tests/` | **EXTEND**: the need builders carry cycle dates |
| Code comments that say "until the next Refinement" (`RefinementNeedCalculator`, `NeedRange`, `RefinementOutlook`, `RefinementVerdict`, `IRefinementNeed`, `NeedVerdict`, `needWording`) | as listed | **EXTEND**: reworded in the same change (stale comments otherwise) |
| `ARCHITECTURE.md` §4 row 8 and §10 row 214–218 | repo root | **EXTEND in DELIVER**, in the commit that ships the cycle (it describes what is built) |

## Wave: DESIGN / [REF] Story #6204 — ports

- **Driving**: `GET /teams/{teamId}/refinement` (`IRefinementViewQuery`, TeamRead): unchanged route and guard, two
  additive response members. No write port is touched. The read path stays write-free (E6).
- **Driven**: `IBlackoutPeriodService.GetEffectiveBlackoutDays` (one more, small range on non-Refinement days),
  `IForecastService.HowMany`, `ITeamMetricsService.GetForecastThroughputStatus`, `ILighthouseClock`: all reused
  unchanged. No new driven dependency, so no new probe is owed (Earned Trust); the invariant this Story adds (DSN-24)
  is proved by a test, below.

## Wave: DESIGN / [REF] Story #6204 — reuse analysis

| Existing | Overlap | Decision | Contract shape / how the crafter asserts it |
|---|---|---|---|
| `RefinementCadenceCalendar.NextAfter` / `IsCadenceDay` | finding a Refinement, skipping blacked-out ones | **EXTEND** (called twice; no second cadence rule) | pure function: return-only, property-style cases over cadences and blackout sets |
| `RefinementCadenceCalendar.DaysSearched` | which days a search reads | **REUSE** to size the tail fetch | pure function |
| `RefinementCalendar` | today, blackout fetch, facts | **EXTEND** | bounded read: only `GetEffectiveBlackoutDays` calls, asserted ranges |
| `BlackoutDaysExtensions.CountWorkingDays` | working days in a span | **REUSE** unchanged | pure |
| `RefinementNeedCalculator` + `NeedBand` | horizon, How Many, band, verdict | **EXTEND** the calculator; `NeedBand` unchanged | read-only composition |
| `NeedUnavailableReason.NoCadence` | "no Refinement to plan for" | **REUSE** (DSN-26) | closed enum, unchanged |
| `RefinementNeedDto.HorizonWorkingDays` | the forecast's day count | **REUSE** name, new span (DSN-27) | additive wire |
| `formatDayAndDate`, `parseLocalDate` | "Wed 8 Oct" from `yyyy-MM-dd` | **REUSE** | pure |
| Terminology `refinement` / `refinements` | the words in braces | **REUSE** | — |

No CREATE NEW beyond the cycle value carried on the facts.

## Wave: DESIGN / [REF] Story #6204 — wire-contract delta

`GET …/teams/{teamId}/refinement` → `need`:

| Member | Before | After |
|---|---|---|
| `horizonWorkingDays` | working days from today to the next Refinement | working days of the cycle (start excluded, end included) |
| `cycleStart` | — | **NEW** `"yyyy-MM-dd"`: today on a Refinement day, else the next Refinement; null with no range |
| `cycleEnd` | — | **NEW** `"yyyy-MM-dd"`: the Refinement after `cycleStart`; null with no range |
| `low`, `high`, percentiles, `verdict`, `unavailableReason` | — | unchanged shape; `low`/`high` now cover the cycle |

Additive only; enums still serialise as strings. **Lighthouse-Clients: N/A for this Story**, because no client reads the
need yet; slice 09's `lh refinement get` / MCP tool read the new members from the start.

## Wave: DESIGN / [REF] Story #6204 — test impact

Scenarios pin the old window and are **rewritten, not deleted** (DISTILL owns the wording):

- `Slice05NeedAndVerdictScenarios` / `…Specifications`: the scripted forecasts are keyed by day count, and today is
  Fri 2 Oct with the next Refinement Thu 8 Oct. The cycle Thu 8 → Thu 15 is 7 days, not 6, so every
  `TheTeamIsLikelyToPull(6, …)` and `NeedReading(…, 6)` moves to the cycle's count. `Blackout_days_before_the_next_Refinement_are_not_counted`
  (Mon 5 Oct) becomes "a blackout day inside the cycle is not counted" (for example Mon 12 Oct → 6), plus one case
  pinning that a blackout **before** the next Refinement no longer changes the number.
  `On_a_Refinement_day_the_number_is_for_the_following_Refinement` keeps 7. `The_range_uses_the_same_horizon_as_the_manual_forecast`
  is restated per DSN-30.
- `RefinementNeedCalculatorTest.A_blackout_day_before_the_next_Refinement_is_not_a_working_day` (5): same move.
- `RefinementNeedAcceptanceTest`: the fake forecast "per horizon" and its doc comment ("working days until the next
  Refinement").
- New cases owed: Monday and Tuesday read the same number (day-of-week independence); two refinement weekdays
  alternate gap sizes (Mon/Thu: 3 and 4 with weekend blackouts); a blacked-out Refinement after next extends the cycle
  to the next one that happens; today a blacked-out Refinement day is not a Refinement day; **DSN-24's invariant**: a
  blackout on the Refinement after next that lies past the first 366-day window is still honoured (the test that
  fails if the tail fetch is dropped); no cycle → `NoCadence`; `cycleStart`/`cycleEnd` null exactly when the range
  is.
- Frontend: `needWording.test.ts` (four Below rows, three origin rows, one Terminology row);
  `RefinementView.need.test.tsx` (lines ~198, ~203, ~246 and the field list at ~578); `RefinementTabTestKit`. E2E
  pins no need copy; nothing to change there. `RefinementView.cadence.test.tsx` and slice 04's scenarios
  (next date, days until) are unaffected.
- Mutation (Stryker, both stacks) on the touched files after the code is frozen.

## Wave: DESIGN / [REF] Story #6204 — open questions

None blocks DISTILL or DELIVER. **One copy question only the maintainer can answer**, left open on purpose because
the approved copy does not cover it and it is user-visible:

- **MQ-6204-1: the Settings → Refinement band block.** Its ⓘ reads "Based on the {Team}'s {Throughput}: a How Many
  forecast for the working days until the next {Refinement}." (`RefinementBandSettings.tsx:65-68`), which is **false
  once #6204 ships**. Its heading "{Work Items} needed before the next {Refinement}" still reads true. Suggested,
  not chosen: "…a How Many forecast for the working days between the next {Refinement} and the one after." Until
  answered, DELIVER leaves both unchanged and should not close #6204 with the ⓘ still saying "until".
- The "enough for the next {Refinement} (85%) · not needed before then" line (maintainer decision 2026-10-05) is
  not named in the cycle copy decision. DESIGN reads it as still correct (the rows above the line are what the next
  Refinement must leave on the shelf) and changes nothing. Worth one line of confirmation alongside MQ-6204-1.

## Wave: DESIGN / [REF] Story #6204 — decision taken in AFK mode (2026-10-06)

- **MQ-6204-1, settings ⓘ:** taken as recommended, to be confirmed by the maintainer at the hold. The ⓘ next to the
  band in Settings → Refinement reads "…a How Many forecast for the working days between the next {Refinement} and
  the one after." It follows the approved tooltip wording; the old "until the next {Refinement}" would be false once
  the cycle ships.
- "enough for the next {Refinement} (85%) · not needed before then" stays as it is: the line still names what the
  next Refinement must leave ready.
