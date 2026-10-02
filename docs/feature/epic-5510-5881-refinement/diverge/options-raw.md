# Options (raw) — epic-5510-5881-refinement

**Wave**: DIVERGE, Phase 3 · **Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02
This file holds generation only; scoring lives in `taste-evaluation.md`.

**Structure.** The brief poses four questions:
- (a) the refinement-need number,
- (b) async voting and readiness,
- (c) live modes,
- (d) Premium restrictions.

§2 generates the answer pools for (a)-(c). §3-§5 combine them into **six whole directions**; each one
gives a single coherent answer to (a), (b) and (c), and the taste matrix scores those directions.
**Premium restrictions (d) get a pool of their own in §6.** A gate can sit on top of any direction, so
mixing the two in one matrix would muddle what it measures.

Every option stays inside D1-D19 and C1-C10. In particular:
- Team level only;
- votes are Yes / Yes, but… / No, with comments;
- votes stay in Lighthouse;
- readiness rules are Team settings;
- the SLE fallback is 85th-percentile default cycle time with a hint;
- async comes first;
- the need band has an upper bound.

---

## 1. HMW question

> **How might we help a Team keep just enough right-sized Work Items ready for its next replenishment,
> while spending discussion only where someone has a doubt?**

There is no solution embedded: it names no chart, no vote and no session.

## 2. Answer pools per sub-question

### (a) Refinement-need number

| ID | Answer | Mechanism |
|---|---|---|
| a1 | **Naive gap** | `HowMany` over the Team's Throughput until the next replenishment date (D17), minus the count of Work Items in ready state(s) |
| a2 | **Ratio-corrected gap** | a1 × the share of finished Work Items in the Throughput window that passed through a refinement state |
| a3 | **Commitment-point Throughput** | `HowMany` over a different series: Work Items leaving the last refinement state per day, which is the rate the ready queue is drained. Only Work Items that were refined are counted, by construction. Uses stored `WorkItemStateTransition` history |
| a4 | **Filtered Throughput** | a1 using the Team's existing (Premium) forecast filter rule set to exclude bugs and expedites |

Band shape (same for all four): one `HowMany` run gives both bounds through `GetProbability`. Below the lower bound the
Team is likely to run dry. Above the upper bound the extra Work Items are likely to go unused. The bound
percentiles are a DISCUSS decision. It can be phrased as likelihoods through `GetLikelihood(readyCount)`:
"x% chance the ready Work Items run out before <date>".

### (b) Async voting: how the request reaches the voter, and when it closes

| ID | Answer |
|---|---|
| b1 | **Shareable link and closing time**: the coach opens a round, Lighthouse gives a link and a closing time, and the coach posts it in the Team's own chat |
| b2 | **Pushed requests**: Lighthouse sends invitations, reminders and results through Slack/Teams webhooks or email |
| b3 | **Always-open votes on the Work Item**: no rounds; a vote control on every Work Item in a refinement state; a "N Work Items need your view" count wherever a member opens Lighthouse |
| b4 | **Vote from the assistant or terminal**: Lighthouse MCP tools and CLI commands list the Work Items waiting for a view and take a vote with a comment (D12) |
| b5 | **Cadence-opened rounds**: X days before each replenishment (D17), Lighthouse opens a round over the gap automatically and closes it at the replenishment |
| b6 | **Objection window**: candidates proceed unless someone answers No or Yes, but… before the closing time. Uses D16 with minimum Yes = 0, minimum voters = 0, veto after 1 No |

Readiness framing (shared): D16 is phrased the way developers know it from pull requests. "Minimum Yes"
reads as required approvals, and the optional veto reads as a blocking change request.

### (c) Live modes

| ID | Answer |
|---|---|
| c1 | **Presenter view**: one shared screen (in person, standalone or projector); the coach records votes called out in the room; no other device connects |
| c2 | **Remote facilitated, pushed**: participants' screens update live through SignalR; the coach reveals; a self-declared name per browser in auth-off |
| c3 | **Remote facilitated, polled**: the same as c2, but the page refreshes every few seconds; no push channel |

## 3. SCAMPER directions

### Option S: "Sizing as review requests" *(Substitute)*
**Core idea**: the meeting is replaced by the pull-request review loop. Each candidate Work Item carries a
review-style request: Yes ≈ Approve, Yes, but… ≈ Approve with a condition, No ≈ Request changes. It is
"ready" when it has its required approvals (D16).
**Key mechanism**: b1 + review framing; a1; c1.
**Key assumption**: developers carry their PR-review habit over to Work Items without a notification inbox.
**SCAMPER origin**: Substitute.
**Closest competitor**: GitHub required reviews (N3).

### Option C: "Need sets the agenda" *(Combine)*
**Core idea**: one Refinement tab. The need band sits on top, and under it the candidate list is cut at
the gap. One button, "Put the next N up for sizing", opens a round over exactly those Work Items. Its
closing time is the next replenishment, and it comes with a link to share.
**Key mechanism**: a3 + b1 (+ b5 as an optional schedule); c1 first, c2 later.
**Key assumption**: the coach shares the link, and the Team votes before the closing time.
**SCAMPER origin**: Combine (#5881 sets the scope of #5510).
**Closest competitor**: Agile Poker async session (#5), with no count.

### Option A: "Pushed rounds" *(Adapt)*
**Core idea**: the async machinery of Jira poker apps. A round has a deadline. Lighthouse posts the
invitation to the Team's Slack/Teams channel or by email, reminds before the close, and posts the
result.
**Key mechanism**: b2 + rounds; a2; c2.
**Key assumption**: async votes happen only when the request is pushed into an inbox.
**SCAMPER origin**: Adapt (Async Poker #4, Agile Poker #5, Slack polls N2).
**Closest competitor**: Async Poker for Jira.

### Option M: "Stop sign first" *(Magnify the upper bound)*
**Core idea**: the Refinement tab is a gauge first: below range, in range or above range before <next
replenishment>.
- **Above** or **in** range: the tab says so plainly ("Nothing more needs refining before Thu 8 Oct"),
  and no Work Items are put up for a vote.
- **Below** range: exactly the gap's worth of Work Items is solicited for votes.

Votes sit on the Work Item (no round entity), and the next replenishment is the deadline. One stable link per Team.
**Key mechanism**: a3 + b3 scoped to the gap and closed by D17; c1 first, c2 later.
**Key assumption**: the dominant waste is over-refinement, and a small, dated ask ("3 Work Items need your view by Thursday") is enough to get votes without a push.
**SCAMPER origin**: Magnify (C5, O2).
**Closest competitor**: none found (competitive-research I3).

### Option P: "Vote where you already type" *(Put to other use)*
**Core idea**: Lighthouse's CLI and MCP become the voter's surface. "Which Work Items need my sizing view?"
is asked inside the AI assistant or the terminal, and the vote and comment go in from there. The web
Refinement tab serves the coach and live sessions.
**Key mechanism**: b4 + a1; c1.
**Key assumption**: Team members spend their day in an IDE or AI assistant that has the Lighthouse MCP
configured.
**SCAMPER origin**: Put to other use (the D12 clients, used for a new job).
**Closest competitor**: none for sizing; Slack polls in spirit.

### Option E: "Always-open votes" *(Eliminate the session)*
**Core idea**: no rounds and no sessions. A vote control sits on every Work Item in a refinement
state, and readiness is recomputed continuously from D16. The need band shows ready against needed,
and members see "N Work Items need your view" when they open Lighthouse.
**Key mechanism**: b3 + a2; c1 as a filter on one shared screen.
**Key assumption**: Team members open Lighthouse often enough to notice what is waiting.
**SCAMPER origin**: Eliminate.
**Closest competitor**: Jira native voting / ADO voting extension (N1).

### Option R: "Cadence opens the round" *(Reverse)*
**Core idea**: the coach does not start anything. X days before each replenishment, Lighthouse opens a
round over the gap by itself and closes it at the replenishment. The replenishment view then shows
which Work Items are ready and which are doubted.
**Key mechanism**: b5 + a3; c1.
**Key assumption**: coaches forget to start rounds, and a system-opened round gets votes.
**SCAMPER origin**: Reverse (the system initiates, not the coach).
**Closest competitor**: Simple Poll recurring polls (N2).

## 4. Crazy 8s supplements

| ID | Option | Core idea |
|---|---|---|
| X1 | **Objection window** | b6: candidates proceed unless someone objects before the closing time (Rust FCP lazy consensus, N4); a1; c1 |
| X2 | **Shared-screen first** | c1 ships first as the whole product; async follows |
| X3 | **Predicted fit** | Lighthouse pre-marks each candidate "likely fits" or "likely doesn't" from the cycle times of similar finished Work Items; the Team votes only where the prediction is unsure |
| X4 | **Verdict as a tracker tag** | readiness is written back as a label or tag in the work tracking system |

## 5. Curation to six

| Source | Outcome |
|---|---|
| S (review requests) | **Merged into M and E as a framing.** Review semantics is a *wording* of D16 (shared readiness framing, §2b) rather than its own mechanism. Its delivery mechanism (b1) is already represented in C |
| R (cadence opens the round) | **Merged into C as an optional schedule.** It shares C's mechanism (a round over the gap), cost and scope, and differs only in who presses the button |
| X2 (shared-screen first) | **Removed**: conflicts with D8 (async ships first). c1 remains inside every direction as the in-person mode |
| X3 (predicted fit) | **Removed as a direction**: it adds a prediction layer on top of any direction rather than answering (b) or (c). Recorded as a later idea for DISCUSS |
| X4 (tracker tag) | **Removed**: conflicts with D9 (votes stay in Lighthouse) |
| X1 (objection window) | **Kept** as Option 6 |

### The curated six

| # | Option | (a) need | (b) async | (c) live |
|---|---|---|---|---|
| 1 | **Need sets the agenda** (C, with R's schedule) | a3 | b1 (+b5) | c1 → c2 |
| 2 | **Always-open votes** (E) | a2 | b3 | c1 |
| 3 | **Pushed rounds** (A) | a2 | b2 | c2 |
| 4 | **Stop sign first** (M) | a3 | b3 scoped to the gap, deadline = D17 | c1 → c2 |
| 5 | **Vote where you already type** (P) | a1 | b4 | c1 |
| 6 | **Objection window** (X1) | a1 | b6 | c1 |

### Diversity test

"Mechanism" means how a vote is requested and closed. "Assumption" means what must be true of voters.
"Cost" means the new infrastructure needed.

| # | Mechanism | Assumption about users | Cost profile | Distinct? |
|---|---|---|---|---|
| 1 | Coach-opened round over the gap, shared link | Coaches curate and share; voters respond to a dated link | New round entity, link, schedule | Yes |
| 2 | Votes on the Work Item, no rounds, no deadline | Voters visit Lighthouse unprompted | Smallest: a vote log and readiness evaluation only | Yes |
| 3 | Round plus outbound push | Async needs an inbox push | New outbound channel (webhooks/email, secrets, standalone config) and a push hub | Yes |
| 4 | The gauge gates whether voting happens; votes on the Work Item scoped to the gap, closed by the replenishment date | Over-refinement is the main waste; a small dated ask suffices | Small: vote log, readiness, gauge state | Yes. Differs from 2 in mechanism (need-gated, deadline) and assumption (O2 dominant); cost is similar |
| 5 | Voting through MCP/CLI clients | Voters live in the IDE/AI assistant | Lighthouse-Clients release plus API | Yes |
| 6 | Silence proceeds; only objections count | Voters engage only to object | Small: closing time and objection evaluation | Yes |

Options 2 and 4 are the closest pair. They differ in two of the three tests (mechanism and assumption)
and were kept on purpose: whether the count should **gate** the vote is the central question the
evaluation must settle.

## 6. Premium-restriction pool (question d)

These are generated against the existing gating patterns listed in competitive-research §5. Scoring is in
`taste-evaluation.md` §5.

| ID | Proposal | Existing pattern it reuses |
|---|---|---|
| P1 | No new gate on scale: Community refinement is already limited to the free 3-Team cap | Entity cap (`MaxAllowedTeams`) |
| P2 | Accountable, named votes (each vote tied to a signed-in account) come with authentication, which is already Premium; Community votes carry self-declared names | Auth is Premium (`BlockedPage`) |
| P3 | When the Team has a forecast filter, the need number uses filtered Throughput; free licences ignore the filter, as forecasts already do | Config kept, effect off (`GetEffectiveRuleSet`) |
| P4 | Basic readiness (minimum Yes, minimum voters) is Community; the optional veto and rule overrides are Premium | `RequirePremium` on settings writes + config kept, effect off |
| P5 | **Sizing calibration**: afterwards, compare votes with outcomes ("of the Work Items voted Yes, 82% finished within the SLE; of the Yes, but… ones, 55%") | `RequirePremium` on a read endpoint + `LicenseTooltip` |
| P6 | Vote log history: Community keeps the last N replenishments; Premium keeps everything | Competitor history caps ([PA3], [S1]) |
| P7 | Outbound reminders (Slack/Teams/email), if ever built, are Premium | `RequirePremium` (as OAuth connections) |
| P8 | Live remote sessions (c2) are Premium; async and in-person (c1) stay Community | `RequirePremium` |
| P9 | Cross-Team refinement overview: every Team's need band on one page | Bulk/cross-entity actions are Premium (Update All Teams) |
| P10 | Voter cap per Team in Community (e.g. 7) | Participant caps (PlanITpoker) |
| P11 | Renaming "replenishment" (if it becomes a Terminology term) is Premium automatically | Terminology editing is Premium |

## Gate G3

| Check | Result |
|---|---|
| 6 curated options | **PASS** |
| Each passes the diversity test | **PASS** (2 vs 4 flagged and justified) |
| All 7 SCAMPER lenses applied | **PASS**: S, C, A, M, P, E, R, plus 4 Crazy 8s |
| No evaluation language in this file | **PASS**: removals cite D-decisions or structural merges, not merit |
