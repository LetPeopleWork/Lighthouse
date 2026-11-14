<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-6455-deeper-hierarchies

**Epic**: ADO #6455 "Deeper hierarchies: portfolio items that are not direct parents of the team's items".
Supersedes #5806 and #6119 (both set to Removed on 2026-10-30).
**Waves present**: DISCOVER (2026-10-30), DIVERGE (2026-10-30; `recommendation.md`, `diverge/`), DISCUSS (started 2026-11-02, scope and split only so far).
**Density**: lean (Tier-1 [REF] sections only). Decisions in `discover/wave-decisions.md`.
**Anonymisation**: customers are "Customer A" and "Customer B". No company, person or host names appear in
this workspace. Tracker type names (Story, Epic, Change, Project, Program, Feature, Business Epic) are the
customers' literal Jira issue types and are written as such; Lighthouse concepts use the default terms
(Feature, Work Item, Team, Portfolio, Delivery).

---

## Wave: DISCOVER / [REF] Evidence

| # | Source | Date | Kind | What happened (past behaviour) |
|---|---|---|---|---|
| E1 | Customer A, call with the maintainer, summarised in ADO #6455 | 2026-10-30 | Observed attempt (second-hand, maintainer's summary) | Jira Cloud. Hierarchy bottom-up: Story ← Epic ← Change ← Project ← (optional) Program. They configured Change as the Portfolio's Feature type and skipped Epics. Stories link to Epics, not Changes, so no Change has child Work Items. The forecasts were not usable, and they stopped. No workaround in use: blocked. |
| E2 | Customer A, maintainer follow-up answers | 2026-10-30 | Stated need (multi-select) | Wants answered at Change/Project level: when a Change will land; Change-level flow metrics (cycle time, throughput, WIP); a Project/Program roll-up. Did not select "an Epic-level view too". |
| E3 | Customer B, Slack message to the maintainer with a screenshot | 2026-10-23 | Observed warning in a real instance (screenshot; "struggling" is the maintainer's word) | Jira. Hierarchy top-down: Business Epic → Feature (SAFe) → Epic → Story. A Business Epic, configured as a Lighthouse Feature, shows the warning "No child Work Items were found for this Feature" (`Lighthouse.Frontend/src/utils/features/featureWarningSentences.ts:36`). Struggling with it; no workaround reported. |
| E4 | Maintainer, from other customers | 2026-10-30 | Maintainer-reported, unnamed | "Seen it before": more cases of shape (a) below. Count and names unknown. |
| E5 | Maintainer, from other customers | 2026-10-30 | Maintainer-reported, unnamed | Shape (b): Epics map cleanly onto Lighthouse Features (value streams as Portfolios), but the level above (Flight Level III: initiatives, strategy) is missing. Count and names unknown. |
| E6 | Code (this wave's inventory, below) | 2026-10-30 | Structural certainty | The failure in E1 and E3 is deterministic: a Feature only receives Work Items whose own parent reference equals the Feature's id (`WorkItemService.cs:703`). Any hierarchy where the forecast level is two or more steps above the Team's items produces it. |

Grading: E1 and E3 are past behaviour (a real configuration, a real result, a real stop). E2 is stated
preference from a customer who has already acted, so moderately strong. E4 and E5 are hearsay until named.
E6 converts "does this failure exist?" from a question into a fact; what remains uncertain is **how many**
customers have the shape and **what they need instead**.

---

## Wave: DISCOVER / [REF] Persona IDs

SSOT ids in `docs/product/personas/`. No new persona.

| Persona | Role here |
|---|---|
| `config-admin` | **Primary for shape (a).** Sets up the Portfolio, picks the Feature type and the Parent Override Field, and is the one who "skips a level without noticing". Customer A's contact acted in this role. |
| `delivery-forecaster` | **Primary consumer for shape (a).** Wants "when will this Change / Business Epic land" and flow metrics at that level. |
| `delivery-lead-rte` | **Primary for shape (b)**, carried from Epic 5806: needs the strategic level above correctly mapped Features. Also the likely reader of a Project/Program roll-up (E2). |
| `forecasting-prospect` | Customer A is a prospect: the failure happens during onboarding, so the cost of the problem is a lost evaluation. |

---

## Wave: DISCOVER / [REF] Opportunity statement

Two related but distinct problem shapes. They are kept apart on purpose: one solution may serve both, but
that is a DIVERGE question, not an assumption.

**Shape (a): the forecast level is two or more steps above the Team's Work Items.**
> When the level my organisation plans and promises at (a Change, a Business Epic) sits two or more steps
> above the items my Teams work on, Lighthouse finds no Work Items under it and forecasts it from a default
> size, so I cannot tell when it will land and I stop using Lighthouse.

In the customers' words: "Lighthouse finds no team items under the Changes, and none of the forecasts are
usable" (E1); "No child Work Items were found for this Feature" on their top-level item (E3).

**Shape (b): the level above a correctly mapped Feature is missing.**
> When my Features and Portfolios map cleanly (Epics as Features, value streams as Portfolios), I still have
> no view of the strategic level above them, so I answer "when does this initiative land" by hand.

This is the Epic 5806 problem, now maintainer-reported from customers (E5), not only the maintainer's own idea.

**The frame around both (maintainer, 2026-10-30).** The aim is flexibility in modelling, not a fix for one
customer's hierarchy. The underlying opportunity is:
> When my organisation's structure does not match Lighthouse's fixed Work Item → Feature shape, I want to
> describe my own levels on Lighthouse's concepts (what a level is, what it rolls up, what it can answer), so
> forecasts, flow metrics and roll-ups appear at the levels we actually plan and promise at.

Customer A, Customer B, SAFe, OKR-style setups and today's two-level setup are the test cases (see
"Structures that must be expressible"). Only Customers A and B are customer evidence; SAFe and OKR are
framework knowledge and the maintainer's direction.

### Provisional Opportunity Solution Tree (scores are low-confidence: 2 customers plus maintainer reports)

Score = Importance + max(0, Importance − Satisfaction), each 1-10.

```
Desired outcome: a Portfolio configured at the level an organisation promises at gives usable forecasts
  |
  +-- (a) O1 Know when a top-level item (Change, Business Epic) lands, from Work Items 2+ steps below  [I9 S1 = 17]
  +-- (a) O2 Notice during setup that a level is being skipped, before forecasts go wrong             [I8 S2 = 14]
  +-- (a) O3 Roll up one level further (Project, Program)                                             [I6 S1 = 11]
  +-- (a) O4 Team Feature WIP that counts the level the Portfolio forecasts                           [I6 S2 = 10]
  +-- (a) O5 Flow metrics (cycle time, throughput) at the top-level item                              [I6 S6 =  6]
  +-- (a) O6 Still see the intermediate level (Epics) as its own row                                  [I3 S3 =  3]
  |
  +-- (b) O7 See the strategic level (initiatives) above correctly mapped Features, with when it lands [I7 S2 = 12]
```

Notes on the scores:
- O1 satisfaction 1: today's answer is a default-size guess (`WorkItemService.cs:742-754`), shown with a warning.
- O2 satisfaction 2: the warning exists per Feature (E3), but nothing at setup time says why, and Customer A stopped.
- O5 satisfaction 6: Portfolio cycle time and throughput are computed from the Features' **own** tracker
  states (`PortfolioMetricsService.cs:276`), so they already work at Change level once Change is the Feature
  type. Depth does not break them. Not verified with Customer A, who stopped before looking.
- O6 importance 3: Customer A did not ask for it (E2). Weak signal, one customer.
- O3 and O7 overlap: a Project/Program roll-up (shape a) and an initiative view (shape b) are both "a level
  above the forecast level". DIVERGE must decide whether they are one opportunity.

---

## Wave: DISCOVER / [REF] Current-State Hierarchy Inventory

Read from the worktree on 2026-10-30 (HEAD `f9521a42f`).

### How a Feature gets its Work Items (all connectors)

| # | Finding | Citation |
|---|---|---|
| H1 | Every Work Item and Feature carries exactly **one** `ParentReferenceId` string. No ancestor chain is stored. | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemBase.cs:24` |
| H2 | The roll-up is an **exact match on the direct parent**, not a query: for each Portfolio Feature, Lighthouse takes every stored Work Item whose `ParentReferenceId == feature.ReferenceId`, groups by Team, and counts remaining and total. It is not scoped by connection or Team. | `Services/Implementation/WorkItems/WorkItemService.cs:696-724` (match at `:703`) |
| H3 | A Feature with no matched Work Items that is not Done is **extrapolated** with the Portfolio's default size (fixed or percentile) and assigned to the owning Team, Feature-owner Teams, or all involved Teams, and flagged `IsUsingDefaultFeatureSize`. So forecasts are not empty: they are guesses. The UI says "No child Work Items were found for this Feature…". | `Models/Portfolio.cs:190-193`, `WorkItemService.cs:726-755`, `:819-830`; `Lighthouse.Frontend/src/utils/features/featureWarningSentences.ts:36` |
| H4 | Team Work Items and Portfolio Features are fetched **independently** (Team query, Portfolio query). Nothing fetches the level in between. The only other level fetched is the Feature's own parent, stored as a Feature row flagged `IsParentFeature` (one level above the Feature, display only). | `WorkItemService.cs:1302-1329` |
| H5 | The parent source rule is one pure selector shared by connectors: either the tracker's own parent, or whatever the Team's/Portfolio's **Parent Override Field** names (a field, or on Jira a link type). It replaces the parent; it never walks further. | `Services/Implementation/Parents/ParentSourceSelector.cs:33-83`; `ARCHITECTURE.md:63` |

### Per connector

| Connector | Team Work Item → Feature | Feature → its parent | Can it reach two levels up today? |
|---|---|---|---|
| **Jira** (Cloud and DC) | `issue.ParentKey` (`JiraWorkTrackingConnector.cs:1783`), replaced by the Parent Override Field when set: a field value, or the key at the other end of a named issue-link type (`:1811-1840`). DC may read a named parent field (`:1868-1897`). | Same mechanism on the Portfolio. | **No.** Only if the tracker itself carries the grandparent key in a field on each Story (e.g. kept by a Jira automation) and the Team's Parent Override Field names it. That is tracker-side work, undocumented as a pattern. |
| **Azure DevOps** | The Parent Override Field's value when non-empty, else the native Hierarchy-Reverse parent link read from relations (`AzureDevOpsWorkTrackingConnector.cs:136-153`, `:1384-1418`, `:1516-1527`). | Same (`:672-694`). | **No.** Same tracker-side field workaround only. A relation type named as Parent Override reads nothing (`docs/concepts/worktrackingsystems/azuredevops.md:222`). |
| **Linear** | The issue's project. If the issue has no project, Lighthouse **walks up the sub-issue chain** until an ancestor issue has one (`LinearWorkTrackingConnector.cs:389-421`, used at `:342-367`). | The project's first initiative (`:423-438`); initiatives fetched as parent rows (`:164-200`). Parent Override is not read. | **Partly, and only below the Feature.** The sub-issue walk is the one place in Lighthouse that already "looks through" intermediate items. The Feature level is fixed to Linear Projects, so an initiative cannot be the forecast level. Documented in `docs/concepts/worktrackingsystems/linear.md:34-43`. |
| **CSV** | The "Parent Reference Id Column" per row (`CsvWorkTrackingConnector.cs:208`, `:239`). | Same column; `GetParentFeaturesDetails` returns nothing (Epic 5806 S7). | **No.** Rows whose type is not a Portfolio Feature type are skipped (Epic 5806 S8), so an intermediate row cannot even be stored. |
| **ServiceNow** | Team level only. | Portfolios cannot use ServiceNow: "ITSM tables carry no parent record Lighthouse could roll a portfolio up over" (`ServiceNowWorkTrackingConnector.cs:30-33`, `:972`). | **N/A** today. Any concept must leave room for it but cannot be tested on it. |

### What users are told

- `docs/teams/edit.md:371-395` and `docs/portfolios/edit.md:260-285`: Parent Override Field "overrides" the
  native parent, with a per-tracker table. Nothing says the Team's items must be **direct** children of the
  Portfolio's Features, nor what happens if they are not. This is the setup gap Customer A fell into (O2).
- `docs/concepts/worktrackingsystems/csv.md:83`: "the child items must be linked to these Features" — the
  only place the direct-child requirement is stated, and only for CSV.

### What depends on the direct link (for "what would a relayed level mean")

| Capability | What it reads | Effect of shape (a) today | Question a relayed or third level must answer |
|---|---|---|---|
| Feature forecast | Remaining Work Items per Team under the Feature (H2), else default size (H3); Monte Carlo per Team throughput | Default-size guess for every top-level item | Which Work Items count: all under every intermediate item? How is an intermediate item with no Work Items sized: default per intermediate item, or per top-level item? |
| Team Feature WIP | Count of distinct **direct** parents of the Team's in-progress items when auto-adjust is on (`TeamMetricsService.cs:899-908`, `InProgressFeatureResolution.cs:17-18`); drives how many Features a Team works at once in the simulation (`Forecast/SimulatedRun.cs:110-115`) | Counts Epics, not Changes. The Team page lists each Epic as "(Item not tracked by Lighthouse)" (`InProgressFeatureResolution.cs:48-62`). Simulation parallelism is set from a level the Portfolio does not forecast. | Does Feature WIP count the forecast level (Changes) or the intermediate level (Epics)? They can differ by a large factor. |
| Portfolio cycle time, throughput, WIP | The Features' own states and dates (`PortfolioMetricsService.cs:21-31`, `:269-276`) | **Not broken** by depth: works at Change level already | None for the metric itself; only for whether the Features list is the right one. |
| Feature size charts, progress bars, Delivery burn-up | `FeatureWork` totals (child counts) | All default-size | Same as forecast. |
| Parent column, parent rows | One `ParentReferenceId` on the Feature (H4) | Shows Project for a Change; Program never | Is the level above the forecast level (Project, Program, initiative) the same thing as shape (b)? |

### How Deliveries relate

A Delivery is a hand-picked or rule-selected set of a Portfolio's Features with one joint forecast
(`Models/Delivery.cs:61`, `:129-145`, `:556-575`). Rule-based Deliveries (Premium) can match on
`feature.parentreferenceid` (`Services/Implementation/WorkItemRules/FeatureFieldProvider.cs:23`, `:60`).
So today a **partial workaround** exists for Customer A: set the Portfolio's Feature type to Epic (the direct
parent), then create one rule-based Delivery per Change ("parent reference = CHG-1"). That yields "when will
this Change land" with real Work Item counts, but not Change flow metrics, one Delivery per Change by hand,
Premium only, and no Project/Program roll-up. Customer A was using no workaround (E1); whether they knew of
this one is not known. Whether to lean on
it is a DIVERGE question (old D8, now open).

### How far today's concepts go towards configurable structure, and where they are fixed at two levels

| Concept | Already flexible | Fixed at two levels |
|---|---|---|
| Team / Work Item | Any tracker type can be a Team's Work Item; the Team's query decides which | A Work Item has exactly one parent, and only its **direct** parent counts (H1, H2) |
| Portfolio / Feature | Any tracker type can be the Feature type, so the forecast level can already be "Change" or "Business Epic"; a Feature can be in several Portfolios; Portfolio flow metrics read the Feature's own states, so they work at any level (`PortfolioMetricsService.cs:276`) | A Feature's size and forecast come only from Work Items one step below (H2, H3). Nothing between Feature and Work Item exists as a concept |
| Parent of a Feature | Shown in the Parent column, fetched as an `IsParentFeature` row; any type | Display only, one level, no roll-up, no forecast (H4; old 5806 S2) |
| Parent Override Field | Per Team and per Portfolio, a field or (Jira) a link type; so "parent" need not mean the tracker's own parent link (H5) | Still yields exactly one key; never walks (H5) |
| Delivery | A cut across a Portfolio's Features, by hand, by rule (including on parent reference) or by Jira Release; one joint forecast | A set of Features in one Portfolio, not a level; nothing groups Deliveries |
| Terminology | Users rename team(s), work item(s), feature(s), portfolio(s), delivery/deliveries and more (`Services/Implementation/Seeding/TerminologySeeder.cs:29-161`) | One term pair per **fixed** concept; there is no key for a level that does not exist, and no notion of "level N" |
| Linear | Walks up sub-issues to find the project (`LinearWorkTrackingConnector.cs:389-421`) | Feature level fixed to Linear Projects; Initiative display-only |

In short: Lighthouse is already flexible about **which tracker type plays which role**, and about **naming**.
It is fixed in **how many roles exist** (Work Item, Feature, a display-only parent) and in **how roles connect**
(one direct parent).

---

## Wave: DISCOVER / [REF] Validated assumptions with confidence

| # | Assumption | Evidence | Confidence |
|---|---|---|---|
| VA1 | Shape (a) makes Lighthouse's forecasts unusable for the affected Portfolio | E6 (code, deterministic), E1, E3 | **High** |
| VA2 | Shape (a) occurs at real customers, on Jira, with different tracker vocabularies (Change; Business Epic) | E1, E3 (two independent instances, same shape) | **Medium** (2 named-anonymised cases; more maintainer-reported, E4) |
| VA3 | Shape (a) is an onboarding failure: the person setting up does not understand why the Feature is empty | E1 (stopped), E3 (struggling); no setup copy explains it (inventory "What users are told") | **Medium** |
| VA4 | No Lighthouse setting reaches two levels up on Jira, Azure DevOps or CSV | Inventory H2, H5, per-connector table | **High** |
| VA5 | The customers' trackers do hold the full chain (each Story names its Epic, each Epic its Change) | E1 explicit; E3 implied by a Jira issue hierarchy | **Medium** (not seen in data) |
| VA6 | Shape (b) exists at customers, not only in the maintainer's view (reverses part of 5806 G1) | E5 | **Low-Medium** (maintainer-reported, unnamed) |

## Wave: DISCOVER / [REF] Invalidated assumptions with evidence

| # | Assumption (was) | Evidence against |
|---|---|---|
| IA1 | "Forecasts are empty" (wording in ADO #6455) | They are filled with a default-size extrapolation and flagged (H3). The failure is worse than empty: a plausible-looking date built on a guess. The warning is the only signal. |
| IA2 | Epic 5806 D14 "one level up only" is enough | For shape (a) the whole problem is the step beyond one level. |
| IA3 | Epic 5806 framing: "the problem is a page for the level above Feature" | Customer evidence shows the first problem is configuration and forecasting at a deep level (a); a page is at most part of (b). |
| IA4 | Change-level flow metrics are blocked by the depth | Portfolio cycle time and throughput read the Features' own states (`PortfolioMetricsService.cs:276`). Only Team Feature WIP is affected (it counts direct parents). |

---

## Wave: DISCOVER / [REF] Open assumptions, risk-scored

Risk = Impact×3 + Uncertainty×2 + Ease×1 (each 1-3; Ease 3 = months). >12 test first.

| # | Assumption | Cat. | I | U | E | Risk | Cheapest test |
|---|---|---|---|---|---|---|---|
| OA1 | Every team item reaches the forecast level through the **same** number of steps (no Story links straight to a Change, no mixed depths) | Feasibility | 3 | 3 | 1 | **16** | Ask Customers A and B for a JQL count: Stories whose parent is a Change; Epics with no Change |
| OA2 | Forecasting a Change from all Stories under all its Epics is meaningful, including Epics not yet broken down | Value/Feasibility | 3 | 2 | 2 | **15** | Desk spike on demo data plus a walk-through with Customer A |
| OA3 | Shape (a) recurs beyond two customers | Value | 3 | 2 | 1 | **14** | Maintainer names the "seen it before" cases; count Portfolios with a high share of default-sized Features on reachable instances |
| OA4 | One concept serves every connector (Jira, Azure DevOps, Linear, CSV; ServiceNow later) | Feasibility | 3 | 2 | 1 | **14** | Desk check per connector of how an intermediate level would be fetched |
| OA5 | A level above the forecast level (Project/Program for a; initiatives for b) is one opportunity | Value | 2 | 3 | 2 | **14** | Ask Customer A what a Project roll-up is used for, and one shape-(b) customer what an initiative view is used for |
| OA6 | The intermediate level only needs to be looked through, not shown | Value | 2 | 3 | 1 | **13** | Ask Customer B (not yet asked); Customer A said so weakly |
| OA7 | Fetching an intermediate level is affordable per refresh | Feasibility | 2 | 2 | 1 | **9** | Count extra requests on the largest Jira demo |

---

## Wave: DISCOVER / [REF] Dropped options

| Option | Why dropped |
|---|---|
| A tracker-specific fix (e.g. Jira-only "use the Jira hierarchy level" setting) | Maintainer: the solution must be a Lighthouse concept, not tracker-specific. |
| Telling customers to add a grandparent field via tracker automation | Shifts the problem to the customer's admin, is unknown to most, and fails for CSV and Linear. Can be documented as an interim note, not as the answer. |
| Building the 5806 Initiatives page as specified (D1-D27) | Its problem framing is superseded (IA3). Its decisions are reused selectively (see reuse section). |
| Choosing between relay, third level, or both now | DIVERGE's job. Framed below as candidate directions only. |

### Candidate directions for DIVERGE (not a recommendation)

The maintainer's aim (2026-10-30) is **flexibility in modelling**, not a fix for one customer's hierarchy:
Lighthouse defines its own concepts clearly (what a level is, what it rolls up, what each level can answer:
forecast, flow metrics, roll-up), and organisations model their structure on those concepts (C8). Every
direction is judged against the setups in "Structures that must be expressible" below.

- **Relay level**: an intermediate level the Portfolio looks through. Must answer: where is it configured
  (Portfolio, connection, Team)? How is it fetched per connector? Does Team Feature WIP count it or the
  forecast level? How is an un-broken-down intermediate item sized? Is it visible anywhere? Does it stretch
  to two relayed levels (SAFe with Capability) or does it stop at one?
- **Extra levels (a fixed third, maybe fourth)**: Team → intermediate → Portfolio as a first-class
  hierarchy. Must answer: what is a Feature then? Which level gets forecasts, WIP and flow metrics? Does it
  also deliver Project/Program and shape (b)? What happens to an org that needs one more level than built?
- **Configurable N levels**: the org declares its levels and, per level, what it answers (forecast, flow
  metrics, roll-up, looked through). Must answer: how much setup does a two-level user see (ideally none)?
  How are levels named (Terminology per level)? Where does Team Feature WIP and the simulation's "Features
  at once" attach? How is it explained so nobody skips a level unnoticed?
- Combinations, and a **query-defined scope** (forecast any set of Work Items chosen by query, as some
  competitors do; Epic 5806 competitive research: Nave, Broken Build) as a structurally different
  alternative to hierarchy, especially for levels that are links or goals rather than parents (OKRs).
- In every direction: setup must make a skipped level visible (O2), and the two-level setup keeps working
  unchanged (C5).

### Structures that must be expressible (maintainer, 2026-10-30)

Written bottom-up. Lighthouse's own default terms (Work Item, Feature) are not the same as SAFe's words of
the same spelling: SAFe's "Feature" is roughly where a Lighthouse Feature sits only in a two-level setup,
and SAFe's "Epic" sits two or three levels above the Team's items.

| Setup | Levels (bottom-up) | Depth from Team item to top | Structural traits |
|---|---|---|---|
| Today's simple setup | Work Item ← Feature (← optional parent shown in the Parent column) | 1 (2 with display parent) | Strict parent links; must keep working unchanged |
| Customer A | Story ← Epic ← Change ← Project ← (optional) Program | 4 (3 without Program) | Strict parent links in Jira; optional top level; forecast wanted at Change, roll-up at Project/Program |
| Customer B | Story ← Epic ← Feature ← Business Epic | 3 | Strict Jira issue hierarchy; forecast level is the top |
| SAFe (Essential to Full) | Story ← Feature ← (optional, Large Solution) Capability ← Epic (Portfolio Epic) ← Strategic Theme | 2-3 to Epic; Theme above | **Optional middle level**, so depth differs between ARTs in one org; Strategic Themes are usually not parents of Epics but a tag or alignment link, and one Epic can serve several Themes |
| OKR-style | Work Items ← Initiatives / Key Results ← Objectives | 2 | Upper levels are often **goals or links, not work items**; contribution is often **many-to-many** (one Initiative moves several Key Results); Key Results carry a measure, not a done state |

### Questions DIVERGE must answer about structure

1. **Depth.** Is depth fixed per Portfolio, or can it vary per item (SAFe with and without Capability in
   one Portfolio; OA1)?
2. **Optional levels.** When a level is optional, does an item that skips it still roll up correctly, and
   is the skip shown?
3. **Non-parent links.** Can a level be reached through a link type or a field rather than a parent (OKR
   contribution, SAFe Themes, Jira "caused by" links already used as parents per `ARCHITECTURE.md:63`)?
4. **Many-to-many.** If one item rolls up into several above it, does its work count once in each? How do
   forecasts and flow metrics avoid double counting? Today a Work Item has exactly one parent (H1).
5. **What each level answers.** Which levels get a forecast, which get flow metrics from their own states,
   which only roll up, which are looked through? Can a level that is a goal (no done state) answer "when"?
6. **Naming.** One configurable term per level, or a fixed set of term keys?
7. **Where WIP lives.** Which level is a Team's Feature WIP (and the simulation's parallelism) counted at?
8. **Deliveries.** Is a Delivery a level, a cut across levels, or neither?

---

## Wave: DISCOVER / [REF] Decision gate G1-G4

| Gate | Criterion | Status | Detail |
|---|---|---|---|
| G1 Problem | 5+ interviews, >60% confirm, customer words, 3+ examples | **Partial. Count not met (2 of 5).** | Met: past-behaviour evidence from 2 real instances, 2 of 2 confirm, customer words recorded, existence proven in code (E6). Not met: 5 cases, 3+ named examples. Shape (b): maintainer-reported only. |
| G2 Opportunity | OST, top 2-3 >8, team aligned | **Provisional.** | OST drafted; for shape (a) O1 (17), O2 (14), O3 (11) score >8, and for shape (b) O7 (12), but on 2 customers' signal, not 5+ scored interviews. Maintainer alignment not yet confirmed. |
| G3 Solution | >80% task completion, 5+ users | **Not reached** (by design: no solution chosen; DIVERGE next). | |
| G4 Viability | 4 risks addressed, go/no-go | **Not reached.** | Early read: Value medium-high (onboarding loss of a prospect); Feasibility open (OA1, OA4); Usability open (O2); Viability: Premium boundary undecided. |

**Go / no-go: conditional GO to DIVERGE for shape (a).** The usual rule is not to act on 1-2 data points.
It is relaxed here, and only this far, because the failure is a structural certainty in the code, not an
inferred preference: what is uncertain is prevalence and the right fix, and DIVERGE commits nothing to
build. Conditions before DISCUSS: OA1 and OA3 answered (names or counts), and Customer B asked what they
need at the Business Epic level. Shape (b) is recorded as its own opportunity (O7); whether it rides along
is a DIVERGE output, not an input.

Peer review by `nw-product-discoverer-reviewer` (2026-10-30): APPROVED, conditional GO to DIVERGE stands. Its findings were applied: A5 now names the Delivery workaround, G2 lists O7, and the Evidence table marks E1 and E3 as second-hand. The OST's importance and satisfaction scores are the discoverer's judgement, not customer ratings.

---

## Wave: DISCOVER / [REF] Constraints established

| # | Constraint | Source |
|---|---|---|
| C1 | A Lighthouse concept, valid for Jira, Azure DevOps, Linear and CSV; must not preclude ServiceNow | Maintainer, 2026-10-30 |
| C2 | Nothing goes silently missing: a skipped or mis-set level must be visible at setup and on the Feature | Epic 5806 D13 (holds); E1, E3; the "unmapped states" lesson |
| C3 | Parent resolution stays the Team's/Portfolio's to say, decided in one pure selector; connectors only read | `ARCHITECTURE.md:63`; `ParentSourceSelector.cs` |
| C4 | Any new level carries a configurable term; never "Epic", "Change", "Initiative" hardcoded | Epic 5806 D9; CLAUDE.md terminology rule |
| C5 | Existing one-level Portfolios must behave exactly as today | Implied by C1 and the installed base |
| C6 | Migrations are expand-only | Project rule |
| C7 | No customer names, people or hosts in committed files | Coordinator, 2026-10-30 |
| C8 | Tracker-agnostic Lighthouse concept; org structure is configured, not hard-coded. Customer A, Customer B, SAFe (with optional Capability), OKR-style and today's two-level setup must all be expressible | Maintainer, 2026-10-30 |

## Wave: DISCOVER / [REF] Pre-requisites

1. Maintainer names (or counts) the "seen it before" cases for shape (a) and the shape (b) customers (OA3, VA6).
2. Customer A or B answers OA1 (uniform depth) with one JQL count.
3. Customer B is asked what they need at Business Epic level (forecast, flow metrics, a roll-up, the
   intermediate level shown or not).
4. Optional, cheap: count Portfolios where most open Features are default-sized, on instances we can see.
   It is a direct prevalence signal for shape (a).

---

## Wave: DISCOVER / [REF] Reuse from Epic 5806/6119

Workspace: `docs/feature/epic-5806-initiatives/`. Its DISCOVER ended NO-GO because the problem was "the
maintainer's words, not yet a customer's".

**What changed.** Shape (a) is new customer evidence and a different problem: not a missing page but a
configuration that forecasts nothing real. Shape (b) is the old 5806 problem, now maintainer-reported from
customers (E5): this **partly reverses the 5806 NO-GO**. It moves (b) from "unproven idea" to "reported,
unnamed demand". It does not yet meet G1, and the 5806 validation sprint (data count, Mom Test calls) still
applies to (b).

### Assumptions (5806 `discover/assumptions.md`)

| 5806 | Status now | Why |
|---|---|---|
| A3 customers' data has a populated level above Feature | **Validated for shape (a)** (E1, E3: their data has 3-5 levels); open for (b) | |
| A5 existing tools are not enough | **Validated for (a)**: nothing reaches two levels natively (VA4); a partial workaround exists, one rule-based Delivery per top-level item, Premium only, set up by hand and without top-level metrics (see How Deliveries relate) | |
| A7 one consistent parent type | **Still invalid** (maintainer, 2026-09-29) and reinforced: Change, Business Epic | |
| A6 Initiatives span Portfolios | Open; not raised by either customer | |
| A1 drives Premium, A2 executives open it, A4 forecast vs status | Open; parked with (b) | |
| A8 a roll-up forecast is meaningful | **Holds** as a candidate for Project/Program and for (b) | |

### DISCUSS decisions (5806 `feature-delta.md`)

| Decision | Status now | Why |
|---|---|---|
| D4 an Initiative is whatever a Feature names as its parent | **Holds for (b); open for (a)** | In (a) the forecast level is the Feature itself; the issue is the level **below** it. |
| D14 one level up only | **Invalidated for (a)** (IA2); open for (b) (Program roll-up) | |
| D5 dates from the existing all-Features-done composition | **Holds** for any roll-up above the forecast level; not needed for (a) itself, where the top-level item is a Feature with its own forecast once Work Items reach it | |
| D8 Deliveries stay separate | **Open** | Rule-based Deliveries on parent reference are today's only partial workaround for (a). |
| D9 configurable term | **Holds** (C4) | |
| D12 one unforecastable Feature makes the row "cannot forecast"; D13 nothing silently missing | **Hold, strengthened** (C2) | Shape (a) is a silent-ish failure. |
| D17 done when every Feature under it is done | **Holds** for (b) and roll-ups | |
| D1-D3, D6-D7, D10-D11, D15-D16, D18-D27 (page, RBAC scope, status bands, demo, Premium split) | **Parked with (b)** | Not evidence-backed by the new cases; revisit only if DIVERGE folds (b) in. |

### Current-state inventory (5806 S1-S18)

S1, S2, S4, S7, S8, S18 still hold and are cited above. S16 (stale docs) was resolved then.

### Competitive findings (5806 `diverge/competitive-research.md`)

Still hold: nobody rolls per-Team Monte Carlo into a strategic-level likelihood; the strategy layer is
usually a paid tier. New relevance for (a): tools that forecast **any query scope** (Nave, Broken Build) are
immune to hierarchy depth, which makes a query-defined scope a real alternative to model in DIVERGE.

## Wave: DISCOVER / [REF] Maintainer answers (2026-10-30, after review)

| Open question | Answer | Effect |
|---|---|---|
| Who are the other cases (OA3)? | Four customers in total have shown it, some with shape (a), some with shape (b). In the shape (a) cases they jump over the intermediate level because they do not care about those items. Customers are not named here. | VA2 and E4/E5 strengthen: 4 customers, maintainer-reported. G1 is still below 5 cases but closer; OA3 drops to a lower risk. Shapes (a) and (b) both stay real. |
| Uniform depth at Customer A (OA1)? | Yes. No Story links straight to a Change; it always goes through an Epic. | OA1 is validated for Customer A: the depth is uniform there. It is still open for other customers. |
| What does Customer B need at the top level? | Unknown until a session with them on 2026-11-02. | Still a condition before DISCUSS. Not needed to start DIVERGE. |

**The maintainer's direction for DIVERGE (a preference, not evidence or a decision).** If Lighthouse could
model each org's own hierarchy as named levels (for example Team → Agile Release Train → Program), every
level could be required to be visible, and no level would need to be jumped over. That would make "jump over
a level" a symptom to remove rather than a feature to build. DIVERGE should weigh "model every level, all
visible" against "relay level" and "query scope" on its merits, including whether a customer who does not
care about the intermediate items accepts seeing them as a level.

**Maintainer decisions (2026-10-30, second round).** These close the DISCOVER conditions so DIVERGE can start:

- **Prevalence is treated as common.** The maintainer judges deeper hierarchies to be a common use case that Lighthouse must account for. G1's case count is accepted on that judgement plus the four reported customers (OA3 closed by decision).
- **Jumping over a level is not Lighthouse's problem.** Items that skip a level can already be handled with the Parent Override Field. The new concept may require every item to go through all configured levels, and another option can be worked out later from feedback. O2/O6 shrink accordingly; the relay level is no longer required.
- **Customer B is assumed to have the same problem as Customer A** until the 2026-11-02 session says otherwise.

## Input for DISCUSS: UI surfaces and onboarding (maintainer, 2026-11-01)

After the event storming (ADR-270), the maintainer accepted
[ADR-271](../../product/architecture/adr-271-ui-surfaces-follow-units-and-levels-and-settings-split-by-audience.md)
as the direction for the UI. This Epic is where most of it lands, so DISCUSS answers explicitly:

- **One unit page, per-level navigation (ADR-271 U4).** Do Teams and Portfolios become units on levels with one page
  whose tabs depend on leaf versus non-leaf? How does the Overview list units by level?
- **Landing on your level.** Which level a person sees first is navigation, not Access. Does this Epic set it, and
  how?
- **Levels and their names live in Organisation.** Where are levels configured, and how do they relate to
  Terminology, which today has one key pair per concept and no notion of a level?
- **Org default, local override (U2).** If the Epic adds per-level settings, they use the one shared control.
- **Onboarding (ADR-271 Decision 7, U5).** The maintainer's idea: once Organisation exists, first-run set-up asks
  how the organisation works (tracker, levels and their names, what rolls up into what, which states mean started
  and done) and fills the defaults from the answers. DISCUSS decides whether this Epic delivers a first slice of it
  or only leaves room for it. Each Epic must ship on its own, so onboarding is its own Epic unless it is small.
- **Splitting System Settings (ADR-271 U1) belongs to this Epic** (maintainer, 2026-11-01). No ADO Story yet: DISCUSS
  scopes it (its own slice or folded into the levels work) and only then is it mirrored to the board.

---

## Wave: DISCUSS / [REF] Locked decisions

Only what the maintainer has already decided. Everything else in this wave is a proposal until the walk-through.

**D1. The event storming supersedes the DIVERGE recommendation on four points** (maintainer, 2026-11-01 and
2026-11-02).

1. Levels are defined **once per installation**, not per Portfolio. An organisation that says "Initiative" says it
   everywhere, and one structure per installation is fair to support.
2. Each level **pairs an owning unit** (a group of people or a board) **with its unit of work** (the items on that
   level). The recommendation stacked levels of work only and kept Teams flat.
3. **Deliveries hang off a non-leaf level and hold that level's items** (the target; see D2 for now).
4. What is special is **leaf versus non-leaf**, not Team versus Portfolio. The leaf unit is the one that generates
   throughput; everything above it is a unit on a level.

What DIVERGE got right and stays: forecasts composed upward with the joint "all children done" maths; flow
metrics per level from the items' own states; one parent per item per level, read by the existing parent-source
rule (native parent, a field, or a Jira link type); Feature WIP and the simulation unchanged at the leaf's direct
parent; setup counts ("N {features} have no {level}"); the connector approach; and the Premium boundary as an open
question. The DISCOVER decision that every item passes through every configured level, with a skipped level being
the Parent Override Field's business, also stands.

Sources: `../story-6357-s01-bounded-contexts-context-map/event-storming/board.md` (sections A, D, F, G, H, Hold 1);
`../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md` (Decision 4, Consequences);
maintainer, 2026-11-02.

**D2. Deliveries stay as they are, locked to the second level, for now** (maintainer, 2026-11-02). The level above
the leaf (today's Portfolio level, items named Feature) keeps today's Deliveries unchanged. Upper levels get no
Deliveries in this Epic family's first deliveries. Deliveries on any non-leaf level, with generic units and levels
underneath, may come through the behaviour-free refactoring in Epic 6264; switching them on for an upper level is
a later step.

**D3. Migration of existing installs: direction** (maintainer, 2026-11-02). Today's structure becomes the leaf
level (Team, Work Item) plus one level above it (today's Portfolio), whose item is named "Feature" and whose item
types are extracted from all existing Portfolios' Feature types. Users can then add more levels. Details
(per-Portfolio lists, two trackers, shape (a) installs) are open; see the questions below.

**D4. Splitting System Settings into Organisation, Access and System belongs to this Epic family** (maintainer,
2026-11-01, "Input for DISCUSS" above). It is **its own small Epic, delivered first** (maintainer, 2026-11-02): it
has value without levels and gives levels their home in Organisation.

**D5. Units all the way up** (maintainer, 2026-11-02). A level above the second has its own units (for example a
programme board), each with its own query and page, reusing what a Portfolio has today; their items' children are
the second level's items. Upper levels are not shown as a stack inside each Portfolio.

**D6. Level names live on the levels, next to the rest of the terminology** (maintainer, 2026-11-02). Team, Work
Item, Portfolio and Feature move out of Terminology onto the two seeded levels: each level names its unit and its
item. Organisation gets one place to manage the levels, and the remaining terms (cycle time, throughput, WIP,
blocked, …) sit close to it, in the same tab, so naming is not split over two screens.

**D7. Structure first, and deep: one Unit type before any level is added** (maintainer, 2026-11-02). Today's
Teams and Portfolios move onto an installation-wide level structure as one generic unit type on a level, without
users noticing, and only then are levels added above. Building new levels beside a Team and a Portfolio that stay
special concepts would leave two ways of saying "a level". Chosen over a thin step (levels plus Team and Portfolio
kept as separate classes). Consequences to carry into the story map and DESIGN: the work is several slices, not
one; it overlaps Epic 6264's packaging Stories for Portfolio/Delivery and Organisation set-up and its aggregates
Story, whose scope must be re-cut; expand-only migrations mean the unit structure is added beside today's tables;
the `/teams` and `/portfolios` routes stay for the CLI and MCP clients.

**D8. The structure is its own Epic in this family, before the levels above** (maintainer, 2026-11-02), not an
Epic 6264 Story. The maintainer deliberately accepts an Epic whose value is mostly preparation ("they can live with
1–2 weeks more of waiting"), as an exception to "each Epic adds visible value"; it is also not invisible:
- the number of levels is part of set-up and Organisation settings, **fixed at two for now**;
- **item types come from the structure**: each level lists the tracker types of its items, and a unit no longer
  overrides them. A unit narrows what it reads with its filters. The maintainer sees this as an improvement.
Order: the structure Epic first, then the levels above, in that order.

**D9. Public API: today's routes stay** (maintainer, 2026-11-02). `/teams` and `/portfolios` remain for the CLI and
MCP clients; if their shape has to change they are deprecated, not removed. How the API looks in a units-on-levels
world is an open question for DESIGN.

**D10. The migration carries today's set-ups over as they are** (maintainer, 2026-11-02). An install that
configured Change as the Portfolio's Feature type keeps Change on the second level; fixing that waits for the
levels above. No automatic restructuring on upgrade.

**D11. Two Epics in order: "One structure", then "Levels above"; Deliveries everywhere comes later still**
(maintainer, 2026-11-02). "One structure" ships first (one Unit type on two fixed levels, levels in set-up and
Organisation, names on the levels, item types owned by the level, migration as is). "Levels above" starts once it
has shipped (third and further levels, their units, dates built up from the level below, flow metrics per level,
set-up counts, connectors). Deliveries on any upper level is a later Epic: Deliveries stay on the second level
until the Promises context is ready in code.

**D12. A unit narrows its level's item types to a subset** (maintainer, 2026-11-02). It may untick types from its
level's list, never add one. The migration ticks exactly what each Team and Portfolio reads today, so nobody's
throughput or forecasts change on upgrade.

**D13. State mapping belongs to the unit entirely** (maintainer, 2026-11-02). Across one organisation the same
state may mean "to do" for one Team and "doing" for another, so there is no organisation default to inherit from.

**D14. The guided set-up covers structure only** (maintainer, 2026-11-02): levels, their names and item types,
from a template such as today's two levels, SAFe or Flight Levels, editable during set-up and later. States and
blocked rules stay out.

**D15. Every level above the second is Premium** (maintainer, 2026-11-02). The free tier stays at three units on
the leaf level and one unit on the second level, and has no further levels.

**D16. The leaf level keeps "Team" and "Work Item" as its seeded names** (maintainer, 2026-11-02); users rename
them on the level. How the leaf's item is named inside the code is a separate code-naming question.

**D17. The guided set-up with templates ships after "Levels above"** (maintainer, 2026-11-02), once, with every
template including those with three or more levels.

**D18. One parent per item per level; several parents are not planned** (maintainer, 2026-11-02: "several parents
are never needed"). Revisit only once several levels are in use.

**D19. A missing parent is fine, as today** (maintainer, 2026-11-02). A Work Item without a Feature simply does not
count towards any Portfolio; the same holds on every level. How items without a parent are shown on an upper
level is left to the "Levels above" DISCUSS.

**D20. A context that a feature Epic changes heavily is refactored inside that Epic** (maintainer, 2026-11-02). If
Deliveries on every level needs Deliveries to become the Promises module (module boundary, domain events), that
refactoring moves out of Epic 6264 into "Deliveries on every level". The same applies to the others where they
touch a context hard; Forecasting may matter for building dates up from the level below in "Levels above". Epic
6264's catalogue is re-cut accordingly through its own plan.

**D21. ADO tracking** (maintainer, 2026-11-02):

| Epic | ADO | Board column |
|---|---|---|
| Settings split by who decides: Organisation, Access and System | #6473 | Planned |
| One structure: Teams and Portfolios as units on levels | #6474 | Planned |
| Levels above the Feature: dates and flow at every level of the hierarchy | #6455 (retitled) | Options |
| Guided set-up with structure templates | #6475 | Options |
| One page per unit, open on your level | #6476 | Options |
| Deliveries on every level | #6477 | Options |
| Organisation defaults with a local override | #6349 (re-scoped by a discussion note, title unchanged) | Options |

Links: all Related to #6455; #6474 precedes #6455, #6476 and #6477; #6455 precedes #6475; #6473 precedes #6349.

**Input for the guided set-up (#6475), maintainer, 2026-11-02.** Set-up connects to the tracker first, then
proposes the levels and their item types from what the tracker already knows: Jira's issue type hierarchy levels,
Azure DevOps' backlog levels and work item types, Linear's issues, projects and initiatives (Lighthouse reads two
of those levels today; initiatives would be the next). A CSV file carries no hierarchy, so its levels are entered
by hand. Whether each connector can deliver this, and how, is that Epic's DISCUSS. "Teams only" (one level) is
one of the set-up's choices (#6474 M13).

**Customer B session (2026-11-02), maintainer's notes.** Customer B's tracker is linked in many directions with no
clear hierarchy; neither the maintainer nor the coach in the session could work out how it is meant to fit
together. Their main problem is a lack of structure, not the missing levels; the maintainer still expects the
multi-level model to help them, because it asks an organisation to state its structure. The session did **not**
answer the three questions that waited on it (which level a person lands on, Epics as the items on level 2,
whether the SAFe Feature level is shown); they stay open for "Levels above" (#6455) and "One page per unit"
(#6476), to be decided from other evidence or by the maintainer. Two consequences: the DIVERGE assumption about
Customer B is neither confirmed nor refuted, and the set-up counts ("N items have no {level}") and the guided
set-up (#6475) matter more than assumed, since they are what shows a messy structure for what it is.

**Superseded by D22–D25 below.** **Future direction: connection-level mapping moves to the level** (maintainer,
2026-11-02, not scheduled). The
settings that sit on the connection today and describe how the hierarchy is wired (Parent Override Field,
dependency fields and similar) belong with the level's configuration, next to its item types. Open: installs with
two connections, where a level would need one value per tracker or a per-connection override on the level (the
same question as ADR-270's "organisation default or per-tracker default"). To be placed when a level Epic or #6349
next touches these settings.

### The vision (maintainer, 2026-11-06)

Shared across #6473, #6474, #6455, #6475 and #6349 and walked through in a session. It is the direction, not what ships
first. Sketches: https://claude.ai/artifact/L4BsQRqLCJVee6UY1pnyja

**D22. Three layers** (2026-11-06). The **structure** is set up once per instance, knows no tracker and can exist before
any connection: levels, their names, and which one is where the work is done. A **connection** maps its types onto the
levels. A **unit** is an instance of a level: it picks a connection (picked for you when there is only one) and narrows.
Replaces D8's "item types come from the structure". ADR-274 is revised accordingly.

**D23. The mapping is per connection, not per kind of tracker.** Shown under Organization, one tab per connection, named
after the connection (`letpeoplework.atlassian.net`), so two Jira connections are two tabs. Each kind of tracker brings
its own panel, which only loads if such a connection exists. Credentials and health stay in System.

**D24. A type sits on one level per connection.** Jira's Epic and Azure DevOps' Epic may sit on different levels. A type
on no level is ignored. Narrows MD6 of #6474.

**D25. Parent sources, dependencies and shared defaults are keyed by connection and level.** Adding a level adds a row per
connection, with its own parent setting. Replaces the 2026-11-02 "future direction" note above and answers ADR-270's
"org default or per-tracker default". The ordered parent source list from Story #6501 is the value; it needs that key.

**D26. A chain may cross connections.** A Team on one connection may point at a Portfolio on another through a custom
field. This was the original use case (two Azure DevOps instances after a merger). It keeps working for forecasting and
is not validated. Known risk, not new: the roll-up matches on the key alone (H2), so equal keys on two instances collide.

**D27. One type with two meanings inside one connection** is not supported. Later, only on demand. A second connection
to the same tracker covers it.

**D28. Linear's mapping is read-only; CSV's is entered by hand.**

**D29. A unit may change its connection.** Keep what the new connection maps to that level, name what is dropped, and
require a pick if nothing is left. "Copy mapping from…" makes a swap to a write-back connection lose nothing.

**D30. A unit's To Do, Doing and Done states stay its own for now; a default per connection and level comes later.** This
softens D13 without contradicting its reason, since a unit can still differ.

**D31. #6349 returns to its original idea: set shared settings once on the connection,** keyed by connection and level,
including Closed Items Cutoff. Organization-wide defaults only if someone asks. When it comes, think holistically:
creating a Team and changing a default must be as easy as possible. Work before it keeps that in mind.

**D32. Chain check (Epic #6560, Options).** On a connection, give a key such as `PROJ-1337` and see the full chain up, or the
children down, with the settings on screen, during set-up and after. It uses the same parent logic as the refresh. It
reports no parent, a parent whose type is not mapped, a skipped level, and a parent on another connection (not
followed). Useful with today's two levels, so it does not wait for #6455.

**D33. Words.** Users see "Where the work is done" and "Groups the level below". Docs say **work level** and **levels
above**. "Leaf" stays a code word.

**D34. Set-up has two ways in, one screen.** Structure first, then mapping; or connect first, and the tracker's hierarchy
proposes levels and a mapping. Templates (Teams only, today's two levels, SAFe, Flight Levels) prefill the structure.
Refines D14 and the #6475 input above: set-up's types come from the mapping step.

### Proposed, not locked

- Guided first-run set-up that picks an initial structure, changeable later (maintainer idea, 2026-11-02).
- Templates (for example SAFe) prefilling levels, names and item types, editable during the guided set-up
  (maintainer idea, 2026-11-02).
- **Open, waiting for the Customer B session (2026-11-02, later today)**: the level a person lands on; whether
  Customer B accepts Epics as the items directly above the leaf; whether the SAFe Feature level is shown.
- **Open, no session needed**: the leaf item's name and the leaf unit's name (ADR-270 "Left open"); sequencing
  against Epic 6264's "Package Organisation set-up" (#6468), which waits for the Metrics pilot and the
  test-infrastructure track.

---

## Wave: DISCUSS / [REF] Changed Assumptions

DISCOVER and DIVERGE files are not edited; the changes are recorded here.

| Original (verbatim) | Source | New assumption | Why |
|---|---|---|---|
| "**Proceed with Option 1, a level stack above the Feature, on the Portfolio.** The Feature stays the Teams' direct parent. A Portfolio may declare an ordered list of named levels above it (for Customer A: Change, Project, Program)." | `recommendation.md`, Decision for DISCUSS | Levels are declared once per installation, in Organisation. Each level has an owning unit and an item. | D1. The storming placed levels with Terminology as "how the organisation is set up", one per installation. |
| "\| What a level is \| A named, ordered entry on the Portfolio above its Feature level: tracker types plus an attach field \|" | `recommendation.md`, What DISCUSS inherits | A level is an installation-wide tier pairing an owning unit with its item; tracker types and the attach rule stay per level. | D1 points 1 and 2. |
| "\| Deliveries \| Unchanged: a cut across the Feature level. A Delivery rule may match an upper-level item, replacing today's "one rule per Change" workaround \|" | `recommendation.md` | Target: a Delivery holds items of the level it hangs off. For now: unchanged and locked to the second level. Whether a Delivery rule may match an upper-level item is open. | D1 point 3, D2. |
| "\| Terminology \| One name pair per declared level, set where the level is declared. The Feature level keeps the instance-wide Feature term. [...] \|" | `recommendation.md` | Proposed: every level, including the leaf and the second level, carries its unit name and item name; today's Team, Work Item, Portfolio and Feature terms become the names of the two seeded levels. | Maintainer idea B3; Terminology has one key pair per fixed concept and no notion of a level (ADR-270 glossary). |
| "\| Migration \| None for two-level instances (no levels declared = today). [...] \|" | `recommendation.md` | Every install is migrated to two seeded levels (D3), with the second level's item types taken from all Portfolios' Feature types. | D3. |
| "\| Setup \| Portfolio settings: "Levels above {feature}" → add a level, pick types, name it. [...] \|" | `recommendation.md` | Levels are set up in Organisation (ADR-271 Decision 2); first run may be a guided set-up with templates (proposed). The per-level counts stay. | D1 point 1; ADR-271; maintainer ideas B1, B2. |
| "A two-level user sees nothing new. One action, "Add a level above {feature}", reveals the rest; a level page appears only once a level exists" | `diverge/taste-evaluation.md`, T3 | A two-level user's forecasts, metrics and Deliveries behave as today, but they will see their two levels in Organisation and, if B3 is taken, find the Team and Portfolio names there instead of under Terminology. | Behaviour stays (DISCOVER constraint C5); the surface changes. |
| "The answer is not to build Option 3 now (2.85, highest cost) but to let Option 1 grow into it." | `recommendation.md`, Dissenting case | The installation-wide model is chosen now. Its cost is managed by splitting: behaviour-free generalisation in Epic 6264, behaviour in this Epic family. | D1; maintainer input on scope (2026-11-02). |
| "If Customer B says the same on Monday, the recommendation should become **Option 1 plus a per-Portfolio "home level"**." | `recommendation.md`, Dissenting case | Which level someone sees first is navigation per person, not a setting of a Portfolio (ADR-271 Decision 5). Still open until the Customer B session. | ADR-270 Decision 4: hiding what someone may not see is Access, choosing what they see first is navigation. |
| "**Key assumption**: an organisation has one shared structure across its Teams and Portfolios (per connection)." | `diverge/options-raw.md`, Option 3 | One structure per installation, not per connection. Whether type and state defaults need a per-tracker layer stays open (ADR-270 "Left open"). | Board, interlude: "Usually there is one tracker". |
| "**2. Org default, local override** \| Work Item types · Feature types · state mapping · [...]" | `../story-6357-s01-bounded-contexts-context-map/event-storming/board.md`, interlude; ADR-270 settings buckets | Item types are owned by the level, and a unit only narrows them to a subset. State mapping is local to the unit, with no organisation default. | D12, D13. ADR-270's bucket table and ARCHITECTURE.md §4 must be updated in the same feature. |
| "Premium boundary \| Candidate: the Feature level and today's Parent column stay free; declared levels above are Premium [...] Open question below for prospects" | `recommendation.md` | Every level above the second is Premium; the free tier has no upper levels. | D15. |
| "Conditions before DISCUSS: OA1 and OA3 answered (names or counts), and Customer B asked what they need at the Business Epic level." | this file, DISCOVER decision gate | DISCUSS starts before the Customer B session, for scope and split only. Nothing that depends on Customer B is locked. | Maintainer, 2026-11-02. |

---

## Wave: DISCUSS / [REF] Scope Assessment

### Oversize heuristics

| Signal | Evidence | Verdict |
|---|---|---|
| More than 10 user stories | Settings split (2–3), levels with naming and migration (3–4), upper-level forecasts and flow (3–4), per-connector fetch (Jira, Azure DevOps, Linear, CSV: 4), guided set-up and templates (3–4), unit page and landing level (3+) | **Oversized**: about 20–25 |
| More than 3 bounded contexts | Organisation set-up, Tracker integration, Work Items, Forecasting, Flow metrics, Promises, Access, Licensing (ADR-270) | **Oversized**: 8 |
| Walking skeleton needs more than 5 integration points | Level set-up → connector fetch per level → parent attach per level → composed forecast → upper-level flow metrics → a page to read it on | **Oversized**: 6 |
| Effort over 2 weeks | Upper levels alone touch fetch, storage, forecasting and UI across four connectors | **Oversized** |
| Independent outcomes that could ship apart | Settings split; levels above the Feature; guided set-up; one unit page with a landing level; Deliveries on upper levels | **Oversized**: 5 |

**Verdict: oversized on all five signals.** The DIVERGE scope (levels on one Portfolio) was already large; the
event storming widened it to the organisation's whole structure. Split before journeys or stories are written.

### Where behaviour-free ends and behaviour begins

The test: does a user, an API client (the CLI or MCP server) or an existing test see any difference?

- **Behaviour-free, so Epic 6264 can do it.** Re-shaping storage and code so today's Team and Portfolio are units
  on two levels. A Delivery pointing at "a unit on a non-leaf level" while only one such level exists. Item rows
  carrying a level number instead of the flag that marks a Feature's parent row. The data step of the migration
  (D3): the second level's item types become an organisation default equal to the union of the Portfolios'
  Feature types, while each Portfolio keeps its own list as its override. That is the existing
  "follows the default or set here" pattern, so no Portfolio changes what it fetches. Routes, payloads and pixels
  stay the same.
- **Behaviour, so this Epic family does it.** Anything a person can see or do: a levels list in Organisation, even
  with only the two seeded levels; names moving from Terminology to the levels; a third level, its items, its
  composed dates and flow metrics; templates and the guided set-up; Deliveries on a third level; a new unit page.
  A level exposed on the public API is behaviour too, because the Lighthouse clients read it.
- **The grey zone is the migration.** Its data step is behaviour-free; showing the result is not. So the migration
  lands with whichever piece of work first builds the level, and the other side builds on it.

### Proposed Epic split (DRAFT — for maintainer walk-through)

Summary, in the proposed order:

| # | Candidate | Value alone, for whom | Contexts touched | ADR-271 step | Depends on (earlier only) | Home |
|---|---|---|---|---|---|---|
| A | Organisation, Access and System settings | config-admin finds "how we work" apart from running the instance; whoever administers the server stops wading through Terminology | none in the domain; UI surfaces of Organisation set-up, Access, Tracker integration (connections), Licensing, platform lane | U1 | nothing | 6455 family (Epic or first Story, Q3) |
| B | Levels above the Feature | Customers A and B (and SAFe and Flight Levels setups) get a real "likely by" date and flow metrics at Change or Business Epic level | Organisation set-up, Tracker integration, Work Items, Forecasting, Flow metrics, Access, Licensing | towards U4 (keeps Team and Portfolio pages parallel); levels live in Organisation | A soft (gives levels their home) | 6455 feature work, the core |
| C | Guided set-up with structure templates | forecasting-prospect and config-admin get the structure right on the first run, the failure Customer A hit in evaluation | Organisation set-up (UI only) | U5, structure part | B | 6455 family, its own Epic |
| D | One page per unit, landing on your level | everyone reads any level the same way; leaders open on their level | Organisation set-up, Access (kept apart from landing) | U4, Decision 5 | B; the Customer B session | 6455 family, its own Epic, later |
| E | Deliveries on any level above the Team | a programme board promises a set of Changes the way Deliveries promise Features today | Promises, Forecasting | Decision 1 (Deliveries tab where Deliveries hang off) | B; Epic 6264 "units on levels" | 6455 family, small, after 6264 |
| F | Organisation defaults with a local override (state mapping, SLE, staleness …) | config-admin sets "how we work" once instead of per Team | Organisation set-up, Tracker integration | U2, U3 | A | needs its own Epic, outside 6455 |
| R1 | Package Organisation set-up (exists, #6468) | developers | Organisation set-up | none | Metrics pilot (S02) | Epic 6264 |
| R2 | Units on levels, two levels only (proposed new Story) | developers; makes D and E small | Organisation set-up, Promises | none | R1, Package Promises (S03d); B if B builds the level first | Epic 6264 |
| R3 | One item model with a level (proposed new Story) | developers; ADR-270's "one item model" | Work Items | none | Package Work Items (S03b), forecasts off the Feature (S13) | Epic 6264 |

**A. Organisation, Access and System settings.**
IN: split today's System Settings tabs by who decides (ADR-271 Decision 2), moving each existing setting
unchanged, with the sketch and the maintainer's yes before building. OUT: any new setting, levels, the shared
default-or-override control, mapping. Size: 1–2 Stories, a few days. Can start now; nothing in it depends on
Customer B.

**B. Levels above the Feature.**
IN: levels defined once in Organisation, each with an owning-unit name, an item name, its tracker types and how
its items attach; the migration to two seeded levels (D3, the data step if Epic 6264 has not done it); adding a
third and further level; units on those levels and their items fetched per connector (Jira and Azure DevOps first,
Linear and CSV as later slices); composed "likely by" dates, with a visible count of default-sized parts; flow
metrics per level; the per-level setup counts. Context map: Organisation set-up publishes "level introduced"; the
level's items reach Work Items through the existing mapping path; Forecasting composes upward after its forecast
round. ADR-270's "levels above the Feature" stop being target-only, so the map and glossary are updated in the same
Epic. OUT: Deliveries on upper levels (D2), the unit page and landing level, templates, onboarding, many-to-many
and link-typed goal levels (OKR, SAFe Themes), reworking Feature WIP. Walking skeleton: a Customer-A-shaped Jira
structure (Story, Epic, Change) where one added level shows a Change with a composed date. That skeleton does not
depend on Customer B. If B's story map exceeds about ten Stories, Linear and CSV split out as their own Epic.

**C. Guided set-up with structure templates.**
IN: a first-run step that picks an initial structure (levels, names, item types) from a template such as today's
two levels, SAFe or Flight Levels, editable during the set-up and later in Organisation; it reuses the existing
onboarding stepper and wizard shell. OUT: state mapping and blocked rules in the interview (they wait for F), OKR
templates. Releasable once B has shipped.

**D. One page per unit, landing on your level.**
IN: Team and Portfolio pages become one unit page whose tabs depend on leaf or non-leaf; the Overview lists units
by level and opens on the person's level. OUT: anything Access-related beyond what exists. Its main question waits
for Customer B, so it is not scoped further now.

**E. Deliveries on any level above the Team.**
IN: a Delivery on a third or higher level, holding that level's items. OUT: the promise lifecycle (kept, missed,
renegotiated). Small once Epic 6264 has made a Delivery point at a unit on a non-leaf level.

**F. Organisation defaults with a local override.**
Listed because C's full interview and ADR-271's onboarding need it, but it is its own Epic: it changes how every
Team's settings behave and has value without levels.

**Ordering.** A, then B, then C. D after B and once Customer B's answers are in. E after B and Epic 6264's R2.
F whenever it is wanted, before C grows an interview about states. Every dependency points from a later candidate
to an earlier, finished one; none waits on a slice of a sibling.

**Recommended first Epic: A (settings split) to deliver, with B's DISCUSS continuing straight after the Customer
B session.** A is small, unblocked today, and gives levels their home, so B does not put them into System
Settings only to move them again. B is the Epic with the customer value and should follow without a gap.

### Questions for the maintainer

1. **Order against Epic 6264.** (a) Epic 6264 first: R1 and R2 make Team and Portfolio units on levels, then B
   builds on them; clean, but behind the test-infrastructure track and the Metrics pilot, so weeks before any
   customer value. (b) B first: B builds the level and the migration, keeping Team and Portfolio as they are and
   placing new code in the target layout; R1 and R2 later fold Team and Portfolio into units. (c) Pull R2 ahead of
   the Metrics pilot. **Recommendation: (b).** Customer value does not wait, and D2 already accepts Deliveries
   locked to the second level.
2. **Where do upper-level items live?** (a) Units all the way up: a unit on level 3 (for example a programme board)
   has its own query and page, reusing the Portfolio machinery, and its items' children are the second level's
   Features. (b) No unit above level 2 yet: upper levels are shown on each Portfolio. **Recommendation: (a)**,
   because pairing is decided (D1); (b) would rebuild the per-Portfolio stack the storming rejected. DESIGN must
   answer how a level-3 date stays as fresh as the level-2 dates it is built from.
3. **How is the settings split tracked?** (a) Its own small Epic, delivered first. (b) The first Story of B.
   (c) Folded into B's levels Story. **Recommendation: (a)**, because it has value without levels.
4. **Shape (a) installs in the migration.** Customer A configured Change as the Portfolio's Feature type, so D3
   would carry Change into the second level, where nothing has children. (a) Migrate as is; the setup counts show
   it and Organisation offers "move Change one level up". (b) Detect and move automatically. (c) Document only.
   **Recommendation: (a)**: no install changes behaviour without consent.
5. **Names per level (idea B3).** (a) In B, Team, Work Item, Portfolio and Feature move from Terminology into the
   two seeded levels, and new levels carry their own names. (b) Terminology keeps those four; only new levels get
   names on the level. **Recommendation: (a)**: one place per level name, and "Portfolio" stops being a standalone
   word.
6. **Leaf names.** (a) Keep "Team" and "Work Item" as the seeded user-facing names of the leaf level; renaming
   the leaf's item in code is Epic 6264's concern. (b) Pick new names now. **Recommendation: (a)**: it does not
   block B, and the user already renames both.
7. **Guided set-up scope.** (a) C covers structure only (levels, names, types, templates). (b) C also asks about
   states and blocked rules, which needs F first. **Recommendation: (a).**
8. **Premium line.** (a) All levels above the second are Premium. (b) The first level above the second is free,
   more are Premium. (c) All levels free; upper-level units count against the free tier's unit limit.
   **Recommendation: (b)**: an evaluating prospect like Customer A sees a Change date without a licence.
9. **After the Customer B session** (not to be answered now): the level a person lands on; Epics as the items
   directly above the leaf; whether the SAFe Feature level is shown. These decide D and part of B's UI, not B's
   walking skeleton.

Nothing is mirrored to the ADO board until the maintainer has walked through each candidate.

## Input for #6476 "One page per unit": the top bar (maintainer, 2026-11-14)

At the end of Epic 6473 the maintainer said the top bar (Overview, Features, Reports, Settings, and the icon row on
the right) is "not specifically well done" and should be looked at too, not only what sits under the Overview.
ADR-271 so far only plans the unit pages, the per-level Overview and a possible Deliveries entry (Decision 6); it says
nothing about the top bar as a whole. When #6476 reaches DISCUSS, take the top bar into its scope or split it into its
own item, and ask the maintainer what in it does not work for them.
