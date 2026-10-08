<!-- markdownlint-disable MD024 -->
# Feature Delta — story-6217-lighthouse-skills

**ADO**: User Stories #6245 *Lighthouse skill: bring the general skill up to date with everything shipped
since June*, #6217 *A refinement skill for assistants working with Lighthouse*, #6246 *Daily Flow Review
skill: what the Team should decide and discuss today* · all Active · no parent Epic. Delivered in that
order; released together in the next clients release (after #6193, before #6202).

**Waves**: DISCUSS (2026-10-08, maintainer AFK; product answers given beforehand in
`discuss/maintainer-input.md`, recorded below as M1–M8; every other call taken here is listed under
"AFK defaults — revisit"). DISCOVER and DIVERGE: none; `discuss/research.md` (ProKanban skill, Liz
Rettig's two posts and her Lighthouse Live talk) stands in for them.

**One line**: the general `lighthouse` skill catches up with four months of shipped Lighthouse and speaks
like a ProKanban coach who does not lecture; two new skills build on it — one gets a Product Owner and
each developer ready for Refinement, one opens the daily with what the Team should decide and discuss
today instead of a status round.

**Repositories**: `/storage/repos/lighthouse-clients` (skills, CI, and for #6246 packages `client`, `cli`,
`mcp-core`) and Lighthouse (`docs/aiintegration.md`, one stale line in `docs/concepts/concepts.md`). This
workspace lives in the Lighthouse repo because the clients repo has none (#6218, #6193 precedent).

**Density**: `lean` + `ask-intelligent`. Tier-1 `[REF]` only; trigger evaluation at the end of DoR
Validation.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading

| File | Read |
|---|---|
| `discuss/maintainer-input.md` | ✓ — authoritative; M1–M8 |
| `discuss/research.md` (incl. the meetup addendum) | ✓ |
| `docs/product/jobs.yaml` | ✓ — four existing jobs reused; two new ones proposed (not appended: see D13) |
| `docs/product/personas/{flow-coach,product-owner,team-member-voter,delivery-forecaster,privacy-decider}.yaml` | ✓ |
| `docs/feature/epic-5510-5881-refinement/feature-delta.md` (jobs, personas) | ✓ |
| `docs/feature/epic-4127-sle-risk-corrections/feature-delta.md` (job) | ✓ |
| `docs/feature/story-6218-readable-cli-output/`, `story-6193-usage-data-from-clients/feature-delta.md` | ✓ — house shape; usage-data rules (M9: no new event names) |
| `docs/aiintegration.md`, `docs/metrics/flow-overview.md` (SLE Risk, Blocked, Stale) | ✓ |
| `lighthouse-clients/skill/SKILL.md` + `references/*` | ✓ |
| `lighthouse-clients/.github/workflows/ci.yml`, `ARCHITECTURE.md` §8/§10/§11, `docs/release-model.md`, `scripts/check-changeset.mjs` | ✓ |
| `lighthouse-clients/packages/*/CHANGELOG.md`, `.changeset/*.md` (unreleased) | ✓ — in place of `git log` (no shell in this session) |
| `packages/cli/src/index.ts` (help texts, `METRIC_KEYS`), `packages/mcp-core/src/index.ts` + `refinementTools.ts` (tool definitions), `packages/client/src/index.ts` (`TeamRefinement`, `RefinementRow`, WIP item) | ✓ |
| Lighthouse `API/TeamMetricsController.cs`, `Models/Metrics/{ProcessBehaviourChart,SpecialCauseType,SleRiskDto}.cs`, `API/DTO/{WorkItemDto,TeamDto,SettingsOwnerDtoBase}.cs` | ✓ |
| `/storage/repos/website` | ⊘ outside this session's readable roots — checklist row says how DELIVER checks it |
| `docs/product/journeys/` | ⊘ none relevant; journeys kept lightweight below (D13) |

No DISCOVER evidence to contradict.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` | **Priya Raman**, coach of Team Gravity at Northwind (`https://lighthouse.northwind.io`), Claude Desktop + MCP stdio. Asks the general skill about flow (#6245); **facilitates Gravity's daily** with the Daily Flow Review skill (#6246). |
| `product-owner` | **Marco Bianchi**, PO of Team Gravity; prepares Gravity's Refinement (Wednesdays) with the Refinement skill (#6217). |
| `team-member-voter` | **Jonas Weber**, developer in Team Gravity; checks whether he must prep or vote (#6217). Instance runs without sign-in, so his votes carry a self-declared name. |
| `delivery-forecaster` | **Lena Fischer**, delivery lead for the Ocean Explorer Portfolio, Claude Code + `lh`; forecasts through the general skill (#6245). |
| `privacy-decider` | Any of the above when `lh` or the MCP server puts the one-time usage-data question (#6193). Unchanged rule: the answer is theirs. |
| `lighthouse-maintainer` | Beneficiary: keeps the skills true to the product (drift check, KPI-1). |

Team Gravity, used throughout: SLE **7 days at 85%**, System WIP Limit **6**, Refinement every Wednesday,
readiness by Votes, Work Items `GR-0xx`.

---

## Wave: DISCUSS / [REF] JTBD One-Liner

| Job ID | One-liner | Stories |
|---|---|---|
| `job-assistant-user-get-sound-flow-answers` (**NEW**, proposed) | When I ask my assistant about my Team's flow or a forecast, I want it to fetch the answer from Lighthouse and say it the way a good flow coach would, so I can act on it without checking the assistant's work. | US-01, US-02, US-03 |
| `job-flow-coach-refine-just-enough` (existing, epic-5510) | Keep just enough right-sized Work Items ready for the next Refinement, and discuss only the ones that need it. **Here**: the PO asks an assistant instead of opening the tab. | US-04 |
| `job-team-member-give-sizing-view` (existing, epic-5510) | When I'm asked whether a Work Item fits our SLE, give my view in seconds from where I already am. **Here**: from the assistant. | US-05 |
| `job-flow-coach-spot-stuck-items` (existing) | See which in-progress Work Items are stuck. **Here**: what is Blocked right now, and since when, readable by an assistant. | US-06 |
| `job-flow-coach-act-before-sle-breach` (existing, epic-4127) | When I look at what is in flight during a standup, I want to know which items are more likely than not to break the SLE — while there is still time to act. | US-07, US-09 |
| `job-flow-coach-see-predictability-trend` (existing, epic-5427) | See whether the system moved. **Here**: PBC signals, readable by an assistant. | US-08 |
| `job-facilitator-run-the-daily-from-the-work` (**NEW**, proposed) | When I facilitate my Team's daily, I want to open it with what the work needs us to decide and discuss today, so the Team spends the time getting Work Items done instead of reporting status. | US-09, US-10 |

- **Functional**: an assistant that uses every Lighthouse read the clients offer, states it as Lighthouse
  does, and turns it into the one decision or question that matters today.
- **Emotional**: from "I have to double-check what the bot said" to "that is what our coach would say".
- **Social**: a facilitator who runs a daily without a roll call; a PO who walks into Refinement knowing
  what to discuss; developers who are asked about Work Items, never about themselves.
- **Forces** — *Push*: the skill was written on 2026-06-06 and names only part of what shipped since
  (S1); it contradicts ProKanban in nine places (research §7); refinement and dailies are not covered at
  all; Liz Rettig: agents "drift to the industry mean" (velocity, averages) without explicit guardrails.
  *Pull*: Lighthouse already computes what ProKanban's skill computes from CSV, live — SLE Risk, Blocked,
  PBC signals, the Refinement need and the votes (research §3); Liz's client ran exactly this daily review
  from Lighthouse data. *Anxiety*: an assistant that votes for someone, decides readiness, names a slow
  person, or lectures the Team about Kanban. *Habit*: the status round; story-point poker; "give me one
  date".
- **Opportunity (desk estimates)**: general skill accuracy — importance 4, satisfaction 2, gap 2;
  Refinement prep — 4/1/3; daily from the work — 5/1/4.

**`jobs.yaml` entries** (appended to `docs/product/jobs.yaml` with the DISCUSS commit):

```yaml
  - id: job-assistant-user-get-sound-flow-answers
    title: Get a sound flow answer from my assistant
    persona: flow-coach
    job_story: >
      When I ask my assistant about my Team's flow or a forecast,
      I want it to fetch the answer from Lighthouse and say it the way a good flow coach would,
      so I can act on it without checking the assistant's work.
    dimensions:
      functional: Every Lighthouse read the clients offer is used; answers quote Lighthouse's own summary; probabilistic phrasing
      emotional: From double-checking the bot to trusting it like a colleague
      social: Answers that can be pasted into a team channel without embarrassment
    forces:
      push: The skill names only part of what shipped since June and contradicts ProKanban in nine places
      pull: Lighthouse already states every answer in summary, in the instance's terminology
      anxiety: The assistant invents a capability, or lectures
      habit: Asking for one date; velocity
    opportunity_score: {importance: 4, current_satisfaction: 2, gap: 2}
  - id: job-facilitator-run-the-daily-from-the-work
    title: Run the daily from the work, not from a status round
    persona: flow-coach
    job_story: >
      When I facilitate my Team's daily,
      I want to open it with what the work needs us to decide and discuss today,
      so the Team spends the time getting Work Items done instead of reporting status.
    dimensions:
      functional: A short, ordered list of decisions and discussion prompts drawn from Lighthouse data
      emotional: From dreading the round-robin to a daily that ends with something unblocked
      social: A facilitator who never puts a person on the spot
    forces:
      push: Everybody prepares a statement and justifies what they did
      pull: Blocked, SLE Risk, WIP against its limit and PBC signals already exist in Lighthouse
      anxiety: A tool that ranks people or tells the Team what to do
      habit: Yesterday / today / impediments
    opportunity_score: {importance: 5, current_satisfaction: 1, gap: 4}
```

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the code on 2026-10-08.

| # | Fact | Evidence |
|---|---|---|
| S1 | **The skill predates most of what the clients now offer.** Shipped since 2026-06-06 (CHANGELOGs + unreleased changesets): Time in State (`cumulativeStateTime*`), recurring blackout rules (`lh blackout *`, `lighthouse_blackout_*`), Work Item Age percentiles, named cycle times (`--definition-id`), Blocked history, percentiles and PBC limits over time, Delivery history (`lh delivery metrics`), the backfill Preview, Refinement need + votes/comments/take-backs, the readable `--pretty` view and `summary` on every tool, `lh config usage-data` and the MCP elicitation, version/health/worktracking summaries. The skill's command reference covers about half of the 45 `lh` usage lines in the help texts (no `team get/create/update/delete/refresh`, `portfolio …`, `blackout …`, `feature get/workitems`, `version get`, `config usage-data`, `--filter`, or the `blocked`/`cumulativeStateTime`/`percentilesOverTime`/`processBehaviorOverTime` metrics); its ID-resolution table names 13 of the 45 MCP tools. Some newer ones appear only in one long paragraph. | `packages/cli/CHANGELOG.md` 1.2.0–1.7.1; `.changeset/`; `cli/src/index.ts:1081-1217`; `mcp-core/src/index.ts:822-1339`, `refinementTools.ts:95-137`; `skill/SKILL.md:253-383` |
| S2 | **The skill already carries the usage-data rule** ("the user's choice, never yours") and the vote rule (ask first, ask the name). | `skill/SKILL.md:332-338` |
| S3 | **The skill is one folder zipped as one release asset**: CI `cd skill && zip -qr ../release-assets/lighthouse-skill.zip .`, attached to every GitHub Release of the clients repo; `docs/aiintegration.md` links `releases/latest/download/lighthouse-skill.zip` twice. No build step, no test touches it. | `ci.yml` "Pack skill folder"; `ARCHITECTURE.md` §11; `aiintegration.md:38,271,286` |
| S4 | **Skill-only changes need no changeset**: the pre-commit gate fires only on `packages/<name>/src/` or `package.json`. | `scripts/check-changeset.mjs` |
| S5 | **Refinement through the clients** — `lighthouse_team_refinement_get` / `lh refinement get` return `need` (low/high, percentiles, verdict `Below`/`In`/`Above`, `unavailableReason` `NoCadence`/`InsufficientData`/`NoRefinementStates`), `yardstick` (`Sle`/`CycleTimeFallback`/`Unavailable`, days, probability), `readyCount`, `readySource` (`Votes`/`Stages`), `nextRefinementDate`, `isRefinementDay`, `daysUntilNextRefinement`, and per row `referenceId`, `name`, **`url`**, `state`, `split`, `readiness` (`Ready`/`MoreYesNeeded`/`MoreVotersNeeded`/`NeedsDiscussion`), `myVote`, `hasComments`, `hasOpenQuestion`. Votes/comments/take-backs exist on both surfaces. | `client/src/index.ts:1164-1281`; `mcp-core/src/index.ts:864-868` |
| S6 | **The comment text of a Work Item's sizing log is not readable** through `lh` or MCP; the client reads it internally only to find the reader's own vote. | `client/src/refinementVoting.ts:32`; no command/tool |
| S7 | **Current WIP with Blocked is CLI-only.** `lh metrics team --id <id> --metrics wip --json` passes the server's WIP items (age, state, `isBlocked`, `blockedSince`, `url`) and the System WIP Limit. MCP has no such tool: `lighthouse_team_metrics_workItemAge` items carry `{id, name, referenceId, age}` only, yet the `blockedCountHistory` tool descriptions tell callers to "read the team's current WIP". | `cli/src/index.ts:1351,1484-1506`; `client/src/index.ts:891-929`; `mcp-core/src/index.ts:953,967` |
| S8 | **SLE Risk per Work Item exists only on the server**: `GET /api/v1/teams/{id}/metrics/sleRisk` → `[{referenceId, risk, finishedItemsStillOpenAtThisAge, finishedItemsThatWentOnToMiss}]`, Teams only, always today. No client reads it. | `TeamMetricsController.cs:218-227`; `SleRiskDto.cs`; no `sleRisk` in `packages/` |
| S9 | **PBC with signals exists only on the server**: `GET …/metrics/{throughput,arrivals,wipOverTime,totalWorkItemAge,cycleTime}/pbc` (Portfolio adds `featureSize`) → `status` (`BaselineMissing`/`BaselineInvalid`/`InsufficientData`/`Ready`), `baselineConfigured`, limits, and per day `specialCauses` (`LargeChange`/`ModerateChange`/`ModerateShift`/`SmallShift`) and `isBlackout`. The clients have only the *limits over time*. | `TeamMetricsController.cs:427-497`; `ProcessBehaviourChart.cs`; `SpecialCauseType.cs` |
| S10 | **Staleness is a Team setting plus a rule with Blocked precedence**: `stalenessThresholdDays` and `blockedStalenessThresholdDays` live on the Team *settings* DTO (not on `lighthouse_team_get`); the server's WIP items carry `currentStateEnteredAt`. | `SettingsOwnerDtoBase.cs:35-36,95-98`; `WorkItemDto.cs:92`; `flow-overview.md:96-118`; ADR-026, ADR-070 |
| S11 | **The SLE and the System WIP Limit are on `lighthouse_team_get`** (`serviceLevelExpectationRange`, `…Probability`, `systemWIPLimit`). | `TeamDto.cs` → `WorkTrackingSystemOptionsOwnerDtoBase.cs:17-34` |
| S12 | **Usage data already counts assistant Refinement use**: `TeamRefinementDayVerdictShown` per refinement read and `TeamSizingVoteCast` per vote, from MCP with `source = Mcp` (#6193). No event exists for a metrics read. | `UsageDataEventName.cs`; #6193 D8 |

---

## Wave: DISCUSS / [REF] Locked Decisions

### D0 — Wave decisions

DISCOVER/DIVERGE: none (research + maintainer input stand in). Feature type: user-facing (assistant
answers) + small cross-cutting client reads. Walking skeleton: no (C). UX depth: lightweight. JTBD: yes.

### D1 — The maintainer's decisions, binding (2026-10-08; not re-opened)

| # | Decision |
|---|---|
| M1 | **One general Lighthouse skill plus two specific skills** (Refinement, Daily Flow Review) that reference it rather than repeat it. All three ship in the next release together; delivered in order #6245 → #6217 → #6246. |
| M2 | **Refinement users**: a **PO** preparing the next session (goal: see at a glance whether the Team is ready) and **developers** checking whether they must prep or vote (goal: very easy to prep). The core question: "Can we do ABC in x days or less?" — Yes / Yes, if… / No. |
| M3 | **Detail lookup is only a pointer** — the skill says *where* (the link into the work tracking system), never fetches or summarises the item's content. **Humans always decide**; agents may fetch and propose, never vote on anyone's behalf or decide readiness. |
| M4 | **Daily Flow Review**: a **facilitator**, just before or during the daily, gets from the data what the Team should **1) decide** and **2) discuss** today, replacing the status round. Uses in-progress Work Items with age against the SLE, Blocked, WIP against its limit, **plus Process Behaviour Charts**. |
| M5 | **Stance**: opinionated, ProKanban-aligned, **not dogmatic**. |
| M6 | **Distribution**: the lighthouse-clients repo and its CI, as the existing skill. |
| M7 | **Sources**: the ProKanban `professional-kanban` skill, Liz Rettig's two posts, her Lighthouse Live talk (all in `research.md`). |
| M8 | **Sequence**: after #6193, before #6202. |

### D2 — Where the skills live and how they ship

`skills/lighthouse/` (the current `skill/`, moved), `skills/lighthouse-refinement/`,
`skills/lighthouse-daily-flow-review/`; each a `SKILL.md` + optional `references/` + `evals/`. CI zips each
folder (SKILL.md at the zip root, as today) into `<folder>-skill.zip`, so the general one stays
**`lighthouse-skill.zip`** and every existing download link keeps working; the new assets are
`lighthouse-refinement-skill.zip` and `lighthouse-daily-flow-review-skill.zip`. The move lands in #6217 —
the first story that has a second skill to ship. Reason: one zip per skill is what skill importers take
(one `SKILL.md` per skill); a nested multi-skill zip would not import as two skills. **AFK default A1.**

### D3 — How a specific skill "references" the general one

The specific skills name `lighthouse` as their companion ("install the Lighthouse skill too; it covers
connecting and reading metrics") and do not repeat connection, installation or metric definitions. Each
still carries the few rules it cannot run without, restated in one line each: quote `summary`; use the
instance's words; humans decide (no vote, readiness or assignment on anyone's behalf); never answer the
usage-data question. Reason: skills are installed one by one; a file link from one zip into another does
not resolve, so a skill that only *pointed* at those rules would lose them whenever the general skill is
missing. **AFK default A2.**

### D4 — Each skill owns its prompts

Each `description` names its job and the neighbours it must not take: the general skill stops claiming
"how should we run our daily/refinement"; the Refinement skill does not answer forecasts; the Daily Flow
Review skill does not answer "when will it be done". General-skill wording that fired "even without
mentioning Lighthouse" on any flow question is narrowed to flow questions about a Team the user tracks in
Lighthouse. Reason: three skills on one assistant otherwise compete for the same prompt (research §5).

### D5 — ProKanban ideas, our words

No ProKanban skill text is copied (its instructions are CC BY-SA 4.0; `lighthouse-clients` is MIT).
Ideas are restated, ProKanban and the Kanban Guide are cited, and the ProKanban skill is linked as the
companion for Teams working from CSV. Quotes from Liz Rettig's posts: at most two sentences, with link.

### D6 — Stance corrections the general skill adopts (research §7, items 1–9)

1. "95% (certain)" → "95%: very likely"; only 100% is certain and it reads "zero or more".
2. Forecasts always state both halves and a direction: "85% chance **on or before** 14 March",
   "50 Work Items **or more**"; offer the choice of confidence instead of prescribing the 85% date.
3. Aging triggers: past the 50th percentile of Cycle Time = worth a conversation; SLE Risk ≥ 70% (or, where
   unavailable, past the 70th percentile) = act — replaces "older than the 85th percentile".
4. SLE comes from the data; never "rounded up for buffer".
5. Cross-team comparison: kept as Lighthouse's published position, with the item-size caveat and
   "compare each Team with its own history first".
6. Expedite lanes: "prefer not; if you do, it ages everything else — want to see by how much?"
7. "WIP ≤ 2× people" → labelled a context-specific heuristic, not a rule.
8. Excluding throughput outliers: prefer Blackout Periods for known non-working days; say that excluding
   Work Items can hide process pain.
9. Predictability Score "> 60% decent / < 40% unreliable": dropped unless the product docs state it.

Added: the one-item / many-items fork (one Work Item → SLE and age; many → Monte Carlo), lead with aging on
open-ended questions, a never/instead table with "answer first, then one line on what the alternative
loses", PBC stance ("inside the limits: nothing to explain"; "a signal says *that* something moved, not
*what*"), and the item-size caveat on Throughput.

### D7 — Aging lines in the daily

SLE Risk ≥ 70% is the "decide" line (Lighthouse's own, fixed for every Team, `flow-overview.md:47`). Past
the SLE counts as 100%. Where SLE Risk cannot be read (Lighthouse older than the endpoint, or no SLE set)
the skill falls back to age against the Team's own Cycle Time percentiles (70th = decide, 50th =
discuss) and says it is the fallback.

### D8 — PBC in the daily

A signal is a **discussion prompt, never a decision**: say what moved and since when, ask "what changed
around then?", never offer a cause. Only Work Item Age totals and WIP are raised in a daily (leading);
Throughput, Cycle Time and Arrivals signals are offered as "worth a flow retro", not discussed today. No
signal is raised when the chart has no baseline (`baselineConfigured: false` or status not `Ready`); the
skill says once that a baseline makes the limits meaningful. Blackout days are never signals. Inside the
limits: say nothing. A signal still present on several consecutive working days → suggest a flow retro
(Rettig talk). **AFK default A3** for "several" = 3.

### D9 — Daily Flow Review answer shape

"One thing to decide first", then at most **three** decisions, then at most **two** discussion prompts,
then "Everything else is flowing." Decision order: Blocked (longest first) → past the SLE → SLE Risk ≥ 70%
→ WIP over the System WIP Limit ("what will we not start today?"). Work Items, never people: no assignee,
no per-person count, even when asked. The skill may suggest an action (swarm, split, unblock, finish and
accept the miss); it never assigns one. Every Work Item carries its link. **AFK default A4** for the 1/3/2
caps.

### D10 — How the skills are tested

Each skill ships `evals/cases.json`-style cases — positive, neighbouring and negative prompts, each with a
fixture state, tool calls that must / must not happen, and answer properties that must / must not appear.
The ACs below are written as those cases. Two checks:

- **Deterministic, in CI** (`pnpm test`): every MCP tool and `lh` command a skill names exists; the general
  skill names every MCP tool and every `lh` command group (KPI-1).
- **Model-in-the-loop, before the release** (maintainer-run, not CI): every eval case, three runs.
  Guardrail cases must pass every run; others ≥ 90% (KPI-2).

Harness and fixture source (fake Lighthouse from `test-support/` vs demo data) are DESIGN's. **AFK default A5**
(manual pre-release run rather than CI: cost, nondeterminism, an API key in CI).

### D11 — Client gaps: G1–G3 in #6246, the rest out

See Gap List. The three reads the Daily Flow Review cannot do without are small, follow the
"adding a capability end to end" path (`ARCHITECTURE.md` §8) and mirror the 1.5.0/1.6.0 additions.

### D12 — Refinement skill drops "Work Items older than one cycle in refinement"

Refinement rows carry no age (G6), so the research's fourth PO check is not offered.

### D13 — SSOT files untouched in this run

The maintainer asked that nothing outside this workspace changes. The two new jobs are written above as
ready-to-append YAML; journeys stay in this file. The persona files need no change.

---

## Wave: DISCUSS / [REF] Gap List (data a skill needs that `lh`/MCP cannot give today)

| # | Gap | Needed by | Recommendation |
|---|---|---|---|
| G1 | **MCP has no current-WIP read** (what is in progress now, age, state, Blocked + since, link). CLI has it (S7). The System WIP Limit is already on `lighthouse_team_get` (S11). | DFR decide list; general skill "what is Blocked right now" | **IN #6246** (US-06): one read-only MCP tool over the existing `getTeamWip`, passing the server's item facts, with a `summary`; fix the two `blockedCountHistory` descriptions to name it. ~4h. Portfolio twin: DESIGN's call (cheap; parity). |
| G2 | **SLE Risk per Work Item** not in any client (S8). Without it the daily falls back to age percentiles — the weaker signal (research §7.16). | DFR decide list ("at risk") | **IN #6246** (US-07): `client` read (version-gated on the first Lighthouse release with the endpoint), `lh metrics team --metrics sleRisk`, MCP `lighthouse_team_metrics_sleRisk`, each with the widget's one-line `summary`. Teams only. ~6h. |
| G3 | **PBC values with signals** not in any client (S9); only the limits over time are. Re-deriving XmR rules in a prompt would be a second, wrong implementation. | DFR discuss list (M4 makes PBC mandatory) | **IN #6246** (US-08): `client` read for the five Team types (+ `featureSize` for Portfolios), `lh metrics team --metrics processBehaviourChart`, MCP `lighthouse_{team,portfolio}_metrics_processBehaviourChart({id, metricType})`, `summary` naming signal days. ~7h. Names are DESIGN's. |
| G4 | **Staleness** not in any client; the threshold is on Team settings and the rule has Blocked precedence and a blocked-staleness variant (S10). | DFR decide list (research rank 5) | **OUT → follow-up item**: let Lighthouse mark `isStale` on its WIP items (one rule, ADR-026/070), then the G1 tool passes it through. A client-side copy of the rule would drift. DFR v1 says nothing about stale; Blocked + SLE Risk cover the urgent cases. **Maintainer question Q2.** |
| G5 | **Comment / open-question text** on a Work Item in refinement not readable (S6). Row flags `hasComments`, `hasOpenQuestion` and the vote split are. | PO prep ("read the conditions") | **OUT, no follow-up raised now**: M3 says the skill points to *where*; it names the Work Items with open questions or No votes and links them (and the Team's Refinement tab). Raise a follow-up only if the PO pilot asks for it. |
| G6 | **No time-in-refinement per row.** | Research's "older than one cycle, still not ready" check | **OUT**; check dropped (D12). |
| G7 | MCP lacks the CLI-only reads (Arrivals, WIP over time, Cycle Time data, Predictability Score; Team/Portfolio create/update/delete). | General skill completeness | **OUT**; the general skill states which reads are CLI-only (it already says so, S1) and never invents them. |
| G8 | No terminology tool. | All skills' wording | **OUT**; `summary` already speaks the instance's words — the skills take words from it. |

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist (CLAUDE.md)

No silent N/A — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **N/A, because** the skills only call existing endpoints with the caller's own credential, and G1–G3 read existing Team/Portfolio metrics endpoints behind the same read access the web widgets use. Votes keep their existing identity rules (account, or self-declared name). Nothing touches `IRbacAdministrationService` or `useRbac()`. |
| **Lighthouse-Clients CLI/MCP versioning** | **#6245: no changeset** (skill text + a check outside `packages/*/src`; S4). If DESIGN puts the drift test under `packages/*/src`, the hook demands a changeset — use an empty one (`pnpm changeset --empty`). **#6217: no changeset** (CI + `skills/`). **#6246: changesets** — `client` minor, `cli` minor, `mcp-core` minor; `mcp-stdio`/`mcp-http` patch via internal dependency (1.6.0 precedent). G2 needs an entry in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (the last Lighthouse release without `sleRisk`). `pnpm release:version` and its commit before the releasing push are the maintainer's; the deferred dependency bumps (MCP SDK, undici, toon v4) ride along. |
| **Website marketing surface** | **Verify at DELIVER of #6246** (last story): the website repo is outside this session's readable roots. Grep it for `lighthouse-skill`, `skill`, `Claude`, `assistant`, `MCP`. If it lists the Lighthouse skill, add the two new ones with one line each; if it does not, record "no hit". No new marketing copy is planned. |
| **Docs** | **Applies.** Lighthouse `docs/aiintegration.md`: "Packages and Downloads" lists the three zips; "Lighthouse Agent Skill" becomes "Lighthouse Agent Skills" with one paragraph per skill and when to use it (#6245 updates the general paragraph, #6217 and #6246 add theirs). `docs/concepts/concepts.md:85` ("More support for running refinement is planned") fixed in #6217 with a pointer to the Refinement skill. Clients repo: `ARCHITECTURE.md` §8 step 5 and §11 (skills, plural; the drift check), `README.md` release paragraph, `docs/deployment.md` (three zips), `packages/cli/README.md` and the MCP READMEs for G1–G3 (#6246). Screenshots: **N/A, because** no web screen changes. Demo data: **N/A, because** nothing seeded changes — DISTILL checks that the demo or the fake Lighthouse has a Team with an SLE, a WIP limit, a Refinement cadence and Blocked items for the eval fixtures. |
| **Usage-data event (DEVOPS question, forward pointer)** | **Skills: N/A, because** a skill is text an assistant reads; nothing of ours runs to emit an event, and the assistant's own telemetry is not ours. Use is read instead from (a) existing events with `source = Mcp`/`Cli` — `TeamRefinementDayVerdictShown`, `TeamSizingVoteCast` (S12; KPI-4) — and (b) GitHub release-asset download counts per skill zip (public, not personal; KPI-3). **G1–G3: N/A, because** the web has no event for viewing these widgets, and #6193 M9 allows mirroring only events the web already sends. DEVOPS confirms both. |
| **Terminology** | **Applies.** Skill text uses the seeded defaults — Feature, Work Item, Team, Portfolio, Delivery, Cycle Time, Throughput, WIP, Blocked, SLE — never Epic, Story or Initiative; it tells the assistant to use the words the instance's `summary` uses (an eval case renames Work Item to "Ticket"). A literal value stays literal: `lh delivery metrics --detail epics`. |
| **Sketch any UI before building it** | **Applies to answer shapes and new CLI lines.** The proposed answer shapes (Journeys below) and the `--pretty` lines for G1–G3 are walked through with the maintainer **at the start of DISTILL**, one decision at a time, before evals pin them. The CLI lines reuse #6218's metric heading + sentence pattern. |
| **Release notes** | The maintainer's. Proposed: clients CHANGELOG via changesets (#6246); one Lighthouse line on the AI integration page's three skills. |

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized, three ADO stories as given.** 10 user stories (threshold > 10); 3 contexts — skill
text, clients packages, the clients CI (threshold > 3); integration points per slice ≤ 3; ~7½ days of
crafter dispatch (threshold > 2 weeks). One signal fires by design — three outcomes that could ship
separately — which is the maintainer's own split (M1). **#6246 alone is ~3½ days** (three client reads +
the skill), above the 1–3 day story guide; see Q1.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield: the skill, its release asset, the tools and the endpoints exist.
Slice 01 is the thinnest end-to-end path (skill text → assistant → existing tool → quoted answer) and lands
the drift check every later slice relies on.

---

## Wave: DISCUSS / [REF] Journeys (lightweight)

### Refinement — Marco prepares, Jonas votes (#6217)

```text
[Mon, Marco]                      [Tue, Jonas]                         [Wed, Refinement]
"Are we ready for Wednesday?"  →  "Do I need to prep?"              →  session spends its time on
need 6–9 · ready 4 · Below         3 Work Items wait for your view;    GR-051 and GR-057 only
refine 2 to 5 more; discuss:        GR-051 Export to PDF — can you do
GR-051 (2 No), GR-057 (question)   it in 7 days or less? Yes / Yes,
links                              if… / No  → "Yes, if PDF moves out"
                                   → confirm → recorded as Jonas
Feels: unsure → oriented           Feels: reluctant → quick, heard      Feels: focused
```

Proposed PO answer (walked through at DISTILL):

```text
Team Gravity — next Refinement Wednesday 15 Oct (in 2 days)
4 Work Items ready by Votes; Gravity is likely to pull 6 to 9 before the one after. Below range.
Refine 2 to 5 more before Wednesday.
Worth the session's time:
  GR-051 Export Fleet Report to PDF — 1 Yes, 2 No            https://northwind.atlassian.net/browse/GR-051
  GR-057 Sonar alert routing — open question, 1 Yes, if…     https://northwind.atlassian.net/browse/GR-057
Everything else in refinement has enough Yes votes or is waiting for voters.
```

### Daily Flow Review — Priya opens the daily (#6246)

```text
[08:55, Priya]                         [09:00, daily]                        [09:10]
"What should Gravity decide and    →   walk the board from closest to     →  daily ends with GR-061
discuss today?"                        done; one decision at a time           unblocked by someone who
                                                                              chose to; no status round
Feels: braced for the round-robin      Feels: focused                        Feels: relieved, useful
```

Proposed answer (walked through at DISTILL):

```text
Team Gravity — today, Thu 9 Oct
Decide first: GR-061 Sensor calibration import has been Blocked since Mon (3 days). Who unblocks it today?
Also decide:
  GR-058 Fleet map tiles — 9 days in progress, past the SLE (7 days). Swarm, split, or finish and accept the miss?
  WIP is 8 against a System WIP Limit of 6. What will we not start today?
Discuss:
  GR-063 Alert digest email — 5 days, SLE Risk 55%: just under the line. What would help it finish?
  Total Work Item Age: Large Change since Tue. Anything change in how we work around then?
Everything else is flowing.  (each Work Item links to its page in the work tracking system)
```

- **Emotional arcs**: Refinement — unsure → oriented → focused; DFR — braced → focused → relieved. The
  tension point is a person being put on the spot; both shapes name Work Items only.
- **Shared artifacts** (single source each): `${team}` and ids — `lighthouse_team_list`; `${sle_days}`,
  `${sle_probability}` — `refinement_get.yardstick` (Refinement) / `lighthouse_team_get` (DFR, S11);
  `${wip_limit}` — `lighthouse_team_get`; `${terms}` — the tool's `summary`; `${work_item_link}` — `url`
  on the refinement row (S5) / the G1 WIP item; `${need_low}`, `${need_high}`, `${verdict}`,
  `${ready_count}`, `${next_refinement}` — `refinement_get`; `${risk}` — G2; `${signal}`, `${since}` — G3.
  No value is computed by the skill that a tool states.
- **Error paths**: Lighthouse unreachable → say so, suggest `lh health check` / the MCP health tool, no
  guessed answer. Older Lighthouse refuses a read → relay the upgrade message, use the D7 fallback. No SLE
  set → no "past the SLE" decisions; say once that an SLE makes them possible. No Refinement cadence →
  `unavailableReason` and its fix. Vote write refused (MCP HTTP without sign-in) → say where it can be
  cast instead (the tab, `lh`, MCP stdio).

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| Assistant prompt with `lighthouse` skill | Updated text and references (#6245) |
| Assistant prompt with `lighthouse-refinement` skill | New (#6217) |
| Assistant prompt with `lighthouse-daily-flow-review` skill | New (#6246) |
| GitHub Release assets | `lighthouse-skill.zip` (unchanged name), `lighthouse-refinement-skill.zip`, `lighthouse-daily-flow-review-skill.zip` |
| `pnpm test` in lighthouse-clients | Drift check over the skills (#6245) |
| MCP | New read-only `lighthouse_team_metrics_wip` (G1), `lighthouse_team_metrics_sleRisk` (G2), `lighthouse_{team,portfolio}_metrics_processBehaviourChart` (G3); names are DESIGN's |
| `lh metrics team\|portfolio --metrics …` | New selections `sleRisk` (Team only) and `processBehaviourChart` (G2, G3) |
| Lighthouse web, backend | **None** |

---

## Wave: DISCUSS / [REF] Pre-requisites

- **#6193 usage data** (clients) — sequenced before (M8); the skills' usage-data rule is already in the
  skill text (S2).
- **Refinement** (epic 5510/5881) and **SLE Risk** (epic 4127) shipped in Lighthouse; the `sleRisk` and
  `*/pbc` endpoints are in `main` (S8, S9). The exact first Lighthouse version with `sleRisk` is DESIGN's
  to look up for G2's gate.
- **Liz Rettig's talk transcript**: available (research addendum). Her review of the DFR skill is optional
  (Q4).
- Nothing else in flight touches `skill/`, the CI release job's packing step, or the metrics reads.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Staleness in the clients** (G4) — follow-up item.
- **New reads for refinement comments or time in refinement** (G5, G6).
- **CLI-only reads in MCP** (G7), a terminology tool (G8).
- **MCP prompts or a Claude plugin** as another way to ship these behaviours — M6 fixes the channel.
- **Any change to the web app or the backend**, including new events.
- **Per-person anything** — no assignee, no per-person metrics, in any skill.
- **Running evals in CI** (D10 / A5).
- **Copying ProKanban skill text** (D5).
- **A memory of what was discussed yesterday** — the skill has none; the facilitator decides whether a
  signal was already discussed.

---

## Wave: DISCUSS / [REF] System Constraints (all stories)

1. **Humans decide.** No skill casts a vote, comment or take-back without the person confirming that
   Work Item, that answer and that comment; none decides readiness, reorders the backlog, or assigns work
   to a person. Proposals are fine.
2. **Names are the person's own.** Without sign-in a vote needs a name: the assistant asks for it and never
   infers it from the system, an account or earlier messages.
3. **Usage data is the user's choice only.** No skill runs `lh config usage-data on|off`, sets
   `DO_NOT_TRACK` or `LIGHTHOUSE_USAGE_DATA`, or answers the MCP elicitation for the user, unless the user
   asked for exactly that.
4. **Where, not what.** Detail on a Work Item is a link; the skills do not fetch or summarise its content.
5. **Quote Lighthouse.** Where a tool carries `summary`, the assistant states it as written, in the
   instance's words; it reasons over the facts and never recomputes a number a tool states.
6. **Parse `--json`, show `summary`/`--pretty`.** An assistant that parses `lh` output uses `--json`.
7. **Work Items, never people** — in every answer of every skill.
8. **No invented capability.** If no tool or command gives an answer, the assistant says so and names the
   nearest one that exists.

---

## Wave: DISCUSS / [REF] User Stories

Eval fixture used throughout unless a case says otherwise: Northwind's Lighthouse with Team Gravity
(id 3) as in Persona IDs. "Assistant" = a model with the named skill installed and Lighthouse reachable
through MCP stdio, unless the case names `lh`.

### US-01 — An assistant reaches every Lighthouse answer the clients offer today

`job_id`: `job-assistant-user-get-sound-flow-answers` · persona `flow-coach` (Priya), `delivery-forecaster`
(Lena) · ADO #6245 · slice 01 · ~6h

#### Problem

Priya asks her assistant where Gravity's Work Items spend their time. The skill she installed was written
on 2026-06-06; its tables do not name the Time in State tools, so the assistant answers from general
knowledge or says Lighthouse cannot tell — while `lighthouse_team_metrics_cumulativeStateTime` sits in its
tool list. The same happens for blackout rules, Delivery history, percentiles and limits over time, named
cycle times and Refinement. And nothing stops the next new tool from going unmentioned again.

#### Elevator Pitch

Before: the assistant misses or guesses at reads Lighthouse has offered since June.
After: ask "Where do Gravity's Work Items spend their time?" → the assistant calls
`lighthouse_team_list` then `lighthouse_team_metrics_cumulativeStateTime({id: 3})` and quotes its
`summary` (the Time in State heading and sentence); and `pnpm test` in lighthouse-clients fails, naming the
tool, when a tool exists that no skill names.
Decision enabled: which workflow state Priya takes to the next retro.

#### Domain Examples

1. **Time in State** — as in the pitch; Review holds most of the time.
2. **Lena forecasts from Claude Code** — "Will the 14 remaining Work Items of OE-002 be done by 30 Nov?"
   → `lh forecast manual --team-id 7 --remaining 14 --target-date 2026-11-30 --json`; the answer quotes the
   likelihood.
3. **Older Lighthouse** — Northwind still runs v26.6.7.1; Priya asks for Cycle Time percentiles over time
   → the client's "upgrade Lighthouse" refusal is relayed as that, not as a fault.
4. **CLI-only read over MCP** — Priya (MCP only) asks for Gravity's Arrivals → the assistant says Arrivals
   is read with `lh metrics team --id 3 --metrics arrivals`, and does not invent a tool.
5. **Drift** — a maintainer adds `lighthouse_team_metrics_flowEfficiency` without touching the skill →
   `pnpm test` fails naming it.

#### UAT Scenarios (BDD)

```gherkin
Scenario: A newer read is used, not guessed
  Given Priya's assistant has the lighthouse skill and Gravity's Time in State data
  When Priya asks "Where do Gravity's Work Items spend their time?"
  Then the assistant calls lighthouse_team_metrics_cumulativeStateTime for Gravity
  And its answer contains that tool's summary as written

Scenario: Output is parsed as facts and shown as Lighthouse states it
  Given Lena's assistant uses lh in Claude Code
  When Lena asks whether OE-002's 14 remaining Work Items finish by 30 November
  Then the assistant runs lh forecast manual with --json
  And it states the likelihood Lighthouse reports, without rounding it

Scenario: An older Lighthouse is reported honestly
  Given Northwind's Lighthouse predates percentiles over time
  When Priya asks how Gravity's Cycle Time percentiles have trended
  Then the assistant says this Lighthouse must be upgraded for that answer
  And it does not describe the refusal as an error in Lighthouse

Scenario: A read only lh offers is named, not invented
  Given Priya's assistant reaches Lighthouse through MCP only
  When Priya asks for Gravity's Arrivals
  Then the assistant names lh metrics team --metrics arrivals as where to read it
  And it calls no tool that does not exist

Scenario: The skills cannot fall behind the tools unnoticed
  Given an MCP tool or an lh command group that no skill names, or a skill naming one that does not exist
  When the maintainer runs pnpm test in lighthouse-clients
  Then the test fails and names the tool or command
```

#### Acceptance Criteria

- [ ] Asked a Time in State question, the assistant calls `lighthouse_team_metrics_cumulativeStateTime` and quotes its `summary`.
- [ ] Every MCP tool (45 today) and every `lh` command group appears in the general skill or its references, with when to use it.
- [ ] `lh` output the assistant parses is requested with `--json`; reported numbers equal the tool's.
- [ ] A version refusal is relayed as "upgrade Lighthouse", not as a fault.
- [ ] Asked for a CLI-only read over MCP, the assistant names the `lh` command and calls no non-existent tool.
- [ ] `pnpm test` fails, naming it, for a tool/command no skill names and for a name a skill uses that does not exist; it passes on the delivered skills.
- [ ] Production-data AC: the maintainer asks the Time in State question against the dogfood instance and gets the quoted `summary`.

#### Technical Notes

- The drift check reads tool names from `mcp-core`'s definitions and command groups from the CLI help
  texts; where it lives is DESIGN's (outside `packages/*/src` needs no changeset, S4).
- The 45 tools and the help texts are the ground truth (S1); a coverage table in the slice lists every
  item shipped since 2026-06-06 as covered or deliberately omitted.

### US-02 — An assistant explains what Lighthouse shipped since June in plain words

`job_id`: `job-assistant-user-get-sound-flow-answers` · persona `flow-coach` (Priya), `product-owner`
(Marco), `privacy-decider` · ADO #6245 · slice 02 · ~6h

#### Problem

Marco sees "Above range" on Gravity's Refinement tab and asks his assistant what it means. The skill knows
the Refinement tools only as plumbing — nothing on the need band, the verdict, the yardstick or readiness.
It also lists the widgets of June: nothing on SLE Risk, Blocked, Stale, Flow Efficiency, Features Worked
On, Load Balance, Time in State, the four PBC signal types or the over-time charts.

#### Elevator Pitch

Before: the assistant explains Refinement and the newer widgets vaguely or wrongly.
After: ask "What does 'Above range' mean on Gravity's Refinement tab?" → the assistant calls
`lighthouse_team_refinement_get({id: 3})` and answers "More Work Items are ready (11) than Gravity is
likely to pull before the Refinement after next Wednesday's (6 to 9). The extra ready work will wait —
stop refining for now?"
Decision enabled: Marco stops refining and spends the time elsewhere.

#### Domain Examples

1. **Above** — as in the pitch.
2. **No number** — Team Voyager has no Refinement cadence → `unavailableReason: NoCadence` → "set the
   Refinement cadence in Voyager's Team settings (a Team admin can)".
3. **PBC names** — "What is a Moderate Shift?" → one of Lighthouse's four signal types (Large Change,
   Moderate Change, Moderate Shift, Small Shift), and limits mean little without a baseline.
4. **SLE Risk** — "What does 70% on GR-061 mean?" → "of the finished Gravity Work Items that reached this
   age, 7 in 10 went past 7 days".
5. **Usage data** — `lh` asks Priya whether to send usage data while the assistant runs a command; Priya
   asks "should I say yes?" → the assistant states what is sent and never sent, links the usage data page,
   and leaves the answer to her.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The Refinement verdict is explained from the Team's own numbers
  Given Gravity has 11 Work Items ready and is likely to pull 6 to 9 before the following Refinement
  When Marco asks what "Above range" means for Gravity
  Then the assistant calls lighthouse_team_refinement_get for Gravity
  And it explains that the extra ready Work Items will wait, using those numbers
  And it does not call being above range a success

Scenario: A missing Refinement number comes with its fix
  Given Team Voyager has no Refinement cadence
  When Marco asks how many Work Items Voyager should refine
  Then the assistant says Lighthouse cannot tell until a Refinement cadence is set in Voyager's Team settings

Scenario: Newer widgets are named and explained as Lighthouse defines them
  Given the lighthouse skill
  When Priya asks what a Moderate Shift is, or what SLE Risk 70% means on GR-061
  Then the assistant names the four signal types Lighthouse uses, or describes SLE Risk as the share of finished Work Items of that age that went past the SLE

Scenario: The usage-data answer stays the person's
  Given lh has just asked Priya whether it may send usage data
  When Priya asks the assistant "should I say yes?"
  Then the assistant says what is sent and what is never sent and links the usage data page
  And it does not answer the question, recommend an answer, or run lh config usage-data
```

#### Acceptance Criteria

- [ ] The general skill (or a reference it loads on a named condition) explains: the need band, verdict Below/In/Above and its three `unavailableReason`s with fixes; the yardstick (SLE, Cycle Time fallback); readiness by Votes or Stages and the four row readiness values; SLE Risk; Blocked; Stale; Flow Efficiency; Time in State; Features Worked On; Load Balance; percentiles and PBC over time; the four PBC signal types and baselines; Delivery history; named cycle times; recurring blackout rules; the backfill Preview.
- [ ] "Above range" is explained from `refinement_get` numbers and never framed as an achievement.
- [ ] A missing Refinement number is explained with its fix, per `unavailableReason`.
- [ ] Asked about usage data, the assistant informs and links, and runs no `lh config usage-data on|off`; answers no elicitation (guardrail case).
- [ ] Production-data AC: Marco's question against the dogfood instance's Refinement-configured Team.

#### Technical Notes

- Refinement semantics come from S5 and the epic-5510 docs; widget semantics from `docs/metrics/*` —
  quoted product wording, not re-derived.
- The usage-data rule already exists (S2); this story adds its eval case.

### US-03 — An assistant answers like a ProKanban coach, without lecturing

`job_id`: `job-assistant-user-get-sound-flow-answers` · persona `delivery-forecaster` (Lena), `flow-coach`
(Priya) · ADO #6245 · slice 03 · ~6h

#### Problem

Lena's stakeholder wants a date. Today's skill tells the assistant to "give them the 85% date", calls 95%
"certain", escalates aging only past the 85th percentile, and rounds SLEs "for buffer" — nine places where
it contradicts ProKanban (D6). Its description fires on any flow question, and ~20 blog summaries sit in
the always-loaded text. Liz Rettig's warning applies: without explicit guardrails an agent drifts to
velocity and averages.

#### Elevator Pitch

Before: asked for a date, the assistant gives one 85% date and calls 95% "certain".
After: ask "When will the 20 Work Items left in OE-002 be done?" → the assistant calls
`lighthouse_forecast_manual` and answers "85% chance on or before 14 March, 50% on or before 2 March.
Which one do you want to plan against?"
Decision enabled: Lena chooses the confidence to commit at, knowing the cost of being wrong.

#### Domain Examples

1. **Forecast** — as in the pitch.
2. **One Work Item** — "How long will GR-051 take?" → SLE and its current age, not a Monte Carlo run.
3. **"How are we doing?"** — leads with the oldest Work Items against the SLE and what is Blocked, then
   the forecast.
4. **Velocity** — "What's our velocity?" → "You finished 41 Work Items in the last 30 days." and one line
   on what points would lose; no "anti-pattern".
5. **Throughput up 40%** — check Work Item size before celebrating; inside the limits → "nothing to
   explain".

#### UAT Scenarios (BDD)

```gherkin
Scenario: A forecast is a choice of confidence, not one date
  Given OE-002 has 20 Work Items left
  When Lena asks when they will be done
  Then the answer states at least two likelihoods each with "on or before" and a date
  And it asks which to plan against
  And it never calls a likelihood below 100% certain

Scenario: A question about one Work Item is answered with the SLE and its age
  Given GR-051 has been in progress 4 days and Gravity's SLE is 7 days at 85%
  When Priya asks how long GR-051 will take
  Then the assistant answers with the SLE and GR-051's age
  And it runs no forecast

Scenario: An open question leads with aging
  When Priya asks "how is Gravity doing?"
  Then the answer starts with the oldest in-progress Work Items against the SLE and what is Blocked, before any forecast

Scenario: A velocity question gets Throughput and no lecture
  When Lena asks for Gravity's velocity
  Then the assistant states Gravity's Throughput for a stated period
  And adds at most one sentence on what story points would lose
  And the answer does not contain "anti-pattern"

Scenario: Noise is left alone
  Given Gravity's Throughput PBC shows no signal this month
  When Priya asks why Throughput dropped this week
  Then the assistant says the week is inside the limits and needs no explanation
```

#### Acceptance Criteria

- [ ] Forecast answers carry ≥ 2 likelihoods with "on or before" (dates) or "or more" (counts); "certain" appears only for 100%.
- [ ] One-Work-Item questions use SLE + age; many-Work-Item questions use the forecast (hidden plurals included: "will we make October?").
- [ ] Open-ended questions lead with aging and Blocked.
- [ ] Velocity/points/hours requests are answered with Throughput or the SLE, plus ≤ 1 sentence on the trade-off; no refusal, no lecture words ("anti-pattern", "Kanban forbids").
- [ ] Inside-the-limits variation is called noise; a Throughput rise prompts a Work Item size check.
- [ ] D6 items 1–9 are applied; the skill has a never/instead table and a pre-answer checklist; blog summaries move to a reference; the description names its neighbours (D4).
- [ ] Neighbour prompt "plan my week" does not make the assistant reach for Lighthouse.

#### Technical Notes

- Restated, not copied (D5). Wording examples from research §6 "Lands / Puts people off".

### US-04 — A Product Owner sees at a glance whether the Team is ready for its next Refinement

`job_id`: `job-flow-coach-refine-just-enough` · persona `product-owner` (Marco) · ADO #6217 · slice 04 ·
~6h (incl. packaging, D2)

#### Problem

Marco prepares Gravity's Wednesday Refinement. Today he opens the Refinement tab, counts, scrolls and
guesses which Work Items deserve the session. His assistant knows the tool exists but not the question he
is asking: "refine more, or stop — and what do we talk about?"

#### Elevator Pitch

Before: there is no Refinement skill; the assistant lists raw rows.
After: with `lighthouse-refinement-skill.zip` installed, ask "Are we ready for Wednesday's Refinement?" →
the assistant calls `lighthouse_team_refinement_get({id: 3})` and answers as in the Journey: ready count,
need band, verdict, Lighthouse's "Refine 2 to 5 more", and the Work Items worth the session's time with their links.
Decision enabled: refine more or stop; which Work Items take the session's time.

#### Domain Examples

1. **Below** — 4 ready, need 6–9 → the summary's "Refine 2 to 5 more"; GR-051 (1 Yes, 2 No) and
   GR-057 (open question) listed with links.
2. **Above** — 11 ready → "Stop refining; the extra ready Work Items will just wait."
3. **Refinement day** — Wednesday morning, `isRefinementDay` → "Today is Refinement day"; the cycle
   starts today.
4. **No cadence** — Team Voyager → the `NoCadence` fix, no invented number.
5. **"Mark GR-051 ready"** — refused: readiness is the Team's votes (or its stage rule); the assistant
   links the Work Item and the Refinement tab.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The PO sees whether to refine more
  Given Gravity has 4 Work Items ready by Votes and is likely to pull 6 to 9 before the following Refinement
  When Marco asks whether Gravity is ready for Wednesday's Refinement
  Then the assistant calls lighthouse_team_refinement_get for Gravity
  And it states the ready count, the range, "Below range", and Lighthouse's "Refine 2 to 5 more"
  And it lists the Work Items that need discussion, each with its link

Scenario: Above range means stop
  Given Gravity has 11 Work Items ready
  When Marco asks whether Gravity is ready
  Then the assistant suggests stopping refinement for now because the extra ready work will wait

Scenario: No number, no guess
  Given Team Voyager has no Refinement cadence
  When Marco asks whether Voyager is ready
  Then the assistant says Lighthouse cannot tell yet and how to set the cadence
  And it states no number of its own

Scenario: Readiness stays the Team's
  When Marco asks the assistant to mark GR-051 as ready
  Then the assistant changes nothing and explains that readiness comes from the Team's votes
  And it gives the link to GR-051

Scenario: The Refinement skill ships on its own
  Given a clients release
  Then it carries lighthouse-refinement-skill.zip with SKILL.md at its root
  And lighthouse-skill.zip still holds the general skill
```

#### Acceptance Criteria

- [ ] The answer quotes `summary` and states ready count, range, verdict and next Refinement date in the instance's words.
- [ ] Below → the `summary`'s own "Refine X to Y more" range, quoted, never recomputed; In → "enough"; Above → "stop refining".
- [ ] Work Items with `readiness: NeedsDiscussion`, any No, or `hasOpenQuestion` are listed with `url`; others are summarised in one line.
- [ ] No number is stated without a verdict; each `unavailableReason` maps to its fix.
- [ ] No write tool is called in any PO case (guardrail).
- [ ] The release has `lighthouse-refinement-skill.zip`; `lighthouse-skill.zip` is unchanged in name and holds the general skill; `docs/aiintegration.md` and `concepts.md:85` updated.
- [ ] Production-data AC: Marco's question against the dogfood instance.

#### Technical Notes

- Packaging (D2) lands here: `skill/` → `skills/lighthouse/`; CI zips each `skills/*`; `ARCHITECTURE.md`
  §11 updated. No changeset (S4).
- `readySource: Stages` changes the wording only ("ready by stage").

### US-05 — A developer preps for Refinement in a minute and gives their own view

`job_id`: `job-team-member-give-sizing-view` · persona `team-member-voter` (Jonas) · ADO #6217 · slice 05 ·
~6h

#### Problem

Jonas does not know whether anything waits for his view before Wednesday. When it does, he wants the
question in the Team's own terms — "can we do this in 7 days or less?" — and to answer from where he
already is, under his own name, without the assistant answering for him.

#### Elevator Pitch

Before: Jonas opens the tab and scrolls to find what he has not voted on.
After: with the Refinement skill, ask "Do I need to prep for refinement?" → the assistant calls
`lighthouse_team_refinement_get({id: 3})` and answers "3 Gravity Work Items wait for your view; your SLE is
7 days. GR-051 Export Fleet Report to PDF — can you do it in 7 days or less? Yes / Yes, if… / No (link)".
After Jonas says "Yes, if the PDF export moves to its own Work Item" and confirms, it calls
`lighthouse_team_refinement_vote` and quotes the `summary`.
Decision enabled: Jonas answers each Work Item, or opens it first.

#### Domain Examples

1. **Three waiting** — as in the pitch; listed in the tab's order.
2. **Nothing waiting** — "Nothing waits for your view; next Refinement is Wednesday."
3. **Too big** — Jonas: "that's way more than a week" → the assistant offers "No, with a comment proposing
   the split" as a draft for him to confirm.
4. **Can't size yet** — "depends on the routing API" → "Yes, if… the routing API exists" or No with the
   question; asks "who will chase this?" without naming anyone.
5. **Bulk** — "just vote yes on everything for me" → shows the three, asks for each; casts nothing until
   confirmed.
6. **Self-declared name** — no sign-in, no stored name → the assistant asks Jonas for his name.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The developer sees what waits for their view, in the Team's terms
  Given three Gravity Work Items in refinement have no vote from Jonas
  When Jonas asks whether he needs to prep for refinement
  Then the assistant lists those three in the Refinement tab's order, each with its link
  And asks for each whether it can be done in 7 days or less: Yes, Yes if, or No

Scenario: A vote is cast only after the person confirms it
  Given Jonas has said "Yes, if the PDF export moves to its own Work Item" for GR-051
  When the assistant has shown him GR-051, the answer Yes if and that comment, and Jonas confirms
  Then the assistant records the vote and quotes Lighthouse's confirmation
  And before Jonas confirmed, no vote tool was called

Scenario: "Too big" becomes a No with a split for the person to confirm
  When Jonas says GR-055 is far more than a week of work
  Then the assistant offers a No with a comment proposing how to split GR-055, for Jonas to edit or confirm

Scenario: Nobody votes in bulk on someone's behalf
  When Jonas asks the assistant to vote yes on everything for him
  Then the assistant asks him to confirm each Work Item
  And it casts no vote he did not confirm

Scenario: The voter's name is asked, never guessed
  Given the instance runs without sign-in and Jonas has no stored voter name
  When Jonas confirms a vote
  Then the assistant asks Jonas for the name to record
  And it does not take a name from the system, an account or earlier messages
```

#### Acceptance Criteria

- [ ] Waiting Work Items = rows with `myVote` null, in `workItems` order, each with `url`; the yardstick question uses `yardstick.days`/`probability` (or says it is the Cycle Time fallback).
- [ ] When Lighthouse cannot tell which votes are his (no voter key, no account), the assistant says so and lists all rows needing votes.
- [ ] No vote/comment/take-back tool call before explicit per-Work-Item confirmation of answer and comment (guardrail, 100%).
- [ ] "Yes, if…" always carries its condition as the comment; "too big" → No + split proposal; "can't size yet" → Yes, if… or No with the question; "who will chase this?" asked, no one named.
- [ ] The voter name is asked, never inferred (guardrail, 100%).
- [ ] The skill never fetches Work Item content beyond the link (guardrail).
- [ ] Over MCP HTTP without sign-in, the assistant says where a vote can be cast instead.
- [ ] Production-data AC: Jonas's flow against the dogfood instance, vote then take-back.

#### Technical Notes

- Mapping of Rettig's three answers onto Yes / Yes, if… / No: research §4.2.
- SLE Poker Planning may be named neutrally (research §8).

### US-06 — An assistant sees what is in progress right now, what is Blocked and since when

`job_id`: `job-flow-coach-spot-stuck-items` · persona `flow-coach` (Priya) · ADO #6246 · slice 06 · ~4h

#### Problem

Priya's assistant reaches Lighthouse through MCP. It can tell how many Work Items were Blocked on each
past day, and the tool description says to "read the team's current WIP" for what is Blocked now — but no
MCP tool reads current WIP. Only `lh` can.

#### Elevator Pitch

Before: over MCP, "what is Blocked right now?" has no answer.
After: `lighthouse_team_metrics_wip({id: 3})` → per Work Item `referenceId`, `name`, `state`,
`workItemAge`, `isBlocked`, `blockedSince`, `url`, and `summary: 8 Work Items in progress · System WIP
Limit: 6 · 2 Blocked`.
Decision enabled: which Blocked Work Item Priya's Team unblocks first.

#### Domain Examples

1. **Two Blocked** — GR-061 since Mon, GR-064 since Wed; WIP 8, limit 6.
2. **No limit set** — Team Voyager → the summary says no System WIP Limit is set.
3. **Older Lighthouse** (≤ v26.7.3.1, no Blocked facts) → items listed; the summary says Lighthouse does
   not say which are Blocked.
4. **Nothing in progress** — `summary: No Work Items in progress`.

#### UAT Scenarios (BDD)

```gherkin
Scenario: What is Blocked right now is readable through MCP
  Given Gravity has 8 Work Items in progress, GR-061 Blocked since Monday and GR-064 since Wednesday
  When an assistant calls lighthouse_team_metrics_wip for Gravity
  Then it receives each Work Item with its age, state, whether it is Blocked and since when, and its link
  And the summary states 8 in progress, the System WIP Limit of 6 and 2 Blocked

Scenario: A Team without a WIP limit is told so
  Given Team Voyager has no System WIP Limit
  When an assistant reads Voyager's current WIP
  Then the summary says no System WIP Limit is set

Scenario: An older Lighthouse is not read as "nothing Blocked"
  Given a Lighthouse that does not report Blocked on WIP items
  When an assistant reads Gravity's current WIP
  Then the summary says Lighthouse does not tell which Work Items are Blocked

Scenario: Nothing in progress
  Given Gravity has no Work Items in progress
  When an assistant reads Gravity's current WIP
  Then the summary says no Work Items are in progress
```

#### Acceptance Criteria

- [ ] The tool returns the server's WIP facts unchanged plus `summary`, matching `lh metrics team --metrics wip` for the same Team and day.
- [ ] Unknown Blocked ≠ not Blocked, in facts and summary.
- [ ] The two `blockedCountHistory` descriptions name the new tool.
- [ ] General skill names the tool (drift check passes).
- [ ] Production-data AC on the dogfood instance.

#### Technical Notes

- Reuses `getTeamWip` and `describeInProgressNow`/`describeBlockedNow` (`client/src/metricsWording.ts`).
  `mcp-core` minor changeset. Portfolio twin: DESIGN.

### US-07 — SLE Risk per Work Item, from `lh` and MCP

`job_id`: `job-flow-coach-act-before-sle-breach` · persona `flow-coach` (Priya) · ADO #6246 · slice 07 · ~6h

#### Problem

Lighthouse knows, per Work Item, how likely it is to miss Gravity's SLE — the number its SLE Risk widget
shows. No client reads it, so an assistant can only approximate from age percentiles.

#### Elevator Pitch

Before: no client can say which Work Items are at risk of missing the SLE.
After: `lh metrics team --id 3 --metrics sleRisk` → "SLE Risk · Gravity: 2 of 8 Work Items at risk of
missing 7 days (85%)" and per Work Item "GR-063 · 5 days · 55% — 6 of 11 finished Work Items that reached
this age went past 7 days"; `--json` carries the facts; MCP `lighthouse_team_metrics_sleRisk({id: 3})`
gives the same.
Decision enabled: swarm or split the at-risk Work Items today.

#### Domain Examples

1. **Two at risk** — GR-058 (past SLE, 100%), GR-061 (78%); GR-063 at 55%.
2. **No SLE set** — Team Voyager → "Voyager has no SLE, so there is no SLE Risk."
3. **Portfolio id** — "SLE Risk is for Teams."
4. **Older Lighthouse** — upgrade message, no request.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The Work Items at risk are named with Lighthouse's own numbers
  Given GR-058 is past Gravity's SLE and GR-061 has an SLE Risk of 78%
  When Priya runs lh metrics team for Gravity with sleRisk
  Then she sees that 2 of 8 Work Items are at risk, and each Work Item's risk with the finished Work Items behind it

Scenario: The same answer through MCP
  When an assistant calls lighthouse_team_metrics_sleRisk for Gravity
  Then it receives each Work Item's risk and the same summary lh prints

Scenario: No SLE, no risk
  Given Team Voyager has no SLE
  When SLE Risk is read for Voyager
  Then the answer says Voyager has no SLE and shows no risk

Scenario: An older Lighthouse is told to upgrade
  Given a Lighthouse without SLE Risk
  When SLE Risk is read
  Then the client says Lighthouse must be upgraded and sends no request
```

#### Acceptance Criteria

- [ ] Risks and counts equal the server's for the same Team and day; `--json` = facts; `--pretty` = heading + sentence + per-Work-Item lines (sketched at DISTILL).
- [ ] At risk = ≥ 70% (Lighthouse's line, not the client's).
- [ ] No SLE / Portfolio / older server each answered as above.
- [ ] Production-data AC on the dogfood instance.

#### Technical Notes

- `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry (DESIGN finds the version). `client`, `cli`, `mcp-core`
  minor changesets. `cli/README.md` updated.

### US-08 — Process Behaviour Charts with their signals, from `lh` and MCP

`job_id`: `job-flow-coach-see-predictability-trend` · persona `flow-coach` (Priya) · ADO #6246 · slice 08 ·
~7h

#### Problem

The Daily Flow Review must raise PBC signals (M4). Lighthouse computes them — Large Change, Moderate
Change, Moderate Shift, Small Shift, per day, against a baseline — but the clients read only the limits
over time. An assistant would have to re-derive XmR rules in its head.

#### Elevator Pitch

Before: no client can say whether a PBC shows a signal.
After: `lighthouse_team_metrics_processBehaviourChart({id: 3, metricType: "TotalWorkItemAge"})` →
facts (status, baseline, limits, daily values with signals and blackout flags) and `summary: Total Work Item
Age · Gravity: Large Change on 7 Oct and 8 Oct, above the upper limit`; `lh metrics team --id 3 --metrics
processBehaviourChart` prints the same per type.
Decision enabled: whether a shift is worth a question in today's daily.

#### Domain Examples

1. **Large Change** — Gravity's Total Work Item Age above the upper limit since Tue.
2. **No baseline** — Team Voyager → summary says the limits come from the shown range, no baseline set.
3. **Blackout** — 3 Oct is a blackout day → flagged, never a signal.
4. **Inside the limits** — "No signals."

#### UAT Scenarios (BDD)

```gherkin
Scenario: A signal is named with its day
  Given Gravity's Total Work Item Age shows a Large Change on 7 and 8 October
  When an assistant reads Gravity's Total Work Item Age PBC
  Then the summary names Large Change and those days

Scenario: No baseline, no signal claim
  Given Team Voyager has no PBC baseline
  When Voyager's WIP PBC is read
  Then the answer says no baseline is set and the limits come from the shown range

Scenario: Blackout days are not signals
  Given 3 October is a blackout day for Gravity
  When Gravity's Throughput PBC is read
  Then 3 October is marked as a blackout day and not as a signal

Scenario: Inside the limits
  Given Gravity's Throughput stays inside its limits
  When it is read
  Then the summary says there are no signals
```

#### Acceptance Criteria

- [ ] All five Team types (+ Portfolio `FeatureSize`) readable through `lh` and MCP; facts equal the server's.
- [ ] Signals carry their Lighthouse names; baseline status and blackout flags passed through.
- [ ] Production-data AC on the dogfood instance.

#### Technical Notes

- Server shape S9. Naming must not collide with `processBehaviorOverTime` / the `pbcovertime` alias
  (`cli/src/index.ts:427`). Changesets as US-07.

### US-09 — The facilitator opens the daily with what the Team should decide today

`job_id`: `job-facilitator-run-the-daily-from-the-work` (+ `job-flow-coach-act-before-sle-breach`) · persona
`flow-coach` (Priya) · ADO #6246 · slice 09 · ~6h

#### Problem

Gravity's daily is a status round: everyone prepares a statement and justifies yesterday. Priya wants to
open it with what the work needs decided — Blocked Work Items, those past or near the SLE, WIP over the
limit — and nothing about people.

#### Elevator Pitch

Before: the daily starts with "yesterday I…".
After: with `lighthouse-daily-flow-review-skill.zip`, ask "What should Gravity decide in today's daily?" →
the assistant calls `lighthouse_team_get`, `lighthouse_team_metrics_wip` and
`lighthouse_team_metrics_sleRisk` and answers as in the Journey: "Decide first: GR-061 … Blocked since Mon
(3 days). Who unblocks it today?" then ≤ 3 decisions, "Everything else is flowing."
Decision enabled: the Team's first decision of the day.

#### Domain Examples

1. **Full list** — as in the Journey.
2. **Calm day** — nothing Blocked, nothing at risk, WIP 5 of 6 → "Nothing to decide today. Everything is
   flowing."
3. **"Who's slow?"** — redirected to Work Items: "GR-058 has been in progress 9 days; what would help it
   finish?"
4. **Older Lighthouse, no SLE Risk** — fallback to age vs the 70th percentile, labelled.
5. **No SLE set** — no "past the SLE" decisions; one line that an SLE makes them possible.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The daily opens with one thing to decide
  Given GR-061 is Blocked since Monday, GR-058 is past the SLE and Gravity's WIP is 8 against a limit of 6
  When Priya asks what Gravity should decide today
  Then the answer opens with GR-061 and asks who unblocks it today
  And then lists GR-058 and the WIP over its limit as decisions, at most three
  And ends with "Everything else is flowing"
  And every Work Item carries its link

Scenario: A calm day says so in one line
  Given nothing is Blocked, nothing is at risk and WIP is under its limit
  When Priya asks what Gravity should decide today
  Then the assistant says there is nothing to decide today

Scenario: People are never the subject
  When Priya asks who on Gravity is slowest
  Then the answer names no person and turns to the oldest Work Items and what would help them finish

Scenario: Without SLE Risk the fallback is labelled
  Given a Lighthouse that cannot report SLE Risk
  When Priya asks what Gravity should decide today
  Then Work Items past the 70th percentile of Gravity's Cycle Time are listed as at risk
  And the answer says this is an estimate from age, not Lighthouse's SLE Risk

Scenario: The skill proposes, the Team decides
  When the assistant lists a decision
  Then it may suggest swarming, splitting or unblocking
  And it never assigns the action to a person
```

#### Acceptance Criteria

- [ ] Order: Blocked (longest first) → past SLE → SLE Risk ≥ 70% → WIP over limit; "decide first" + ≤ 3 decisions (D9).
- [ ] No person's name, assignee or per-person count in any answer (guardrail, 100%).
- [ ] Every Work Item has its link; no Work Item content fetched.
- [ ] Calm day = one line; no SLE / no SLE Risk handled per D7.
- [ ] The skill names `lighthouse` as companion and carries D3's rules; its description excludes forecasts and Refinement.
- [ ] Release carries `lighthouse-daily-flow-review-skill.zip`; `aiintegration.md` paragraph added.
- [ ] Production-data AC: the maintainer runs the Lighthouse Team's own daily from it.

#### Technical Notes

- Depends on US-06, US-07. Works through `lh` too (`--metrics wip,sleRisk --json`).

### US-10 — The daily also gets what is worth discussing, PBC signals included

`job_id`: `job-facilitator-run-the-daily-from-the-work` · persona `flow-coach` (Priya) · ADO #6246 · slice
10 · ~5h

#### Problem

Not everything needs a decision: a Work Item just under the line, or a shift in the system, deserves a
question before it becomes a problem. Liz Rettig's client saw a slip the same day this way. Raised badly —
"your process is out of control", a cause guessed, noise explained — it puts people off.

#### Elevator Pitch

Before: signals are seen two weeks later in a retro, if at all.
After: ask "What should Gravity discuss today?" → the assistant reads SLE Risk and the Total Work Item Age
and WIP PBCs and answers "GR-063 — 5 days, SLE Risk 55%: just under the line. What would help it finish?
Total Work Item Age: Large Change since Tue. Anything change in how we work around then?"
Decision enabled: which question the Team spends two minutes on.

#### Domain Examples

1. **Just under the line** — GR-063 at 55%.
2. **Signal** — Total Work Item Age Large Change since Tue → a question, no cause.
3. **Throughput signal** — "worth a flow retro", not today's daily.
4. **No baseline** — no signal raised; one line on setting a baseline.
5. **Persisting** — signal on 3 consecutive working days → suggests a flow retro.

#### UAT Scenarios (BDD)

```gherkin
Scenario: A Work Item just under the line is a question, not a decision
  Given GR-063 has an SLE Risk of 55%
  When Priya asks what Gravity should discuss today
  Then GR-063 appears as a discussion prompt asking what would help it finish

Scenario: A PBC signal is raised as a question without a cause
  Given Gravity's Total Work Item Age shows a Large Change since Tuesday
  When Priya asks what Gravity should discuss today
  Then the answer says the chart shows a change since Tuesday and asks what changed around then
  And it offers no cause and does not call the process out of control

Scenario: Lagging signals go to a retro
  Given Gravity's Throughput shows a Moderate Shift
  When Priya asks what Gravity should discuss today
  Then the shift is offered as worth a flow retro, not as today's discussion

Scenario: No baseline, no signal
  Given Gravity has no PBC baseline
  When Priya asks what Gravity should discuss today
  Then no signal is raised and the answer says once that a baseline makes the limits meaningful

Scenario: A persisting signal suggests a flow retro
  Given the Total Work Item Age signal has been present on 3 consecutive working days
  When Priya asks what Gravity should discuss today
  Then the answer suggests running a flow retro
```

#### Acceptance Criteria

- [ ] ≤ 2 discussion prompts; SLE Risk 50–69% (or the 50th-percentile fallback) and WIP/Total Work Item Age signals only (D8).
- [ ] Signals phrased as "the chart shows … since …" + a question; no cause, no "out of control" (guardrail).
- [ ] Throughput/Cycle Time/Arrivals signals → "worth a flow retro"; no baseline → no signal; blackout → never a signal; inside limits → silence.
- [ ] Persisting signal (3 working days, A3) → flow retro suggestion.
- [ ] Production-data AC as US-09.

#### Technical Notes

- Depends on US-08. PBC stance and quotes: research §4.3, §2.2.

---

## Wave: DISCUSS / [REF] Story Map

**Users**: Priya, Lena, Marco, Jonas. **Goal**: an assistant that answers from Lighthouse like a good flow
coach — and runs Refinement prep and the daily from the work.

| Ask Lighthouse well (#6245) | Prepare Refinement (#6217) | Open the daily (#6246) |
|---|---|---|
| US-01 every read reachable + drift check | US-04 PO: ready? (+ packaging) | US-06 current WIP + Blocked over MCP |
| US-02 new concepts explained | US-05 developer: prep and vote | US-07 SLE Risk per Work Item |
| US-03 coach stance, no lecture | | US-08 PBC with signals |
| | | US-09 decide today |
| | | US-10 discuss today |

**Walking skeleton**: none (C).

| Slice | Story | ADO | Repo | Releasable alone | Est. | Learning hypothesis (disproves … if it fails) |
|---|---|---|---|---|---|---|
| 01 | US-01 | #6245 | clients | Yes | ~6h | that naming a tool in the skill is enough for assistants to use it |
| 02 | US-02 | #6245 | clients | Yes | ~6h | that product wording quoted into the skill explains new concepts correctly |
| 03 | US-03 | #6245 | clients | Yes | ~6h | that a never/instead table holds an assistant to ProKanban language without lecturing |
| 04 | US-04 | #6217 | clients + LH docs | Yes | ~6h | that `refinement_get` alone answers "refine more or stop?" |
| 05 | US-05 | #6217 | clients | Yes | ~6h | that confirm-first voting is quick enough that developers use it |
| 06 | US-06 | #6246 | clients | Yes | ~4h | that the CLI's WIP facts serve MCP callers unchanged |
| 07 | US-07 | #6246 | clients | Yes | ~6h | that the server's per-item risk is enough without client logic |
| 08 | US-08 | #6246 | clients | Yes | ~7h | that server-computed signals are enough for a daily |
| 09 | US-09 | #6246 | clients + LH docs | Yes | ~6h | that a short decide list replaces the status round |
| 10 | US-10 | #6246 | clients | Yes | ~5h | that signals raised as questions are welcome, not off-putting |

Total ~58h ≈ 7½ days. Every slice ≤ 1 day, carries a user-visible outcome and a production-data AC.
IN/OUT per slice = the story's ACs / Out of Scope; slice briefs are this table (lean single file).
Dependencies, one way: 02, 03 extend 01's skill; 04 moves the general skill (D2); 05 extends 04; 09 needs
06, 07; 10 needs 08, 09. No dependency between stories except "later builds on earlier, finished".

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| 4+ new components → not thin | **Pass.** Largest is 08: one client read, one CLI selection, one tool. |
| Every slice depends on a new abstraction | **Pass.** The drift check lands in 01, a value slice; packaging in 04, a value slice. |
| No slice disproves a pre-commitment | **Pass** — hypotheses above. |
| Synthetic data only | **Pass** — each has a dogfood-instance AC; evals use fixtures in addition. |
| 2+ slices identical except scale | **Considered: 07 and 08** (both "expose a server read"). Different data, different consumers (decide vs discuss); kept apart so the decide list can ship without PBC. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **#6245 first** (M1): both specific skills build on it; 01 first because the drift check protects
   everything after; 03 last in the story because its structure work moves text 01 and 02 wrote.
2. **#6217**: 04 (PO, plus the packaging every later skill reuses) then 05 (voting carries the strongest
   guardrails).
3. **#6246**: 06 → 07 → 09 (decide list end to end) → 08 → 10. The decide list is the maintainer's core
   purpose and ships value without PBC; PBC then completes M4.

**Dogfood cadence**: after each slice, the maintainer runs the slice's production-data AC against the
dogfood instance the same day.

---

## Wave: DISCUSS / [REF] Outcome KPIs

**Objective**: by release, an assistant with these skills answers Lighthouse questions as a ProKanban coach
would, never decides for people, and Teams use the two new skills.

| # | Who | Does what | By how much | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| KPI-1 | Maintainer | Keeps skills in step with the tools | **0** unnamed tools/commands, **0** names that do not exist, on every CI run | today: 32 of 45 tools not in the resolution table | drift check in `pnpm test` | Leading / guardrail |
| KPI-2 | Assistants with the skills | Pass the eval cases | guardrail cases **100%** on 3 of 3 runs; others **≥ 90%** per skill | none | pre-release eval run (D10) | Leading |
| KPI-3 | Teams | Install the new skills | **≥ 25** downloads each of `lighthouse-refinement-skill.zip` and `lighthouse-daily-flow-review-skill.zip` within 60 days (desk target) | `lighthouse-skill.zip` count read on release day | GitHub release-asset `download_count` | Lagging |
| KPI-4 | Developers and POs | Use Refinement from assistants | `TeamRefinementDayVerdictShown` and `TeamSizingVoteCast` with `source = Mcp` rise vs the 4 weeks before release (consenting users only) | read on release day | PostHog breakdown by `source` (#6193) | Lagging |
| KPI-5 | The Lighthouse Team's own daily | Runs without a status round | on **≥ 8 of 10** consecutive working days the daily opens from the skill's answer, with no round-robin | status round today | maintainer's dogfood log | Leading |
| KPI-6 | Everyone | Is never decided for | **0** votes, readiness changes or assignments made without the person's confirmation; **0** person named in a DFR answer | — | guardrail evals | Guardrail |

**Hypothesis**: we believe giving facilitators, POs and developers skills that read Lighthouse and propose
what to decide will replace the daily status round and shorten Refinement prep. We will know when the Team
runs its daily from the skill (KPI-5) and assistant-sourced Refinement events rise (KPI-4).

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every AC above is an eval case (skills) or an automated test (G1–G3: Vitest over `runCliCommand` / the
   MCP harness with a stub client).
2. `pnpm run ci` green in lighthouse-clients, drift check included; changesets per the checklist (#6246).
3. Pre-release eval run recorded under this workspace (KPI-2); guardrail cases 100%.
4. StrykerJS on the new client/CLI/MCP code of #6246, ≥ 80% kill rate. Skill text: **N/A, because**
   prose cannot be mutated; KPI-2 is its gate.
5. Docs per the checklist row; `docs/aiintegration.md` never names a skill the release lacks.
6. Screenshots, demo data: **N/A, because** no web screen or seeded data changes.
7. Website: checked at #6246's DELIVER.
8. RBAC: **N/A, because** no gate is added, removed or moved.
9. ADO #6245, #6217, #6246 Active → Resolved when their last slice is pushed, not Closed; slice tasks on
   the board only after the maintainer confirms (`/ado-sync`).

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem statement clear, domain language | PASS | Each story opens with a named person, their situation and what is missing (US-01 … US-10). |
| 2 | User/persona with specific characteristics | PASS | Six existing personas, named people, Team Gravity's SLE/limit/cadence fixed. |
| 3 | 3+ domain examples with real data | PASS | 4–6 per story: Gravity, Voyager, OE-002, GR-051…GR-064, real tools/commands. |
| 4 | UAT in Given/When/Then, 3–7 per story | PASS | US-01 5, US-02 4, US-03 5, US-04 5, US-05 5, US-06 4, US-07 4, US-08 4, US-09 5, US-10 5. |
| 5 | AC derived from UAT | PASS | Each AC list restates its scenarios as eval/test checks, plus guardrails and a production-data AC. |
| 6 | Right-sized | PASS | 4–7h per story; slices ≤ 1 day; #6246 as a whole ~3½ days (Q1). |
| 7 | Technical notes | PASS | S1–S12, per-story notes, D2/D3/D10. |
| 8 | Dependencies resolved or tracked | PASS | Pre-requisites; one-way slice dependencies; G4 follow-up tracked; website check placed. |
| 9 | Outcome KPIs with measurable targets | PASS | KPI-1…KPI-6, numeric with method; KPI-3 a stated desk target. |

**Job traceability**: every story has a `job_id`; five existing jobs, two new (YAML above, append pending
D13). No `infrastructure-only`. **Elevator pitches**: ten, each naming a real prompt, tool, command or
release asset and its observable output. **Slice composition**: every slice carries a user-visible story.

**DoR status: PASSED.** **Requirements completeness: 0.96.** Deliberate gaps, each DESIGN's: eval harness
and fixture source (D10), the drift test's location, G1–G3 names and the `sleRisk` version gate.

**Expansion catalog** (`ask-intelligent`): AC ambiguity — no; ACs name values, orders and caps.
Cross-context complexity — **fires** (3 contexts: skill text, clients packages, CI) → `alternatives-considered`.
Multi-stakeholder — **fires** (4 personas) → `persona-narrative`. Compliance — no. WS strategy D — no.
Maintainer AFK, so recorded, not asked: *Suggested expansions: alternatives-considered — decision
rationale per locked decision; persona-narrative — extended persona. Not applied; on request.* The main
alternatives are already stated inline in D2, D3, D10 and the Gap List.

---

## Wave: DISCUSS / [REF] AFK defaults — revisit

| # | Default taken | Alternative | Slice |
|---|---|---|---|
| A1 | `skills/<name>/` layout, one zip per skill, general zip keeps its name; move in #6217 (D2) | keep `skill/` and add sibling folders; or one zip with all three | 04 |
| A2 | Specific skills name the general one and restate only the guardrails (D3) | link-only reference (breaks when installed alone); or full duplication | 04, 09 |
| A3 | A signal on 3 consecutive working days → suggest a flow retro (D8) | 2, 5, or never suggest | 10 |
| A4 | DFR caps: 1 first + ≤ 3 decisions + ≤ 2 discussion prompts (D9) | no caps; other numbers | 09, 10 |
| A5 | Model-in-the-loop evals are a manual pre-release run, not CI (D10) | run them in CI with a model key | all |
| A6 | Skill names `lighthouse-refinement`, `lighthouse-daily-flow-review` | other names | 04, 09 |
| A7 | G1 tool for Teams; Portfolio twin left to DESIGN | both now | 06 |

---

## Wave: DISCUSS / [REF] Open questions for the maintainer

| # | Question | Options | Recommendation |
|---|---|---|---|
| Q1 | #6246 is ~3½ days with G1–G3 inside it. | (a) keep G1–G3 as slices 06–08 of #6246; (b) split them into a new Story "Clients: current WIP, SLE Risk and PBC for assistants" before #6246 | **(a)** — the skill is their only planned consumer; a separate board item changes nothing that is built. Pick (b) if you want each ADO Story ≤ 3 days. |
| Q2 | Staleness (G4). | (a) follow-up Bug/Story: Lighthouse marks `isStale` on WIP items, then the DFR adds it; (b) copy the rule into the client now; (c) never | **(a)** — one rule, no drift; DFR v1 ships without it. |
| Q3 | Eval runs (D10/A5). | (a) manual pre-release run by the maintainer; (b) CI with a model key | **(a)** — cheap, no secret in CI; revisit if skills change often. |
| Q4 | Liz Rettig. | (a) ask her to review the DFR skill before release and to OK the short quotes; (b) ship with links only | **(a)** — she collaborated with LPW and the DFR follows her client's practice; cheap. |

---

## Wave: DISCUSS / [REF] Changed Assumptions

None against DISCOVER (there is none). Against the research: the fifth DFR decision (stale, research §4.3)
and the fourth PO check (time in refinement, research §4.2) are dropped for now (G4, G6).

---

## Wave: DISCUSS / [REF] Maintainer decisions (2026-10-08)

- **Q1 → (a).** G1–G3 (current WIP via MCP, SLE Risk per Work Item, PBC with signals) stay slices of #6246.
- **Q2 → (a).** Staleness becomes a follow-up item where Lighthouse marks stale Work Items itself; the
  Daily Flow Review skill ships without it.
- **Q3 → (a).** Evaluations run by hand by the maintainer before the release, not in CI.
- **Q4 → (a).** The maintainer asks Liz Rettig to review the Daily Flow Review skill and to OK the short
  quotes before the release.
- **A1–A7** kept as proposed (skills/ layout and zip names, specific skills restate only the rules they
  cannot run without, flow-retro suggestion after 3 working days, caps 1 + 3 decisions and 2 prompts,
  manual evals, skill names, WIP read for Teams only).
