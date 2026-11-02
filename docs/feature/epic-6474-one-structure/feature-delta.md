<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-6474-one-structure

**Epic**: ADO #6474 "One structure: Teams and Portfolios as units on levels". Part of the Epic 6455 family
(D21); precedes #6455 "Levels above", #6476 and #6477.
**Waves present**: DISCOVER and DIVERGE inherited from `../epic-6455-deeper-hierarchies/` (not re-run, by
instruction). DISCUSS 2026-11-02 (this file, plus `slices/`).
**UX depth**: comprehensive. **Feature type**: cross-cutting, backend-heavy with visible parts. **Walking
skeleton**: brownfield.
**Locked input**: 6455 `feature-delta.md` → DISCUSS "Locked decisions" D1–D21. Not re-litigated here; cited as
D-numbers. Everything else in this file is a proposal until the maintainer walk-through.
**Terminology**: default terms (Team, Portfolio, Work Item, Feature). Tracker type names (Story, Bug, Epic,
Change, User Story) are literal tracker values. Personas below are fictional; customers stay anonymous.

---

## Wave: DISCUSS / [REF] Locked decisions

D1–D21 are inherited from the 6455 DISCUSS. Added for this Epic:

### Maintainer decisions (2026-11-02)

Numbered as the open questions Q1–Q12 they answer.

- **M1.** An API write (`/teams`, `/portfolios`) naming a type the level does not list is refused with **400
  naming the type, for everyone, System Admins included**; no automatic adding. Removing that ability is
  deliberate. It is a **breaking API change** for the Lighthouse-Clients CLI/MCP: major version bump and a
  breaking-change note, to be confirmed at DESIGN. Later idea, out of scope: separate AI skills for
  administering Lighthouse and for using it.
- **M2.** A type newly added to a level is ticked for no existing unit; the dialog shows "N units could read it".
- **M3.** Removing a type that units tick is allowed after a confirmation naming them.
- **M4.** **Only System Admins add types to a level, and only in Organisation → levels.** Unit forms show only
  the level's list, with no "Add to the level" control; a non-admin missing a type is pointed to a System Admin.
- **M5.** Slice 05 waits for #6473's Organisation shell; the invisible slices go first.
- **M6.** The union of types is deduplicated ignoring case.
- **M7.** Renaming **levels** (each level's unit and item names) is **free**, still System Admin only; the rest
  of Terminology stays Premium. `TerminologyController`'s Premium guard on `PUT /terminology` must no longer
  apply to the 8 moved keys (slice 06).
- **M8.** All slices stay in this Epic.
- **M9.** The CLI and MCP get a `levels` read **now, in this Epic**, released together with the breaking-change
  client release (new slice 14; the Lighthouse-Clients repo is handled at DESIGN/DELIVER).
- **M10.** Code names Unit and Level; the leaf item's code name is settled at DESIGN.
- **M11.** Empty type subsets are allowed (Linear, ServiceNow).
- **M12.** Two-tracker installs and Customer-A-shaped installs are left to DESIGN under D10 (migrate as is);
  DESIGN adds migration tests for both (slice 02 notes).
- **M13.** An install can switch the level above Teams off ("Teams only": one level). Off: no Portfolios,
  Features or Deliveries shown; Teams keep throughput, flow metrics and forecasts. First-run set-up offers
  "Teams only". Switching off with existing Portfolios asks first, naming them; they are hidden, data kept, and
  shown again when switched on. Locked while more than two levels exist. Prototyped, approved. No licence rule;
  hidden Portfolios stop refreshing (to confirm at DESIGN). Slice 15.

---

## Wave: DISCUSS / [REF] Job grounding

DIVERGE artifacts exist (`../epic-6455-deeper-hierarchies/diverge/job-analysis.md`); JTBD is not re-run.

| Job (SSOT `docs/product/jobs.yaml`) | How this Epic serves it |
|---|---|
| `job-config-admin-describe-our-levels-once` | Directly, for the two levels that exist: the admin sees the organisation's structure in one place, in its own words, and says once which tracker types each level holds. "Will adding levels change the Portfolios that work today? (It must not.)" is this Epic's acceptance bar. |
| `job-forecaster-know-when-each-promise-level-lands` | Preparation only (D8). Nothing a forecaster reads changes; the guard is that nothing does. |
| **New, proposed**: `job-config-admin-say-item-types-once` | See "Proposed SSOT and ADR updates". D8 calls level-owned types an improvement in its own right. |

Mapping: 4 visible slices → `job-config-admin-describe-our-levels-once`; slice 09 → the new job; the 8
invisible slices → `infrastructure-only` with a rationale naming the job they prepare.

---

## Wave: DISCUSS / [REF] Scope Assessment

| Signal | Evidence | Verdict |
|---|---|---|
| > 10 stories | 15 slices, each ≤ 1 day (14 added by M9, 15 by M13) | **Over**; kept in one Epic by M8 |
| > 3 bounded contexts | Organisation set-up (core of it), Work Items (refresh per unit), Forecasting (leaf extension), Access + Licensing (re-expressed, no new rules) | 3 changed, 2 re-expressed: borderline |
| Walking skeleton > 5 integration points | migration → level store → Terminology read → Organisation UI: 4 | Pass |
| > 2 weeks | ~13 slice-days plus review | Borderline |
| Independent outcomes | One: "the structure is one thing". The visible parts have no value without the levels store | Pass |

**Verdict: PASS with a flag.** 13 slices is one over the ~12 guideline. The natural split line is between
"levels visible" (01–06, 08, 09, 11) and "one Unit type underneath" (07, 10, 12, 13). The second half is
invisible on its own, so under "each Epic ships on its own" it cannot be its own Epic; the only alternative
home is the opening slices of #6455 (D7 demands one Unit type before a level is *added*, which is #6455's
first step). Recommendation: keep one Epic (D7, D8, D20). Open question Q8.

---

### D8 waiver: eight infrastructure-only slices

Slices 01, 02, 03, 04, 07, 10, 12 and 13 change nothing a person or an API client can observe; the maintainer
accepted an Epic whose value is mostly preparation (D8) and asked for the deep step as several slices (D7).
Slice 04 keeps the `/teams` and `/portfolios` payloads identical, with one deliberate exception decided by the
maintainer (M1): a type the level does not list is refused for everyone. That is a breaking change for API
clients, announced through slice 14's major client release, not a silent one. Slice 13 keeps the
free-tier limits and messages exactly. The guard on all eight is slice 01's check (OUT-1).

---

## Wave: DISCUSS / [REF] Team and Portfolio difference inventory (code, 2026-11-02)

Source: `Models/WorkTrackingSystemOptionsOwner.cs`, `Team.cs`, `Portfolio.cs`, `Data/LighthouseAppContext.cs`,
`LicenseGuardAttribute.cs`, `RbacAdministrationService.cs`, `DataRetrievalSchemaDto.cs`, `TerminologySeeder.cs`.
Storage today: two tables, two id sequences (Team 3 and Portfolio 3 can both exist), mapped from one abstract
base. Several side tables already carry a kind discriminator: `OwnerType` (snapshots), `PermissionScopeType`
(RBAC scopes), `ReportOwnerKind` (reports).

### Shared already (the abstract base `WorkTrackingSystemOptionsOwner`) → the Unit

Identity (`Id`, `Name`, `ConcurrencyToken`), connection, query (`DataRetrievalValue`), `UpdateTime`,
`FetchFingerprint`, `WorkItemTypes` (abstract, different defaults), To Do / Doing / Done states,
`StateMappings`, `WaitStates`, `BlockedRuleSetJson`, `CycleTimeDefinitions`, SLE (probability, range),
`SystemWIPLimit`, `DoneItemsCutoffDays`, staleness thresholds, process-behaviour baseline dates, Estimation
bundle and Parent Override (each "follows the connection default / set here", ADR-241).

### Leaf only (Team) → leaf extensions

| Field or behaviour | Target owner (ADR-270) |
|---|---|
| `FeatureWIP`, `AutomaticallyAdjustFeatureWIP` | Forecasting extension keyed by unit id (slice 07) |
| Throughput window: `ThroughputHistory`, `UseFixedDatesForThroughput`, start/end dates | Forecasting extension (slice 07) |
| `ForecastFilterRuleSetJson` | Forecasting extension (slice 07) |
| `RefinementSettings` | Refinement (already its own owned config; stays) |
| `WorkItems`; `WorkItem.TeamId`, `FeatureWork.TeamId`, `WhenForecast/StartForecast.TeamId` | Work Items / Forecasting reference the unit by id; unchanged ids |
| Connection defaults `ParentOverrideForTeams`, `EstimationForTeams` | per level (leaf) |
| `TeamUpdater`, `TeamDataRefreshed`, `TeamDeleted` (published from a controller) | one unit refresh (slice 12), `UnitDeleted` (slice 03) |
| Free tier: 3 Teams | "3 units on the leaf level" (slice 13) |
| Manual forecasts, feature-size metrics, Refinement routes, Team members | leaf tabs; membership per unit |

### Level 2 only (Portfolio, the leaf's direct parent) → level-2 extension

| Field or behaviour | Target owner |
|---|---|
| Default-size guess: `DefaultAmountOfWorkItemsPerFeature`, `UsePercentileToCalculateDefaultAmountOfWorkItems`, `PercentileHistoryInDays`, `DefaultWorkItemPercentile`, `OverrideRealChildCountStates` | Work Items' roll-up, keyed by unit id (slice 10) |
| Mapping fields: Size Estimate, Feature Owner, Dependency field, `IgnoreDependencies` | Organisation set-up, level-2 part of the unit (slice 10) |
| `OwningTeamId` (a Portfolio's owning Team) | Organisation set-up, level-2 part (slice 10) |
| `ForecastInputFingerprint` | Forecasting (slice 10) |
| `Features` (many-to-many: one Feature can be in several Portfolios), `FeatureBlockedTransition.PortfolioId` | Work Items, by unit id; unchanged |
| Deliveries, rules, sources (`/portfolios/{id}/delivery-*`) | Promises; stay on level 2 (D2, D11) |
| `PortfolioUpdater` (refresh, then direct forecast trigger), `PortfolioFeaturesRefreshed`, `PortfolioForecastsUpdated` | one unit refresh (slice 12) |
| Free tier: 1 Portfolio; `CanCreatePortfolio` needs a Team first | "1 unit on level 2" (slice 13) |
| Linear: no query; ServiceNow: no Portfolio at all (ADR-116) | level-2 capability per connector, unchanged |

**Finding.** Almost nothing is "non-leaf" in general: what a Portfolio adds is tied to being the **leaf's direct
parent** (its items' children are Work Items). For #6455 this means a level 3 unit needs only the shared Unit
part, which is why D7's deep step pays off there. Proposed term: **"Feature level"** in conversation, coded as
"the level above the leaf" (never "level 2" in code; ADR-270 Decision 4).

### Not a difference, but a trap

- Item types are matched per tracker and may differ in case ("Bug" from Jira and from Azure DevOps). The union
  is deduplicated ignoring case (M6); DESIGN confirms today's matching rule.
- Linear does not use item types (`IsWorkItemTypesRequired = false`); a ServiceNow Team that names no types reads
  nothing on purpose (`docs/concepts/worktrackingsystems/servicenow.md`). An empty subset must stay legal.
- Renaming Terminology is **Premium and System Admin** today (`TerminologyController.cs:27-28`); item types
  are free and set by Team/Portfolio Admins. Level names inherit the first rule, level types the free tier.

---

## Wave: DISCUSS / [REF] What the one Unit holds

```
Level (installation-wide, exactly two for now)            Organisation set-up
  ordinal · is-leaf · unit name (sg/pl) · item name (sg/pl) · item types
    └── Unit (one type)                                   Organisation set-up
          identity, level, connection, query, item-type SUBSET of its level,
          states + state mapping (local, D13), blocked rules, cycle-time defs,
          SLE, WIP limit, staleness, estimation, parent override
          ├── leaf extension, keyed by unit id            Forecasting (+ Refinement)
          │     Feature WIP, auto-adjust, throughput window, forecast filter rules
          └── feature-level extension, keyed by unit id   Work Items roll-up / Org set-up / Forecasting
                default-size guess, size/owner/dependency fields, owning Team, fingerprint
Deliveries hang off a Feature-level unit (Promises, unchanged, D2)
```

Ids: the ids the API, the CLI/MCP clients, RBAC scopes, report owners and snapshot rows use **do not change**
(D9). How the unit keeps them (a unit table beside today's tables, kind + legacy id, or another shape) is
DESIGN's call within expand-only migrations.

---

## Wave: DISCUSS / [REF] Context map placement

| Question | Answer |
|---|---|
| Contexts touched | **Organisation set-up** (levels, units, names, types: changed heavily, so refactored here, D20). **Work Items** (one refresh per unit, slice 12). **Forecasting** (leaf extension, slice 07). **Access** and **Licensing** re-expressed per unit and level, no rule changes (slice 13). Tracker integration: unchanged; it receives each unit's type subset with the fetch as today. Promises: unchanged (Deliveries stay on the Feature level). |
| New terms | **Level** (stops being target-only for two fixed levels), **Unit** (one code type), **level's item types**, **unit's subset** ("narrows"), **Feature level** (the level above the leaf). Team, Portfolio, Work Item, Feature become the seeded **names of levels 1 and 2**, not concepts of their own. |
| Domain events published | `LevelRenamed` (Organisation set-up; no backend handler needed now, the frontend re-reads names), `LevelItemTypesChanged` (handled by Work Items: refresh the units whose subset changed), `UnitCreated`, `UnitDeleted` (replace `TeamDeleted`, fill ADR-270's "Team or Portfolio created / Portfolio deleted" gaps; handled by Access membership clean-up, Reports, snapshots as today's inline clean-up does). "Level introduced" stays reserved for #6455. |
| Domain events handled | `TeamDataRefreshed` / `PortfolioFeaturesRefreshed` kept as published names (handlers unchanged) until slice 12 decides whether one `UnitDataRefreshed` replaces them. |
| UI surface (ADR-271) | **Organisation**: a Levels section in the same tab as Terminology (D6), above "Other terms". **Unit Settings tab** (Team and Portfolio pages, kept parallel per ADR-271 Decision 1): item types become a subset picker. **Overview**: onboarding stepper names the two levels. Step towards target: Terminology loses the 8 structure keys to the levels; no move towards U4 (one unit page) in this Epic, said explicitly. |

---

## Wave: DISCUSS / [REF] Journeys

Four journeys, each `journey → steps → emotional arc → Gherkin`. Visual and YAML are combined here to keep
one file; DISTILL takes the Gherkin from the slice files.

### J1 Upgrade keeps every number (config-admin Priya Raman, delivery-forecaster Jonas Weber)

```
[Upgrade to vNext] → [Migration seeds 2 levels] → [First refresh] → [Jonas opens "Checkout Revamp"]
 Feels: wary          Sees: nothing new            Sees: same counts    Sees: same 85% date, same throughput
 "will it break?"     (log: levels seeded,          per unit             Feels: unbothered (nothing to notice)
                       N units carried over as is)
```

```yaml
journey: {name: upgrade-keeps-every-number, persona: config-admin, emotional_arc: {start: wary, middle: unaware, end: unbothered}}
steps:
  - id: 1
    name: Migration carries set-ups over as they are (D10, D12)
    shared_artifacts: [level_names, level_item_types, unit_type_subset]
    failure_modes: ["custom Terminology lost", "a type dropped by case dedupe", "an empty Linear subset rejected"]
    gherkin: |
      Scenario: Every unit reads exactly what it read before the upgrade
        Given Priya's install has Team "Payments Crew" reading Story and Bug and Portfolio "Checkout Revamp" reading Epic and Change
        When the install is upgraded
        Then the Team level lists Story and Bug and the Feature level lists Epic and Change
        And "Payments Crew" ticks Story and Bug and "Checkout Revamp" ticks Epic and Change
  - id: 2
    name: Numbers unchanged after the first refresh
    gherkin: |
      Scenario: Forecasts and throughput are the same after the upgrade
        Given the pinned numbers of every unit before the upgrade
        When the first refresh after the upgrade has run
        Then each unit's item counts, throughput series and forecast percentiles equal the pinned numbers
```

### J2 See and rename the levels (Priya, System Admin, Premium)

```
[Organisation → Structure & terms] → [Levels: 2 rows] → [Edit level 1] → [Save] → [Team pages say "Squad"]
 Feels: curious                       "so that's where    focused          relieved   confident
                                       Team went"
```

Gherkin: slices 05 and 06.

### J3 Say the item types once, let a Team narrow them (Priya; Team Admin Tom Becker)

```
[Level 1 → Edit types] → [Add "Task"] → [Remove "Bug": 4 Teams lose it, confirm?] → [Tom ticks Task on his Team]
 Feels: in control        quick           careful (explicit warning buffer)         satisfied, nothing typed
```

Gherkin: slices 08 and 09.

### J4 New install sees its structure (first-time-system-admin Ana Lima, free tier)

```
[Overview, empty] → [Stepper: "Teams work on Work Items, which roll up into Features of Portfolios"] → [Add Team]
 Feels: unsure       oriented ("this is the shape")                                          ready
```

Gherkin: slice 11.

---

## Wave: DISCUSS / [REF] UI sketches (DRAFT, for the maintainer's yes before DISTILL)

Organisation → Structure & terms (new section above the existing Terminology list; reuses the Terminology
table look and `WorkItemTypesComponent` chips in the edit dialog):

```
Organisation   [Structure & terms] [Calendar] [Behaviour] ...                         DRAFT
┌ Levels ───────────────────────────────────────────────────────────────────────────────┐
│ Your organisation has two levels. Levels above come in a later release.               │
│ Level  Unit                    Item                     Item types                    │
│ 2      Portfolio · Portfolios  Feature · Features       Epic, Change            [Edit]│
│ 1      Team · Teams            Work Item · Work Items   Story, Bug, Task, User Story [Edit]│
│        generates throughput                                                           │
└───────────────────────────────────────────────────────────────────────────────────────┘
┌ Other terms ──────────────────────────────────────────────────────────────────────────┐
│ Cycle Time [Cycle Time]  Throughput [Throughput]  WIP [WIP]  Blocked [Blocked] ...    │
└───────────────────────────────────────────────────────────────────────────────────────┘
Loading: both boxes dimmed with a small spinner; no seeded names shown as if loaded.
Failure: "Could not load the levels. [Try again]"; Edit disabled.
Any licence: level names and types editable by System Admins (M7); others see no [Edit].
```

Unit Settings → Work Item Types (Team page; the Portfolio page mirrors it with "Feature Types"):

```
Work Item Types — from the Team level                                                 DRAFT
 [x] Story   [x] Bug   [ ] User Story
 The list belongs to the Team level. Change it in Organisation (System Admins).
 Load from board found "Sub-task", which is not on the Team level. A System Admin can add it in Organisation.
 (No add control here, for anyone: M4.)
Loading: the Team's own ticks as read-only chips + spinner.
Failure: "Could not load the Team level's types. Your selection is kept." Types not editable; other fields are.
```

Level edit dialog (slices 06 and 08; reuses the Terminology TextFields and `WorkItemTypesComponent` chips):

```
Edit level 1                                                                         DRAFT
 Unit   [Team      ] singular   [Teams      ] plural      (System Admin, any licence)
 Item   [Work Item ] singular   [Work Items ] plural
 Item types  (Story x) (Bug x) (Task x) (User Story x)  [+ Add type]
 Adding a type ticks it for no existing Team. 12 Teams could read it.
                                                                       [Cancel] [Save]
Loading the impact of a removal: the Remove button shows a small spinner; nothing is removed yet.
Save failed: "Could not save level 1. Nothing was changed. [Try again]"; the dialog keeps the edits.
Impact could not be loaded: "Could not check which Teams read Bug"; Remove stays disabled.
```

Remove-a-type confirmation (reuses the existing confirm dialog):

```
Remove "Bug" from the Team level?                                                    DRAFT
 4 Teams read Bug today: Payments Crew, Checkout Crew, Search, Platform.
 They stop reading Bug at their next refresh, and their throughput and forecasts change.
                                                                  [Cancel] [Remove "Bug"]
```

---

## Wave: DISCUSS / [REF] Shared artifacts registry

| Artifact | Source of truth | Consumers | Risk |
|---|---|---|---|
| `level_names` (unit and item, singular/plural) | Level store (slice 02) | `GET /terminology/all` (same 8 keys, served from levels), every `useTerminology()` screen, onboarding stepper, Levels section | HIGH: two sources = names drift |
| `level_item_types` | Level store | Levels section, unit subset picker, Load-from-board diff, migration check | HIGH |
| `unit_type_subset` | Unit | the fetch (Tracker integration), `/teams` and `/portfolios` payload `workItemTypes`, subset picker | HIGH: any drift moves numbers |
| `pinned_numbers` | Slice 01 characterisation fixture | every invisible slice's gate, J1 | HIGH |
| unit ids | unchanged legacy ids | routes, clients, RBAC scopes, reports, snapshots, usage data | HIGH |

---

## Wave: DISCUSS / [REF] Story map and slices

Backbone: **Carry over** → **Hold the structure** → **See it** → **Shape it** → **Unify underneath**.
Walking skeleton (brownfield): 01 → 02 → 05: an upgraded demo install shows its two levels in Organisation, with
the names it had under Terminology, and every unit's numbers unchanged.

| # | Slice (≤ 1 day) | Visible? | Depends on |
|---|---|---|---|
| 01 | Pin today's numbers per unit (characterisation on demo data + an upgrade fixture) | no | — |
| 02 | Two levels seeded from today's Terminology and types; Terminology served from them | no | 01 |
| 03 | Every Team and Portfolio is a unit on its level; ids unchanged; `UnitCreated`/`UnitDeleted` | no | 02 |
| 04 | A unit reads its level's types narrowed to its subset; migration ticks today's list | no | 03 |
| 05 | Levels shown in Organisation (read-only) + `GET /levels` | **yes** | 02, **#6473 Organisation shell** |
| 06 | Rename a level's unit and item there; Terminology loses the 8 keys | **yes** | 05 |
| 07 | Leaf capacity inputs move to Forecasting's extension by unit id | no | 03 |
| 08 | Add and remove a level's item types, with impact preview | **yes** | 04, 05 |
| 09 | A unit narrows its level's types (settings and create wizards) | **yes** | 08 |
| 10 | Feature-level settings move to an extension by unit id | no | 03 |
| 11 | New-install set-up shows the two levels | **yes** | 05 |
| 12 | One refresh for every unit (leaf and Feature-level steps by level) | no | 07, 10 |
| 13 | Permissions, free-tier limits, reports and history stay with each unit | no | 03 (12 soft) |
| 14 | CLI and MCP read the levels; unknown-type 400 surfaced; major client release (M1, M9) | **yes** (clients) | 04, 05, 06 |
| 15 | "Teams only": switch the level above Teams off, Portfolios hidden with data kept (M13) | **yes** | 05, 06 (11) |

**Delivery order (M5: invisible first):** 01, 02, 03, 04, 07, 10, 12, 13, then 05 (once #6473's shell is
there), 06, 15, 08, 09, 11, 14. Slice 04's server release and slice 14's client release ship together.

### Priority rationale

1. 01 first: every later slice is gated on "numbers unchanged"; it costs a day and de-risks the whole Epic.
2. 02–04 make one source for names and types before anything shows them (else two sources ship).
3. The deep slices 07, 10, 12, 13 follow straight away (M5): none needs a visible slice or #6473, which is the
   decoupling from #6473.
4. 05 is the first visible outcome; it waits for #6473's shell (M5). The visible slices then come back to back.
5. 14 last: the client release ships with the server release that carries slice 04's 400.

Each slice's file: `slices/slice-NN-<name>.md` (hypothesis, IN/OUT, examples, UAT, AC, KPI).

---

## Wave: DISCUSS / [REF] Outcome KPIs

| Id | Who | Does what | Target | Measured by | Baseline |
|---|---|---|---|---|---|
| OUT-1 | every upgraded install's units | read the same items and show the same numbers | 100% of units identical (counts, 90-day throughput, forecast percentiles on pinned draws) on the fixture and demo data; 0 "my forecasts changed after the upgrade" reports in the 30 days after release | slice 01 harness in CI; support channel | new guard |
| OUT-2 | config-admin creating another unit on a level | picks types instead of typing or loading them | 0 typed types for a unit whose types are on the level | maintainer walk-through; DEVOPS may add a name-only event | today: every unit types or loads its list |
| OUT-3 | config-admin renaming Team/Feature words | finds them on the levels first try | 3 of 3 walk-through testers; every screen that renders the 8 terms shows the new word | walk-through; term-coverage test | today under Terminology |
| OUT-4 (leading) | developers of #6455 | add level 3 without a new unit kind | 0 places outside adapters that branch on Team vs Portfolio in Organisation set-up and the refresh | source scan / ArchUnit count | measured at DESIGN |

---

## Wave: DISCUSS / [REF] Checklist answers (no silent N/A)

- **RBAC.** No new roles. Membership stays per unit (Team members, Portfolio members); scopes keep their wire
  values (`PermissionScopeType.Team/Portfolio`), resolved through the unit and its level (slice 13). Levels
  (names, types) are edited by **System Admin**, as Terminology is today. Narrowing a unit's subset needs that
  unit's Admin, as editing its types does today. Adding a type to a level happens only in Organisation, by a
  System Admin (M4); unit forms have no add control.
  `useRbac()` gates every new control; nothing reads `my-summary` directly.
- **Licensing.** Free tier unchanged: 3 units on the leaf level, 1 on the Feature level (today's `count >
  limit` checks, re-expressed per level in slice 13). Renaming levels and editing level types are **free** (M7);
  the rest of Terminology stays Premium, so `PUT /terminology`'s Premium guard changes for the 8 moved keys. D15 (levels above are Premium) is #6455's.
- **Lighthouse-Clients (CLI/MCP).** Repo not in this checkout, so usage was not read; to verify at DESIGN.
  `/teams` and `/portfolios` keep routes, ids and payload shape (`workItemTypes` = the unit's subset), but a
  write naming a type the level lacks is now a 400 for everyone (M1): a **breaking change**, so the clients get
  a **major version bump** and a breaking-change note (confirm at DESIGN), released with a new `levels` read in
  CLI and MCP (M9, slice 14). `GET /terminology/all` keeps its 8 keys.
- **Public API.** Added: `GET /api/latest/levels`, `PUT` for a level's names and types. Kept, not deprecated:
  every `/teams` and `/portfolios` route. Behaviour change: a write carrying a type not on the level is refused
  with 400 naming the type, for everyone (M1). No route deprecated; the change is announced via slice 14.
- **Website / marketing.** N/A, because the website names Teams and Portfolios and their names do not change;
  the marketable story is #6455's.
- **Docs and screenshots** (at finalisation): `docs/settings/configuration.md` (Terminology section, new Levels
  section, `assets/settings/terminologyConfiguration.png` per theme), `docs/teams/edit.md` and
  `docs/portfolios/edit.md` ("Work Item Types"), `docs/concepts/concepts.md` (levels), the "Work Item Types"
  passages of the Jira, Azure DevOps, CSV and ServiceNow connector pages, the Overview onboarding screenshot.
- **Demo data.** Demo units carry over by the same migration; DELIVER checks the demo seeder writes the levels.
- **Usage data.** DEVOPS decides; candidates `LevelRenamed`, `LevelItemTypesChanged` (name-only).
- **Loading and failure looks.** In the sketches above for every new or changed view.
- **Terminology rule.** Copy uses the configured words via `useTerminology()`; "Feature level" in copy renders
  as "{feature} level".

---

## Wave: DISCUSS / [REF] Epic 6264 re-cut proposal (for 6264's own plan; nothing edited there)

| 6264 Story | What this Epic absorbs | What 6264 keeps |
|---|---|---|
| S03i Package Organisation set-up (#6468) | Team and Portfolio as units, their settings, Terminology/levels, `TeamDeleted` ratchet (slice 03), Portfolio deletion event | Moving blackouts, refresh cadence, Feature ordering policy and connection-default inheritance into the module. M–L → S. New unit/level code is written in the Organisation set-up module layout if S02 has settled it. |
| S03c Package Forecasting | The schema move of Feature WIP + throughput window (+ forecast filter rules) to an extension keyed by unit id (slice 07) | The behaviour-free folder move |
| S03d Package Promises | Nothing; Deliveries keep pointing at the Feature-level unit's id | Unchanged; can run before or after |
| S08 Aggregates and invariants | The Organisation set-up catalogue: Level and Unit roots, extensions by id, the "subset ⊆ level types" invariant | Every other context's catalogue |
| S10 Data ownership | Owners of Levels, Units and the two extension tables, recorded here | The rule and every other table |
| 6455's proposed R2 "Units on levels, two levels only" | All of it | Drop from 6264 |

---

## Wave: DISCUSS / [REF] Proposed SSOT and ADR updates (merge later; not applied)

Not applied in this wave only because another DISCUSS (#6473) edits the same files in parallel. They are
applied **within this Epic** (CLAUDE.md: same feature): the SSOT entries when the two DISCUSS waves are merged,
the ADR-270 and ARCHITECTURE.md §4 edits with the slice that makes them true, checked by `nw-finalize`.

`docs/product/jobs.yaml`: add `epic-6474-one-structure` to `feature_context`, and:

```yaml
  - id: job-config-admin-say-item-types-once
    title: Say once which tracker types each level holds, and let each unit only narrow them
    persona: config-admin
    feature_context: epic-6474-one-structure
    ado: "Epic #6474"
    created: 2026-11-02
    note: >
      NEW in DISCUSS of epic-6474-one-structure. Maintainer D8/D12 (6455 DISCUSS): item types are owned by the
      level and a unit only narrows them; the maintainer sees this as an improvement. Desk estimates.
    job_story: >
      When I set up many Teams and Portfolios on the same trackers,
      I want to say once which tracker types count as Work Items and as Features, and let each unit untick
      what it does not read,
      so a new unit takes a minute and no unit silently reads a type the rest of the organisation does not count.
    forces:
      push: Every Team and Portfolio types or loads its own list; nothing shows which units read which type.
      pull: The lists are mostly identical already; Load from board finds them.
      anxiety: "Will my Teams' numbers move when the list moves?" (Not on upgrade; only on an explicit, previewed removal.)
      habit: Copying a working Team's settings by hand.
    opportunity_score: {importance: 3, current_satisfaction: 2, gap: 1, rationale: "Desk estimate; preparation value dominates (D8)."}
```

`docs/product/journeys/epic-6474-one-structure.yaml`: the four journeys above (J1–J4) in the journey schema.
Personas: none new.

ADR-270:
- Decision 1, Organisation set-up row: Owns adds "Levels (two, fixed), Units (one type)"; Packaged-or-target:
  "two fixed levels: built by Epic 6474; levels above the Feature: target".
- Decision 3, pivotal events: add `LevelRenamed`, `LevelItemTypesChanged`, `UnitCreated`, `UnitDeleted`
  (Organisation set-up); `TeamDeleted` is replaced; strike "Team or Portfolio created" and "Portfolio deleted"
  from the "facts with no event" gaps.
- Decision 4, settings buckets: new row "**Owned by the level, unit narrows** | Work Item types; Feature types
  | a unit may untick, never add (D12)"; move "state mapping" from "Org default, local override" to "Local only"
  (D13); Terminology row: "Terminology (the remaining terms); level names live on the levels".
- Decision 5 glossary: Level "Target only" → "two fixed levels exist (Epic 6474); more are target"; Unit "one
  code type `Unit`"; Team, Portfolio → "seeded names of level 1 and level 2; Portfolio overload resolved into
  'unit on the Feature level'"; Terminology drift → "no notion of a level" resolved; add "Feature level: the
  level above the leaf"; Member: "per unit (wire scopes unchanged)".
- Decision 8 table: Forecasting leaf capacity "moved by Epic 6474 slice 07"; Organisation set-up "Levels, Units
  and extensions".
- Left open: "leaf unit's name" → code name Unit (M10); "leaf item's name" → settled at DESIGN of 6474.
- Licensing (Decision 1 / glossary "Licence"): level names are free to rename (System Admin); the remaining
  Terminology stays Premium (M7).

Lighthouse-Clients (for its own repo, at DESIGN/DELIVER): major version, breaking-change note "a Team or
Portfolio update naming a type its level does not list is refused" (M1), new `levels` read in CLI and MCP (M9).

ARCHITECTURE.md §4: Organisation set-up holds Level and Unit (with leaf and Feature-level extensions); Team and
Portfolio are the seeded names of the two levels; refresh is one per unit (after slice 12).

---

## Wave: DISCUSS / [REF] Definition of Ready

| Item | Status | Evidence |
|---|---|---|
| Problem in domain language | PASS | each slice opens with the admin's or developer's pain |
| Persona specific | PASS | Priya Raman (System Admin, Premium, Jira Cloud, 12 Teams/2 Portfolios), Tom Becker (Team Admin), Ana Lima (free-tier first admin), Jonas Weber (forecaster) |
| 3+ domain examples | PASS | 3 per slice, real tracker types |
| UAT 3–7 | PASS | 3–5 per slice |
| AC from UAT | PASS | per slice |
| Right-sized | PASS with flag | ≤ 1 day each; 12 and 13 flagged "may split at DESIGN" |
| Technical notes | PASS | per slice |
| Dependencies tracked | PASS | table above; #6473 named on 05 |
| Outcome KPIs | PASS | OUT-1..4, linked from slices |

---

## Wave: DISCUSS / [REF] Open questions for the maintainer

All twelve **answered on 2026-11-02**; see "Maintainer decisions" under Locked decisions.

| Q | Question | Answer |
|---|---|---|
| Q1 | API write with a type not on the level | M1: 400 for everyone; breaking client change, major bump (not the recommendation) |
| Q2 | New level type ticked for existing units? | M2: no; show "N units could read it" |
| Q3 | Removing a ticked type | M3: allowed after a confirmation naming the units |
| Q4 | Who adds types to a level | M4: System Admins, in Organisation only; no add control in unit forms (not the recommendation) |
| Q5 | Wait for #6473 or sit beside Terminology | M5: wait; invisible slices first |
| Q6 | Union ignoring case | M6: yes |
| Q7 | Level renaming Premium? | M7: free, System Admin only; other terms stay Premium (not the recommendation) |
| Q8 | Keep all slices in one Epic | M8: yes |
| Q9 | Clients `levels` read | M9: now, with the breaking client release; slice 14 (not the recommendation) |
| Q10 | Code names | M10: Unit and Level; leaf item at DESIGN |
| Q11 | Empty subsets | M11: allowed |
| Q12 | Two-tracker and Customer-A-shaped installs | M12: DESIGN, migrate as is (D10), with tests for both |

Still open for DESIGN: whether `PUT /terminology` rejects the 8 moved keys or checks the Premium guard per key
(slice 06); whether slices 12 and 13 split; the unit's id-keeping shape; the leaf item's code name.
