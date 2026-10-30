# Competitive Research (top-up) — how tools let an organisation model its own levels

Wave: DIVERGE, phase 2 · 2026-10-30 · research depth: **top-up**.

**Base**: `../../epic-5806-initiatives/diverge/competitive-research.md` (2026-09-29, 21 tools, 17 in depth).
Its findings still hold and are not repeated: the strategy layer above the Epic is usually a paid tier; its
forecasts are deterministic or people-paced; no tool rolls per-team Monte Carlo into a level above the
Feature; tools that forecast any query scope (Nave, Broken Build) are immune to hierarchy depth.

**This delta asks one new question**: *how does each tool let an organisation declare its own hierarchy
levels, how do items attach to a level, and what does each level then answer?* That is the modelling
question behind maintainer decision 2 (a tracker-agnostic, configured structure).

Legend: **[V]** verified on the vendor's own page during this session. **[I]** inferred from search
snippets, community answers or third-party pages; lower confidence. "Absent" means the fetched page did not
mention it, not proof the product lacks it.

## Tool by tool

### 1. Jira Plans (ex-Advanced Roadmaps) — custom hierarchy levels

- **Where levels are declared** [V]: Settings → Work items → Work type hierarchy. "Changes to the hierarchy
  settings will apply to all existing plans and company-managed projects using this work item scheme." Only
  Jira administrators change it; levels are created with "+ Create level" and reordered by dragging; each
  level is associated with Jira work types.
  [Configure custom hierarchy levels](https://support.atlassian.com/jira-software-cloud/docs/configure-custom-hierarchy-levels-in-advanced-roadmaps/)
- **Where levels can go** [V/I]: above Epic only; nothing new can be inserted between Epic and Story [I:
  partner answer on [Atlassian Community](https://community.atlassian.com/forums/Jira-questions/Hierarchy-posibilites-in-Jira/qaq-p/2669411)].
  The common workaround for SAFe is to rename Epic to Feature and add a new level above it [I].
- **Attach**: the Parent field, per level [V: the page says the Parent field must be on the work items].
- **What each level answers** [I: Atlassian docs via search]: roll-ups of dates (earliest child start, latest
  child end) and estimates, shown as previews and not written back; a progress column that "includes
  estimates from child issues in all hierarchy levels".
  [Planning tools in plans](https://support.atlassian.com/jira-software-cloud/docs/planning-tools-in-advanced-roadmaps)
- **Tier** [V]: "only available as part of Jira Cloud Premium and Enterprise".
- **Lesson for Lighthouse**: Atlassian models levels **once per instance**, as an ordered list mapped to types,
  and **progress flattens through all levels** while **dates compose** from children. Customer A and B's
  hierarchies are exactly this setting. Its instance-wide reach is also its weak point: one hierarchy for
  every project (community answers warn of this).

### 2. Azure DevOps — portfolio backlog levels (inherited process)

- **Where** [V]: Organization settings → Process → Backlog levels → "+ New top level portfolio backlog".
  "When you customize an inherited process, any projects that use the process automatically reflect the
  customizations." [Customize backlogs and boards](https://learn.microsoft.com/en-us/azure/devops/organizations/settings/work/customize-process-backlogs-boards?view=azure-devops)
- **Limits** [V]: "up to a total of five portfolio backlogs"; "You can't insert a new custom backlog level
  within the existing set"; "You can't reorder the backlog levels"; "Each WIT can belong to only one backlog
  level"; a level can be renamed and can hold several work item types.
- **Attach**: the Hierarchy (parent/child) link between items of adjacent levels [V: "hierarchical view of
  work, which includes items defined in lower-level backlogs"].
- **What each level answers**: a backlog and a board per level, roll-up progress of children (see 5806 §5);
  no forecast.
- **Tier**: included in Azure DevOps; no upsell.
- **Lesson**: levels are **named, ordered and typed per process**, each level gets **its own board (own
  states)**, and **a type belongs to exactly one level**. Lighthouse's Azure DevOps customers already have their
  levels declared in the tracker, which a connector could read.

### 3. Jira Align — SAFe configurations and custom hierarchy

- [I]: Jira Align follows SAFe configurations (Essential, Large Solution, Portfolio, Full), where the
  Capability (solution) level and the Portfolio level are additive and optional per configuration
  ([SAFe 6.0 configurations, overview](https://framework.scaledagile.com/safe-6-0-configurations);
  [Inflectra summary](https://inflectra.com/ideas/Topic/SAFe-Configurations.aspx)).
- [V]: Jira Align also supports **custom hierarchy levels assigned to work items**, listed through a "Domains"
  endpoint and attached through a separate relation, apart from the parent chain.
  [How to manipulate custom hierarchy at the work item level](https://support.atlassian.com/jira-align/kb/how-to-manipulate-custom-hierarchy-at-the-work-item-level-using-the-jira-align-apis/)
- Jira Align's help centre returned 403 in the 5806 session; its configuration UI is not verified here.
- **Lesson**: even the most SAFe-committed tool keeps **two structures**: a fixed work hierarchy (Story →
  Feature → Capability → Epic) and a **separate, custom organisational hierarchy** that items are tagged
  into. That matches the ambiguity in the maintainer's example "Team → Agile Release Train → Program", which
  names groups of teams, not item types (see `recommendation.md`, questions).

### 4. Businessmap — card links at any depth

- [I: knowledge-base search results; 5806 recorded a 403 on the KB]: parent/child links between cards and
  between initiatives, created by dragging with Ctrl and choosing the relationship type; a "Links Overview"
  and a full "hierarchy view" show parent and child cards across levels; articles exist on syncing planned
  dates between parent and child initiatives and on "hierarchical outcome rules".
  [Initiatives and cards](https://knowledgebase.businessmap.io/hc/en-us/sections/115001219065-Initiatives-and-Cards-Workflow-Management)
- No declared level model found: depth comes from links, and the "level" is the **board or workspace** a card
  sits on (Team board, Portfolio board, Management workspace with a Strategy board; see 5806 §2).
- **Lesson**: Flight Levels tooling models a level as **a board with its own workflow**, and the hierarchy as
  **links between cards on different boards**. A Lighthouse equivalent is "a Portfolio per level, linked".

### 5. IBM Targetprocess (Apptio) — a fixed extra level, renamable

- [V]: Portfolio Epic was added as "an additional level to the work hierarchy" above Epic; its terminology,
  custom fields, permissions and workflow are customisable. "Portfolio Epics can't hold User Stories or Bugs
  directly": stories must sit under Features that belong to a Portfolio Epic.
  [Portfolio epics guide](https://targetprocess.com/guide/entity-types/portfolio-epics/),
  [Epics as one more hierarchy level](https://targetprocess.com/blog/upcoming-feature-epics-as-one-more-hierarchy-level/)
- **Lesson**: a tool that added **fixed, renamable levels one at a time**, and **requires every item to pass
  through each level** (no Story directly under a Portfolio Epic). That is the same rule as maintainer
  decision 4, and it shipped.

### 6. Linear — sub-initiatives

- [V]: "Sub-initiatives let you nest Initiatives up to five levels deep in a tree-like structure." "Initiatives
  can have multiple parents." "Projects added to any sub-initiative will automatically be included in the
  parent's view of progress." Enterprise plan only. No computed health or forecast roll-up described (health
  is set by hand, see 5806 §12).
  [Sub-initiatives](https://linear.app/docs/sub-initiatives)
- **Lesson**: the one tracker Lighthouse connects to that has **many-to-many levels** (multiple parents) and
  **progress that flattens** through nesting. The OKR-style setup in `../feature-delta.md` looks like this.
  Today Lighthouse fixes Linear's Feature level to Projects and shows only the first initiative
  (`LinearWorkTrackingConnector.cs:423-438`).

### 7. Planview AgilePlace — multi-parent card connections

- [I: Planview success-centre pages via search]: parent/child card connections, across boards; "multi-parent
  and child card connections"; parent cards show statistics about their children; Roadmaps group imported
  items by parent card "with up to five levels of sub-cards"; 2025 automations move a parent when its first
  child moves.
  [About card connections](https://success.planview.com/Planview_AgilePlace/Cards/Connections/About_Card_Connections),
  [Working with items imported from AgilePlace](https://success.planview.com/Shared_capabilities/Planview_Roadmaps/Working_with_Planview_Roadmaps/Working_with_items_imported_from_AgilePlace)
- No declared level model; levels are boards (team, program, ART boards; 5806 §7).
- **Lesson**: like Businessmap, **levels are boards, depth is links**, and a parent's **state can follow its
  children** automatically.

### 8. Non-obvious alternative (different category, same job): query-scope forecasters — Broken Build, Nave

- Broken Build, Monte Carlo apps for Jira [I: Marketplace listings via search]: data sources include "boards,
  projects, releases, initiatives", JQL, saved filters or custom JQL; remaining work set "through a JQL filter
  or a fixed value".
  [Monte Carlo simulations for Jira](https://marketplace.atlassian.com/apps/3888405750),
  [Monte Carlo capacity planning](https://marketplace.atlassian.com/apps/3709319691)
- Nave (5806 §9) [I]: throughput from "another board, project, release, epic, initiative, or custom JQL".
- **Lesson**: these tools **never model a level**. A "level" is whatever a query returns, so depth is the
  tracker's problem (in Jira, a recursive descendant query needs a function the tracker or an app provides).
  They answer "when" for any scope but have **no flow metrics for the scope item itself** and **no setup
  safety net**: a wrong query is a wrong forecast, silently. They are the structural opposite of a
  configured hierarchy.

### 9. Non-obvious alternative: OKR tools — Quantive, Perdoo

- Quantive Results for Jira [I: Quantive help via search]: an Objective or Key Result is linked to a Jira
  issue, which then appears "as a task under the OKR"; epics, stories, tasks and bugs can be associated with
  any OKR. [Jira + Quantive](https://help.quantive.com/en/articles/6714802-jira-quantive-where-agile-meets-okrs)
- Perdoo [I: third-party listing]: deliberately does **not** let one Key Result align to several Objectives,
  because that "ruins the cascade". [Software Advice listing](https://www.softwareadvice.co.uk/software/425388/perdoo)
- **Lesson**: OKR levels attach by **link or tag, not parent**, and the field is split on many-to-many. A
  Key Result carries a measure, not a done state, so "when does it land" is only meaningful for the work
  linked to it. Any Lighthouse option should let a goal level answer "when is the linked work done", never
  "when is the goal met".

## Cross-tool synthesis

| Tool | Levels declared where | Attach by | Depth / count | Many-to-many | Each level's own states | Roll-up maths | Tier |
|---|---|---|---|---|---|---|---|
| Jira Plans | Instance, ordered list ↔ types | Parent field | Any above Epic, none below | No | Yes (issue states) | Dates compose; progress flattens | Premium/Enterprise |
| Azure DevOps | Process, ordered list ↔ types | Hierarchy link | Max 5 portfolio levels, no insert | No | Yes (board per level) | Progress roll-up | Included |
| Jira Align | Fixed SAFe chain + custom org hierarchy | Parent + separate relation | Per SAFe config | Via custom hierarchy | Yes | PI/points | Enterprise |
| Targetprocess | Fixed added levels, renamable | Parent; must pass every level | Fixed | No | Yes (own workflow) | Progress | Enterprise |
| Linear | Fixed (issue, project, initiative) + nested initiatives | Parent | 5 initiative levels | **Yes** | Manual health | Progress flattens | Enterprise |
| Businessmap, AgilePlace | No level model; boards are levels | Card links across boards | Unbounded | AgilePlace yes | Yes (board workflow) | Child statistics | Mid/enterprise |
| Broken Build, Nave | No level model; a query is a scope | Query | n/a | Yes (queries overlap) | No | Monte Carlo over the scope | Per board |
| Quantive (OKR) | Objectives and Key Results | Link | 2-3 | Contested | No (measures) | Progress | Paid |

**What this means for the options**

1. **Two mainstream modelling styles.** Trackers (Jira Plans, Azure DevOps, Targetprocess) declare an
   **ordered list of named levels mapped to types**, and every item sits at the level of its type. Flow tools
   (Businessmap, AgilePlace) make **each level a board** and link cards across boards. Lighthouse has both
   raw materials: a type-to-role mapping (Feature types) and a container with its own flow (the Portfolio).
2. **Levels go above, not below.** Neither Jira nor Azure DevOps lets a level be inserted between existing
   ones; both add levels on top, and Targetprocess requires every item to pass through each level. That
   supports the reading of decision 4 in `job-analysis.md` §4: make the Team's direct parent the Feature, and
   add levels above it.
3. **Roll-up maths splits.** Progress usually flattens through all levels; dates compose from children. No
   competitor composes probabilistic forecasts level by level; 5806's gap still stands.
4. **Many-to-many only appears at the top** (Linear sub-initiatives, AgilePlace multi-parent, OKR links).
   Below the strategic level every tool keeps one parent.
5. **Paid tier is the norm** for levels above Epic (Jira Plans Premium, Linear Enterprise); Azure DevOps is
   the free exception.
6. **Nobody guards setup.** No tool found warns that items do not reach the level above. That is open
   ground for OS3.

**Gate G2**: 9 real products named with cited behaviour (Jira Plans, Azure DevOps, Jira Align, Businessmap,
Targetprocess, Linear, Planview AgilePlace, Broken Build/Nave, Quantive/Perdoo); non-obvious alternatives
from different categories: query-scope forecasters and OKR tools; no generic market claims. Pass.
