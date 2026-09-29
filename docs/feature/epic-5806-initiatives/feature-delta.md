<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-5806-initiatives

**Feature**: A Premium page that lists every Initiative (the level above Feature, whatever the
organisation calls it) grouped by Portfolio, and answers two questions per row: *when will all its
Features likely be done*, and *is that in time for the date we promised*.
**ADO**: this one workspace covers **two Epics** (D27).

- **Epic #5806 "Visualize Initiatives"** (release A, the page answers "when"): slice 01 = #6113,
  02 = #6114, 03 = #6115, 04 = #6116, 06 = #6118.
- **Epic #6119 "Initiative access and on-track status"** (release B): slice 07 = #6120, 09 = #6121,
  08 = #6122, 10 = #6123, 05 = #6117 (moved here from #5806).

Board state is not touched by this wave.
**Maintainer answers (2026-09-29)**: every proposal is settled. P1-P5 → D16-D20 (P1 with softer
thresholds than proposed); P6 → D21 (a dedicated Initiative scope in RBAC) and D22 (Features the reader
cannot open show only as a count); P7-P10 → D23-D26; the split into two Epics → D27.
**Waves present**: DISCOVER (desk only), DIVERGE, DISCUSS.
**Density**: lean (Tier-1 [REF] sections only).

> **Binding inputs.** The maintainer answers at the bottom of `discover/discover-summary.md` and the
> "Maintainer decisions (2026-09-29)" table at the bottom of `diverge/recommendation.md` are binding. Where
> the recommendation and that table differ, the table wins. Most visibly: the default term is
> **Initiative**, not "Parent".

---

## Wave: DISCUSS / [REF] Persona IDs

SSOT ids in `docs/product/personas/`. No persona is invented for the executive: they are the audience,
not the user (maintainer, 2026-09-29).

| Persona | Role here |
|---|---|
| `delivery-lead-rte` | **Primary.** Prepares the leadership or steering conversation. Opens the Initiatives page, sets target dates, reads status, and shows the same screen to an executive without cleaning it up. |
| `config-admin` | Renames the level under Settings → Terminology if the organisation says "Objective", "Theme" or "Epic". Touched by slice 02 only. |
| `lighthouse-maintainer` | Runs the enterprise demo. Needs demo Portfolios that carry named Initiatives of different kinds. Slice 01. |
| System Admin / scoped admin (the personas of the existing jobs `job-rbac-manage-users` and `job-rbac-scoped-admin`) | Grants Initiative access (slices 07, 08, 10); an Initiative's Admin manages its audience (slice 09). |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

All four are new and are appended to `docs/product/jobs.yaml` by this wave.

- **`job-lead-answer-when-every-initiative-lands`** (delivery-lead-rte) — When leadership or a sponsor
  asks about our strategic Initiatives, I want every Initiative in one place with the date by which all
  its Features are likely done, so I can answer on the spot with a number I can defend.
- **`job-lead-say-whether-each-initiative-is-on-track`** (delivery-lead-rte) — When an Initiative has a
  promised date, I want to see how likely it is that all its Features land by then, in a plain status
  word, so I know which Initiatives need a conversation before the meeting, not in it.
- **`job-lead-name-the-strategic-level-in-our-own-words`** (delivery-lead-rte, set by config-admin) —
  When I show Lighthouse to our leadership, I want the level above Feature to carry the word we use for
  it, so the tool reads as ours and not as built for another methodology.
- **`job-maintainer-demo-the-strategic-level`** (lighthouse-maintainer) — When I demo Lighthouse to an
  enterprise sponsor, I want the demo data to carry named Initiatives of different kinds across the
  Portfolios, so the strategic-level story can be told on the screen instead of described.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from `main` on 2026-09-29 (HEAD `b905a4751`), before any decision below.

| # | Surface | What is actually there |
|---|---|---|
| S1 | `Models/WorkItemBase.cs:23` | Every Feature carries one `ParentReferenceId` string. One level up only. |
| S2 | `Models/Feature.cs:78`, `WorkItemService.RefreshParentFeatures` (`:1066-1088`) | Parents are stored as Feature rows flagged `IsParentFeature`, fetched per Portfolio refresh through the connector's `GetParentFeaturesDetails`, and looked up by `ReferenceId` **alone**, across every connection. They belong to no Portfolio. |
| S3 | `API/FeatureReadability.cs:13-17` | "A Feature in no Portfolio is visible to everyone." Parent rows are therefore readable by any signed-in reader, and `GET /api/latest/features/references` returns them. |
| S4 | `Models/Forecast/DeliveryCompletionForecast.cs` | The joint "all these Features done" distribution for **any list of Features**: remaining work only, minimum within a Team, combined across Teams, `null` ("cannot forecast") if any contributing Team/Feature pair has no forecast. No new simulation needed. |
| S5 | `API/DTO/DeliveryWithLikelihoodDto.cs:79` | Deliveries read their dates at 70/85/95 and one likelihood for their target date through `Delivery.CalculateMetrics`, blackout days included. |
| S6 | `Lighthouse.Frontend/src/models/Delivery.ts:124-131` and `components/Common/Forecasts/ForecastLevel.ts:14-51` | Two likelihood band sets with the same cut points (50/70/85) but **different boundary rules**: Delivery uses `<` (85 is "certain"), ForecastLevel uses `<=` (85 is "Confident"). |
| S7 | `CsvWorkTrackingConnector.cs:115-120` | `GetParentFeaturesDetails` returns an empty list. CSV parents show as bare ids. |
| S8 | `CsvWorkTrackingConnector.cs:222-225` | A CSV row whose type is not one of the Portfolio's work item types is skipped. A parent row of type "Objective" in a Portfolio whose Features are "Epic" would therefore **not** become a Feature. |
| S9 | `Factories/DemoData/Project *.csv` | Five demo Portfolios, 84 Feature rows, all typed `Epic`, `Parent` column empty in every row. Altobelli and Orion hold Features with identical names (ALT-1xx / ORI-1xx). Two demo **Feature** names contain the word "Initiative" (OE-001 "Deep Sea Mapping Initiative", AP-001 "Lunar Settlement Initiative"). |
| S10 | `Services/Implementation/Seeding/TerminologySeeder.cs` | Seeds workItem(s), feature(s), team(s), portfolio(s), delivery/deliveries and others. Nothing for the level above Feature. |
| S11 | `FeatureListDataGrid/columns.tsx:246-261` | The Parent column on Portfolio and Team Feature tables: header hardcoded `"Parent"`, not sortable. |
| S12 | `components/Common/Charts/WorkDistributionChart.tsx` | Also says "No Parent", but there "parent" means a work item's **Feature**, not an Initiative. Must not be renamed by this Epic. |
| S13 | `Models/Authorization/RbacGuardRequirement.cs`, `DeliveriesController` | All gating goes through `IRbacAdministrationService.CanSatisfyRequirementAsync(User, PortfolioRead/PortfolioWrite, portfolioId)` and `GetReadablePortfolioIdsAsync`. Frontend gating through `useRbac()`. |
| S14 | `LicenseStatusDto.CanUsePremiumFeatures`, `DeliveriesController:336` | The Premium gate: backend refuses with 403, frontend shows the Premium prompt. |
| S15 | `App/Header/Header.tsx:64` | Top navigation entries are built from terminology (`getTerm(TERMINOLOGY_KEYS.FEATURES)`). The new entry slots in beside Features. |
| S17 | `Models/Authorization/PermissionScopeType.cs`, `UserRole.cs`, `IRbacAdministrationService`, `docs/settings/rbac.md` | Scopes are System, Team, Portfolio; roles SystemAdmin, TeamAdmin, PortfolioAdmin, Viewer. Grants go to users (Settings → Access → Users) or SSO groups (Group Mappings, computed from the claim on every request, no row per user). A scoped admin manages their own scope's members (`CanManagePortfolioMembershipAsync` is true for PortfolioWrite). RBAC is Premium and needs OIDC; while it is not enforced (disabled, auth off, or bootstrap) everyone has full access. There is no Initiative scope. |
| S18 | `OrphanedFeatureCleanupService.cs:17` | Stored parent rows are never cleaned up (`!f.IsParentFeature`), so an Initiative that no Feature names any more keeps its row. |
| S16 | `docs/portfolios/detail.md` | Fixed on 2026-09-29 (`b905a4751`): the removed Group by Parent toggle is gone and the Parent column is described. DISCOVER's "stale docs" finding is resolved; slice 02 re-words it with the new term. |

---

## Wave: DISCUSS / [REF] Locked Decisions

D1-D10 are binding maintainer decisions carried in. D11-D15 are decided here and follow from them.
D16-D20 are the maintainer's answers to this wave's proposals P1-P5 (2026-09-29). D21-D22 answer P6,
D23-D26 answer P7-P10, and D27 is the split into two Epics.

### D1 — A new Premium page, one table, grouped by Portfolio

Direction A. The heatmap stays a candidate for a later Epic. Features-page grouping is not built.

### D2 — The user is the Delivery Lead / RTE; the executive is the audience

The page is designed for the person preparing the answer. It must be presentable as-is: status always
written as a word next to its colour (readable in print and by a colour-blind reader), no internal ids
in headings, no jargon a sponsor would need explained.

### D3 — v1 answers "When will it land?" and "Is it on track?" and nothing else

Signals (alerts) and Reports (export, scheduled briefs) are other Epics. WIP balancing, the heatmap,
OKR linkage, trend over time and levels above the Initiative are out.

### D4 — An Initiative is whatever a Feature names as its parent, of any type

Rows are keyed by the parent reference (S1, S2). The tracker's type name may be shown as a secondary
label; it never filters rows in or out. Mixed types across Portfolios (Epics in one, Objectives in
another) are the normal case, not an edge case. No SAFe structure (portfolio Epics, PIs, ARTs) is modelled.

### D5 — The row's date and likelihood come from the existing "all Features done" composition

Reuse `DeliveryCompletionForecast` (S4) over the Initiative's Features. Never sum, average or take the
worst of per-Feature percentiles. No new simulation, and none triggered by opening the page.

### D6 — Grouped by Portfolio; an Initiative spanning Portfolios appears once and still works

Maintainer: Initiatives mostly live in one Portfolio. One whose Features sit in two or more Portfolios
appears exactly once, in a "Spans {Portfolio A}, {Portfolio B}" group, with all its Features in the
forecast.

### D7 — The target date is entered in Lighthouse and stored by Lighthouse

One target date per Initiative, keyed by its reference. Not read from the tracker in v1.

### D8 — Deliveries stay separate

No Delivery is created per Initiative. Deliveries stay hand-made milestones.

### D9 — A new configurable term, seeded "Initiative" / "Initiatives"

New Terminology key, singular and plural, renamable like every other term. The UI never hardcodes
"Initiative", "Epic", "Objective" or any tracker or framework word for this level. Docs, release notes
and UI fallbacks use "Initiative".

### D10 — Demo data carries named Initiatives of mixed types, and ships first

The feature is sold through enterprise demos. The demo cannot show it today (S7, S9), so demo readiness
is the first slice.

### D11 — Premium boundary: the page and target dates are Premium; the Initiative column stays free

Free instances see the navigation entry and the usual Premium prompt. The column on Portfolio and Team
Feature tables (renamed by D9) stays free.

### D12 — One Feature that cannot be forecast makes the row "cannot forecast", and the row says why

Same rule as Deliveries (ADR-112): a number that quietly ignores work that must still happen is not
shown. The row names the Team(s) with no forecast.

### D13 — Nothing goes silently missing

The page footer counts readable Features that have no Initiative. A parent the tracker could not
resolve still gets a row, shown by its id with "name not available". This mirrors the lesson that an
item vanishing without a warning is the worst failure.

### D14 — One level up only

Grandparents are not fetched and not shown.

### D15 — The term renames only the Feature → Initiative relationship

The column on Feature tables takes the new term. The work-distribution chart on Team pages, where
"parent" means a work item's Feature (S12), keeps its own wording.

### D16 — Status bands: On track at 70% or more, At risk 50-69%, Off track below 50%

Maintainer, 2026-09-29, choosing softer bands than the proposed 85/50. The status reads the likelihood
that all remaining Features are done by the target date:

| Status | Likelihood of meeting the target |
|---|---|
| On track | 70% or more. Exactly 70 is On track |
| At risk | 50% or more and below 70%. Exactly 50 is At risk |
| Off track | below 50%, or the target date has passed and Features are left |
| No target | no target date set |
| Unknown | the row cannot forecast (D12) |

- **Boundaries.** Each band includes its lower bound. That is the convention Deliveries already use
  (`Delivery.getLikelihoodLevel` compares with `<`), not the forecast colour bands' `<=` (S6), which put
  exactly 50 and 70 in the lower band. The comparison is made on the whole-percent number the reader
  sees, so a row showing "70%" never reads At risk.
- **The status no longer follows from the date column.** The row shows the 85% date (D20), but On track
  starts at 70%. An Initiative can therefore be On track while its 85% date is after the target. No
  criterion may assume "On track means the 85% date is on or before the target"; the status is read
  from the likelihood alone.

### D17 — An Initiative is done when every Feature Lighthouse knows under it is done

The parent's own state in the tracker is ignored: parent states are in no Portfolio's state mapping.
An Initiative closed in the tracker with open Features still shows and still forecasts, because the work
still exists. (Was proposal P2.)

### D18 — The CSV connector resolves Initiative names from rows in the same file, as a product change

A row whose ID another row names as its parent, and whose type is not one of the Portfolio's Feature
types, is that parent. This serves CSV users as well as the demo, and is documented. (Was proposal P3.)

### D19 — Customer data readiness does not block building or shipping; release copy waits for a count

Before release notes, website copy or pricing claims are written, count the share of open Features with
a resolvable Initiative on the dev instance and on 3 friendly Premium instances. The page itself ships
without waiting, because it degrades honestly (D13). (Was proposal P4.)

### D20 — The row shows the 85% date; the 70% and 95% dates on hover, as Deliveries do

The same three percentiles a Delivery computes. No percentile selector in v1. (Was proposal P5.)

### D21 — Initiatives get their own RBAC scope, with their own read and write grants, in v1

Maintainer, 2026-09-29. Access to an Initiative is granted on the Initiative itself, not borrowed from
the Portfolios its Features sit in. It mirrors how Portfolio grants work today (S17): a read grant
(Viewer) and a write grant (Admin), given to a user under Settings → Access → Users or to an SSO group
under Group Mappings, merging into the user's effective permissions like any other grant. Setting an
Initiative's target date needs the write grant. All checks go through `IRbacAdministrationService`;
the UI gates only through `useRbac()`. Its semantics are D23-D26.

### D22 — An Initiative's numbers cover all its Features; Features the reader cannot open are only counted

Maintainer, 2026-09-29 (was the first half of proposal P6). The date, likelihood and status always
cover every Feature of the Initiative, because a number over a subset would quietly ignore work that
still has to happen (the same reason as D12). A Feature in a Portfolio the reader cannot open appears
only as a count ("2 more {features} in a {portfolio} you cannot open"); its name, state and Teams are
never shown.

### D23 — A Portfolio reader sees that Portfolio's Initiatives without any Initiative grant

Every Initiative with at least one Feature in a Portfolio the reader can read is visible to them. An
Initiative grant only adds Initiatives; it never removes one. Neither a Portfolio read nor an Initiative
grant (Viewer or Admin) reveals Features in Portfolios the reader cannot open; those stay a count (D22).
This is what lets Epic #5806 ship safely before any Initiative grant exists. (Was proposal P7.)

### D24 — Who can grant Initiative rights

A System Admin grants Viewer and Admin on any Initiative, to users and to SSO groups. An Initiative's
Admin grants Viewer and Admin on that Initiative only, and to users only. A Portfolio Admin of the
Initiative's Portfolios gets no Initiative right from that role. This mirrors today, where a Portfolio
Admin manages their Portfolio's members and group mappings stay with the System Admin. (Was proposal P8.)

### D25 — Grants, like target dates, are keyed by the tracker reference and never removed automatically

While no Feature names an Initiative, its grants are kept, marked "not in current data" in Settings →
Access, show the reader nothing, and can be removed by an admin. When the Initiative returns under the
same reference, they apply again. DESIGN decides whether the key also carries the connection, for grants
and target dates alike (risk S2). (Was proposal P9.)

### D26 — Without RBAC enforced, nothing new appears and nothing is restricted

With RBAC disabled, authentication off, or before bootstrap, everyone sees every Initiative and may set
target dates (the page and targets still need Premium). Initiative grants are only offered where other
grants are offered. Grants made while RBAC was enforced are kept if it is switched off or the licence
lapses. (Was proposal P10.)

### D27 — Two Epics, one workspace

The re-run scope assessment found the work oversized. The maintainer split it into two Epics rather than
two releases of one: **#5806 "Visualize Initiatives"** (slices 01, 02, 03, 04, 06) answers "when", and
**#6119 "Initiative access and on-track status"** (slices 07, 09, 08, 10, 05) adds the Initiative scope
and then the target date and status. Story #6117 (slice 05) moved from #5806 to #6119. This workspace
stays single and covers both; every slice states its Epic. Consequence: in #5806 nothing has a target
date, so slice 06's expander shows dates only; its likelihood column is added by slice 05 in #6119.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS for each Epic after the split (D27).** History: the first assessment passed with one signal;
adding the Initiative scope (D21) made the combined work trip two signals, so it was split.

| Signal | Combined (before split) | Epic #5806 | Epic #6119 |
|---|---|---|---|
| Stories (limit 10) | 10, at the limit | 5 | 5 |
| Modules or bounded contexts (limit 3) | 5 — **fires** | 4: CSV connector, terminology, Initiative read model, frontend — **fires** | 3: authorization, Initiative read model (target date storage), frontend and Access settings |
| Walking skeleton integration points (limit 5) | 4 | 4 (nav, API, RBAC read, licence gate) | 3 (Access settings, RBAC check, page) |
| Effort (limit 2 weeks) | ~6.5 days | ~24h, about 3 days | ~27h, about 3.5 days |
| Independent outcomes that could ship separately | 2 — **fires** | 1: the page answers "when" | 1: access, then "on track"; the target date needs the write grant, so they are not separable |
| **Verdict** | Oversized (2 signals) | **PASS** (1 signal) | **PASS** (0 signals) |

Epic #5806 ships safely on its own because of D23: without any Initiative grant, visibility follows
Portfolio read.

---

## Wave: DISCUSS / [REF] User Stories

Persona examples use demo data after slice 01 (see slice brief for the full demo table). Maria Keller is
the RTE of the "Ocean Explorer" train; Tomás Ribeiro is a Viewer on Project Ocean Explorer only; Aiko
Tanaka is a System Admin.

### US-01 — The demo Portfolios carry named Initiatives of different kinds

**Job**: `job-maintainer-demo-the-strategic-level` · **Persona**: lighthouse-maintainer (secondary:
delivery-lead-rte on a CSV instance) · **Slice**: 01

#### Problem

Benjamin demos Lighthouse to enterprise sponsors. Every demo Portfolio's Initiative column reads "No
Parent" on all 84 Features, and a CSV upload that names a parent shows only its bare id, so the
strategic level cannot be shown at all.

#### Elevator Pitch

Before: open Portfolios → Project Ocean Explorer → Features and the Parent column says "No Parent" on
every row.
After: the same column reads "Healthy Oceans 2027" or "Blue Planet Knowledge", linked, and Project
Apollo's reads "Humans on Mars" (an Objective); a CSV upload that lists its Initiatives as rows shows
their names the same way.
Decision enabled: whether the enterprise demo can lead with the strategic level, and whether a CSV user
keeps their Initiatives in the file.

#### Domain examples

1. Ocean Explorer: OE-002 "Coral Reef Restoration Program" shows "Healthy Oceans 2027" (type Initiative).
2. Orion and Altobelli: ORI-104 and ALT-104 "Deep Space Navigation Enhancements" both show "Deep Space
   Readiness" (type Theme), one Initiative spanning two Portfolios.
3. A CSV user's file names parent "GOAL-9" in a row, but no row "GOAL-9" exists: the column shows
   "GOAL-9" as today, and the refresh succeeds.

#### UAT scenarios

```gherkin
Scenario: Demo Features show the name of the Initiative they belong to
  Given the demo data is loaded
  When Maria opens Project Ocean Explorer's Features
  Then "Coral Reef Restoration Program" shows the Initiative "Healthy Oceans 2027"
  And "Tidal Energy Harvesting" shows that it has no Initiative

Scenario: A CSV file names its Initiatives as rows of their own
  Given a CSV Portfolio whose Features are typed "Epic"
  And the file holds a row "OBJ-1 Humans on Mars" typed "Objective" that "AP-005" names as its parent
  When the Portfolio refreshes
  Then "Mars Colonization" shows the Initiative "Humans on Mars"
  And "Humans on Mars" is not listed as one of the Portfolio's Features

Scenario: A parent that is not in the file is shown by its id
  Given a CSV row names parent "GOAL-9" and no row "GOAL-9" exists
  When the Portfolio refreshes
  Then the refresh completes and the Feature shows "GOAL-9" as its Initiative
```

#### Acceptance Criteria

- **AC-1.1** — In a CSV Portfolio, a row whose ID another row names as its parent, and whose type is not
  one of the Portfolio's work item types, resolves as that parent: its name and URL (if given) appear
  wherever the parent is shown. The row does not become a Feature of the Portfolio.
- **AC-1.2** — A parent id that names no row in the file keeps today's behaviour (bare id), and the
  refresh does not fail.
- **AC-1.3** — Each of the five demo Portfolios holds 1-3 named Initiatives, typed with at least three
  different type names across the demo (for example Initiative, Objective, Theme), none of them `Epic`
  (the demo Features' own type).
- **AC-1.4** — One demo Initiative has Features in both Orion and Altobelli, under the same id and name
  in both files.
- **AC-1.5** — One demo Initiative has only Done Features; at least one Portfolio keeps Features with no
  Initiative.
- **AC-1.6** — No existing demo Feature is renamed, re-typed, re-ordered or moved to another state; the
  existing Playwright suite passes unchanged.
- **AC-1.7** — No demo Initiative name equals or contains a demo Feature name, and none contains the word
  "Initiative" (two demo Feature names already do, S9).

#### Technical notes

CSV connector change is a product change, not demo-only (D18). Parent rows are read regardless
of the Portfolio's type filter. Demo-data changes have broken unrelated E2E before (`docs/ci-learnings.md`
2026-07-11, 2026-06-14, 2026-08-21): run the full Playwright suite on demo data before commit.

---

### US-02 — The level above Feature carries our own word

**Job**: `job-lead-name-the-strategic-level-in-our-own-words` · **Persona**: config-admin (reader:
delivery-lead-rte) · **Slice**: 02

#### Problem

The Feature tables head the column "Parent", a word no organisation uses for its strategic level, and
nothing can rename it. An OKR shop shown "Epics", or a SAFe shop shown "Initiatives", reads the tool as
built for someone else.

#### Elevator Pitch

Before: the Feature table on a Portfolio page heads its column "Parent" and nothing renames it.
After: Settings → Terminology lists "Initiative / Initiatives"; the column on Portfolio and Team pages
reads "Initiative"; rename it to "Objective" and the column reads "Objective".
Decision enabled: whether Lighthouse can be put in front of this organisation's leadership in its own
language.

#### Domain examples

1. Fresh install: the column reads "Initiative", empty cells "No Initiative".
2. Aiko renames the term to "Objective" / "Objectives": the column reads "Objective", empty cells "No
   Objective".
3. On Team Equinox's page the work-distribution chart still says "No Parent" for work items without a
   Feature, because that is a different relationship (D15).

#### UAT scenarios

```gherkin
Scenario: A fresh instance calls the level "Initiative"
  Given a new Lighthouse instance with demo data
  When Maria opens Project Ocean Explorer's Features
  Then the column naming each Feature's parent is headed "Initiative"

Scenario: An organisation renames the level to its own word
  Given Aiko renames "Initiative" to "Objective" and "Initiatives" to "Objectives"
  When Maria opens Team Equinox's Features
  Then the column is headed "Objective" and a Feature without one reads "No Objective"

Scenario: An upgraded instance keeps every term it already renamed
  Given an instance that renamed "Feature" to "Epic" before this release
  When it upgrades
  Then "Feature" still reads "Epic" and "Initiative" is added with its seeded default
```

#### Acceptance Criteria

- **AC-2.1** — Settings → Terminology lists new singular and plural entries seeded "Initiative" and
  "Initiatives", described as the level above Feature.
- **AC-2.2** — Upgrading an instance adds the two entries and changes no other term.
- **AC-2.3** — The Feature table column on Portfolio and Team pages is headed by the singular term; a
  Feature with none reads "No {initiative}".
- **AC-2.4** — Renaming the term changes that header and empty text on the next page load.
- **AC-2.5** — The Team work-distribution chart's wording is unchanged (D15).

#### Technical notes

Seeder plus frontend terminology keys. Docs: `docs/settings/configuration.md` (Terminology list),
`docs/portfolios/detail.md` (column now "Initiative").

---

### US-03 — Every Initiative on one page, grouped by Portfolio

**Job**: `job-lead-answer-when-every-initiative-lands` · **Persona**: delivery-lead-rte · **Slice**: 03

#### Problem

Maria is asked in a steering meeting which strategic Initiatives are in flight. To list them she opens
each Portfolio in turn and reads the Initiative column row by row, or keeps a spreadsheet. She finds it
slow and never sure she has them all.

#### Elevator Pitch

Before: listing the Initiatives means opening each Portfolio and reading the Initiative column row by
row.
After: click "Initiatives" in the top navigation and see every Initiative grouped by Portfolio, each
with "0 of 5 Features done" and a link to the tracker, and a footer counting the Features that have no
Initiative.
Decision enabled: which Initiatives go on the review agenda, and whether the Initiative level in our
data is complete enough to rely on.

#### Domain examples

1. Maria (System Admin view) sees Ocean Explorer: "Healthy Oceans 2027 — 0 of 5 Features done",
   "Blue Planet Knowledge — 0 of 4"; Apollo: "Humans on Mars — 0 of 3"; and a group "Spans Altobelli,
   Orion: Deep Space Readiness — 0 of 4".
2. Tomás, Viewer on Ocean Explorer only, sees Ocean Explorer's two Initiatives and nothing from Apollo.
3. A free (unlicensed) instance: the "Initiatives" entry is there; the page shows the Premium prompt.

#### UAT scenarios

```gherkin
Scenario: Every Initiative is listed once, under the Portfolio it lives in
  Given the demo data with a Premium licence
  When Maria opens Initiatives
  Then she sees "Healthy Oceans 2027" under "Project Ocean Explorer" with "0 of 5 Features done"
  And "Deep Space Readiness" appears once, under "Spans Altobelli, Orion"

Scenario: Features without an Initiative are counted, not hidden
  Given 4 of Ocean Explorer's Features name no Initiative
  When Maria opens Initiatives
  Then the footer counts those Features as having no Initiative

Scenario: A Portfolio-scoped Viewer sees only what they can read
  Given Tomás is a Viewer on Project Ocean Explorer only
  When Tomás opens Initiatives
  Then he sees Ocean Explorer's Initiatives and no Apollo Initiative

Scenario: Without a licence the page offers Premium
  Given an instance without a Premium licence
  When Maria opens Initiatives
  Then she sees the Premium prompt and no Initiative rows
```

#### Acceptance Criteria

- **AC-3.1** — A top-navigation entry named by the plural term opens a page with one row per Initiative
  that has at least one Feature in a Portfolio the reader can read.
- **AC-3.2** — Rows are grouped under their Portfolio. An Initiative with Features in two or more
  Portfolios appears exactly once, in a "Spans {portfolio}, {portfolio}" group.
- **AC-3.3** — Each row shows the name linked to the tracker (or the id and "name not available"), the
  tracker's type as a secondary label, and "{done} of {total} {features} done".
- **AC-3.4** — The footer counts readable Features, in readable Portfolios, whose parent is empty.
- **AC-3.5** — By default Initiatives whose Features are all done are hidden; a "Show: All" choice shows
  them marked "All done" (D17).
- **AC-3.6** — Without a Premium licence the navigation entry shows and the page shows the Premium
  prompt; the API refuses with 403.
- **AC-3.7** — Before slice 07, a row appears when the reader can read at least one Portfolio holding
  its Features (D23). Its counts and numbers cover all its Features (D22); Features in
  Portfolios the reader cannot read are never named. Slice 07 adds visibility through an Initiative grant.

#### Technical notes

New read endpoint; one request returns all rows. RBAC through `IRbacAdministrationService` and
`useRbac()`, no direct fetch of `my-summary`. Pre-existing risk for DESIGN: parents are matched by
reference id alone across connections (S2), so two trackers using the same id would merge.

---

### US-04 — Each Initiative says when all its Features are likely done

**Job**: `job-lead-answer-when-every-initiative-lands` · **Persona**: delivery-lead-rte · **Slice**: 04

#### Problem

"When will Healthy Oceans land?" has no answer today unless Maria builds a rule-based Delivery for that
Initiative by hand and keeps it alive next to the real releases.

#### Elevator Pitch

Before: a date for an Initiative needs a hand-built rule-based Delivery per Initiative.
After: each row on the Initiatives page reads "Likely by 12 Dec 2026 (85%)", with the 70% and 95% dates
on hover, or "Cannot forecast: Team Zenith has no throughput".
Decision enabled: answering leadership on the spot with a date computed from the Teams' real history.

#### Domain examples

1. "Healthy Oceans 2027": five Features, two of them worked by more than one Team; the row reads
   "Likely by {date} (85%)", the same date a rule-based Delivery on that parent shows.
2. "Deep Space Readiness" spans Orion and Altobelli: one date over all four Features.
3. An Initiative with one Feature on a Team with no throughput: "Cannot forecast: {team} has no
   throughput", no date.

#### UAT scenarios

```gherkin
Scenario: An Initiative's date is the date all of its Features are likely done
  Given "Healthy Oceans 2027" groups five Features, some worked by more than one Team
  And a rule-based Delivery selecting the same five Features
  When Maria opens Initiatives
  Then the row's 85% date equals that Delivery's 85% date

Scenario: One Feature that cannot be forecast stops the row from guessing
  Given one Feature of an Initiative belongs to a Team with no throughput
  When Maria opens Initiatives
  Then the row reads "Cannot forecast" and names that Team

Scenario: Opening the page does not start a forecast
  Given forecasts were last updated at 07:40
  When Maria opens Initiatives
  Then no forecast runs and the page states "Forecasts updated 07:40"
```

#### Acceptance Criteria

- **AC-4.1** — Each row shows the 85% date of "all its remaining Features done", with the 70% and 95%
  dates available on hover (D20).
- **AC-4.2** — For the same Features the row's dates equal those of a Delivery holding them, through the
  same composition (D5); a test asserts the identity.
- **AC-4.3** — Any contributing Feature that cannot be forecast makes the row "Cannot forecast" and
  names the Team(s); no date is shown (D12).
- **AC-4.4** — Done Features do not contribute; an Initiative with all Features done shows "All done"
  and no forecast.
- **AC-4.5** — Opening the page runs no simulation. On the dev instance, the request for all rows
  answers in under 1 second for 50 Initiatives and 300 Features. The page shows the oldest forecast time
  among the rows.
- **AC-4.6** — Dates skip blackout days exactly as Delivery dates do.

#### Technical notes

Reuse `DeliveryCompletionForecast.Build` and the Delivery metrics path; no new maths.

---

### US-05 — Each Initiative says whether it is on track for its target date

**Job**: `job-lead-say-whether-each-initiative-is-on-track` · **Persona**: delivery-lead-rte ·
**Slice**: 05 · **Epic**: #6119 (moved from #5806, D27)

#### Problem

No Initiative carries a promised date anywhere in Lighthouse, so "is it on track?" is answered by gut
feel, and the at-risk Initiative is discovered in the meeting rather than before it.

#### Elevator Pitch

Before: no Initiative has a promised date in Lighthouse, so "is it on track?" is answered by feel.
After: on the Initiatives page, click "Set target" on "Humans on Mars", pick 31 Mar 2027, and the row
reads "At risk · 62%" beside the date.
Decision enabled: which Initiatives need a scope, order or date conversation before the steering
meeting.

#### Domain examples

1. Maria sets 31 Dec 2026 on "Healthy Oceans 2027"; likelihood 96% → "On track".
2. "Humans on Mars" target 31 Mar 2027; likelihood 62% → "At risk".
3. Tomás (Viewer on Ocean Explorer, no Initiative write grant) sees targets and statuses but no
   "Set target" control; Maria holds the Admin grant on "Healthy Oceans 2027" and sets it.

#### UAT scenarios

```gherkin
Scenario: Setting a target date shows how likely the Initiative is to make it
  Given "Humans on Mars" has no target date
  When Maria sets its target to 31 Mar 2027
  Then the row shows the likelihood that all its Features are done by then
  And a status word beside it

Scenario: A reader without the Initiative's write grant sees the status but cannot change the date
  Given Tomás is a Viewer on Project Ocean Explorer and holds no grant on "Healthy Oceans 2027"
  When Tomás opens Initiatives
  Then he sees "Healthy Oceans 2027" with its target and status
  And no control to set or clear a target

Scenario: A passed target with work remaining is off track
  Given "Blue Planet Knowledge" had a target of 1 Sep 2026 and has Features left
  When Maria opens Initiatives on 29 Sep 2026
  Then the row reads "Off track"
```

#### Acceptance Criteria

- **AC-5.1** — A permitted user sets, changes and clears an Initiative's target date on its row; it is
  kept across refreshes and restarts, and a Portfolio refresh never clears it.
- **AC-5.2** — The likelihood shown is that all remaining Features are done by the target date, the same
  number a Delivery with those Features and that date shows.
- **AC-5.3** — Status follows D16, read from the whole-percent likelihood shown: 70% or more On track,
  50-69% At risk, below 50% (or target passed with Features left) Off track, No target when unset, Unknown
  when the row cannot forecast. Exactly 70 is On track and exactly 50 is At risk. The word is always
  written next to the colour.
- **AC-5.4** — Only a System Admin or a holder of the Initiative's write grant (D21, slice 09) sees the
  set/clear control; everyone else who can see the row sees target and status only, and the API refuses
  their write. A Portfolio Admin of the Initiative's Portfolios has no write right from that alone.
- **AC-5.5** — Without a Premium licence setting a target is refused with 403.
- **AC-5.6** — The status is taken from the likelihood alone, never from comparing the 85% date with the
  target: a row with a 78% likelihood reads On track even though its 85% date falls after the target.
- **AC-5.7** — Once a target is set, the row's expander (slice 06) adds each Feature's own likelihood by
  the target, labelled "this one alone", and the closing line adds the joint likelihood ("All 5 Features
  by 31 Dec 2026: 88%"); nothing is summed or averaged.

#### Technical notes

New stored value keyed by Initiative reference; expand-only EF migration via `CreateMigration`.

---

### US-06 — Open an Initiative to see which Feature carries its date

**Job**: `job-lead-answer-when-every-initiative-lands` · **Persona**: delivery-lead-rte · **Slice**: 06
(severable) · **Epic**: #5806

In Epic #5806 no Initiative has a target date (target and status are slice 05, Epic #6119, D27), so the
expander shows **dates only, no status and no likelihood**. Slice 05 adds the "this one alone"
likelihood column to it (AC-5.7).

#### Problem

The row says "Likely by 12 Mar 2027", and the executive's next question is "why so late?". Maria has to
leave the page and open each Feature to find the one that carries the date.

#### Elevator Pitch

Before: an Initiative's row gives one date and does not say which of its Features pushes it out.
After: click the row's expander and see each Feature with its Teams, state and own 85% date, ending with
"All 5 Features likely by 12 Mar 2027 (85%)".
Decision enabled: which Feature to discuss, re-order or cut.

#### Domain examples

1. "Deep Space Readiness" expands to ORI-104, ORI-105, ALT-104, ALT-105, each with its own date.
2. The closing line repeats the row's joint date, which can be later than every single Feature's date;
   it is never the latest or an average of them.
3. Tomás expands "Deep Space Readiness" without Orion access: Altobelli's Features are listed, and "2
   more Features in a Portfolio you cannot open".

#### UAT scenarios

```gherkin
Scenario: The Feature that carries the date is visible inside the Initiative
  Given "Healthy Oceans 2027" is likely done by 12 Mar 2027
  When Maria expands the row
  Then each Feature shows its own 85% date
  And the last line repeats that all of them are likely done by 12 Mar 2027

Scenario: Features in Portfolios the reader cannot open are counted, not named
  Given Tomás cannot read Project Orion
  When Tomás expands "Deep Space Readiness"
  Then Orion's Features are counted but not named

Scenario: The expander shows dates and nothing else
  Given "Blue Planet Knowledge" is listed on the Initiatives page
  When Maria expands the row
  Then each Feature shows its Teams, state and own 85% date, and no status or likelihood
```

#### Acceptance Criteria

- **AC-6.1** — Expanding a row lists its readable Features with Teams, state and own 85% date. It shows
  no status and no likelihood.
- **AC-6.2** — The closing line repeats the row's joint 85% date; no Feature date is summed, averaged or
  taken as the maximum to produce it.
- **AC-6.3** — Each Feature name opens that Feature's detail page.
- **AC-6.4** — Features in unreadable Portfolios are counted, never named (D22).
- **AC-6.5** — Removing this slice leaves US-03 and US-04 whole.

#### Technical notes

The data grid is the MIT tier; an expandable row is hand-built (as noted in Epic 6033).

---

### US-07 — Share one Initiative with someone who cannot open its Portfolios

**Job**: `job-rbac-manage-users` · **Persona**: System Admin (Aiko Tanaka) granting; reader Jonas Weber ·
**Slice**: 07

#### Problem

Jonas Weber leads the "Deep Space Readiness" Initiative, whose Features sit in Orion and Altobelli. For
him to see its status, Aiko must make him a Viewer on both Portfolios, which shows him every other
Feature in them. She finds it too wide, so Jonas gets a screenshot instead.

#### Elevator Pitch

Before: letting someone see one Initiative means making them a Viewer on every Portfolio its Features
sit in.
After: Settings → Access → Users → edit Jonas → role Viewer, scope "Initiative: Deep Space Readiness";
Jonas opens Initiatives and sees that one row with its date, its four Features shown only as a count.
Decision enabled: whether an Initiative's status can be shared with its lead or sponsor without opening
the Portfolios to them.

#### Domain examples

1. Jonas, Viewer on "Deep Space Readiness" only: one row, "Likely by {date}", "4 {features} in
   {portfolios} you cannot open".
2. Tomás, Viewer on Ocean Explorer and now also Viewer on "Humans on Mars" (Apollo): sees Ocean
   Explorer's Initiatives by default (D23) plus "Humans on Mars", and Apollo's Features only as a count.
3. Aiko removes Jonas's grant: on his next page load the page shows no rows and says he has access to no
   {initiatives}.

#### UAT scenarios

```gherkin
Scenario: A reader granted one Initiative sees exactly that Initiative
  Given Jonas holds only a Viewer grant on "Deep Space Readiness"
  When Jonas opens Initiatives
  Then he sees "Deep Space Readiness" with its date covering all four of its Features
  And its Features are shown only as a count
  And no other Initiative, Portfolio or Team is visible to him anywhere

Scenario: An Initiative grant adds to what a Portfolio reader already sees
  Given Tomás is a Viewer on Project Ocean Explorer and on the Initiative "Humans on Mars"
  When Tomás opens Initiatives
  Then he sees Ocean Explorer's Initiatives and "Humans on Mars"

Scenario: Removing the grant removes the Initiative
  Given Aiko removes Jonas's grant on "Deep Space Readiness"
  When Jonas opens Initiatives
  Then he sees no Initiatives and a message that none are shared with him
```

#### Acceptance Criteria

- **AC-7.1** — When granting Viewer to a user, the scope choice offers Initiatives by name, with their
  tracker type as a secondary label, beside Teams and Portfolios.
- **AC-7.2** — A user whose only grant is Viewer on an Initiative sees exactly that Initiative on the
  Initiatives page; its date and status cover all its Features, which appear only as a count unless the
  user can read their Portfolio (D22).
- **AC-7.3** — That grant shows no Portfolio, Team, Feature detail or other Initiative anywhere else.
- **AC-7.4** — Removing the grant removes the row on the next page load.
- **AC-7.5** — The check runs in the API through `IRbacAdministrationService`; the page reads it through
  `useRbac()`; no component fetches `my-summary`.
- **AC-7.6** — Without RBAC enforced, nothing changes for anyone (D26).

#### Technical notes

A new scope type beside System, Team and Portfolio; the grant is keyed per D25. Expand-only migration via
`CreateMigration`.

---

### US-08 — Share an Initiative with an SSO group

**Job**: `job-rbac-manage-users` · **Persona**: System Admin (Aiko Tanaka) · **Slice**: 08

#### Problem

Aiko manages access through the identity provider's groups. The steering group for the space programme
changes every quarter; granting each sponsor one by one is the manual work group mappings exist to
avoid.

#### Elevator Pitch

Before: Initiative access can only be granted user by user.
After: Settings → Access → Group Mappings → Add → group "space-steering", role Viewer, scope
"Initiative: Deep Space Readiness"; everyone in that group sees it on their next request.
Decision enabled: whether Initiative access can follow the organisation's own steering groups.

#### Domain examples

1. Group "space-steering" → Viewer on "Deep Space Readiness"; Jonas and two sponsors see the row.
2. Group "ocean-leads" → Admin on "Healthy Oceans 2027"; Maria is in it and can set its target (slice 05).
3. Jonas has a direct Viewer grant and is in a group mapped to Admin on the same Initiative: Admin wins.

#### UAT scenarios

```gherkin
Scenario: Members of a mapped group see the Initiative
  Given the group "space-steering" is mapped to Viewer on "Deep Space Readiness"
  When a member of "space-steering" opens Initiatives
  Then they see "Deep Space Readiness"

Scenario: A group-mapped Initiative grant behaves exactly like a direct one
  Given Jonas is a direct Viewer and his group is mapped to Admin on the same Initiative
  When Jonas opens that Initiative
  Then he has the Admin grant's rights

Scenario: Removing the mapping takes the access away
  Given the mapping for "space-steering" is removed
  When a member opens Initiatives
  Then "Deep Space Readiness" is no longer shown to them
```

#### Acceptance Criteria

- **AC-8.1** — Group Mappings offer an Initiative scope for the Viewer and Admin roles.
- **AC-8.2** — A mapped group's members get the grant on their next request; no per-user row is created.
- **AC-8.3** — When a direct grant and a group grant overlap on the same Initiative, the higher role wins.
- **AC-8.4** — Removing or changing the mapping takes effect on the next request.

#### Technical notes

Same mapping mechanism as Team and Portfolio scopes (S17).

---

### US-09 — An Initiative's Admin decides who can see it

**Job**: `job-rbac-scoped-admin` · **Persona**: Maria Keller as Admin of "Healthy Oceans 2027" · **Slice**: 09

#### Problem

Once Initiatives are shared one by one, every change goes through Aiko. Maria owns "Healthy Oceans 2027"
and knows who should see it, but has to file a request each time a sponsor changes.

#### Elevator Pitch

Before: only a System Admin can change who sees an Initiative.
After: Aiko makes Maria Admin of "Healthy Oceans 2027"; on the Initiatives page Maria opens that row's
"Access" action and adds Tomás as Viewer, with no request to Aiko.
Decision enabled: who owns an Initiative's audience, and therefore who may promise its date (the write
grant slice 05 checks).

#### Domain examples

1. Maria, Admin of "Healthy Oceans 2027", adds Tomás as Viewer and later removes him.
2. Maria opens "Access" on "Humans on Mars", where she holds only Viewer: the action is not offered.
3. Maria, Admin of "Healthy Oceans 2027" but not a reader of Apollo, still sees Apollo Features of any
   Initiative only as a count.

#### UAT scenarios

```gherkin
Scenario: An Initiative Admin shares their Initiative
  Given Maria is Admin of "Healthy Oceans 2027"
  When Maria adds Tomás as Viewer from the Initiative's Access action
  Then Tomás sees "Healthy Oceans 2027" on his next page load

Scenario: An Initiative Admin cannot manage another Initiative
  Given Maria is only a Viewer of "Humans on Mars"
  When Maria opens Initiatives
  Then "Humans on Mars" offers her no Access action

Scenario: Admin of an Initiative is not access to its Portfolios
  Given Maria is Admin of "Deep Space Readiness" and cannot read Project Orion
  When Maria opens that Initiative
  Then Orion's Features are shown only as a count
```

#### Acceptance Criteria

- **AC-9.1** — A System Admin can grant the Admin role on an Initiative to a user.
- **AC-9.2** — An Initiative's Admin can add, change and remove that Initiative's Viewers and Admins;
  nothing else, and to users only (D24).
- **AC-9.3** — The Access action is shown only to a System Admin or that Initiative's Admin, gated through
  `useRbac()`; the API refuses anyone else.
- **AC-9.4** — The Admin grant gives no read of the Portfolios, Teams or Features under the Initiative
  beyond the count (D22).
- **AC-9.5** — The Admin grant is the write right that setting a target date requires (AC-5.4).

#### Technical notes

Mirrors `CanManagePortfolioMembershipAsync` (S17) for the new scope.

---

### US-10 — The Access settings show every Initiative grant, even for Initiatives gone from the data

**Job**: `job-rbac-manage-users` · **Persona**: System Admin (Aiko Tanaka) · **Slice**: 10

#### Problem

Initiatives come and go with the tracker's data. Aiko must be able to see who holds which Initiative
grant, and must not lose, or silently keep, access to an Initiative that left the data and came back.

#### Elevator Pitch

Before: nothing in Settings → Access mentions Initiatives.
After: Settings → Access → Users shows "Jonas Weber — Viewer · Initiative: Deep Space Readiness"; when no
Feature names that Initiative any more, the grant reads "Deep Space Readiness — not in current data",
still removable, and working again the day the Initiative returns.
Decision enabled: whether to keep or revoke access to an Initiative that has left the data.

#### Domain examples

1. Jonas's row lists his Initiative grant by name and type.
2. Orion's Features stop naming "Deep Space Readiness": the grant is marked "not in current data".
3. The Initiative returns under the same tracker reference: the grant works again without re-granting.

#### UAT scenarios

```gherkin
Scenario: An admin sees Initiative grants by name
  Given Jonas holds a Viewer grant on "Deep Space Readiness"
  When Aiko opens Settings, Access, Users
  Then Jonas's row lists "Viewer" on the Initiative "Deep Space Readiness"

Scenario: A grant on an Initiative that left the data is kept and marked
  Given no Feature names "Deep Space Readiness" any more
  When Aiko opens Settings, Access, Users
  Then Jonas's grant reads "not in current data" and can be removed

Scenario: The grant works again when the Initiative returns
  Given the Initiative returns under the same tracker reference
  When Jonas opens Initiatives
  Then he sees "Deep Space Readiness" again
```

#### Acceptance Criteria

- **AC-10.1** — Users and Group Mappings list Initiative grants by Initiative name and type.
- **AC-10.2** — A grant whose Initiative no Feature names any more is kept and marked "not in current
  data" (D25); it can be removed like any other grant.
- **AC-10.3** — The same grant takes effect again when the Initiative returns under the same tracker
  reference; the target date behaves the same way.
- **AC-10.4** — A grant on an Initiative not in current data shows the reader nothing.

#### Technical notes

Stored parent rows are never cleaned up (S18), which makes "left the data" a question of whether any
Feature names it, not of whether the row exists.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone**: make the level visible → name it → list it → date it → explain it → share it → judge it.

Slice numbers are identities, not delivery order: 01-06 were mapped to ADO Stories first, so the access
slices added after D21 are numbered 07-10 rather than renumbering the others. Two Epics (D27).

| Order | Epic | Slice | Story | ADO | Ships | Depends on | Estimate |
|---|---|---|---|---|---|---|---|
| 1 | #5806 | 01 | US-01 | #6113 | CSV parent rows resolve to names; demo Portfolios carry mixed-type Initiatives | — | ~5h |
| 2 | #5806 | 02 | US-02 | #6114 | New Terminology term, used by the Feature table column | — | ~3h |
| 3 | #5806 | 03 | US-03 | #6115 | Initiatives page: nav, grouping, done/total, footer, Premium, Portfolio-derived visibility (D23) | 02 (01 for demo) | ~7h |
| 4 | #5806 | 04 | US-04 | #6116 | Likely-by date per row, "cannot forecast" reason | 03 | ~5h |
| 5 | #5806 | 06 | US-06 | #6118 | Row expands to its Features, dates only, no status (severable) | 04 | ~4h |
| 6 | #6119 | 07 | US-07 | #6120 | Initiative scope; Viewer grant to a user; enforced on the page | Epic #5806 slice 03 | ~7h |
| 7 | #6119 | 09 | US-09 | #6121 | Admin grant on an Initiative; its Admin manages who sees it | 07 | ~6h |
| 8 | #6119 | 08 | US-08 | #6122 | Initiative scope in SSO group mappings, Viewer and Admin | 07, 09 | ~3h |
| 9 | #6119 | 10 | US-10 | #6123 | Access settings list Initiative grants, including Initiatives gone from the data | 07 | ~4h |
| 10 | #6119 | 05 | US-05 | #6117 | Target date, likelihood, status word, and the likelihood column in the expander; write needs the Admin grant | Epic #5806 slices 04 and 06, **09** | ~7h |

Epic #5806 is orders 1-5; Epic #6119 is orders 6-10 and starts after #5806's slice 03 exists.

**Walking skeleton**: slice 03 on the data slice 01 creates. It is the first to cross navigation, API,
RBAC and the licence gate. Slices 07-10 extend the existing RBAC path rather than adding a second one.

### Priority rationale

1. **01 first**: the demo is the sales motion (D10), and nothing later can be seen on demo data
   without it. Also the cheapest proof that parents resolve end to end.
2. **02 second**: the page must be born with the configurable word, never with a hardcoded one that is
   refactored later (D9). Visible on its own on every Portfolio page.
3. **03 third**: the walking skeleton, and the first answer to "one place" (O4). Cheap enough to learn
   whether the Initiative level in real data is populated before dates are drawn on it.
4. **04 fourth**: "when" (O1), reusing the Delivery composition.
5. **06 fifth**: the executive's follow-up question, and it needs no target to be useful.
6. **07, 09, 08, 10**: access before promises. 07 is the smallest useful grant (read, to a user); 09 adds
   the write grant and its first use, managing the audience; 08 extends both roles to SSO groups; 10
   makes grants visible and survivable.
7. **05 last**: "on track" (O3) needs somewhere to store the promise and a write grant to decide who may
   make it (D21), so it waits for 09.

---

## Wave: DISCUSS / [REF] Out of Scope

- Alerts when an Initiative turns at risk (Signals Epic); export, copy, PDF, scheduled briefs (Reports
  Epic).
- Heatmap or timeline view (candidate later Epic). Trend of an Initiative's date over time (needs a
  per-Initiative snapshot).
- WIP, cycle time or ordering of Initiatives; OKR linkage; levels above the Initiative.
- Reading target dates from the tracker; writing Initiative forecasts back to the tracker.
- Auto-created Deliveries per Initiative (D8).
- Changing how Features are forecast.
- Filtering Initiatives by tracker type (D4).
- Initiative grants that reach into Portfolios, Teams or Features: an Initiative grant never reveals more
  than the count of Features in Portfolios the reader cannot open (D22).
- Narrowing an API key to Initiatives: keys inherit their owner's grants, as today.
- Initiative Admins managing SSO group mappings (D24 keeps those with the System Admin).

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Strategy B — extend an existing end-to-end path.** Brownfield. Parents are already fetched and stored
(S2), the joint composition already exists (S4), terminology and navigation are already data-driven
(S10, S15), and the RBAC and licence gates are reused (S13, S14). Slice 03 threads one new read model
through them. One Playwright walking skeleton per flow, driven from demo data through a page object:
open Initiatives, see the grouped rows; slices 04-06 add one assertion each to it rather than new specs.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Slice |
|---|---|---|
| Inbound sync | CSV Portfolio refresh resolves parent rows | 01 |
| HTTP | Terminology read and update carry the new term | 02 |
| UI | Portfolio and Team Feature tables, Initiative column | 02 |
| HTTP | New read of all Initiative rows (name chosen in DESIGN) | 03, 04, 05, 06 |
| UI | Top navigation → Initiatives page | 03-06 |
| HTTP | Set / clear an Initiative's target date | 05 |
| HTTP + UI | Settings → Access → Users: grant Viewer or Admin on an Initiative | 07, 09, 10 |
| HTTP + UI | Settings → Access → Group Mappings: Initiative scope | 08, 10 |
| HTTP + UI | Initiatives page row → Access: an Initiative's Admin manages its members | 09 |

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Time for Maria to state when an Initiative will land | from several minutes (build a rule-based Delivery) to under 30 seconds | Timed dogfood on the demo instance before and after slice 04 |
| Initiative rows whose date differs from a Delivery over the same Features | 0 | AC-4.2 identity test |
| Initiatives page response, 50 Initiatives / 300 Features | under 1 second, no simulation run | Dev instance, AC-4.5 |
| Enterprise demos that show the Initiatives page | every enterprise demo after release | Maintainer's demo log, 90 days after release |
| Premium instances with usage data enabled that open the page at least once within 30 days | at least 30% of those with one or more Initiatives | Usage-data event (DEVOPS decides) |
| Of instances that open the page, share that set at least one target date | at least 50% | Usage-data event, if DEVOPS adds one; otherwise asked in customer calls |
| Share of open Features with a resolvable Initiative, measured before release copy | measured on the dev instance plus 3 friendly Premium instances (no target; this is the baseline) | Data count, D19 |
| Mutation kill rate, both stacks | at least 80% | Stryker.NET and StrykerJS, per feature |

Baseline: no Initiative page, no target date, no Initiative forecast outside hand-built Deliveries.

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P-1 | Features carry one parent reference; parents are stored | **Confirmed** (S1, S2) |
| P-2 | A joint "all Features done" composition exists for any Feature list | **Confirmed** (S4) |
| P-3 | CSV rows of a non-Feature type are skipped as Features | **Confirmed** (S8) |
| P-4 | RBAC and licence gates are reusable | **Confirmed** (S13, S14) |
| P-5 | Demo data has no parents | **Confirmed**, fixed by slice 01 |
| P-7 | RBAC grants can be extended with a new scope type | **Confirmed** (S17): scopes are an enum beside role, grants go to users and groups through one service |
| P-6 | Customer data carries a parent level | **Unmeasured** (DISCOVER A3). Does not block building or shipping; gates release copy only (D19) |

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

1. **RBAC impact — yes, a new scope (D21).** Initiatives get their own Viewer and Admin grants, to users
   and SSO groups (slices 07-10, Epic #6119). A row is visible through an Initiative grant, or (D23)
   through read access to a Portfolio holding its Features; Epic #5806 relies on the latter alone. Its numbers cover all its Features;
   Features in Portfolios the reader cannot open are only counted (D22). Setting a target date needs the
   Initiative's Admin grant or System Admin; Portfolio rights do not grant it. Who may grant: D24.
   Grants survive an Initiative leaving and returning: D25. The parent row itself stays readable through
   the existing Feature endpoints (S3), which discloses only its name. All checks through
   `IRbacAdministrationService`, UI gating only through `useRbac()`, no component fetches `my-summary`.
   While RBAC is not enforced (disabled, authentication off, bootstrap), everyone sees and may do
   everything, as elsewhere: D26. Docs: `docs/settings/rbac.md` gains the Initiative scope, role table
   and capability matrix rows.
2. **Lighthouse-Clients CLI/MCP — N/A for v1, because** the read shape is new and should settle on the
   page first; there is no version bump in this Epic. Recommended follow-up after release: a read-only
   "list Initiatives" CLI command and MCP tool, since "when will X land?" is exactly what an assistant is
   asked. To be decided at DELIVER finalization.
3. **Website (letpeople.work) — yes.** Premium feature page entry and pricing-table row ("Initiatives
   overview"), plus a screenshot of the page on demo data. Owed at finalization, and release claims wait
   for the data count (D19).
4. **Docs, screenshots, demo data — yes.** New docs page for Initiatives; `docs/settings/configuration.md`
   (new term); `docs/portfolios/detail.md` (column renamed); `docs/concepts/worktrackingsystems/csv.md`
   (parent rows in the file); one `@screenshot` per theme of the Initiatives page, needing a Premium
   licence during the run. Demo data is slice 01. All in the configurable term's default, "Initiative".
5. **Usage-data event — decided in DEVOPS.** Candidate: a name-only "Initiatives page opened"; a
   second, name-only "Initiative target date set" only if DEVOPS wants the target-date KPI counted.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Each story opens with a Problem stated as Maria's or the maintainer's pain |
| 2 | Persona with specific characteristics | Maria Keller (RTE, Ocean Explorer), Tomás Ribeiro (Viewer, one Portfolio), Aiko Tanaka (System Admin), Jonas Weber (Initiative lead with no Portfolio access), the maintainer running demos |
| 3 | 3+ domain examples with real data | Three per story, on named demo Portfolios, Features and Initiatives |
| 4 | UAT in Given/When/Then, 3-7 | US-01 3, US-02 3, US-03 4, US-04 3, US-05 3, US-06 3, US-07 3, US-08 3, US-09 3, US-10 3 scenarios |
| 5 | AC derived from UAT | 56 ACs, each observable on screen or through the API |
| 6 | Right-sized | Ten slices, 3-7h each, one severable; five per Epic, each Epic PASSes the scope assessment (D27) |
| 7 | Technical notes | Per story; surface inventory S1-S18 |
| 8 | Dependencies tracked | P-1 to P-5 and P-7 confirmed; P-6 unmeasured and explicitly non-blocking; slice order and dependencies in the story map (05 waits for 09) |
| 9 | Outcome KPIs measurable | Eight KPIs with targets and methods |

Job traceability: US-01 to US-06 trace to the four jobs this wave added; US-07, US-08 and US-10 to the
existing `job-rbac-manage-users`, US-09 to the existing `job-rbac-scoped-admin`.

**DoR: PASS for all ten slices, in both Epics.** Every proposal is answered (D16-D27); nothing blocks.
Two things are handed to DESIGN rather than blocking: whether the tracker-reference key also carries the
connection (S2, D25), and the unmeasured customer data count, which gates release copy only (D19).

---

## Wave: DISCUSS / [REF] Definition of Done

1. `dotnet build` clean; `dotnet test` green with the live-connector categories excluded.
2. `pnpm test` green; `pnpm build` clean with zero warnings; Biome clean.
3. Playwright run locally before commit, through page objects, on demo data.
4. SonarQube Cloud: no new issues. Stryker at or above 80% on both stacks, acceptance suite excluded.
5. EF migrations generated with `CreateMigration`, expand-only.
6. Docs, per-theme screenshots and website copy updated at feature finalization, in the default term.
7. ADO Stories transitioned by the maintainer's `/ado-sync`; each Epic (#5806, #6119) stops at
   Resolved. Each Epic has its own finalization: docs, screenshots and website copy for what it shipped.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Binding and carried in: D1 new Premium page; D2 RTE user, executive audience; D3 when + on track only;
  D6 grouped by Portfolio, spanning works; D7 target date in Lighthouse; D8 Deliveries separate; D9
  term "Initiative"; D10 demo first.
- Decided here: D4 any parent type, keyed by reference; D5 reuse the Delivery composition; D11 Premium
  boundary; D12 cannot-forecast rule; D13 nothing silently missing; D14 one level up; D15 the term
  renames only the Feature → Initiative relationship.
- Maintainer answers to this wave's proposals (2026-09-29): D16 status bands 70/50, each band including
  its lower bound, status read from the likelihood alone; D17 done means all Features done; D18 CSV
  resolves Initiative names as a product change; D19 data readiness gates release copy only; D20 85%
  date shown, 70/95 on hover.
- Answers to P6 (2026-09-29): D21 a dedicated Initiative scope in RBAC with its own read and write
  grants, in v1; D22 numbers cover all Features, Features the reader cannot open are only counted.
- Answers to P7-P10 (2026-09-29, all accepted as proposed): D23 Portfolio readers see their Portfolio's
  Initiatives without a grant; D24 System Admin grants anything, an Initiative's Admin grants on that
  Initiative to users only, Portfolio Admins get nothing from their role; D25 grants and target dates
  keyed by tracker reference, kept and marked while the Initiative is gone; D26 without RBAC nothing is
  restricted.
- D27: two Epics in one workspace. #5806 "Visualize Initiatives" = slices 01-04 and 06 (#6113-#6116,
  #6118); #6119 "Initiative access and on-track status" = slices 07, 09, 08, 10 and 05 (#6120-#6123,
  #6117 moved from #5806). Both PASS the scope assessment; DoR passes for all slices.
- Nothing is open.
- Risks for DESIGN: parents matched by reference id alone across connections (S2); demo-data changes and
  E2E locators. The likelihood band disagreement at exactly 50/70 (S6) is settled for this page by D16;
  the existing forecast colour bands are not changed by this Epic.

---

## Wave: DISCUSS / [REF] Open Proposals for Maintainer

Answered 2026-09-29: P1 rejected as proposed and replaced by softer bands (D16); P2-P5 accepted and
locked as D17-D20. P6 answered the same day: D21 (dedicated Initiative scope) and D22 (count-only).
P7-P10 were accepted as proposed the same day (D23-D26). **No proposal is open.** The original
proposals stay below for the record.

| # | Question | Proposal | Why |
|---|---|---|---|
| P1 (rejected, see D16) | Status thresholds | **On track** at 85% or more; **At risk** 50-84%; **Off track** below 50%, or target passed with Features left; **No target**; **Unknown** when the row cannot forecast | Reuses the Delivery cut points (S6). With the 85% date as the headline, "On track" then means exactly "the 85% date is on or before the target", so the two columns never contradict each other |
| P2 (accepted, D17) | What "done" means | Done when **every Feature Lighthouse knows under it is done**. The parent's own tracker state is not used | Parent states are not in any Portfolio's state mapping, and an unmapped state makes an item behave unpredictably. An Initiative closed in the tracker with open Features still shows, because the work still exists |
| P3 (accepted, D18) | CSV resolves parent names | **Yes, as a product change**: a row whose ID another row names as parent, and whose type is not a Feature type, is the parent | Needed for the demo; costs CSV users nothing; the file already skips such rows (S8) |
| P4 (accepted, D19) | Customer data readiness as a release gate | **Not a gate for building or shipping.** A gate for release notes, website copy and pricing claims: count Features with a resolvable Initiative on the dev instance plus 3 friendly Premium instances first | The page degrades honestly (footer, D13); the sale is a demo on demo data |
| P5 (accepted, D20) | Which percentile | Headline **85%**, 70% and 95% on hover, the same three a Delivery computes; no percentile selector in v1 | Matches Deliveries |
| P6 (answered: D22 accepted; rights replaced by D21) | RBAC on spanning Initiatives and target dates | A row appears if **at least one** of its Features is readable; its date covers **all** its Features; unreadable Features are counted, never named. Setting a target needs PortfolioWrite on **every** Portfolio holding its Features | The honest number includes all the work (same principle as D12); a count and a date reveal far less than names. Writing a promise for someone else's Portfolio should need their right too |
| P7 (accepted, D23) | Default visibility | **A reader of a Portfolio sees, without any Initiative grant, every Initiative with at least one Feature in that Portfolio.** An Initiative grant adds Initiatives on top; it never removes. Neither a Portfolio read nor an Initiative grant reveals Features in Portfolios the reader cannot open: those stay a count (D22), including for an Initiative's Viewer and Admin | Most Initiatives live in one Portfolio (maintainer), so the people who can read that Portfolio are the natural audience; requiring a second grant for them would double the admin work for the common case. It also lets Epic #5806 ship safely before any Initiative grant exists. The Initiative's name is already readable by everyone today (S3), so this reveals no new name |
| P8 (accepted, D24) | Who can grant Initiative rights | **System Admin**: Viewer and Admin on any Initiative, to users and to SSO groups. **An Initiative's Admin**: Viewer and Admin on that Initiative only, to users only. **Portfolio Admins of the Initiative's Portfolios: no grant right** from that alone | Mirrors today exactly: a Portfolio Admin manages their Portfolio's members, but group mappings are System Admin only (S17). Giving Portfolio Admins rights over an Initiative would borrow from Portfolios, which D21 rejects, and would be ambiguous for an Initiative spanning two Portfolios with different admins |
| P9 (accepted, D25) | Lifecycle when an Initiative leaves the data and returns | **Grants, like the target date, are keyed by the Initiative's tracker reference and are never removed automatically.** While no Feature names the Initiative, its grants are kept, marked "not in current data" in Settings → Access, show the reader nothing, and can be removed. When it returns under the same reference, they apply again. DESIGN decides whether the key also carries the connection, for grants and target dates alike (S2 risk) | Trackers re-parent, close and reopen Initiatives; silently losing access on a re-parent, or silently keeping it for years, are both worse than a visible, dormant grant. Stored parent rows are never cleaned up (S18), so keying by reference matches how the product already remembers them |
| P10 (accepted, D26) | When RBAC is not enforced | **Nothing new appears and nothing is restricted.** With RBAC disabled, authentication off, or before bootstrap, everyone sees every Initiative and may set target dates (the page and targets still need Premium). Initiative grants are only offered where other grants are offered. Grants made while RBAC was enforced are kept, not deleted, if it is switched off or the licence lapses | Same behaviour as Team and Portfolio grants today (S17); a second behaviour for one scope would surprise admins |
