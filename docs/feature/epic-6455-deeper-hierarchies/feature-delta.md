<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-6455-deeper-hierarchies

**Epic**: ADO #6455 "Deeper hierarchies: portfolio items that are not direct parents of the team's items".
Supersedes #5806 and #6119 (both set to Removed on 2026-10-30).
**Waves present**: DISCOVER (2026-10-30).
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
