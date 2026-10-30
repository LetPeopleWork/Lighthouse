# Options (raw) — Epic 6455 "Deeper hierarchies"

Wave: DIVERGE, phase 3 · 2026-10-30. Generation only: nothing in this file is scored or ranked. Scoring is in
`taste-evaluation.md`.

Settled inputs every option is written within (maintainer, 2026-10-30): deeper hierarchies are common; the
answer is a tracker-agnostic Lighthouse concept, configured not hard-coded; Customer A, Customer B, SAFe, OKR-style
and today's two-level setup must be expressible, the last unchanged; **an item that jumps a level is the Parent
Override Field's business, and the new concept may require every item to pass through every configured
level**.

## HMW

> **How might we let each organisation tell Lighthouse which levels it plans and promises at, so that every one
> of those levels says when it lands and how it flows, from the Teams' real work?**

No mechanism is embedded: "tell" can be a declaration, a query, a link or a reading of the tracker; "level"
can be a type, a container or a scope.

## The three parts every option must fill (from `job-analysis.md` §2)

- **Attach**: how an item belongs to an item one level up.
- **Compose**: how a level's "when" is built: *flatten* (simulate all Work Items under the top item at once)
  or *compose* (forecast the level directly above the Teams as today, then combine those forecasts upwards
  with the joint "all done" maths Deliveries already use, `Models/Delivery.cs`, ADR-113, ADR-155, ADR-156).
- **Read**: what a level answers from its own states (cycle time, throughput, WIP at that level).

## SCAMPER

### S — Substitute: replace the hierarchy with a query

**S1. Query-scoped forecast items.** A Portfolio's Feature type stays the promise level (Change). Instead of
matching Work Items by direct parent, each Feature's Work Items are whatever a query template returns with the
Feature's key filled in (Jira: a descendant query; Azure DevOps: a tree query; CSV: a column filter). The
tracker does the walking.

### C — Combine: merge levels with Deliveries

**C1. Parent-grouped Deliveries.** The Portfolio's Feature type is the Teams' direct parent (Epic). A new
Delivery source, "one Delivery per parent", makes Lighthouse create and keep one Delivery per distinct parent
item (one per Change), named after it. A further source groups Deliveries by their parent's parent (Project).

### A — Adapt: borrow the trackers' own level model

**A1. Instance-wide Level Model.** Adapted from Jira Plans' work type hierarchy and Azure DevOps backlog levels:
Settings gets an ordered list of named levels (Work Item, Epic, Change, Project, Program), each mapped per
connection to tracker types and an attach field, optionally read from the tracker. "Feature" stops being a
fixed role: it is the name of one level. Teams sit at the bottom level; a Portfolio says which levels it
covers.

### M — Modify / Magnify: magnify the parent row that already exists

**M1. Level stack above the Feature, on the Portfolio.** Today one level above the Feature is fetched and shown
as the Parent column (`IsParentFeature`, H4). Magnify it: a Portfolio declares an ordered stack of named
levels above its Feature type (Change, Project, Program), each with its tracker types and attach field. The
Feature stays the Teams' direct parent. Each level gets rows, a forecast composed from the level below, and
flow metrics from its own states.

### P — Put to other use: a Portfolio as the input of another Portfolio

**P1. Stacked Portfolios.** A Portfolio can take another Portfolio, instead of Teams, as the source of its
work: the lower Portfolio's Features are the upper Portfolio's children. An "Agile Release Train" Portfolio of
Epics feeds a "Program" Portfolio of Changes, which can feed another. Every level is a whole Portfolio with its
own page, settings, Deliveries and metrics.

### E — Eliminate: eliminate the intermediate level from view

**E1. Look-through levels below the Feature (relay).** The Portfolio's Feature type stays the promise level
(Change). The Portfolio declares the types between Work Item and Feature (Epic). Lighthouse fetches them only
to walk from each Work Item up to its Feature, and does not show them. A Change's forecast counts every Work
Item under all its Epics.

### R — Reverse: the Team, not the Portfolio, says where its items belong

**R1. Team-declared ancestor depth.** Each Team says "my Work Items' Feature is N steps up" (or "the first
ancestor of type X"). Lighthouse walks up from each Work Item and records that ancestor as the parent it
matches on. The Portfolio does nothing new.

## Crazy 8s supplements

**X1. Hierarchy discovered once per connection.** The connection reads the tracker's own level list (Jira work
type hierarchy, Azure DevOps backlog levels, Linear's issue / project / initiative chain, CSV columns declared
by the admin) and offers it as the default for every Team and Portfolio on that connection, the way the Parent
Override Field already has a connection default (`ARCHITECTURE.md`, "Settings an owner inherits from its
connection").

**X2. Link-typed goal levels.** A level above the Feature attaches by a link type or a multi-value field instead
of a parent, so one Feature can count towards several items at that level (OKR Key Results, SAFe Strategic
Themes).

**X3. Data-proposed structure.** At setup, Lighthouse samples the Teams' Work Items, follows their parents up
until it meets the Portfolio's Feature types, and proposes the levels it found ("Stories → Epics → Changes:
use Epic as the Feature and Change as a level above?").

**X4. Tracker-side ancestor field.** Document a tracker automation that copies the grandparent key onto every
Work Item, and point the Parent Override Field at it. (Already dropped in DISCOVER; listed for completeness.)

## Curation

| Raw | Fate | Why |
|---|---|---|
| R1 Team-declared depth | **Merged into E1** | Same mechanism (walk up from the Work Item, flatten into the top), same assumption (the intermediate is not wanted), same cost; only the owner of the setting differs |
| X1 Discovered per connection | **Merged into A1** as its definition source; also noted as a growth path for M1 | It is where a level list comes from, not a different structure |
| X2 Link-typed goal levels | **Merged into M1, P1, A1** as an attach mode for the top levels | A capability of any compose option, not an option of its own |
| X3 Data-proposed structure | **Kept as a setup aid** available to every option; recorded under each option's setup line | It does not decide what a level is |
| X4 Tracker-side ancestor field | **Removed** | Dropped in DISCOVER (shifts work to the customer's admin; fails for CSV and Linear) |

## Curated 6

Each option answers the same list: what a level is; attach; what each level answers; Team Feature WIP and the
simulation's parallelism (`TeamMetricsService.cs:899-908`, `InProgressFeatureResolution.cs`,
`SimulatedRun.cs:110-115`); Terminology; Deliveries; migration of today's instances; setup; Premium boundary.

### Option 1: Level stack above the Feature (on the Portfolio)

**Core idea**: An admin keeps the Feature type at the Teams' direct parent and adds named levels above it on the
Portfolio; each level shows its items with a "likely by" date composed from the level below, and its own flow
metrics.
**Key mechanism**: per level, Lighthouse fetches the parents of the level below (the existing parent fetch,
repeated per level) and composes each item's forecast from its children's forecasts with the joint "all done"
maths, inside the Portfolio's forecast round.
**Key assumption**: the organisation accepts the Teams' direct parent (Epic) as the Lighthouse Feature, with the
promise level (Change) as a level above it.
**SCAMPER origin**: M (magnify the existing Parent row).
**Closest competitor**: Jira Plans custom hierarchy levels (levels above Epic, dates composed from children).

- **A level is**: a named, ordered entry on the Portfolio above the Feature level: tracker types plus an attach
  field. The Feature level is level 1; today's display-only parent becomes level 2 when declared.
- **Attach**: one parent per item per level, read by the existing `ParentSourceSelector` (native parent, a field,
  or a Jira link type) configured per level. Top levels may attach by link type or multi-value field (X2).
- **Each level answers**: *when*: joint likelihood that all its children are done, composed from the level below
  (Features from Work Items exactly as today; Changes from Features; Projects from Changes). *Flow*: cycle time,
  throughput and WIP at that level from the items' own states, the same computation Portfolio metrics already do
  for Features (`PortfolioMetricsService.cs:276`). *Progress*: done/total of children and of Work Items beneath.
- **Team Feature WIP and simulation**: unchanged. The Team works on Features (Epics), its Feature WIP counts its
  direct parents, and the simulation's parallelism stays at that level. Upper levels get "WIP at this level" as a
  read-only flow metric, not a simulation input.
- **Terminology**: one name pair (singular/plural) per declared level, set on the Portfolio; the Feature level keeps
  the instance-wide Feature term.
- **Deliveries**: unchanged: a cut across the Feature level. A rule may match on an upper-level item.
- **Migration**: none for two-level instances (no levels declared = today). Shape (a) Portfolios (Feature type =
  Change) are offered: "your Changes have no Work Items, but their children do: make Epic the Feature type and
  Change a level above?" (X3).
- **Setup**: the Portfolio's settings gain "Levels above {feature}": add a level, pick types, name it. Per level a
  count shows "N {features} have no item at this level" before saving.
- **Premium boundary**: candidate line: the Feature level and today's Parent column stay free; declared levels
  above it are Premium.

### Option 2: Stacked Portfolios (a Portfolio fed by another Portfolio)

**Core idea**: An admin builds the hierarchy out of Portfolios: an "Epics" Portfolio fed by Teams, a "Changes"
Portfolio fed by the Epics Portfolio, a "Projects" Portfolio fed by that, each a full Portfolio page.
**Key mechanism**: a Portfolio's source is either a set of Teams (today) or one lower Portfolio; an upper Feature's
children are the lower Portfolio's Features whose parent is that Feature, and its forecast composes their
forecasts.
**Key assumption**: each level is worth a Portfolio of its own (own page, settings, access, Deliveries), and levels
map onto groups an organisation already names (Agile Release Train, Program).
**SCAMPER origin**: P (put the Portfolio to another use).
**Closest competitor**: Businessmap / Planview AgilePlace (a board per level, links across boards).

- **A level is**: a Portfolio. Depth = how many Portfolios are stacked.
- **Attach**: the upper Portfolio's Features are matched to the lower Portfolio's Features by the lower Features'
  parent reference (the existing Parent Override Field on the lower Portfolio).
- **Each level answers**: everything a Portfolio answers today (Features, forecasts, flow metrics, Deliveries,
  Reports), with the upper Portfolio's forecast composed from the lower Portfolio's per-Feature forecasts instead
  of from Teams.
- **Team Feature WIP and simulation**: unchanged at the bottom Portfolio. Upper Portfolios have no Teams of their
  own; their "Feature WIP" is the count of in-progress upper items, not a simulation input.
- **Terminology**: each Portfolio's Features need their own word (Epic, Change, Project), so Terminology gains a
  per-Portfolio override of the Feature term.
- **Deliveries**: each Portfolio keeps its own Deliveries, so a Delivery can exist at any level.
- **Migration**: none for two-level instances. Shape (a) Portfolios: create a lower Portfolio with Epic as the
  Feature type, then set it as the source of the existing Changes Portfolio.
- **Setup**: Portfolio wizard step "Source: Teams | another Portfolio". A count shows how many upper Features have
  no children in the source Portfolio.
- **Premium boundary**: inherent: the free tier allows one Portfolio, so stacking needs Premium.

### Option 3: Instance-wide Level Model

**Core idea**: An admin defines the organisation's levels once under Settings (Work Item → Epic → Change → Project →
Program), and every Team, Portfolio and page works in those levels.
**Key mechanism**: a new Level entity (ordered, named, per connection mapped to tracker types and an attach field,
optionally read from the tracker, X1) replaces the fixed Work Item / Feature pair as Lighthouse's structure.
**Key assumption**: an organisation has one shared structure across its Teams and Portfolios (per connection).
**SCAMPER origin**: A (adapt the trackers' own level model).
**Closest competitor**: Jira Plans work type hierarchy; Azure DevOps backlog levels.

- **A level is**: an instance-wide entity: name pair, position, per-connection type mapping, attach field, and a
  declared role ("Teams work here", "forecast from Work Items here", "composed here").
- **Attach**: per level, per connection; the level list may be read from the tracker (Jira hierarchy, Azure
  DevOps process, Linear's fixed chain) or declared (CSV).
- **Each level answers**: the level marked "forecast from Work Items" is forecast by simulation; every level above
  composes; every level has flow metrics from its own states and its own page.
- **Team Feature WIP and simulation**: attached by definition to the level directly above the Teams' level.
- **Terminology**: each level's name is its term; the fixed "feature(s)" term becomes the name of one seeded level.
- **Deliveries**: a cut across one chosen level (default: the level above the Teams).
- **Migration**: every instance is seeded with two levels (Work Item, Feature) built from today's settings, plus a
  display-only third level where a Parent column is in use. Expand-only.
- **Setup**: a "Levels" settings page, first in the setup order; Portfolios then pick a level range.
- **Premium boundary**: candidate line: two levels free, more levels Premium.

### Option 4: Look-through levels below the Feature (relay)

**Core idea**: An admin keeps the Feature type at the promise level (Change) and tells the Portfolio which types sit
between it and the Work Items; Lighthouse walks through them and the Change's forecast counts every Work Item under
it.
**Key mechanism**: Lighthouse fetches the intermediate items, resolves each Work Item's ancestor at the Feature
level, and matches on that ancestor instead of the direct parent (flatten).
**Key assumption**: the organisation does not want to see the intermediate level (maintainer answer: customers
"do not care about those items").
**SCAMPER origin**: E (eliminate the intermediate from view); R1 merged in.
**Closest competitor**: Linear's sub-issue walk up to the project, already in Lighthouse
(`LinearWorkTrackingConnector.cs:389-421`); Jira Plans' progress that flattens through levels.

- **A level is**: a hidden step. One or more relay types per Portfolio, never shown.
- **Attach**: the intermediate items' own parent (per Portfolio Parent Override Field), walked from Work Item to
  Feature.
- **Each level answers**: only the Feature level answers (forecast from all Work Items beneath, flow metrics from
  its own states, as today). Intermediate items answer nothing. Levels above the Feature stay the display-only
  Parent column.
- **Team Feature WIP and simulation**: must change: either it counts the Feature-level ancestor (Changes) so the
  simulation's parallelism matches the forecast level, or it keeps counting direct parents (Epics) while the
  simulation runs on Changes. An intermediate item with no Work Items needs its own default size.
- **Terminology**: none new (the relay level is not shown), or one term for warnings that name it.
- **Deliveries**: unchanged.
- **Migration**: none for two-level instances. Shape (a) Portfolios keep Change as the Feature type and add the relay
  type.
- **Setup**: Portfolio setting "Work Items reach {features} through: [types]". A count shows Work Items whose
  chain does not reach a Feature.
- **Premium boundary**: candidate line: free (it repairs a broken forecast rather than adding a view).

### Option 5: Query-scoped forecast items

**Core idea**: An admin gives the Portfolio a query template; each Feature's Work Items are whatever the tracker
returns for that template with the Feature's key filled in.
**Key mechanism**: one tracker query per Feature per refresh (or one batched query where the tracker allows it)
replaces the parent match; the hierarchy is never stored.
**Key assumption**: the tracker can answer a descendant query (Jira, Azure DevOps tree queries), and an admin can
write one.
**SCAMPER origin**: S (substitute the hierarchy with a query).
**Closest competitor**: Broken Build Monte Carlo apps and Nave (forecast any JQL scope).

- **A level is**: not modelled. A level is whatever the query reaches.
- **Attach**: by query result. Many-to-many follows naturally: two Features' queries may overlap.
- **Each level answers**: the Feature level answers "when" from its query's Work Items, and flow metrics from its
  own states. No levels in between or above.
- **Team Feature WIP and simulation**: auto-adjusted Feature WIP cannot be tied to a query scope; Teams fall back
  to a fixed Feature WIP, or the count of distinct query-matched Features in progress.
- **Terminology**: none new.
- **Deliveries**: unchanged; a Delivery could also take a query.
- **Migration**: none for two-level instances (no template = direct parent match).
- **Setup**: Portfolio setting "Work Items of a {feature}: [query template with {key}]", with a preview of one
  Feature's result.
- **Premium boundary**: candidate line: Premium (an advanced setting).
- **Per connector**: Jira needs a descendant function from the tracker or an app; Azure DevOps has tree queries;
  Linear has no query language Lighthouse uses; CSV has none.

### Option 6: Parent-grouped Deliveries

**Core idea**: An admin keeps the Feature type at the Teams' direct parent and turns on "one Delivery per parent";
Lighthouse keeps a Delivery per Change, named after it, with its joint forecast.
**Key mechanism**: a new Delivery source in the existing source registry (ADR-166) creates, renames and archives
Deliveries as parents appear and finish; a second source groups those Deliveries by their own parent (Project).
**Key assumption**: an organisation reads its promise level as a set of releases, and Delivery pages carry what it
needs at that level.
**SCAMPER origin**: C (combine levels with Deliveries).
**Closest competitor**: none directly; today's manual workaround (one rule-based Delivery per Change) automated.

- **A level is**: a generated set of Deliveries, one per parent item.
- **Attach**: the Feature's parent reference (already stored; rule-based Deliveries match on it today,
  `FeatureFieldProvider.cs:23`).
- **Each level answers**: *when*: the Delivery's joint forecast, as today. *Flow*: none from the Change's own states
  (a Delivery has no tracker states); Delivery metrics as they exist.
- **Team Feature WIP and simulation**: unchanged (Features are Epics).
- **Terminology**: none new for one level (the Delivery term); a group-of-Deliveries term for a second level.
- **Deliveries**: they are the level. Generated Deliveries sit beside hand-made ones.
- **Migration**: none for two-level instances. Shape (a) Portfolios switch the Feature type to Epic and enable the
  source.
- **Setup**: Deliveries tab, "New Delivery source: one per {parent}".
- **Premium boundary**: inherent: rule-based and sourced Deliveries are Premium.

## Diversity test

| Pair-wise check | Mechanism | Assumption about the user | Cost profile |
|---|---|---|---|
| 1 Level stack | Repeated parent fetch, compose upwards, levels on one Portfolio | Accepts Epic as Feature, wants every level visible | Medium: N-level parent fetch per connector, composition per level, level rows and metrics |
| 2 Stacked Portfolios | Portfolio-to-Portfolio source, compose across Portfolios | Each level is an organisational group worth its own Portfolio | Medium: cross-Portfolio composition and refresh order, per-Portfolio terms |
| 3 Level Model | New instance-wide Level entity replaces the fixed pair | One shared structure per organisation | High: re-models Feature across pages, metrics, terms, access, clients |
| 4 Look-through | Walk from Work Item to ancestor, flatten into the Feature | Does not want the intermediate level | Medium: intermediate fetch, ancestor match, Team WIP and default-size rework |
| 5 Query scope | Tracker query per Feature, no stored hierarchy | Admin writes queries; tracker can recurse | Low build, high per-refresh request cost; uneven across connectors |
| 6 Parent-grouped Deliveries | Generated Deliveries via the source registry | Thinks of the promise level as releases | Low-medium: one new Delivery source, lifecycle of generated Deliveries |

Every pair differs on at least two of the three columns. Options 1 and 6 share the compose maths but differ in
what holds the level (a Portfolio level with its own states vs a Delivery without states) and in the user
assumption (levels vs releases). Options 1 and 3 share "named levels" but differ in scope (one Portfolio vs the
instance), in what happens to the Feature concept (kept vs replaced) and in cost.

## Which options serve which shape (structure check, not a score)

| Setup (from `../feature-delta.md`) | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Today's two-level, unchanged | yes | yes | yes (seeded) | yes | yes | yes |
| Customer A: Story ← Epic ← Change ← Project ← optional Program | yes: Feature = Epic, levels Change, Project, Program | yes: three or four Portfolios | yes | Change only; Project and Program stay display-only/absent | Change only | Change; Project via grouped Deliveries |
| Customer B: Story ← Epic ← Feature ← Business Epic | yes: Feature = Epic, levels Feature (SAFe), Business Epic | yes | yes | Business Epic only, two relay types | Business Epic only | yes, two generated tiers |
| SAFe: Story ← Feature ← optional Capability ← Portfolio Epic ← Theme | yes per Portfolio; a mixed-Capability Portfolio relies on the Parent Override Field (decision 4); Theme by link (X2) | yes | yes | Portfolio Epic only | yes (queries ignore depth) | partial |
| OKR-style: Work Items ← Initiatives / KRs ← Objectives (links, many-to-many) | top levels by link (X2); "when" = linked work done | partial (one parent) | yes with link attach | no | yes (queries overlap) | partial |
| Shape (a) | yes | yes | yes | yes | yes | yes |
| Shape (b) | yes | yes | yes | no | no | yes |

**Gate G3**: HMW framed without a solution; all 7 SCAMPER lenses applied; 4 Crazy 8s supplements; curated to 6;
diversity test documented; no evaluation in this file. Pass.
