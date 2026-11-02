<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-6473-settings-split

ADO Epic #6473 "Settings split by who decides: Organisation, Access and System". ADR-271 step U1. Locked as its own
small Epic, delivered first in the 6455 family (6455 feature-delta, D4, D21). Precedes #6349 (organisation defaults
with a local override). Runs in parallel with #6474 "One structure" DISCUSS.

Status: DISCUSS drafted 2026-11-02; maintainer answered Q1–Q6 the same day (see Locked decisions). Q7 and Q8 are new,
raised by the connections decision. The sketches still go through DISTILL's walk-through. Nothing mirrored to ADO.

---

## Wave: DISCUSS / [REF] Locked decisions

### Maintainer decisions (2026-11-02)

- **M1 (Q1) Navigation.** One header menu, "Settings ▾" → Organisation / Access / System. Scope confirmed: every
  installation-wide setting lands in one of the three areas; each Team's and Portfolio's own settings stay on its page;
  a redesign of units comes later, outside this Epic.
- **M2 (Q2) Labels.** "Settings"; areas "Organisation", "Access", "System"; Organisation tabs "Terminology",
  "Calendar", "Refresh & ordering".
- **M3 (Q3) Licence.** System → Licence. `/settings/license` redirects there, fixing today's broken link from the
  connection wizard and connection settings.
- **M4 (Q4) Connections move to System only** and are removed from the Overview (against the recommendation). Reason: a
  connection belongs there, and a future onboarding flow will connect first (for example to read hierarchies from
  Jira). Slices 04 and 05 carry it.
- **M5 (Q5)** Organisation stays hidden from anyone who is no System Admin, as today.
- **M6 (Q6) DIVERGE skipped explicitly.** The Epic 6455 DIVERGE plus ADR-271 count as this Epic's DIVERGE.
- **M7 Levels.** Organisation later receives the levels section from #6474 (its slice 05 waits for this Epic's
  Organisation shell, delivered by slice 02).

---

## Wave: DISCUSS / [REF] Inputs and grounding

| Input | Used for |
|---|---|
| `../epic-6455-deeper-hierarchies/feature-delta.md` D4, D6, D21, Scope candidate A, "Input for DISCUSS" | Scope (A: IN/OUT), order, boundary with #6474 and #6349 |
| ADR-271 Decision 2 (who decides), Decision 4 (a connection is credentials and health), U1 | Target placement of every setting |
| ADR-270 Decision 1 (contexts), Decision 4 (three setting buckets), Decision 5 (glossary) | Context map placement |
| `docs/product/personas/config-admin.yaml`, `platform-operator.yaml` | Personas |
| `docs/product/jobs.yaml` (8822 lines; too large for ctx_search, read in part) | Reused `job-rbac-manage-users`; two new jobs proposed |
| Current code (see inventory) | Brownfield walking skeleton |

No DIVERGE ran for #6473 itself. It is grounded in the Epic 6455 DIVERGE, the event storming (ADR-270) and ADR-271,
which the maintainer accepted (2026-11-01) and then split out as this Epic (D4, 2026-11-02). **DIVERGE is skipped
explicitly (M6):** the maintainer counts the 6455 DIVERGE plus ADR-271 as this Epic's DIVERGE. **Risk (low):** the three-area split was decided top-down from the storming, not tested
with users; slice 01's learning hypothesis checks it cheaply.

---

## Wave: DISCUSS / [REF] Jobs (JTBD)

| job_id | Persona | Job story | Status |
|---|---|---|---|
| `job-config-admin-find-how-we-work-in-one-place` | config-admin | When I change how my organisation works in Lighthouse (our words, the days that do not count, how often data refreshes, who orders the Features), I want all of it in one place named for that, so I do not wade past backups and encryption keys, and I can leave the server to someone else. | **New**, proposed below |
| `job-operator-run-the-instance-apart-from-how-we-work` | platform-operator | When I look after the Lighthouse instance (backups, encryption key, demo data, log level, usage data), I want those in one place apart from the organisation's settings, so I know what is mine and cannot rename a Team by accident. | **New**, proposed below |
| `job-rbac-manage-users` | system-admin | (existing) control who can view or administer which Teams and Portfolios. | **Reused**: the Access area is where it is done |

The two new job ids do not exist in `jobs.yaml` yet (parallel-work rule). **Merging them, from the "Proposed SSOT"
section, is a precondition of the DESIGN handoff**, or the JTBD gate fails. Related, not overlapping:
`job-operator-decide-usage-data-for-a-shared-mcp-server` (story-6193) decides usage data for the shared MCP server by
a variable; the new operator job is about where the instance's in-app veto is found.

Forces for the two new jobs. Push: one "Configuration" tab holds four groups for two audiences, and "Never send usage
data" sits next to "Let Lighthouse own the order of your Features". Pull: the area's name tells you whether it is yours.
Anxiety: "my bookmark and the docs link stop working"; "I can no longer find Terminology". Habit: seven tabs in one
row, learned by position.

---

## Wave: DISCUSS / [REF] Scope Assessment

## Scope Assessment: PASS — 5 stories, 0 domain contexts changed (UI surfaces of 5 contexts/lanes), estimated 5–6 days

| Signal | Evidence | Verdict |
|---|---|---|
| >10 stories | 5 | right-sized |
| >3 bounded contexts | none changes; the UI re-homes views owned by Organisation set-up, Access, Licensing, platform lane | right-sized |
| >5 integration points | header link, routes and redirects, RBAC gating, in-app deep links, E2E POMs | 5, at the limit, all in the frontend |
| >2 weeks | 5–6 days | right-sized |
| Independent outcomes | one: settings found by who decides | right-sized |

Behaviour stays unchanged: no setting is added, removed, renamed in meaning, or re-guarded. Backend untouched. The one
deliberate change: the views get loading and failure looks where today they hide or bounce (see that section).
M4 adds a second, visible one: connections leave the Overview for System → Connections, with every way of creating,
opening or fixing a connection pointing there. What a connection page does stays the same.

---

## Wave: DISCUSS / [REF] Current-state inventory (brownfield, 2026-11-02)

Page `Lighthouse.Frontend/src/pages/Settings/Settings.tsx`, route `/settings` (exact match only), header link
"System Settings". Tabs are chosen by `?tab=<key>` and written back with `history.replaceState`.

| Today's tab (value, test id) | Who sees it today | Setting / section | Component |
|---|---|---|---|
| Configuration (20, `configuration-tab`) | System Admin | Blackout Periods & Recurring Rules | `System/BlackoutSettings` |
| | | Behaviour Settings row "Let Lighthouse own the order of your {features}" (Premium) | `System/BehaviourSettingsTable` (`FeatureOrdering`) |
| | | Behaviour Settings row "Never send usage data" | `System/BehaviourSettingsTable` (`UsageData`) |
| | | Terminology Configuration | `components/TerminologyConfiguration` |
| | | {Team} Refresh | `Refresh/RefreshSettingUpdater` (Team) |
| | | {Feature} Refresh | `Refresh/RefreshSettingUpdater` (Feature) |
| Demo Data (25, `demo-data-tab`) | System Admin | Demo data scenarios | `DemoData/DemoDataSettings` |
| Database (30, `database-tab`) | System Admin | Backup, restore, clear | `DatabaseManagement/DatabaseManagementSettings` |
| API Keys (40, `api-keys-tab`) | everyone (keys are personal; empty when auth is off) | API keys | `ApiKeys/ApiKeysSettings` |
| Encryption (45, `encryption-tab`) | System Admin | Secret encryption key ring | `Encryption/EncryptionPanel` |
| System Admins (50, `rbac-tab`) | RBAC on and System Admin | System Admins, SSO group mappings, RBAC status | `Rbac/RbacSettings` |
| System Info (99, `system-info-tab`) | everyone; Log Level section System Admin only | System Info, Refresh History, Log level and log viewer, Third Party Packages | `SystemInfo/SystemInfoSettings` |

Settings ADR-271 names that are **not** on this page today:

| Setting | Lives today | Notes |
|---|---|---|
| Connections (credentials, health) | Overview: "Connections" section (System Admin only: `DataGridBase` list with edit and delete, reconnect banners, "No {connection} found. Load Demo Data or Check the documentation" empty state, a connections-refused alert) and the "Add {connection}" button; full pages `/connections/new`, `/connections/:id/edit`, which return to `/` on save and cancel. Entry points elsewhere: Onboarding stepper step 1 "Add {connection}", the "Create a {connection} before adding a {team}" tooltip, Task Manager connections section | ADR-271 puts them in System; M4 moves them (slices 04, 05). The Overview still needs to know whether a connection exists (stepper, disabled "Add {team}") |
| Licence | Header `LicenseStatusIcon` popover (import, clear for System Admin) | ADR-271 puts it in System. `ModifyConnectionSettings` and `CreateConnectionWizard` link to `/settings/license`, which appears to match **no route** today (verify in DESIGN) |
| Usage data, per person | Footer `UsageDataDialog` | A personal choice, not an instance setting: stays |
| Task Manager | Header | Live view, not a setting: stays |
| Mapping, connection defaults, per-unit settings | Connection and unit pages | ADR-271 U2/U3, #6349: OUT |

---

## Wave: DISCUSS / [REF] Target placement (ADR-271 Decision 2)

| Setting | Today | Target area → tab (DRAFT names) | Placed by ADR-271? |
|---|---|---|---|
| Terminology | Configuration | Organisation → Terminology | yes |
| Blackout periods and recurring rules | Configuration | Organisation → Calendar | yes ("calendar") |
| {Team} Refresh, {Feature} Refresh | Configuration | Organisation → Refresh & ordering | yes ("refresh cadence") |
| Let Lighthouse own the order of your {features} | Configuration / Behaviour | Organisation → Refresh & ordering | yes ("Feature ordering policy") |
| Never send usage data (admin veto) | Configuration / Behaviour | System → Usage Data | yes ("usage data") |
| System Admins, SSO group mappings, RBAC status | System Admins | Access → System Admins | yes |
| API Keys | API Keys | Access → API Keys | yes |
| Demo Data | Demo Data | System → Demo Data | yes |
| Database (backup, restore, clear) | Database | System → Database | yes |
| Encryption | Encryption | System → Encryption | yes |
| System Info, log level and viewer | System Info | System → System Info | yes |
| Refresh History | System Info | System → System Info | **not named** (it reads as system info; kept with it) |
| Third Party Packages | System Info | System → System Info | **not named** (same) |
| RBAC status diagnostic | System Admins | Access → System Admins | **not named** (Access diagnostics) |
| Licence | header popover (stays too) | System → Licence (slice 03, M3) | yes, but not on today's page |
| Connections | Overview | System → Connections only (slices 04, 05, M4) | yes, but not on today's page |

Counts: **Organisation 4** (Terminology, Calendar, the two refresh cadences as one, Feature ordering), **Access 2**
(System Admins with group mappings, API Keys), **System 5** (Demo Data, Database, Encryption, Usage Data veto, System
Info) plus Licence and Connections (M3, M4): **System 7**. Three sections ADR-271 does not name, all placed with their current neighbours.

Room for #6474: the Organisation → Terminology tab is where #6474 adds its levels section (D6: levels and the
remaining terms in the same tab). This Epic builds the tab and its first section only; #6474 may rename the tab.

---

## Wave: DISCUSS / [REF] Journey — find the setting by who decides

Persona: Priya Raman, flow coach and System Admin at Northwind Logistics (12 Teams, 2 Portfolios); Tomasz Nowak,
platform operator for the same instance; Morgan Lee, a Team reader.

Emotional arc: **unsure** ("where was that again?") → **oriented** (the area's name says whose it is) → **confident**
(the setting is unchanged and the old link still works).

```text
[Trigger]               [Step 1]                [Step 2]                 [Step 3]                [Goal]
Priya wants to add      Opens Settings in the   Picks Organisation; sees Opens Calendar, adds    Change saved, same
Christmas week as a  →  header; sees three  →   Terminology, Calendar,→  the period as before →  dialog as last year
blackout period         areas, what each holds  Refresh & ordering
Feels: unsure           Feels: oriented         Feels: "this is mine"    Feels: in control       Feels: confident
Artifacts: header       area names, visibility  tab list per area        unchanged panel         URL /settings/
label                   (useRbac)                                                                organisation/calendar

Error paths
E1 Old bookmark /settings?tab=database   → lands on System → Database (redirect, replace)
E2 Tomasz opens an Organisation link     → he is System Admin too: he sees it (gating unchanged)
E3 Morgan opens /settings/system/database → lands on System → System Info, the first tab he may see
E4 Deep link while access is still loading → lone spinner, then the right tab (no bounce to the first tab)
E5 /settings/license (dead today)        → System (slice 01), System → Licence (slice 03)
```

Steps 2 and 3 show the end state after slice 02 (after slice 01 Organisation holds one Configuration tab). Error paths
feel like this: E1, E5 relief ("it still works"); E3 calm, nothing to see that is not his; E4 a short wait instead of
confusion.

Integration checkpoints: one source for area and tab names and slugs; the legacy key map covers every key the old page
accepted (14, matched case-insensitively); gating reads `useRbac()` only; in-app links use the new paths.

Gherkin per step (embedded; no `.feature` file, no tags in code):

```gherkin
Scenario: The organisation's settings are found under Organisation
  Given Priya Raman is a System Admin of Northwind Logistics
  When she opens Settings and chooses Organisation
  Then she sees the tabs Terminology, Calendar, and Refresh & ordering
  And none of them holds backups, encryption, or demo data

Scenario: An old bookmark still opens the same setting
  Given Tomasz Nowak bookmarked "/settings?tab=database" last month
  When he opens the bookmark
  Then he lands on System, Database, with backup and restore as before

Scenario: A reader sees only what is theirs
  Given Morgan Lee reads one Team and is no System Admin, with RBAC on
  When he opens Settings
  Then he sees Access with API Keys and System with System Info
  And he sees no Organisation area
```

---

## Wave: DISCUSS / [REF] UI sketches (DRAFT; area menu, labels, Licence and Connections decided by M1–M4)

Reuses the existing `Settings.tsx` shell (heading, `Paper`, MUI `TabList` with icon tabs, `TabPanel`) for each area,
and every panel component unchanged. New: the area menu (M1: option A, chosen) and the System → Connections tab.

```text
 Overview   Features   Reports   [Settings v]              (licence) (theme) (Update all)
                                 +-----------------+
                                 | Organisation    |  how your organisation works
                                 | Access          |  who may do what
                                 | System          |  running this Lighthouse
                                 +-----------------+

 Settings > Organisation                                       /settings/organisation/terminology
 +---------------------------------------------------------------------------+
 |  [Terminology]        [Calendar]         [Refresh & ordering]              |
 +---------------------------------------------------------------------------+
 |  Terminology Configuration (unchanged)                                    |
 |  ...                                                                      |
 |  (place for #6474: levels, each naming its unit and item)                 |
 +---------------------------------------------------------------------------+

 Settings > Access                                             /settings/access/system-admins
 |  [System Admins]*     [API Keys]                                          |   * RBAC on + System Admin

 Settings > System                                             /settings/system/connections
 | [Connections] [Database] [Encryption] [Demo Data] [Usage Data] [System Info] [Licence] |

 Settings > System > Connections  (slice 04; moved from the Overview, same parts)
 +---------------------------------------------------------------------------+
 |  {Work Tracking Systems}                          [+ Add {Work Tracking System}] |
 |  ! Northwind Jira needs to be reconnected [Reconnect]   (ReconnectBanner)  |
 |  Name            | Type        | ...today's columns...  | [Edit] [Delete] |
 |  Northwind Jira  | Jira Cloud  |                        |                 |
 |  Empty: No {Work Tracking System} found. Load Demo Data or Check the       |
 |         documentation for more information.                                |
 +---------------------------------------------------------------------------+

 Overview after slice 04: Portfolios, Teams; no Connections section, no "Add {connection}" button.
 Onboarding stepper step 1 "Add {connection}" -> /settings/system/connections/new (slice 05).
```

Rejected by M1: a left-hand area list on one page; three header entries. Connections is the first System tab because
first-run set-up starts there (M4); the DISTILL walk-through may change the order.

Refresh & ordering tab (slice 02): `InputGroup`s stacked as today: "{Team} Refresh", "{Feature} Refresh", then
"Behaviour Settings" with one row, "Let Lighthouse own the order of your {features}". System → Usage Data holds the
`BehaviourSettingsTable` with the one row "Never send usage data" and a link to the usage-data docs page.

Page heading: "System Settings" (the `h1`) becomes "Settings › {area}" (M2).

Mobile: the area menu becomes three entries in the header drawer; each area keeps today's scrollable tab list on
small screens. Keyboard and screen readers: the area menu is an MUI `Menu` (arrow keys, Escape, focus return) and each
area's tabs stay MUI tabs, so both keep their built-in roles; the page heading names the area.

Visualization skill: N/A, because no chart, metric or data view is added or changed; panels move unchanged.

---

## Wave: DISCUSS / [REF] Navigation and URLs

New routes: `/settings/:area/:tab?` with areas `organisation`, `access`, `system`. A tab is a path segment, as on Team
and Portfolio pages. Redirects use `replace`, so Back does not loop.

Legacy keys: 14, matched case-insensitively, as `Settings.tsx` does today.

| Old link | New target (slice 01) | After slice 02 (and later slices) |
|---|---|---|
| `/settings` (header, docs) | first area the person may see | same |
| `?tab=configuration`, `?tab=system` | `/settings/organisation/configuration` | `/settings/organisation/terminology` |
| `?tab=demodata`, `?tab=demo-data` | `/settings/system/demo-data` | same |
| `?tab=database`, `?tab=database-management` | `/settings/system/database` | same |
| `?tab=api-keys`, `?tab=apikeys` | `/settings/access/api-keys` | same |
| `?tab=encryption` | `/settings/system/encryption` | same |
| `?tab=rbac`, `?tab=authorization` | `/settings/access/system-admins` | same |
| `?tab=logs`, `?tab=systeminfo`, `?tab=system-info` | `/settings/system/system-info` | same |
| `/settings/license` (dead today) | `/settings/system` | `/settings/system/licence` after slice 03 |
| `/connections/new` | unchanged until slice 05 | `/settings/system/connections/new` (slice 05) |
| `/connections/:id/edit` | unchanged until slice 05 | `/settings/system/connections/:id/edit` (slice 05) |
| Connection page Save / Cancel (today back to `/`) | unchanged until slice 05 | back to System → Connections (slice 05) |
| unknown area or tab, or one the person may not see | first visible tab of that area, else first visible area | same |

In-app links updated to the new paths: Task Manager "the full log" (`RecentProblemsSection`), demo-data links on
`DataOverviewTable` and `OverviewDashboard`, the two `/settings/license` links in the connection forms. Slice 05:
Onboarding stepper "Add {connection}", the "Create a {connection} before adding a {team}" tooltip (adds a link),
Task Manager connections section, `ReconnectBanner` (Q7), the connection pages' return path.

Usage data: `usageDataRouteKeys` only matches `/teams/:id/:tab` and `/portfolios/:id/:tab`; the new paths must stay
event-free, as `/settings` is today (its test asserts that). Whether an event should show the areas are used is
DEVOPS's question.

E2E POM impact (specs change only through POMs): `LighthousePage.goToSettings` (header label and menu),
`SettingsPage.goTo*` (choose the area first, or `page.goto` the path), `RbacSettingsPage.goToAccessTab` (its
"same pathname" workaround becomes unnecessary), `SystemConfigurationPage` (split in slice 02 between Terminology,
Calendar and Refresh & ordering). Slice 04: `OverviewPage.addConnection`, `editConnection`, `deleteConnection` move to a
System connections page object; specs ado, csv, jira, linear, servicenow, OAuthConnection, ConnectionDefaults and
Screenshots call it instead (the connection wizard and edit page objects are unchanged). Specs reached: Screenshots, AuthScreenshots, DatabaseManagement,
RoleBasedAccessControl, ApiKeys, PeriodicRefreshSettings, ViewerEmbedSession, ManualSortingMove, ManualSortingSwitch,
ThirdPartyPackagesPaging. Keep the existing tab test ids so the POMs change in one place.

---

## Wave: DISCUSS / [REF] RBAC impact

Gating moves with the tabs and does not change. All gating reads `useRbac()`; nothing fetches `my-summary` directly.
Backend guards (`RbacGuard SystemAdmin` on blackout, optional features, logs, encryption and others) are untouched.

| Area → tab | RBAC off (everyone is System Admin) | RBAC on, System Admin | RBAC on, anyone else |
|---|---|---|---|
| Organisation (all tabs) | yes | yes | area hidden |
| Access → System Admins | hidden (as today) | yes | hidden |
| Access → API Keys | yes (shows "auth off" state) | yes | yes |
| System → Database, Encryption, Demo Data, Usage Data | yes | yes | hidden |
| System → System Info | yes | yes | yes, without Log Level |
| System → Licence (slice 03) | yes | yes, with import and clear | yes, read-only (as the popover) |
| System → Connections (slice 04) | yes | yes | hidden (the Overview section is System Admin only today) |

Scoped Team and Portfolio admins keep picking a connection in their forms through the existing summary endpoint;
unchanged.

Fix carried by slice 01: today, with RBAC on, `useRbac()` starts with `isRbacEnabled: false`, so a deep link to the
System Admins tab is likely bounced to the first tab before access has loaded. The new shell resolves the fallback
only once `isLoading` is false.

---

## Wave: DISCUSS / [REF] Loading and failure looks

| View | Fetches | While loading | On failure |
|---|---|---|---|
| Settings shell, area menu, tab list | nothing; `useRbac()` | lone spinner in the panel area until `isLoading` is false (new, small) | `useRbac()`'s existing failure handling; no new look |
| Each moved panel | its own, unchanged | its existing look | its existing look |
| System → Connections (new home, slice 04) | connection list | lone spinner in the tab (the list never shows the empty-state alert while loading) | refused (403): today's connections-refused alert; any other failure: the inline "Failed to load …" alert, never the empty state |
| Overview after slice 04 | still asks whether a connection exists (stepper, "Add {team}") | today's look | as today |
| Behaviour Settings rows (ordering in Organisation, usage veto in System) | optional features, licence status | today: section hidden until loaded | today: optional features failing hides the section; licence status failing sets it to none, so the Premium ordering switch shows as locked |

The behaviour rows are the one gap: each now stands alone on its tab, so a hidden section is an empty tab, and an
unreadable licence looks like "no Premium". Slice 02 gives them a spinner while loading and the existing inline alert
style ("Failed to load …", as in `ApiKeysSettings`) on failure of either call. Slice 01 ships the Configuration panel
as it is today, so this gap stays for the days between slice 01 and slice 02 on `main`; accepted, because no release
is cut in between (the Epic is released after slice 02). DESIGN confirms; no `WidgetShell`, because these are settings forms, not dashboard widgets.

---

## Wave: DISCUSS / [REF] Story map

Backbone (user activities): **Open Settings → Pick the area that is mine → Pick the tab → Change the setting →
Come back by link later.**

Slices 04 and 05 (connections, M4) add to "Pick tab" (System → Connections) and "Come back by link" (every way to a
connection points at System; old connection URLs redirect).

| Activity | Walking skeleton (slice 01) | Release 2 (slice 02) | Release 3 (slice 03) |
|---|---|---|---|
| Open Settings | header entry with three areas | | |
| Pick area | Organisation, Access, System, gated as today | | |
| Pick tab | today's tabs re-homed whole (Configuration under Organisation) | Configuration split into Terminology, Calendar, Refresh & ordering; Usage Data in System | Licence in System |
| Change setting | panels unchanged | behaviour rows with loading and failure looks | licence import and clear as in the popover |
| Come back by link | 14 old keys redirect, in-app links updated, POMs updated | `configuration` key → Terminology | `/settings/license` → Licence |

### Priority Rationale

1. Slice 01 first: it carries the whole navigation risk (redirects, gating, POMs) on unchanged panels, and is the
   smallest step a user notices.
2. Slice 02 second: the outcome the Epic exists for (no instance setting in Organisation), built on 01's routes.
3. Slice 03 (M3): fixes a dead link; small and independent.
4. Slice 04 then 05 (M4): the list moves first with its page objects, so `main` stays green; then every entry point
   and old URL follows. Between them, the stepper still opens the old `/connections/new`, which works.

Each slice is shippable to `main` on its own. The Epic is released after slice 05: releasing after 04 alone would
leave Save on a connection page returning to an Overview with no connections on it.

---

## Wave: DISCUSS / [REF] User stories

### System Constraints

- Behaviour unchanged: no setting added, removed or re-guarded; no API, payload or backend change.
- Copy uses configurable terms (`{Team} Refresh`, `{features}`), never "Epic" or "Story".
- All UI gating via `useRbac()`.
- Sketch and maintainer's yes before any slice is built; DISTILL walks the sketches first.
- No nWave scenario tags in test code.
- Keep tab test ids and docs file names; screenshot file names stay (the website hot-links them).

### US-01: Three settings areas, everything where it was inside them

- **job_id**: `job-config-admin-find-how-we-work-in-one-place` (also `job-operator-run-the-instance-apart-from-how-we-work`)
#### Elevator Pitch

- Before: Priya scans seven tabs in one row (Configuration, Demo Data, Database, API Keys, Encryption, System Admins,
  System Info) to find the blackout periods.
- After: she opens Settings → Organisation and sees one tab, Configuration; Database and Encryption are under System.
- Decision enabled: Priya can leave the System area to Tomasz without worrying he will rename a Team.

Details, examples, UAT and AC: `slices/slice-01-three-areas.md`.

### US-02: Organisation holds only how the organisation works

- **job_id**: `job-config-admin-find-how-we-work-in-one-place`
#### Elevator Pitch

- Before: "Never send usage data" sits next to Feature ordering in one Configuration tab.
- After: Settings → Organisation shows the tabs Terminology, Calendar, Refresh & ordering; Settings → System → Usage
  Data shows the one switch "Never send usage data".
- Decision enabled: Tomasz decides about usage data without opening the organisation's settings.

Details: `slices/slice-02-organisation-tabs.md`.

### US-03: The licence has a home in System

- **job_id**: `job-operator-run-the-instance-apart-from-how-we-work`
#### Elevator Pitch

- Before: "import a licence" in the connection form opens a blank page.
- After: it opens Settings → System → Licence, showing licence status, organisation and expiry date, with import and
  clear for a System Admin.
- Decision enabled: Tomasz renews the licence from a link.

Details: `slices/slice-03-licence-in-system.md`. Decided by M3.

### US-04: Connections live in System

- **job_id**: `job-operator-run-the-instance-apart-from-how-we-work`

#### Elevator Pitch

- Before: the {Work Tracking Systems} list, with edit, delete and reconnect, sits at the bottom of the Overview under
  the Portfolios and Teams.
- After: Settings → System → Connections shows that list with "Add {Work Tracking System}"; the Overview shows only
  Portfolios and Teams.
- Decision enabled: Tomasz checks and fixes the instance's connections where he runs the rest of the instance.

Details: `slices/slice-04-connections-in-system.md`.

### US-05: Every way to a connection leads to System

- **job_id**: `job-operator-run-the-instance-apart-from-how-we-work`

#### Elevator Pitch

- Before: the onboarding stepper, Task Manager and old links open `/connections/...`, and saving returns to the Overview.
- After: they open Settings → System → Connections (or its add and edit pages), and Save and Cancel return there.
- Decision enabled: a first-time admin sets up the first connection and lands where connections live.

Details: `slices/slice-05-connection-entry-points.md`.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Who | Does what | Target | Measured by | Baseline |
|---|---|---|---|---|---|
| K1 Right area | System Admins | find a named setting in the right area at the first try | 5 of 5 named settings (Terminology, blackout period, Team refresh, backup, API key) for 3 maintainer-chosen testers | maintainer walkthrough at DELIVER review | today: one page, 7 tabs, Configuration mixes 2 audiences |
| K2 No misplaced setting | product | settings placed per ADR-271 Decision 2 | 0 instance settings in Organisation, 0 organisation settings in System after slice 02 | inventory table above, checked at finalize | 1 misplaced (usage-data veto) plus 4 org settings on the System Settings page |
| K3 No broken link | everyone with a bookmark | old links open the same setting | 14 of 14 legacy keys plus `/settings/license` resolve | unit tests on the key map; E2E walking skeleton | `/settings/license` dead today |
| K4 Support | users | ask "where is X in settings?" | no rise in such questions in the release after | maintainer's support inbox, Slack | not counted today |

Usage-data event: DEVOPS answers whether one is worth it (for example opening an area, name-only).

---

## Wave: DISCUSS / [REF] Shared artifacts registry

| Artifact | Single source (DESIGN names the file) | Consumers | Risk |
|---|---|---|---|
| Area names, slugs, tab names, slugs, test ids | one frontend settings-areas definition | header menu, shell, redirects, POMs, docs | HIGH: a mismatch is a dead link |
| Legacy `?tab=` key map | the same definition | redirect, unit tests | HIGH |
| Visibility per tab | `useRbac()` | shell, header menu | HIGH: leaking an admin tab |
| `{Team}`, `{Feature}`, `{features}` | `TerminologyContext` | refresh and ordering labels | MEDIUM |
| Docs page names and anchors | `docs/settings/*.md` | in-app doc links (`encryption.html`, `usagedata.html`), release notes (`configuration.html#…`) | HIGH: keep names |
| Screenshot file names | `docs/assets/settings/*.png` | docs, website (hot-linked from @main) | HIGH: keep names |

---

## Wave: DISCUSS / [REF] Context map placement (ADR-270, ADR-271)

- **Contexts touched**: none in the domain. The UI re-homes views of Organisation set-up (Terminology, calendar,
  refresh cadence, Feature ordering), Access (System Admins, group mappings, API keys), Licensing (licence, slice 03)
  and the platform lane (database, encryption, demo data, log level, usage-data veto, system info).
- **New terms**: the three settings areas Organisation, Access and System, already named by ADR-271 Decision 2.
  "Access" also names the Team and Portfolio Access tab; both mean the Access context, so no clash.
- **Domain events**: none published or handled, because no domain state or rule changes; only where a view sits.
- **UI surface**: this Epic creates ADR-271's Organisation, Access and System surfaces (U1), and puts Tracker
  integration's connections on System (M4, towards ADR-271 Decision 4). The connection page still holds mapping;
  moving mapping to Organisation is U3, later. Team and Portfolio pages keep their own settings (M1).
- **ARCHITECTURE.md §4**: no change, because no module or edge changes.

---

## Wave: DISCUSS / [REF] DISCUSS checklists

- **RBAC impact**: gating moves unchanged; see the RBAC table. One likely deep-link bug fixed in slice 01.
- **Lighthouse-Clients (CLI/MCP)**: no route, payload or permission change, so no client release. One check owed:
  grep the Clients repo for "System Settings" in help or error copy (not reachable from this worktree); if found,
  the copy changes in a client patch at finalize.
- **Website marketing surface**: the website hot-links `docs/assets` from `@main`, so refreshed screenshots reach it
  without a website change as long as file names stay. Check the website for "System Settings" copy at finalize.
- **Docs (per feature, at finalize)**: `docs/settings/settings.md` (rewrite into the three areas, title "Settings"),
  `configuration.md` (becomes Organisation; the usage-data veto section moves to `usagedata.md`), `apikeys.md`,
  `rbac.md`, `databasemanagement.md`, `demodata.md`, `encryption.md`, `systeminfo.md`, `usagedata.md`,
  `taskmanager.md`, `worktrackingsystems.md` (how to add, edit and delete a connection now starts from System →
  Connections, not the Overview), the Overview and getting-started pages that describe the connection list or the
  onboarding stepper; plus "System Settings" mentions in `aiintegration.md`,
  `Installation/*`, `concepts/*`, `index.md`, `features/features.md` (68 mentions in 26 files outside `feature/`).
  Release notes are history and are not edited. Keep file names and anchors.
- **Screenshots**: page shots change (`settings/configuration.png`, `demodata.png`, `systeminfo.png`,
  `encryption.png`, `databasemanagement.png`, `rbac.png`, `apikeys.png`); element shots likely do not
  (`optionalfeatures.png` does: it loses a row; `teamrefreshsettings.png`, `featurerefreshsettings.png`,
  `systeminfo_auth.png`, `rbac_groupmapping.png`, `apikeys_create.png`); `terminologyConfiguration.png` and
  `blackoutPeriodsSection.png` have no producing spec found — check at finalize. New: a Licence shot and a System →
  Connections shot. Overview page shots lose the Connections section. Connection wizard and edit shots keep their
  file names (the specs reach them through the new page object).
- **Demo data**: N/A, because no data shape changes.
- **Usage data**: DEVOPS question; see KPIs.

---

## Wave: DISCUSS / [REF] Boundary with #6474 and #6349

- #6473 builds the three areas and moves today's settings, unchanged.
- #6474 adds levels (each naming its unit and item) inside Organisation → Terminology, and may rename that tab.
  #6473 builds nothing about levels, units, item types or level names.
- #6349 adds organisation defaults with a local override (U2) in Organisation, in a tab of its own. #6473 leaves the
  Organisation tab list open-ended for it.

---

## Wave: DISCUSS / [REF] Proposed SSOT and ADR updates

To merge later; not edited here (parallel-work rule).

`docs/product/jobs.yaml`, append:

```yaml
  - id: job-config-admin-find-how-we-work-in-one-place
    title: Find how my organisation works in one place, apart from running the server
    persona: config-admin
    created_in_feature: epic-6473-settings-split
    job_story: >
      When I change how my organisation works in Lighthouse (our words, the days that do not count,
      how often data refreshes, who orders the Features),
      I want all of it in one place named for that,
      so I do not wade past backups and encryption keys, and I can leave the server to someone else.
    dimensions:
      functional: Reach Terminology, blackout periods, refresh cadence and Feature ordering from one area
      emotional: Feel that the area is mine and nothing in it can break the server
      social: Hand the instance's running to IT without handing over how we work
    forces:
      push: One Configuration tab mixes four groups for two audiences; seven tabs in one row
      pull: The area's name says whether it is mine
      anxiety: My bookmark and the docs link stop working; I cannot find Terminology any more
      habit: Tabs learned by position
    opportunity_score: { importance: 3, current_satisfaction: 2, gap: 1 }

  - id: job-operator-run-the-instance-apart-from-how-we-work
    title: Run the instance without touching the organisation's settings
    persona: platform-operator
    created_in_feature: epic-6473-settings-split
    job_story: >
      When I look after the Lighthouse instance (backups, encryption key, demo data, log level, usage data),
      I want those in one place apart from the organisation's settings,
      so I know what is mine and cannot rename a Team by accident.
    dimensions:
      functional: Reach database, encryption, demo data, usage-data veto, system info and licence from one area
      emotional: Feel safe changing what is mine
      social: Not be the person who changed the organisation's words by mistake
    forces:
      push: Usage-data veto sits between Feature ordering and Terminology
      pull: One System area that matches the job
      anxiety: Links in runbooks to /settings?tab=database break
      habit: Bookmarked tabs
    opportunity_score: { importance: 3, current_satisfaction: 2, gap: 1 }
```

Personas: `config-admin.yaml` `primary_jobs` add `job-config-admin-find-how-we-work-in-one-place  # epic-6473 (2026-11-02, #6473)`;
`platform-operator.yaml` `primary_jobs` add `job-operator-run-the-instance-apart-from-how-we-work  # epic-6473 (2026-11-02, #6473)`.

Journey: `docs/product/journeys/settings-by-who-decides.yaml` (new), from the journey section above, changelog entry
`2026-11-02, epic-6473-settings-split, created`. `rbac-enhancements.yaml`: its "System Settings → Access" and "License
Info section in System Settings" lines become "Settings → Access → System Admins" and "header licence popover /
System → Licence" (changelog entry).

ADR-271: "Today" column of Decision 2 and the U1 row get "done by Epic #6473" once shipped; the System row notes
Connections moved from the Overview to System by #6473 (mapping still on the connection page until U3). ADR-270: no map or context change; glossary
Organisation set-up gains no term (the areas are UI surfaces, named in ADR-271). ARCHITECTURE.md §4: no change.

---

## Wave: DISCUSS / [REF] Findings outside this Epic

1. `/settings/license`, linked from the connection forms, appears to render nothing (no matching route). Slice 01
   redirects it; slice 03 gives it its target.
2. `docs/settings/settings.md` lists "Configuration export/import"; no such control exists in the frontend, and
   `Lighthouse.EndToEndTests/.../SystemSettings/ImportDialog.ts` looks unused. Fix in the finalize docs pass.
3. Behaviour Settings is hidden when its fetch fails (reads as "no settings"). Slice 02 fixes it for both rows.
4. `rbac-enhancements.yaml` still says licence info lives in System Settings; it lives in the header popover.

---

## Wave: DISCUSS / [REF] Definition of Ready

| DoR item | US-01 | US-02 | US-03 | US-04 | US-05 |
|---|---|---|---|---|---|
| Problem in domain language | PASS | PASS | PASS | PASS | PASS |
| Persona with characteristics | PASS (Priya, Tomasz, Morgan) | PASS | PASS | PASS | PASS |
| 3+ domain examples | PASS | PASS | PASS | PASS | PASS |
| UAT 3–7 scenarios | PASS (6) | PASS (6) | PASS (4) | PASS (5) | PASS (5) |
| AC from UAT | PASS | PASS | PASS | PASS | PASS |
| Right-sized | PASS (~2 days; split redirects from POMs if it grows) | PASS (~0.5–1 day) | PASS (~0.5 day) | PASS (~1 day) | PASS (~1 day) |
| Technical notes | PASS | PASS | PASS | PASS | PASS |
| Dependencies tracked | PASS (none) | PASS (US-01) | PASS (US-01) | PASS (US-01) | PASS (US-04; Q7, Q8) |
| Outcome KPIs | PASS (K1, K3) | PASS (K1, K2) | PASS (K3) | PASS (K1, K2) | PASS (K3) |

DoR status: PASSED for all five. Q7 and Q8 change copy and URL shape inside US-05, not scope. DESIGN handoff
precondition: the two new jobs merged into `jobs.yaml` and the personas.

Peer review (nw-product-owner-reviewer, iteration 1): NEEDS_REVISION, 0 critical, 3 high. All high and medium items
fixed in this revision (licence-failure scenario, JTBD precondition, pitch format, key count 14, testable redirect
scenario, page heading, mobile and keyboard, interim gap accepted, US-01 re-estimated, US-03 conditional, DIVERGE Q6).

---

## Wave: DISCUSS / [REF] Open questions for the maintainer

Q1–Q6 were answered on 2026-11-02 (Locked decisions, M1–M6):

1. **How do people reach the three areas?** ANSWERED (M1): one header menu "Settings ▾" → Organisation / Access /
   System (the recommendation).
2. **Labels.** ANSWERED (M2): "Settings"; "Organisation", "Access", "System"; "Terminology", "Calendar", "Refresh &
   ordering" (the recommendation).
3. **Licence in System?** ANSWERED (M3): yes, System → Licence; `/settings/license` redirects there.
4. **Connections in System?** ANSWERED (M4): connections move to System only and leave the Overview (not the
   recommendation). Slices 04 and 05 added.
5. **Readers and Organisation.** ANSWERED (M5): hidden from non-admins, as today.
6. **DIVERGE.** ANSWERED (M6): skipped explicitly; the 6455 DIVERGE plus ADR-271 count.

New, raised by M4 (for the DISTILL walk-through; neither blocks DESIGN):

7. **Reconnect warnings after the move.** An OAuth connection that needs reconnecting shows a banner at the top of the
   Overview list today. (a) Keep one banner on the Overview, "Northwind Jira needs to be reconnected", linking to
   System → Connections; the list itself moves. (b) Banner only on System → Connections and in the Task Manager.
   **Recommend (a)**: a broken connection stops every Team refreshing, and the Overview is where admins look; it is a
   warning, not the connection list.
8. **Connection page URLs.** (a) `/settings/system/connections/new` and `/settings/system/connections/:id/edit`, old
   `/connections/...` redirecting. (b) Keep `/connections/...` as the pages' address and only change where they are
   opened from and return to. **Recommend (a)**: the address says where the page lives, and the area menu shows
   System as current.
