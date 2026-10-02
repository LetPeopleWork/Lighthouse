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
- **Need number**: `HowMany` over commitment-point Throughput, meaning Work Items leaving refinement per day. This
  designs R2 out. Both bounds come from one run.
- **Open question 2, answered with evidence**: async voting works where the request is pushed into an inbox with a deadline.
  Lighthouse has no outbound channel. The risk is accepted with a pre-committed trigger: if usage data shows votes only
  inside live sessions, build push next.
- **Open question 1, Premium proposals**:
  - Lead lever: sizing calibration (votes compared with outcomes against the SLE).
  - Named, accountable votes come with authentication, which is already Premium.
  - Candidate: a cross-Team overview.
  - Rejected: live sessions as Premium, history caps, voter caps.
- **Persona**: create `team-member-voter` at DISCUSS.
