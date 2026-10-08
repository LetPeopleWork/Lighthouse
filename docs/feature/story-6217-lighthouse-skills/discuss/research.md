# Research: inputs for the Lighthouse, Refinement and Daily Flow Review skills (#6217)

**Date**: 2026-10-08 | **Researcher**: nw-researcher (Nova) | **Confidence**: Medium-High | **Sources**: 14 web, 12 local

Read this alongside `maintainer-input.md` in this folder. Every claim carries its source. Where the text says
**Interpretation**, the point is my analysis and is not sourced.

> **How the quotes were collected.** The ProKanban repository files were read through WebFetch, which hands the
> page to a summarising model. Short quotes from those files are what that model returned. They are faithful in
> substance, but check each one against the raw file before copying it into a skill. The two blog posts and the
> Kanban Guide were quoted on request and are more reliable.

---

## 1. Executive summary

- ProKanban's `professional-kanban` skill is one `SKILL.md` with two references (`data-prep.md`,
  `stakeholder-language.md`), two stdlib Python scripts and an eval file. Its strengths are a hard **first fork**
  (one item → cycle-time percentiles/SLE, many items → Monte Carlo on throughput), a **"never / instead" table**, a
  **pre-answer checklist**, and a rule that open-ended questions **lead with aging**. It says nothing about daily
  meetings or refinement sessions. Its eval cases cover neither.
- Liz Rettig's two posts supply the **PBC stance**: points inside the limits need no explanation, and a signal says
  *that* something moved, not *what*. They also supply the **three-answer right-sizing** ("it fits / too big, slice it
  / can't size it yet") and the warning that partly-ready work sitting still is WIP.
- Lighthouse can do what the ProKanban skill does with scripts, and more, from live data: per-item SLE Risk (the
  same conditional rate the ProKanban skill uses in its worked example), blocked and stale items, WIP limit,
  PBC signals, the Refinement need number and the "Yes / Yes, if… / No" votes. Some of these are **not yet reachable
  through the CLI/MCP**, notably SLE Risk and staleness. That gap matters most for the Daily Flow Review skill.
- The current Lighthouse skill conflicts with ProKanban in several places: "95% = certain", cross-team comparison,
  expedite lanes, an SLE "rounded up for buffer", the 85th-percentile trigger in the daily, and "give them the 85%
  date". It is also stale on widgets (it is missing SLE Risk, Blocked, Stale, Flow Efficiency, Load Balance and Time
  in State) and on Refinement.
- ProKanban's skill text is **CC BY-SA 4.0**. Copying or adapting it into the MIT-licensed `lighthouse-clients`
  repository would carry ShareAlike obligations. Reference it and restate the ideas in our own words.
- The Liz Rettig meetup video transcript **could not be retrieved** (see Gaps).

---

## 2. Source summaries

### 2.1 ProKanban `professional-kanban` skill (v1.1.0)

Repo: <https://github.com/ProKanban/professional-kanban>. File tree from the GitHub API
(<https://api.github.com/repos/ProKanban/professional-kanban/git/trees/main?recursive=1>):
`skills/professional-kanban/{SKILL.md, references/data-prep.md, references/stakeholder-language.md,
scripts/{monte_carlo.py,cycle_time.py,flowlib.py}, assets/example-*.csv, agents/openai.yaml}`, plus
`evals/cases.json`, `.claude-plugin/`, `.codex-plugin/`, `REUSE.toml`, and a design spec under `docs/superpowers/`.

**SKILL.md** (<https://raw.githubusercontent.com/ProKanban/professional-kanban/main/skills/professional-kanban/SKILL.md>):
- *Trigger*: the description is front-loaded with discriminating terms: "Analyze Professional Kanban delivery flow
  from CSV data or answer questions about WIP, throughput, cycle time, work item age, SLEs, predictability,
  right-sizing, and probabilistic delivery forecasts." It names its neighbours explicitly as out of scope: personal
  scheduling, package tracking, file transfer timing and generic probability.
- *First fork*: a question about one item is answered from cycle-time percentiles (the SLE). A question about many
  items is answered by Monte Carlo on throughput. It watches for **hidden plurals** such as "will we make October?".
- *Work Item Age*: "the most important question to ask of any workflow is whether items are aging unnecessarily."
  Past the 50th percentile an item is worth a conversation. Past the 70th, the risk is elevated: swarm, split or
  escalate. "The most effective ways to reduce aging: finish work, and don't start work."
- *SLE*: per single item, both halves (period + probability), "never a commitment, deadline, or promise"; a
  best guess is acceptable without data if labelled as one.
- *Right-sizing*: size against the SLE, not in hours. Aging is often a sign an item is too big, so "ask whether a
  stuck item can be split before asking for revised estimates".
- *Forecast language*: always a probability **and** a range ("60 items **or more**", "**on or before** January 25").
  Higher confidence means a smaller promise, and 100% honestly reads "zero items or more".
- *Open-ended questions*: "Lead with aging, not forecast": aging is leading and still actionable, throughput is
  lagging.
- *Never/Instead table*: forecasting from averages, single dates, SLE × backlog count, points/velocity, burndown,
  estimating in hours, starting the clock at creation, per-person metrics or team comparisons, "assign work",
  expedite lanes and classes of service, cleaning outliers, fitting distributions.
- *Anti-lecture rule*: when asked for a forbidden approach, "Do not refuse outright." Give the flow answer, then say
  in one line what the requested version loses. The goal is "better conversation, not a lecture".
- *Pre-answer checklist* (9 items) and three **wrong/right worked examples**. The "at risk" example is a
  conditional rate: "of 18 completed items that reached age 9, 7 breached SLE".
- *Reference loading* is conditional and named: load `data-prep.md` when the CSV shape is unclear, the export comes
  from Jira or ADO, or data is thin; load `stakeholder-language.md` on pushback or comparison requests.
- *Sources*: Kanban Guide (May 2025), Kanban Pocket Guide, Flow Forecasting Pocket Guide, Vacanti's AAMfP I & II.

**stakeholder-language.md** (<https://raw.githubusercontent.com/ProKanban/professional-kanban/main/skills/professional-kanban/references/stakeholder-language.md>):
the core move is "Do not defend the method; answer the underlying question". "Just give me a date" gets offered
choices (safe / medium / optimistic). "Why is this taking so long?" is routed to age and WIP, not people.
"Velocity?" is answered once with throughput, without relitigating. An expedite lane "ages other work". There is a
four-step bridge from forecast to commitment (what is decided → cost of being wrong → commit at a stated percentile
→ re-forecast). Bad news keeps the number honest and attaches a lever.

**data-prep.md** (<https://raw.githubusercontent.com/ProKanban/professional-kanban/main/skills/professional-kanban/references/data-prep.md>):
default window 8–12 weeks, cut at known breaks; ~10 data points minimum, 20 for confidence; "Do not clean
outliers"; "Never let missing data become a reason to fall back on story points or an average."

**Design spec** (<https://raw.githubusercontent.com/ProKanban/professional-kanban/main/docs/superpowers/specs/2026-08-19-world-class-skill-design.md>):
explicitly **non-goals**: a hosted service, an MCP server, a database or network dependency. One canonical skill
ships to many hosts as thin metadata layers. Evals hold positive, neighbouring and negative prompts, and CI checks
that every named behaviour appears in SKILL.md.

**Evals** (<https://raw.githubusercontent.com/ProKanban/professional-kanban/main/evals/cases.json>): 16 cases, 8
positive and 8 negative. "Which items need attention today?" expects the agent to lead with aging. None cover
standups, refinement or right-sizing sessions.

**Licence**: MIT for scripts/tests, **CC BY-SA 4.0 for skill instructions, references and docs**, CC0 for sample data
(repo README, `REUSE.toml`).

### 2.2 Liz Rettig, "AI, Kanban and a restructure: you can prove something moved, not what moved it" (1 Sep 2026)

<https://prokanban.org/blog/ai-kanban-and-a-restructure-you-can-prove-something-moved-not-what-moved-it>.
Throughput rose 4–5× during a six-month transformation. Kanban training, a restructure, WIP limits, pods and AI all
arrived together, so the gain is real but cannot be pinned on AI. The post's method: an XmR chart of weekly
throughput over 6–12 months, limits computed from the baseline, and two signals to watch: a point outside the limits,
or a run of 8+ on one side of the average.
- "Everything inside those limits is your system being itself. It isn't a result, it isn't caused by anything in particular, and it doesn't need explaining."
- "A signal tells you the system is no longer the system you measured. It doesn't tell you which part of it moved."
- "Split the same quarter's work into twice as many items and your throughput doubles while exactly the same value ships."

### 2.3 Liz Rettig, "AI boosted your throughput. Your strategy didn't move." (28 Sep 2026)

<https://prokanban.org/blog/ai-boosted-your-throughput-your-strategy-didn-t-move>. Two rates diverged: the speed of
*doing* went up, but the speed at which work *arrives ready* did not. The constraint moved upstream into getting
ready (approvals, decisions, refinement) and downstream into review. Neither shows on most boards.
- "Work that's partly ready and sitting still is WIP."
- "Size your items against your SLE giving one of three answers only: it fits, it's too big so slice it, or you can't size it yet."
- "Draw the Start line where the work actually starts, which is when somebody begins assessing it, not when a developer picks it up."
- "Somebody has to be responsible for getting the information at the front, which means chasing the approval, the decision, the answer nobody has yet. And somebody has to own saying it's done at the back."
- "Agent output looks finished... reviewing it properly takes more attention, not less."
- Suggested analyses: the mix of strategic and operational work before and after AI, upstream cycle time, and the age of items in getting-ready and review states.

### 2.4 The Kanban Guide (May 2025) and related ProKanban material

<https://kanbanguides.org/the-kanban-guide/> (licence: CC BY 4.0, Orderly Disruption Ltd and Daniel S. Vacanti, Inc.).
- Meetings are not mandated: "A common practice is for Kanban system members to review the active items regularly."
- Active management: "Controlling WIP." / "Ensuring work items do not age unnecessarily, using the SLE as a reference." / "Unblocking blocked work."
- WIP: "Kanban system members must explicitly control the number of work items in a workflow from started to finished."
- The guide does **not** mention right-sizing. That comes from the Pocket Guide and practice (it is cited in the
  ProKanban SKILL.md).

ProKanban blog on aging (<https://prokanban.org/blog/visualising-work-item-aging-in-jira>) lists the questions to
ask of an aging item: "Is more help needed? Is it blocked? Can we swarm on the ticket to complete it within our
Service Level Expectation? Is this work bigger than we first thought? Can we break it down?"

### 2.5 Liz Rettig at Lighthouse Live, "Agentic AI Meets Flow" (13 Jul 2026)

<https://www.youtube.com/watch?v=YBdlyT00u8k>; event page <https://www.meetup.com/lighthouselive/events/315253705/>.
**The transcript could not be retrieved** (see Gaps). The event page frames the talk as "Designing how work flows
through humans and AI together", with Lighthouse used for visibility and forecasting, and the claim that "operating
models matter more, not less, when AI is helping build the product". I have no timestamped content.

---

## 3. What Lighthouse can do that the ProKanban skill cannot

| Capability | Lighthouse surface | ProKanban skill |
|---|---|---|
| Live data, no CSV export | MCP tools / `lh` (`skill/SKILL.md`; `packages/mcp-core/src/index.ts:104-150`) | CSV in, scripts out |
| Per-item **SLE Risk**: share of finished items still open at this age that missed the SLE; ≥70% = at risk; past SLE = 100% | Web: `docs/metrics/flow-metrics.md:112-146`, `flow-overview.md:40-66` (ADR-192). **Not in clients**: no `sleRisk` in `lighthouse-clients/packages` | Computes the same conditional rate in its worked example |
| Blocked now and since when | `isBlocked` + `blockedSince` on WIP items (`client/src/metricsWording.ts:183-212`); blocked history tool | none |
| Stale items (no state change for N days) | Web widget (`flow-overview.md:96-118`); **not in clients** (no match for stale in `packages/`) | none |
| System WIP limit | `readSystemWipLimit` (`client/src/metricsWording.ts:557`) | none |
| PBC signals (Large Change, Moderate Change, Moderate Shift, Small Shift = 8 in a row on one side) with baseline | `processBehaviorOverTime` tools; `docs/metrics/predictability.md:151-185`; `XmRCalculatorTest.cs` | none (Rettig's blog only) |
| Time in State, wait states, flow efficiency | `cumulativeStateTime*` tools; `docs/metrics/flow-metrics.md:352` | none |
| Feature-level forecasts in backlog order, Feature WIP, multi-team features | `forecast_manual`, `lighthouse-mechanics.md` | single-team only |
| **Refinement need**: likely pull over one Refinement cycle (`need.low`–`need.high`), verdict Below/In/Above, ready count by Votes or Stages | `lighthouse_team_refinement_get` (`index.ts:847-851`) | none |
| **Sizing votes** "Doable within 7 days? (our SLE, 85%)" → Yes / Yes, if… / No, with comments, named, take-back | `refinementTools.ts:87-144`; `epic-5510-5881-refinement/slices/slice-10-sle-yardstick.md` | right-sizing as prose only |
| Backtesting | `forecast_backtest` | none |
| The instance's own terminology in `summary` | `skill/SKILL.md` | fixed vocabulary |

**Interpretation.** The ProKanban skill *computes* flow facts from a file. The Lighthouse skills can *read* them and
spend their effort on what to decide and discuss. The one place where ProKanban's skill currently out-computes the
Lighthouse clients is per-item risk against the SLE, which Lighthouse has but does not expose to agents.

---

## 4. Skill-by-skill design inputs

### 4.1 General "Lighthouse usage" skill

Adopt:
1. **The one-item / many-items first fork** as an explicit routing rule: "how long will *this* take" → SLE and age;
   "when will *these* be done / how many by" → `forecast_manual`. Include the hidden-plural examples. (ProKanban SKILL.md)
2. **Forecast phrasing with "or more" / "on or before"** and both halves stated, replacing "50% by June 6th and 85% by
   June 20th" (current `SKILL.md`, "Communicating Forecasts"). (ProKanban SKILL.md; stakeholder-language.md)
3. **Lead with aging on open-ended questions** ("how are we doing?"): current WIP sorted by age against the SLE, then
   blocked items, then the forecast. (ProKanban SKILL.md; eval "Which items need attention today?")
4. **The never/instead table and the pre-answer checklist**, rewritten in our words and adjusted where Lighthouse
   deliberately differs (see §6).
5. **The "answer, then one line on what you lose" rule** for velocity, single dates and 100% requests.
   (ProKanban SKILL.md; stakeholder-language.md)
6. **Forecast → commitment bridge** (decision, cost of being wrong, chosen percentile, re-forecast).
   (stakeholder-language.md)
7. **PBC stance**: no explanation is owed for points inside the limits, and a signal tells you *that* something
   moved, not *what*. (Rettig, post 1)
8. **The size-of-items caveat on throughput**: splitting items raises throughput without more value shipping, so a
   throughput rise is a prompt to check item size before celebrating. (Rettig, post 1)
9. **Conditional reference loading**, as ProKanban names it: the specific skills load the general skill's metric
   reference only when they need a definition.

### 4.2 Refinement skill

Built on the core question from the maintainer: "Can we do ABC in x days or less?" → Yes / Yes, if… / No
(`maintainer-input.md`). In Lighthouse x is the Team's SLE range ("Doable within 7 days? (our SLE, 85%)"), with a
labelled fallback to the 85th percentile of cycle time when the Team has no SLE
(`epic-5510-5881-refinement/slices/slice-10-sle-yardstick.md`).

Adopt:
1. **Rettig's three answers map cleanly onto the votes.** "It fits" → Yes. "Too big, slice it" → No, with the
   comment proposing the split. "Can't size it yet" → No or "Yes, if…" with the open question as the condition.
   The skill should say this mapping out loud: a No is a useful answer, not a failure. (Rettig, post 2;
   `refinementTools.ts:91`)
2. **Size against the SLE, never in hours or points.** The skill never asks for an estimate. (ProKanban SKILL.md;
   `feature-delta.md` rejects story-point poker, line 77)
3. **"Just enough" readiness**: the need band has an upper bound, and being *Above* is a signal to stop refining,
   not a win. That follows from "Work that's partly ready and sitting still is WIP". (Rettig, post 2;
   `feature-delta.md` D6, C6; `index.ts:849`)
4. **Name who chases.** For a "Yes, if…", the condition needs an owner who will get the answer. The skill can ask
   "who will chase this?" but never assigns anyone. (Rettig, post 2: named owner for "getting the information at the front")
5. **Humans decide.** The skill never votes or comments on its own initiative and never infers the voter's name.
   It is already enforced by the tool descriptions (`refinementTools.ts:67-80, 91`) and the maintainer
   (`maintainer-input.md`).
6. **Where to look, not what it says**: link to the item in the work tracking system rather than fetching its
   detail (`maintainer-input.md`). The Refinement tab already links names out (`docs/teams/detail.md:201`).

What to check and say, by user:

| | **PO preparing the next Refinement** | **Developer prepping / voting** |
|---|---|---|
| Opening line | The `summary` from `refinement_get`, verbatim: the need band, the ready count, and the verdict Below / In / Above, with days until the next Refinement | "N Work Items wait for your view; your SLE is X days" |
| Check | `need.verdict`. If it is missing, `need.unavailableReason` (NoCadence / InsufficientData / NoRefinementStates), each with its fix | Items without *my* vote (`myVote`), in tab order |
| Check | Items with a No or a "Yes, if…": read the comments, collect the open conditions, ask who chases them | Items with "Yes, if…" or No from others: their comment is often the question to answer |
| Check | Items in refinement that are older than one cycle and still not ready. **Interpretation**: these are waiting work in disguise (Rettig: partly ready = WIP) | For each item: the link to open it, and the yardstick question in the instance's wording |
| Say | "Below range: refine about K more before Wednesday", or "Above range: stop refining, the extra ready work will just wait." | "Can you do GR-051 in 7 days or less? Yes / Yes, if… / No. I can record your answer once you confirm." |
| Never | Decide readiness, reorder the backlog, vote | Vote without explicit confirmation, guess the name, fetch item detail beyond the link |

**Interpretation.** For a PO the decision today is "refine more, or stop?". For a developer it is "which items need my
view, and what is my answer?". Discussion belongs in the session itself, on the No and "Yes, if…" items.

### 4.3 Daily Flow Review skill

Grounding: the Kanban Guide does not mandate a daily meeting but describes regular review of active items, with three
active-management duties: control WIP, prevent unnecessary aging with the SLE as reference, and unblock
(<https://kanbanguides.org/the-kanban-guide/>). The ProKanban skill says to lead with aging and to pair every aging
observation with an action (SKILL.md). The maintainer's purpose is to remove the status round
(`maintainer-input.md`).

**Proposed ranking of "what to DECIDE today"** (each item needs a decision, an owner chosen by the Team, and an action):
1. **Blocked items**, longest-blocked first (`isBlocked`, `blockedSince`). Decision: who unblocks or escalates, today.
   (Guide: "Unblocking blocked work.")
2. **Items past the SLE** (risk 100%). Decision: swarm, split, or accept the miss and finish it. Never re-estimate.
   (ProKanban SKILL.md; `flow-metrics.md:132`)
3. **Items at risk** (SLE Risk ≥ 70%, or, where risk is unavailable to the agent, age past the 70th percentile of
   cycle time). Decision: swarm or split. ProKanban uses 70th = urgent and Lighthouse fixes the at-risk line at 70%,
   so the two agree. (ProKanban SKILL.md; `flow-overview.md:47-51`)
4. **WIP over the system WIP limit**. Decision: what we will *not* start today, which item we finish first.
   ("finish work, and don't start work")
5. **Stale items** (no move past the threshold). Decision: is anyone on it; finish, move back, or cancel. (`flow-overview.md:96-118`)

**What to DISCUSS** (lighter: a question to the Team, no decision forced):
- Items **just under** the line: past the 50th percentile, or risk rising but still under 70%. "The items just under the
  line are the ones worth looking at before they cross it." (`flow-overview.md:55`; ProKanban: 50th = worth a conversation)
- A state where several items cluster (Time in State, or the aging chart by state), since that suggests a bottleneck.
  It often sits in review or getting ready. (`coaching-patterns.md` "Items aging in specific states"; Rettig, post 2)
- **A PBC signal**, if one appeared since the last review (see below).

**Output shape.** **Interpretation**, informed by the maintainer's purpose:
lead with "One thing to decide today", then up to three decisions, then up to two discussion prompts, then "Everything
else is flowing". Present items, not people. Walk from closest-to-done to newest. No per-person roll call; ProKanban
forbids per-person metrics (SKILL.md, never/instead).

**PBCs in the Daily Flow Review (maintainer input #1).**
- *What Lighthouse offers*: daily XmR per Team for Throughput, WIP, Total Work Item Age, Cycle Time and Arrivals, plus
  Feature Size for Portfolios. Signals: Large Change (outside the limits), Moderate Change, Moderate Shift, Small Shift
  (8 consecutive on one side; `XmRCalculatorTest.cs`). Status is Act on Large Change, Observe on Moderate Change.
  Limits come from a configured baseline, and without one Lighthouse uses the selected range
  (`docs/metrics/predictability.md:151-185`). The names of the two "Moderate" rules are verified, but their exact
  definitions were not checked.
- *How a signal feeds the daily*: a signal is a **discussion prompt, never a decision**. Say what moved and since when.
  Ask "what changed in our system around then?", and do not supply an answer. "A signal tells you the system is no
  longer the system you measured. It doesn't tell you which part of it moved." (Rettig, post 1)
- *Which signals matter on a given day*: **Total Work Item Age** and **WIP** are leading and actionable today.
  Throughput and Cycle Time signals are lagging and belong in a retro or a weekly review rather than the daily.
  **Interpretation**, consistent with ProKanban's "lead with aging" and the LPW post on TWIA as a leading indicator
  quoted in `skill/SKILL.md`.
- *Keeping it undogmatic*:
  - Say nothing about points inside the limits. Rettig: "it doesn't need explaining".
  - Do not raise a signal that the Team already discussed. The skill has no memory, so the facilitator decides.
  - With no baseline configured, do not raise signals as findings. Say once that the limits are drawn from the
    shown range, and that a baseline makes them meaningful (`predictability.md:161`).
  - Blackout days are hatched; never read them as signals (`predictability.md:175`).
  - Use "the chart shows a shift since Tuesday", never "your process is out of control".

### 4.4 Cross-cutting: AI and Kanban (Rettig)

- Agent-produced work still needs a human "done" owner, and review takes *more* attention (Rettig, post 2). **Interpretation**: the
  Daily Flow Review should treat items aging in review as a first-class finding.
- Throughput gains from AI may be item-size effects; check before celebrating (Rettig, post 1).
- If a Team draws its Start line upstream (assessing), Refinement states may count as WIP. Lighthouse's refinement states
  are chosen from To Do **and Doing** (`docs/concepts/concepts.md:83`), so this choice is already the Team's to make.

---

## 5. Structure patterns

**Copy:**
- A description front-loaded with discriminating triggers, plus **named neighbours** it must not fire on. (ProKanban SKILL.md; design spec)
- A short core that fits in context, with **references loaded on named conditions** ("load X when…"). (ProKanban SKILL.md)
- A **wrong/right worked example** per core question. These teach tone and method at once. (ProKanban SKILL.md)
- A **pre-answer checklist** of 6–9 yes/no checks. (ProKanban SKILL.md)
- **Eval cases** with positive, neighbouring and negative prompts, checked in CI against the behaviours named in the skill. (design spec; `evals/cases.json`)
  Lighthouse would add the cases ProKanban lacks: "what should we talk about in today's stand-up?", "are we ready for
  refinement?", "vote yes on GR-051 for me" (expected: ask for confirmation and name).
- **Report tool output as it is**: don't round, don't drop the lower confidence levels. (ProKanban SKILL.md) This matches
  Lighthouse's own "quote the `summary`" rule (`skill/SKILL.md`).

**Avoid:**
- **Absolute bans where Lighthouse deliberately differs.** ProKanban forbids classes of service and team comparisons
  outright. LPW publishes a nuanced position on both (§6). Use "prefer X; if you do Y, know Z" instead.
- **Over-broad triggering.** The current Lighthouse description fires on any flow-metrics question, "even without
  mentioning Lighthouse" (`skill/SKILL.md` frontmatter). With three skills, each description must say which job it owns,
  or they will compete for the same prompt. **Interpretation**.
- **A long prose blog index inside SKILL.md.** The current skill carries ~20 blog summaries in the always-loaded body.
  Move them to a reference.

---

## 6. Stance: opinionated, ProKanban-aligned, not dogmatic

ProKanban's own anti-dogma mechanics: answer first, then one line on what the alternative loses; answer velocity
"once" and do not relitigate; "Do not defend the method; answer the underlying question"
(SKILL.md; stakeholder-language.md).

| Lands | Puts people off |
|---|---|
| "GR-051 has been in progress 9 days; 6 of the 7 items that got this old went past 7 days. Swarm or split today?" | "GR-051 violates your SLE." |
| "We have an 85% chance of finishing these 20 on or before 14 March, and a 50% chance on or before 2 March. Which one do you want to plan against?" | "Single dates are fiction; I won't give you one." |
| "You finished 41 Work Items last month." (and stop) | "Story points are an anti-pattern because…" |
| "Inside the limits — nothing to explain." | "Throughput dropped 20% this week; what happened?" (noise) |
| "The chart shows a shift since Tuesday. Anything change around then?" | "Your process is out of control." |
| "Above range: the extra ready work will just wait. Stop refining?" | "You over-refined." |
| "If you want an expedite lane: it will age everything else. Want to see by how much?" | "Kanban forbids expedite lanes." |
| "What would help this item finish?" | "Who is working on this and why isn't it done?" |

Use the instance's terminology: the Lighthouse tools already state `summary` in it (`skill/SKILL.md`), and the project
rule says the same for docs (`/storage/repos/Lighthouse/CLAUDE.md`, "Write the configurable term").

---

## 7. Gaps and stale or wrong statements in the current Lighthouse skill

Files: `/storage/repos/lighthouse-clients/skill/SKILL.md`, `references/{flow-metrics,coaching-patterns,lighthouse-mechanics}.md`.

**Conflicts with ProKanban (decide deliberately; don't drift):**
1. "95% (certain)" (`SKILL.md` Response Style; `lighthouse-mechanics.md` percentile list). ProKanban: only 100% is
   certain, and it reads "zero items or more". Suggest "95%: very likely".
2. "give them two: a 50% and an 85% date" and "50% chance we'll be done by June 6th" (`SKILL.md`). Missing "on or
   before"; ProKanban calls "85% by January 25" wrong because it reads as a deadline.
3. "Give them the 85th percentile date… I wouldn't plan around [the 50%]" (`coaching-patterns.md`). ProKanban offers
   the choice and ties it to the cost of being wrong.
4. "Daily Scrum: anything older than the team's 85th percentile cycle time deserves a conversation" (`SKILL.md`), and
   "if an item ages beyond the 85th percentile, it gets escalated or killed" (`coaching-patterns.md`). ProKanban: the
   50th is worth a conversation, the 70th is urgent. Lighthouse's own SLE Risk uses 70%. The skill is late by a full band.
5. SLE: "rounding up slightly for buffer" (`flow-metrics.md`). ProKanban: don't round numbers into friendlier ones;
   the SLE comes from the data.
6. Cross-team comparison is endorsed (`SKILL.md`, "Cross-team metric comparison") against ProKanban's "never compare
   teams". The LPW position is trends, not ranking. Keep it, but say the item-size caveat (Rettig, post 1) and
   ProKanban's alternative: compare each Team with its own history.
7. "Separate flow classes (expedite lanes…) only justified when…" (`SKILL.md`, SLE section) against ProKanban's ban.
   Keep the nuance and add the cost: expediting ages other work.
8. "WIP should ideally not exceed 2x the number of people" (`flow-metrics.md`). This is per-person framing, and the
   Guide and ProKanban have no such rule. Drop it or label it as a rough heuristic.
9. "If [the forecast] includes a low-throughput period … adjusting the history window might help" (`coaching-patterns.md`)
   and the throughput-exclusion filter (`docs/teams/edit.md:83`) against ProKanban's "Do not clean outliers".
   Recommend Blackout Periods for known non-working days, and warn that excluding items hides process pain.

**Stale or missing relative to shipped Lighthouse:**
10. Widget lists in `SKILL.md` and `lighthouse-mechanics.md` are missing SLE Risk, Blocked Overview, Stale Items,
    Features Worked On, Flow Efficiency, Arrivals Run Chart and PBC, Load Balance Matrix, Cumulative Time per State,
    Blocked Over Time, Percentiles Over Time and PBC Over Time (`docs/metrics/widgets.md:20-25`).
11. "Lighthouse provides PBCs for: Cycle Time, Throughput, Total Work Item Age, WIP, and Feature Size"
    (`flow-metrics.md`). Arrivals is missing (`predictability.md:215`). The special-cause rules list only "Large Change"
    and "trends and runs", but Lighthouse names four: Large Change, Moderate Change, Moderate Shift, Small Shift.
12. Refinement is mentioned only as tool plumbing. There is nothing on the need band, verdict, cadence, the SLE
    yardstick, or Votes vs Stages readiness (`index.ts:849`).
13. Predictability Score thresholds "Above 60% decent / Below 40% unreliable" (`flow-metrics.md`) do not appear in
    the product docs, which give only the formula and "closer to 100%" (`docs/metrics/predictability.md:29-39`).
    Verify, or label as a rule of thumb.
14. "Lighthouse only knows about work items if they are linked to a feature in a portfolio" (`lighthouse-mechanics.md`).
    Teams have their own throughput query (`docs/concepts/concepts.md:64-74`), so this needs re-checking. **Unverified**,
    flagged for DISCUSS.
15. The coaching pattern on Work Item Age says the daily question is "which items are aging" (`flow-metrics.md`). That is
    good, and it should become the seed of the Daily Flow Review skill.

**Client/tool gaps that block the specific skills** (for DISCUSS, not the skill text):
16. **SLE Risk per item is not exposed** via CLI/MCP (no `sleRisk` in `lighthouse-clients/packages`). Without it the
    Daily Flow Review must approximate it from age percentiles. That is the weaker signal ProKanban's skill itself
    improves on.
17. **Staleness is not exposed** via CLI/MCP.
18. The MCP list has no current-WIP tool. `SKILL.md` says blocked-now comes from "current WIP", which the CLI reads
    (`cli/src/index.ts:1478`). MCP callers must work from `team_metrics_workItemAge`. Check whether that carries `isBlocked`.
19. Product docs lag: `docs/concepts/concepts.md:85` still says "More support for running refinement is planned".

---

## 8. Reuse and licensing

- **ProKanban skill text (SKILL.md, references) is CC BY-SA 4.0** (repo README, `REUSE.toml`). Adapting it into
  `lighthouse-clients` (MIT, `/storage/repos/lighthouse-clients/LICENSE`) would make the adapted parts ShareAlike.
  **Recommendation**: do not copy text. Restate the ideas, cite ProKanban as a source, and link the skill as a
  companion ("for CSV-only teams").
- **ProKanban scripts are MIT** and could be reused, but Lighthouse has its own forecasting engine. Reusing them is not needed.
- **Kanban Guide: CC BY 4.0**. Quoting the definitions with attribution is fine.
- **Blog posts** (Rettig, prokanban.org): short quotes with links only. Liz has collaborated with LPW
  (`maintainer-input.md`); asking her for review or explicit permission is cheap.
- ProKanban endorses **SLE Poker Planning** (planning-poker-revisited.web.app). LPW treats it as complementary, not a
  threat (`epic-5510-5881-refinement/feature-delta.md` D31). The Refinement skill could name it neutrally.

---

## 9. Knowledge gaps

- **Meetup transcript (maintainer input #2).** Attempted: WebFetch on the watch page (footer only), youtubetranscript.com
  ("YouTube is currently blocking us"), and YouTube `timedtext` (empty). The `ctx_url_read` tool named in the request is
  not in this agent's tool set. Only the title (oEmbed) and the meetup description were obtained. **Recommendation**: the
  main session fetches the transcript with `ctx_url_read(mode="transcript")` or `yt-dlp --write-auto-subs`, and appends
  §2.5.
- **Exact ProKanban wording.** It was read through a summarising fetch. Verify quotes against raw files before use.
- **Lighthouse "Moderate Change" / "Moderate Shift" exact rules.** Only the names and Small Shift (8 in a row) were verified.
- **ProKanban material on the daily meeting.** None found beyond the Guide's "review the active items regularly" and the
  aging questions. The ProKanban skill and its evals are silent on it, so the Daily Flow Review design rests on the Guide
  plus Lighthouse's own SLE Risk and PBC semantics.
- **Kanban Pocket Guide** (source of the 50/70 percentile triggers) was not read directly. The thresholds are cited via the ProKanban SKILL.md.

## 10. Sources

Web (accessed 2026-10-08): ProKanban repo, SKILL.md, stakeholder-language.md, data-prep.md, design spec, evals (URLs in §2.1);
Rettig posts (§2.2, §2.3); Kanban Guide May 2025 and v2020.12 (<https://kanbanguides.org/the-kanban-guide/2020.12>);
ProKanban aging blog (§2.4); meetup page and YouTube (§2.5).

Local: `maintainer-input.md`; `/storage/repos/lighthouse-clients/skill/SKILL.md` and `references/*`;
`/storage/repos/lighthouse-clients/{ARCHITECTURE.md,LICENSE}`; `packages/mcp-core/src/{index.ts,refinementTools.ts}`;
`packages/client/src/metricsWording.ts`; `/storage/repos/Lighthouse/docs/metrics/{flow-overview,flow-metrics,predictability,widgets}.md`;
`docs/concepts/concepts.md`; `docs/teams/{detail,edit}.md`; `docs/feature/epic-5510-5881-refinement/{feature-delta.md,slices/slice-10-sle-yardstick.md}`;
`Lighthouse.Backend.Tests/Services/Implementation/XmRCalculatorTest.cs`.

## Addendum — Liz Rettig, "Agentic AI Meets Flow" (Lighthouse Live meetup)

Source: https://www.youtube.com/watch?v=YBdlyT00u8k — transcript read by the orchestrator via the
YouTube transcript (the research agent could not fetch it). Paraphrased unless quoted; quotes ≤ 2
sentences. No timestamps were available in the transcript text.

- **A daily flow review, fed by Lighthouse, is what changed the teams.** Her client built a "flow
  review" that runs every day, "pulls data directly from Lighthouse, and it puts it right in front of
  the engineers and the … product managers every day." It is the direct precedent for #6246: the
  daily look at WIP and at how old items are getting, not a status round.
- **Cadence collapsed to daily.** Instead of a retro every two weeks, they looked at their data daily
  and could run a "flow retro at the drop of a hat"; the daily review *nudges* the team to run one.
  Example: a team with an SLE of "two days or less 85% of the time" saw a slip the same day instead of
  in a scatter plot two weeks later. → Daily Flow Review may *suggest* a retro when a signal persists;
  it does not try to be the retro.
- **Nudge, never block; ask for a reason.** At the start of new work, their tooling checked the pod's
  WIP limit and SLE in Lighthouse and asked "Did you know that you're over your WIP limit? Are you sure
  you want to start something?" — people could still start, but had to document why, which fed the
  retro. "Did you know you have work that's already over that service level expectation?" → matches
  the maintainer's "humans always decide": the skills surface the question and the data; people decide
  and say why.
- **"AI without flow is a WIP bomb."** With agents, starting is nearly free; flow practices are what
  make the extra capacity land as finished work. → stance line for the general skill: stop starting,
  start finishing — framed as making AI-scale output actually deliver.
- **Agents have no culture; they drift to the industry mean** (sprints, velocity, averages). Their
  framework carried explicit guardrails, including a *banned-words list* for how to speak about flow,
  and still had to be watched because the model "forgets". → the skills need an explicit
  never/instead wording table (ProKanban has one too) and should avoid velocity/average language.
- **The bottleneck moved to the hand-off.** With agents building, the constraint is unambiguous
  scoping before work starts — not detailed up-front specification. → Refinement skill: the question
  is "is it clear enough to start and small enough to finish within the SLE", not "is every detail
  known"; details live in the work tracking system (link only), per the maintainer.
- **People are less interested in the data than the coach is.** Teams want to know what it *means*;
  they liked a combination of their board items and forecasts in one place. → lead with the
  decision/discussion item and the one number behind it, not with charts.
- WIP limit heuristic used there: a blanket "two per person" for pods where one person specifies
  while validating another item's agent output — context-specific; the skill should not present it as
  a rule (the current skill's "WIP ≤ 2× people" is flagged above as too absolute).
