# Recommendation — Epic 6455 "Deeper hierarchies"

Wave: DIVERGE, handoff to DISCUSS · 2026-10-30. Supporting artifacts in `diverge/`: `job-analysis.md`,
`competitive-research.md` (top-up on Epic 5806's), `options-raw.md`, `taste-evaluation.md`. Customers are
anonymised as Customer A and Customer B.

## Decision for DISCUSS

**Proceed with Option 1, a level stack above the Feature, on the Portfolio.** The Feature stays the Teams'
direct parent. A Portfolio may declare an ordered list of named levels above it (for Customer A: Change, Project,
Program). Each level lists its items. Each item gets a "likely by" date, composed from the level below with the
joint "all done" maths Deliveries already use. Each level also gets cycle time, throughput and WIP from its own
states. Two-level Portfolios declare no levels and behave exactly as today. Every item must pass through every
declared level; an item that skips one is the Parent Override Field's business (decision 4), and the setup
shows how many items do not reach each level.

This holds **on the assumption that** Customer B (session 2026-11-02) wants the Business Epic level forecast,
and accepts Epics as the Lighthouse Feature with the levels above them shown. If Customer B instead needs to
plan *only* at the Business Epic and will not see Epics, the dissent below becomes the live question.

Option 1 scored 4.20, ahead of Stacked Portfolios (3.55), Look-through levels (3.55) and Parent-grouped Deliveries
(3.50), and it won under every weighting tried (`diverge/taste-evaluation.md` §5).

## Traceability (job → research → scores)

- **Job** (`diverge/job-analysis.md` §2): "a defensible 'when, and how is it flowing?' at every level where the
  organisation promises". Most under-served outcomes: OS1/OS2, a real forecast at the promise level (17), and
  OS4, a level above it (13).
- **Key reframe** (§4): under decision 4, shape (a) is "Epics as Features plus levels above them", which is
  shape (b) with more levels. **So one direction serves both shapes, and Option 1 is it.** OA5 (are the
  Project/Program roll-up and the initiative view one opportunity?) is answered yes: both are a level composed
  from the level below.
- **Research** (`diverge/competitive-research.md`): Jira and Azure DevOps both add levels **above** existing
  ones and never between them; Targetprocess requires every item to pass through each level and shipped that
  way. No competitor composes probabilistic forecasts level by level, so the gap Epic 5806 found is still open.
  No tool warns at setup that items do not reach the level above.
- **Scores**: Option 1 is the only option at 4 or 5 on every taste criterion, and it has the highest
  desirability (5) with mid feasibility (3).

## Top 3

### Option 1: Level stack above the Feature — 4.20

**Why it scores well**: a two-level user sees nothing new (T3 5). One concept, anchored to the Parent column
everyone already sees (T2 4). It serves Customer A's three asks (E2: Change "when", Change flow metrics,
Project/Program roll-up), Customer B, SAFe and shape (b), and every level is named and visible, as the
maintainer wants.
**Core trade-off**: the organisation's promise level is no longer the Lighthouse "Feature". For Customer A,
the Portfolio's Features are Epics, the level they told the maintainer they do not care about, and Changes live
one level up.
**Key risk**: OA2. Composing a Change from its Epics' forecasts is only as good as the Epics. An Epic that is
not broken down yet takes the default size, as any Feature does today, so a Change with many empty Epics is
still partly a guess. That guess must stay visible on the Change ("3 of 8 {features} use the default size"),
never hidden in the composed date.
**Hire criteria**: an organisation whose tracker has a strict parent chain from the Teams' items to the
level it promises at, and that wants dates and flow at each level above the Teams' direct parent.

**What DISCUSS inherits (the answers to the brief's questions):**

| Question | Option 1 answer |
|---|---|
| What a level is | A named, ordered entry on the Portfolio above its Feature level: tracker types plus an attach field |
| How items attach | One parent per item per level, read by the existing `ParentSourceSelector` (native parent, a field, or a Jira link type), set per level. Link-typed or multi-value attach for top goal levels (OKR, SAFe Themes) is a later step, not v1 |
| What each level answers | *When*: joint likelihood that all its children are done, composed upwards (Work Items → Features exactly as today; Features → Changes → Projects). *Flow*: cycle time, throughput, WIP at that level from the items' own states (the computation Portfolio metrics already run for Features). *Progress*: children done/total and Work Items beneath. A goal level with no done state answers only "when is the linked work done" |
| Team Feature WIP and the simulation's parallelism | **Unchanged.** Teams work on Features (Epics); auto-adjusted Feature WIP keeps counting direct parents; `SimulatedRun`'s "Features at once" stays at that level. Upper levels show WIP as a read-only metric, not a simulation input. Today's "(Item not tracked by Lighthouse)" rows for Customer A disappear because Epics are now the tracked Features |
| Terminology | One name pair per declared level, set where the level is declared. The Feature level keeps the instance-wide Feature term. Customer B would rename Feature to "Epic" and name its levels "Feature" and "Business Epic"; SAFe users the same pattern |
| Deliveries | Unchanged: a cut across the Feature level. A Delivery rule may match an upper-level item, replacing today's "one rule per Change" workaround |
| Migration | None for two-level instances (no levels declared = today). Shape (a) Portfolios (Feature type = Change) are offered a one-step change: "your Changes have no Work Items, but their children do. Make Epic the Feature type and Change a level above?" Expand-only storage (C6) |
| Setup | Portfolio settings: "Levels above {feature}" → add a level, pick types, name it. Before saving, a count per level: "N {features} have no {level}" and "N {level items} have no {features}". The shape (a) detection above runs from data Lighthouse already holds (the untracked parents behind Team Feature WIP) |
| Premium boundary | Candidate: the Feature level and today's Parent column stay free; declared levels above are Premium (the norm: Jira Plans Premium, Linear Enterprise). Open question below for prospects |
| Connectors | Jira and Azure DevOps: repeat the existing parent fetch per level. Linear: Projects stay the Feature level, initiatives and sub-initiatives become levels (sub-initiatives allow several parents, so v1 takes one and reports the rest). CSV: lift Epic 5806 S7 and S8 (parent details, rows of non-Feature types). ServiceNow: not precluded, untestable today |

### Option 2: Stacked Portfolios — 3.55

**Why it scores well**: every level is a whole Portfolio with everything a Portfolio already has (Deliveries,
Reports, access), and the Premium line is inherent because the free tier allows one Portfolio.
**Core trade-off**: two new concepts (a Portfolio fed by a Portfolio, and a Feature word per Portfolio), and
forecasts that chain across Portfolios refreshed at different times.
**Key risk**: an upper date computed from a lower Portfolio's older forecast reads as current; composition
across Portfolios is new code with no precedent (each Portfolio forecasts alone today).
**Hire criteria**: an organisation whose levels are also groups of people with their own owners and access (a
Program office, an Agile Release Train), each wanting its own space.

### Option 4: Look-through levels below the Feature (relay) — 3.55

**Why it scores well**: one setting, and the promise level stays the Lighthouse Feature, so nothing on the
Portfolio page changes for Customer A.
**Core trade-off**: it fixes the forecast of shape (a) only; no Project/Program roll-up, nothing for shape (b),
and the intermediate level is invisible, against the maintainer's direction.
**Key risk**: Team Feature WIP. Either it counts Changes, which the Team does not work on one at a time, or it
counts Epics while the simulation runs on Changes (OS6). Both bend the simulation.
**Hire criteria**: an organisation that only ever promises at one level and never wants to see the level
between.

Below the top 3: Parent-grouped Deliveries 3.50 (no Change flow metrics; floods Deliveries with generated
rows); Query-scoped items 2.90 (no levels; Linear and CSV cannot; one query per Feature per refresh);
Instance-wide Level Model 2.85 (the right long-run shape, at the highest cost; see the dissent).

## Dissenting case

**Against Option 1, for keeping the promise level as the Lighthouse Feature (Option 4, or Option 1 with a
"home level").** The maintainer reported that customers with shape (a) "jump over the intermediate level because
they do not care about those items". Option 1 makes exactly those items (Epics) the Lighthouse Features: the
Portfolio's main list, the size charts and the Feature WIP the Team page reads. Customer A selected Change "when",
Change flow metrics and a Project roll-up, and did **not** select an Epic-level view (E2). The level they care
about moves one click deeper. They lose nothing they had, since today's Change forecasts are guesses, but
the first screen they see is the one they did not ask for. If Customer B says the same on Monday, the
recommendation should become **Option 1 plus a per-Portfolio "home level"**. The Portfolio would open on
Changes, with Epics one click down. That keeps Option 1's maths and WIP semantics and answers the dissent
without the relay's WIP problem. DISCUSS should keep that variant open until the 2026-11-02 session.

**A second, longer-run dissent: Option 3, the Instance-wide Level Model.** Option 1 declares levels per
Portfolio. An organisation with twelve Portfolios on one tracker declares the same levels twelve times, and
their names can drift. That is the maintainer's direction in full ("model each org's hierarchy"), and the
trackers themselves model it once (Jira's work type hierarchy, Azure DevOps backlog levels). The answer is not
to build Option 3 now (2.85, highest cost) but to let Option 1 grow into it. Levels declared on the Portfolio
can later follow a connection default, the way the Parent Override Field already does ("Settings an owner
inherits from its connection", `ARCHITECTURE.md`). DISCUSS should keep that door open in the level's data shape.

## Questions only the maintainer (or Monday's Customer B session) can answer

1. **"Team → Agile Release Train → Program": item levels or groups of Teams?** Those names are groups of people,
   not item types. If the maintainer meant groups that each own a level of items, Option 2 (Stacked Portfolios)
   gains, and the two can combine (a stacked Portfolio is a level with an owner).
2. **Home level** (dissent): should a Portfolio open on its Feature level or on a chosen upper level? *Customer B
   session.*
3. **Customer B**: what do they need at Business Epic (when, flow metrics, a roll-up)? Do they accept Epics as
   the Lighthouse Feature, renamed? Does every Epic have a SAFe Feature parent (uniform depth, OA1)? Do they
   want the SAFe Feature level shown?
4. **Premium line for prospects**: levels above the Feature are Premium in this proposal, but Customer A failed
   during an *evaluation*. Does a trial see Premium levels, or should the first declared level be free?
5. **Levels per Portfolio now, connection default later**: acceptable, or must v1 already define levels once
   per connection?
6. **Goal levels and many-to-many** (OKR, SAFe Themes, Linear sub-initiatives with several parents): v1 takes one
   parent per level and reports the rest. Acceptable to defer link-typed and multi-parent levels?
7. **Optional top level** (Customer A's Program): items with no Program are listed under "No {program}", and the
   level stays declared. Acceptable?
8. **Migration of existing shape (a) Portfolios**: offer the one-step "make Epic the Feature type" suggestion
   automatically, or only document it?
