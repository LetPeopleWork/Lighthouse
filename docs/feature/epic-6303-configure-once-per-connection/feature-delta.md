# Feature Delta — epic-6303-configure-once-per-connection

**Feature**: Configure once per Work Tracking Connection. Settings that point at a connection's own Additional
Fields, and are in practice the same for every Team and Portfolio on it, are set once on the connection and
followed, with a per-owner override. The Portfolio's Dependency Field may name a link type as well as a field.
**ADO**: Epic #6303 (reported by Steve Pereira — a credited community contributor, named in release notes —
follow-up to Epic #6028). Four Stories, all in Next, in this order, each shipping on its own and each later one
building only on finished earlier ones (D19):

| ADO Story | Title | Slices | Est. |
|---|---|---|---|
| #6300 | Read dependencies from a configurable Jira link type | 01, 02 | ~1.5d |
| #6312 | Read dependencies from a configurable Azure DevOps relation type | 03, 04 | ~1.5d |
| #6304 | Work Tracking Connection defaults that Teams and Portfolios inherit and can override (the mechanism + the Teams' Parent Override Field) | 05a, 05b, 06 (07 dropped, UI-4) | ~2d |
| #6313 | More Team and Portfolio settings set once on the Work Tracking Connection | 08, 09, 10, 11, 12 | ~3d (ADO says ~2.5d; see Scope Assessment) |

**Feature type**: cross-cutting (Jira and Azure DevOps connectors, configuration aggregates, settings UI, sync
fingerprint).
**Waves present**: DISCUSS. DISCOVER and DIVERGE skipped by explicit maintainer decision.
**State of `main` read**: `897950cb9` (2026-10-16). Pass 1 drafted; pass 2 applied the maintainer's decisions;
pass 3 applies the Story regrouping and the product-owner review (verdict: rejected pending revisions).

---

## Wave: DISCUSS / [REF] Prior Wave Consultation

```
⊘ DISCOVER artifacts         (skipped by maintainer decision)
⊘ DIVERGE artifacts          (skipped by maintainer decision)
✓ docs/product/jobs.yaml
✓ docs/product/personas/config-admin.yaml
✓ docs/product/journeys/parent-from-issue-links.yaml       (D1-D7)
✓ docs/product/journeys/epic-4365-dependencies.yaml        (D4, D15)
✓ docs/product/journeys/story-6307-connection-field-change-takes-effect.yaml
✓ docs/product/architecture/adr-193-parent-source-is-a-declared-connector-capability.md
⊘ docs/product/architecture/brief.md                       (too large for the search tooling; ADR-193 read instead)
✓ docs/feature/parent-from-issue-links/feature-delta.md    (DISCUSS; structure copied)
✓ docs/feature/story-6307-refresh-on-additional-fields-change/feature-delta.md
✓ docs/feature/story-6301-only-features-as-parent-candidates/feature-delta.md (house checklist; Refresh History)
✓ docs/ci-learnings.md
✓ /storage/repos/lighthouse-clients (client, cli, mcp-core)
✓ /storage/repos/website (checked; see the DISCUSS checklist)
✓ Lighthouse.Backend/…/Seeding/TerminologySeeder.cs        (no seeded term for the level above a Feature)
```

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `config-admin` (as System Admin) | **Primary.** Owns the connection; registers the link as an Additional Field; sets defaults once; knows, before saving, how many follow; finds their names in the log. |
| `config-admin` (as Team / Portfolio admin) | **Primary, second hat.** Has `TeamWrite` / `PortfolioWrite`, often not `SystemAdmin`. Picks the Dependency Field; sees what the owner follows and from where; overrides where it really differs. |
| `product-owner`, `delivery-lead-rte` | Read the consequence of #6300 / #6312 in Features → Dependencies. Never open the settings. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

All three new jobs are in `docs/product/jobs.yaml` (2026-10-16).

- **`job-config-admin-name-the-link-our-dependencies-live-in`** (NEW; #6300, #6312) — When our teams record that
  one Feature waits on another with a link type other than the one Lighthouse reads by default, I want to tell
  Lighthouse which link that is, in the place I already name our dependency field, so Features → Dependencies
  shows the waits we already maintain.
- **`job-po-see-what-a-feature-is-waiting-on`** (existing) — the consequence of #6300 / #6312.
- **`job-config-admin-set-a-connection-setting-once`** (NEW; #6304, #6313) — When every Team and Portfolio on one
  connection reads the same field for the same purpose, I want to set it once on the connection and have each
  follow it unless I say otherwise, so adding a Team is not a checklist.
- **`job-config-admin-see-which-owners-differ-from-the-connection`** (NEW; **deferred** — slice 07 dropped, UI-4) —
  When a forecast on one Team looks off, I want to see who follows the connection's setting and who carries its own,
  so I find the odd one out without opening each of them. What this feature now gives instead: the admin knows,
  before saving, how many follow; finds their names in the log.

Journeys: `docs/product/journeys/epic-6303-configure-once-per-connection.yaml` (new);
`epic-4365-dependencies.yaml` D4 and `parent-from-issue-links.yaml` D7 carry REVISION notes.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read on 2026-10-16 at `897950cb9`. Paths relative to `Lighthouse.Backend/Lighthouse.Backend/` and
`Lighthouse.Frontend/src/`.

### Dependencies today (#6300, #6312)

| # | Surface | What is actually there |
|---|---|---|
| S1 | `…/Jira/IssueExtensions.cs:13` | `BlockedByLinkName = "is blocked by"` — the only Jira dependency link. |
| S2 | `…/Jira/IssueExtensions.cs:66-85` | `ExtractDependencyReferences` keeps links whose inward phrase equals the const and takes the `inwardIssue` key — one end only, so no wait is recorded twice. |
| S3 | `…/AzureDevOps/WorkItemExtensions.cs:8`, `:54-78` | ADO: `relation.Rel == "System.LinkTypes.Dependency-Reverse"` (Predecessor), matched on the reference name because the display word is localised and renameable. The default when nothing is named — unchanged. |
| S4 | `…/Dependencies/DependencySourceSelector.cs:30-50`, `:60` | The shared dependency source selection: no Dependency Field → tracker link; a field → split its value and **replace** link reading (epic-4365 D15). Linear and CSV opt out via `TheTrackersOwnLinksOnly`. |
| S5 | `Models/Portfolio.cs:21-27`; `components/Common/ProjectSettings/Advanced/DependenciesComponent.tsx:20-21` | Dependency Field and Ignore Dependencies are Portfolio only; Features are fetched per Portfolio. |
| S6 | `…/Jira/JiraWorkTrackingConnector.cs:1725-1736` | **The silent trap, Jira only today.** A reference that resolved to a link type stores `""` (`:1733`). The Dependency Field dropdown lists every Additional Field (`DependenciesComponent.tsx:42-45`), so a Jira link-type field (registered for 6028's parent override) is selectable, reads `""`, and replaces `is blocked by` — zero dependencies. On ADO it cannot happen today: references resolve against fields alone (S11). Linear and CSV never read it (S4). Slice 03 makes ADO link-type references possible and, until slice 04 reads them, warns instead of staying silent (AC-3.9). |
| S7 | `…/Jira/JiraWorkTrackingConnector.cs:1275-1309`, `:1282` | The "none of your links is called `is blocked by`, here is what I saw" warning is switched off as soon as a Dependency Field is set — silent twice over. |
| S8 | `…/Jira/JiraWorkTrackingConnector.cs:1993-2019`, `:2058-2070`; `IssueExtensions.cs:106-120`, `:190-212` | Epic 6028's Jira resolver: field first, then link type by name / inward / outward phrase, case-insensitive; the **typed phrase sets direction**; a type name or a phrase both ends share yields `Both`. This is how Parent Override Field resolves on Jira. |
| S9 | `…/Jira/JiraWorkTrackingConnector.cs:426`, `:1738-1739` | Validate resolves references against `rest/api/latest/issueLinkType`; refresh asks only about references an admin typed. `issuelinks` already arrives with `*all` — no extra fetch request. |
| S9a | `docs/settings/systeminfo.md`; Story 6301 | **Refresh History** (Settings → System Info) is the existing surface where a refresh's per-owner findings are shown to an admin, not only logged (6301's placed / not-placed counts). |

### Azure DevOps reference resolution today (#6312)

| # | Surface | What is actually there |
|---|---|---|
| S10 | `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1241-1252` | **Parent Override Field on ADO reads only a field value** (`GetParentReference`); an empty value falls back to the native parent. There is no ADO link-type path to reuse — only the field-first lookup shape. |
| S11 | `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:690-698`, `:714-736` | Validation and refresh resolve every Additional Field reference against the organisation's **field list** (`GetWorkItemFieldsAsync`, by reference name or display name). An unresolved reference fails Validate, and at refresh maps to `""` with a warning on every refresh (`:727-729`). |
| S12 | `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs` | ADR-193 §1's link-type port was never added. No relation-type lookup exists anywhere in the backend. |
| S13 | `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1159-1182` | Relations are fetched with `WorkItemExpand.Relations`: **every** relation type already arrives. Reading a different `Rel` costs no request. |
| S14 | `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1142-1145`, `:648-649` | **Trap, in scope.** The relations fetch is skipped when a Parent Override is set and dependencies "do not come from relations" (`ReadsTheTrackersOwnLink`, i.e. "no Dependency Field"). A Dependency Field that resolves to a link type must count as coming from relations — slice 04. |

### Settings that are per connection in practice (#6304, #6313)

| # | Setting | Where | Grain | Read when | In fetch fingerprint |
|---|---|---|---|---|---|
| C1 | Parent Override Field | `Models/WorkTrackingSystemOptionsOwner.cs:76`; `pages/Common/AdvancedInputs/AdvancedInputs.tsx:28-52` | Team and Portfolio (separate defaults) | fetch | yes (`FetchFingerprint.cs:47`, `:91`) |
| C2 | Dependency Field | `Portfolio.cs:25`; `DependenciesComponent.tsx:33-49` | Portfolio | fetch | yes (`:52`, `:94`) |
| C3 | Size Estimate Field | `Portfolio.cs:23`; `FeatureSizeComponent.tsx:178`; read `JiraWorkTrackingConnector.cs:1311-1316`, `AzureDevOpsWorkTrackingConnector.cs:1207-1218` | Portfolio | fetch | yes (`:51`, `:93`) |
| C4 | Feature Owner Field | `Portfolio.cs:21`; `OwnershipComponent.tsx:34-48`; read `JiraWorkTrackingConnector.cs:1245-1250`, `AzureDevOpsWorkTrackingConnector.cs:1200-1205` | Portfolio | fetch | yes (`:50`, `:92`) |
| C5 | Estimation: field, unit, non-numeric flag, categories | `WorkTrackingSystemOptionsOwner.cs:68-74`; `EstimationFieldComponent.tsx` at `ModifyTeamSettings.tsx:293`, `ModifyProjectSettings.tsx:301` | Team and Portfolio | **read time** (`EstimateNormalizer.cs:71-90`) | **no** — takes effect without an update |
| — | States, work item types, state mappings, cutoff; blocked rules, wait states | `WorkTrackingSystemOptionsOwner.cs` | Team and Portfolio | fetch / read | out of scope |

C1–C5 are foreign keys to an `AdditionalFieldDefinition`, which belongs to exactly one connection.

### Which connection types honour which setting (m7)

Searched `Services/Implementation/WorkTrackingConnectors/{Linear,Csv,ServiceNow}` for reads of C1–C4 and of
Additional Field values. The defaults group (D21) offers exactly the settings a tracker honours today.

| Connection | C1 Parent Override | C2 Dependency Field | C3 Size Estimate | C4 Feature Owner | C5 Estimation |
|---|---|---|---|---|---|
| Jira (Cloud, DC) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Azure DevOps | ✓ | ✓ | ✓ | ✓ | ✓ |
| CSV | N/A — never consulted (6028 S4; no read in the CSV connector) | N/A — dependencies are a column; opts out (S4) | N/A — no read in the CSV connector | ✓ — the extrapolation reads the Feature Owner Field from the Additional Field values CSV fills (`WorkItemService.cs:756-772`); the owning Team shown on a Feature stays Jira / ADO only (P-14) | ✓ — CSV reads Additional Field values (`CsvWorkTrackingConnector.cs:244-254`), and estimation reads them at page load |
| Linear | N/A — no read in the Linear connector | N/A — one relation type; opts out (S4) | N/A | N/A | N/A — the Linear connector reads no Additional Field values, so there is nothing to point at |
| ServiceNow | N/A — no Portfolio fetch, no Additional Field values read (`ServiceNowWorkTrackingConnector.cs:758-775`; verified at DESIGN) | N/A | N/A | N/A | N/A |

So the group shows five settings on Jira and Azure DevOps, Feature Owner and Estimation on CSV, nothing on Linear or
ServiceNow (the group is hidden).

### Connection, refresh, permissions, clients

| # | Surface | What is actually there |
|---|---|---|
| S15 | `Models/WorkTrackingSystemConnection.cs` | No setting addressed to the Teams and Portfolios on it. |
| S16 | `components/Common/Connection/ModifyConnectionSettings.tsx:793-818` | Additional Fields editor, Write-Back mappings, Options — where a "Defaults for Teams and Portfolios" group goes. |
| S17 | `Services/Implementation/WorkItems/FetchFingerprint.cs:24-30`, `:78-98`, `:112-116` | Renders the owner's **own** ids. Following a default must render the **effective** value; with no default yet, an inheriting owner's effective value is `null`, which renders exactly as today — so every stored fingerprint survives the upgrade. |
| S18 | `API/WorkTrackingSystemConnectionController.cs:92`, `:157-193` | Story 6307: a connection save that moves `ForConnection` publishes `ConnectionFetchShapeChanged` for every owner; `ConnectionFetchShapeChangedUpdateTriggerHandler` queues them. |
| S19 | `API/TeamController.cs:186-190` | `WorkItemRelatedSettingsChanged` decides whether stored work items are discarded at save — must read the effective value too. |
| S20 | RBAC | Connection PUT `SystemAdmin` (`WorkTrackingSystemConnectionController.cs:81`); Team PUT `TeamWrite` (`TeamController.cs:123`); Portfolio PUT `PortfolioWrite` (`PortfolioController.cs:94`). |
| S21 | `hooks/useModifySettings.ts:60-70` | `null` on these ids means None today — following must not reuse `null`. |
| S22 | `components/Common/FormSelectField/FormSelectField.tsx` | Dropdown with `allowNone` / `noneLabel`; "Connection default (value)" is one more option. |
| S23 | `/storage/repos/lighthouse-clients/packages/client/src/index.ts:2303-2390`, `:2494-2510`; `packages/cli/src/index.ts:1863`, `:2003`; `packages/mcp-core/src/index.ts:2565` | Payloads pass through untyped (`LighthouseWritePayload`); `team update` / `portfolio update` send a caller-supplied payload. MCP reads Team settings only for cycle-time names. No client names C1–C5. |

---

## Wave: DISCUSS / [REF] Locked Decisions

Maintainer decisions of 2026-10-16 (OD-n refers to pass 1's open decision), plus what code and house rules force.

### Dependencies (#6300, #6312)

- **D1 — One setting: the Dependency Field accepts any Additional Field, resolved per tracker as Parent Override
  is on Jira (OD-1 A, maintainer amendment).** If it resolves to a link type, dependencies are read from those
  links; otherwise the field's value is read as the referenced item(s), exactly as today. Field lookup runs first
  and wins on a collision. No new control, no migration.
  - **Jira** (#6300): link type by type name or inward / outward phrase (S8).
  - **Azure DevOps** (#6312): a relation type **reference name with its end** (`System.LinkTypes.Dependency-Forward`,
    `Contoso.DependsOn-Reverse`), checked against the organisation's relation types at Validate and matched
    against the relations that already arrive with the fetch (S13). Display names are not matched (S3).
- **D2 — One-way only (OD-2 A).** The named end sets the direction. A Jira type name or a phrase both ends share,
  an ADO reference name without its end, or a non-directional ADO relation type (`System.LinkTypes.Related`, CA-6)
  reads no dependencies and warns once per refresh naming both phrases / ends, or that the type has no direction.
- **D3 — One link type, replacing the default (OD-3 A, OD-4 A).** The epic-4365 D15 posture is kept.
- **D4 — Portfolio only (OD-5).** Features, and so dependencies, are fetched per Portfolio (S5).
- **D5 — No match warns, never falls back (OD-7 A).** No dependencies, and one warning per refresh naming what was
  looked for and the link phrases (Jira) or `Rel` values (ADO) seen.
- **D5a — Findings surface: Refresh History (B2).** The D2 / D5 warnings, and slice 03's "not read yet" warning,
  appear on the Portfolio's row in **Refresh History** (Settings → System Info), the existing surface Story 6301
  uses; detail also goes to the log as one aggregated line. The shape within the row (a message, a count) is
  DESIGN's, the surface is fixed here.
- **D6 — Azure DevOps is its own Story, #6312 (maintainer).** Two slices: 03 validation, 04 read. The S14 trap
  is closed in slice 04.

### Connection defaults (#6304, #6313)

- **D7 — Five settings follow a connection default, one slice each (OD-8, changed).** #6304 builds the mechanism
  with the Teams' Parent Override Field (C1); #6313 adds the Portfolios' Parent Override Field, Dependency Field
  (C2), Size Estimate Field (C3), Feature Owner Field (C4) and Estimation (C5). States, types and state mappings
  stay out.
- **D8 — Parent Override has separate defaults for Teams and for Portfolios (OD-9 B).** They name different links
  (6028 D7).
- **D9 — Mode beside value (OD-11 A).** Each inheritable setting on an owner stores *Connection default* or *Own*;
  *Own* with nothing selected is a deliberate None. `null` keeps meaning None (S21).
- **D10 — At the upgrade, empty becomes *Connection default*; a value naming one of the connection's Additional
  Fields stays *Own*; a value naming a field the connection no longer has is treated as empty and becomes
  *Connection default* (maintainer, DESIGN pass 2).**
  - No default exists on release day. Owners with an empty value read exactly what they read before.
  - Owners whose setting pointed at a deleted field go back to the tracker's own value: its own parent, its default
    dependency link, or no size / owner / estimate. That is a deliberate fix, called out in the release notes. Each
    of them downloads everything once, at its next scheduled update; no update is started by the upgrade itself.
  - Accepted consequence: an owner that was deliberately None follows a default once one is set. D12 makes that
    visible.
- **D11 — New Teams and Portfolios start on *Connection default* (OD-13 A).**
- **D12 — A default save says, before it completes, how many owners follow the changed default; the save records
  them by name in the log.** When at least one Team or Portfolio follows a default that this save sets or changes,
  the connection's save dialog (Story 6307's) opens and says how many Teams and Portfolios follow it; names are not
  shown there. When nobody follows, no dialog appears and the save works as today. After the save the page navigates
  to the Overview as it does today, with no notice. Every save that sets or changes a default writes one log entry
  naming the followers (or saying none follow), readable by a System Admin in Settings → System Info. The updates it
  starts appear in Task Manager → Activity. AC-F below. (Amended at DESIGN pass 2, maintainer 2026-10-16.)
- **D13 — A changed default updates exactly the followers whose fetched data it changes (OD-14 A).** Reuses Story
  6307's event and queue. Estimation queues nothing (read-time).
- **D14 — Permissions unchanged (OD-15).** Defaults `SystemAdmin`; overrides `TeamWrite` / `PortfolioWrite`; the
  followed value's name is carried on the owner's own settings, so an owner admin sees it without connection
  rights. No new permission.
- **D15 — Shown as the first dropdown entry "Connection default (value)" (OD-10 A).** Copy approved (UI-2): helper
  text "Set on the <tracker> connection."; picking any other entry, None included, makes the value *Own*.
- **D16 — Estimation follows as one bundle (OD-16 A).** Field, unit, non-numeric flag and categories together.
- **D17 — A referenced Additional Field cannot be deleted (OD-17 A).** Refused while a default or an owner points
  at it, naming the default and the owners that use it as their own (UI-6). **Lands in slice 05b** (M4).
- **D18 — A PUT without the mode keeps the stored mode (OD-18, amended by G-R1 (b), maintainer 2026-10-16).**
  Unless the payload sends a present, non-empty value that differs from the effective one — then *Own* with that
  value; an empty or absent value changes nothing. Replaces pass 2's "absent mode = *Own*". No client release.
- **D19 — Four Stories in Next, in order #6300 → #6312 → #6304 → #6313 (maintainer, 2026-10-16).** Each ships on
  its own; later ones build only on finished earlier ones. Resolves review finding B5.
- **D20 — Steve Pereira stays named (maintainer, 2026-10-16).** A public, credited community contributor in the
  release notes. Review finding m6 declined.
- **D21 — The defaults group offers only the settings the connection's tracker honours (m7).** Per the table
  above: five on Jira and Azure DevOps, Feature Owner and Estimation on CSV; hidden on Linear and ServiceNow.
- **D22 — Slice 05 splits into 05a and 05b now (M4).** 05a: the mechanism end to end for the Teams' Parent
  Override Field, with the follower count in the save dialog and the log entry. 05b: D17's delete refusal and the
  older-payload round trip (G-R1 (b)).

### Gates — G-R1 decided, G-07 moot (maintainer, 2026-10-16)

- **G-R1 — What an absent mode means on a PUT. DECIDED: option (b).** A Team / Portfolio PUT without the mode keeps
  the stored mode, unless the payload sends a value for that setting that is **present and non-empty** and differs
  from the effective one; then the owner becomes *Own* with that value. No mode with an empty or absent value
  changes nothing. Option (a) (absent = *Own*) was rejected: it would let a client that drops unknown properties
  silently turn followers into a deliberate None. Amends D18; AC-4.9, AC-4.10; slices 05b and 08.
- **G-07 — Whether slice 07's bulk follow needs a per-owner check. Moot — slice 07 dropped** (maintainer, UI-4).

### Forced by code and house rules

- **L1 — Defaults unchanged for everyone who names nothing.** `is blocked by` on Jira, Predecessor on ADO.
- **L2 — Expand-only migrations** via `CreateMigration`; new columns only, the mode back-filled from whether the
  value is empty (D10). Each slice adds the column its setting needs. #6300 and #6312 need none.
- **L3 — One resolver for the effective value**, read by both connectors, `FetchFingerprint.For` and
  `WorkItemRelatedSettingsChanged` (S17, S19) — so the read and the fingerprint cannot drift.
- **L4 — Linear, CSV, ServiceNow not applicable to #6300 / #6312** (S4; epic-4365 D13).
- **L5 — Lighthouse never authors a link or a dependency** (epic-4365 D4).
- **L6 — Our words, their values.** Configurable terms in copy; there is no seeded term for the level above a
  Feature, so copy says "the item its parent link names". Tracker values (`is blocked by`, `Parent Link`,
  `Epic Link`, `System.LinkTypes.Dependency-Reverse`) stay literal.
- **L7 — Free.** No licence gate (epic-4365 D9, 6028 D5).
- **L8 — Every slice leaves `main` releasable; a Story's release point is its last slice.** A slice may land
  without the next one shipping, and no slice introduces a silent failure (slice 03's AC-3.9 is why).

### The save dialog and the log entry — acceptance criteria (D12; slices 05a and 06–12; amended after DESIGN)

- **AC-F.1** When a System Admin saves a connection on which a default was set for the first time or changed, and at
  least one owner follows it, the save dialog shows, before the save completes, how many Teams and Portfolios follow
  it (for example "Update 3 Teams and 0 Portfolios?"). Every owner in *Connection default* mode counts, including
  those that follow because their setting was empty at the upgrade. The same save writes one log entry naming them.
- **AC-F.2** The dialog never asks to confirm the default itself. Cancel leaves the connection unsaved, as it does
  today. Whether to start updates is asked only when the save would start any (AC-F.5).
- **AC-F.3** When no owner follows the changed default, no dialog appears and the save works as today; the log entry
  says none follow. When no default changed, no log entry is written, and the dialog appears only if an Additional
  Field changed, as today.
- **AC-F.4** Counts in the dialog and names in the log entry use the configurable Team / Portfolio terms. One dialog
  and one log entry per save, covering every default that changed in it.
- **AC-F.5** From slice 06 on, when a changed default is read at fetch time and has followers, the dialog asks Save
  only or Save and update, with the follower count. That is the same question Story 6307 asks for a field change,
  asked once. When only an Estimation default with followers changed, nothing needs updating and the dialog offers
  only Save ("Change the Estimation default?"). Before slice 06 a dialog for a changed default always offers only
  Save.
- After the save the page navigates to the Overview as today, with no notice; the updates appear in Task Manager →
  Activity. Copy approved by the maintainer (UI-3); polish allowed in DELIVER without changing meaning.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS.** 12 live slices (07 dropped, UI-4), ~8 days, 3 contexts (connectors; configuration aggregates; sync
fingerprint + queue), in four ADO Stories that each ship alone (D19) — which resolves the review's story-size finding
(B5): no Story exceeds ~3 days or 7 scenarios per user story.

| Story | User stories | Slices | Est. |
|---|---|---|---|
| #6300 | US-01, US-02 | 01, 02 | ~1.5d |
| #6312 | US-03 | 03, 04 | ~1.5d |
| #6304 | US-04, US-05 | 05a, 05b, 06 | ~2d |
| #6313 | US-07 … US-11 | 08, 09, 10, 11, 12 | ~3d |

**One estimate mismatch, stated rather than rounded:** the ADO Story #6313 says ~2.5d; the slice briefs sum to
~3d (four half-day settings plus Estimation at ~1d, because its bundle and form are the most UI). Either slice 12
comes in at half a day once 05a's form work is reused, or the ADO estimate moves.

---

## Wave: DISCUSS / [REF] User Stories

### Story #6300 — Read dependencies from a configurable Jira link type

#### US-01 — A Jira Portfolio reads dependencies from the link phrase it names

`job_id: job-config-admin-name-the-link-our-dependencies-live-in` · **ADO Story #6300** · slice 01

**Problem.** Priya Raman administers a Jira Data Center instance for the "Payments Platform" Portfolio. Her teams
record waits with Jira's "Dependency" link (`depends on` / `is depended on by`). Features → Dependencies is empty,
and picking the link as Dependency Field today makes it emptier and quieter (S6, S7).
**Who.** config-admin with Portfolio write rights; Jira DC or Cloud; has registered Additional Fields before.

##### Elevator Pitch
Before: Features → Dependencies is empty for every Feature in "Payments Platform", although PAY-112 `depends on`
PAY-98 in Jira; picking a "Depends on" Additional Field as Dependency Field changes nothing and says nothing.
After: in **Portfolio settings → Dependency Settings → Dependency Field** pick the Additional Field whose Reference
is `depends on`, save, let the update finish, open **Features → Dependencies** → sees PAY-112 waiting on PAY-98.
Decision enabled: whether the Portfolio's view of what each Feature waits on can be trusted in planning, or
whether Ignore Dependencies is the honest setting for now.

**Domain examples**
1. Priya names `depends on`; PAY-112 carries an outward `depends on` link to PAY-98 → PAY-112 waits on PAY-98;
   PAY-98 waits on nothing.
2. Kenji Watanabe's instance renamed Blocks' inward phrase to `waits for`; PAY-140 `waits for` PAY-77. He names
   `Waits For` → **PAY-140 waits on PAY-77**, and PAY-77 waits on nothing — the same result `is blocked by` gives
   on a stock instance.
3. Ana Souza names `Dependency` (the type name) → no dependencies, and the warning names `depends on` and
   `is depended on by`.
4. Lukas Brandt's Dependency Field names the ordinary text field "Blocked By Keys" → its value is split and read
   exactly as today.

**UAT scenarios**
```gherkin
Scenario: A Feature waits on what its named one-way link points at
  Given the "Payments Platform" Portfolio names "depends on" as its Dependency Field
  And PAY-112 "depends on" PAY-98 in Jira
  When the Portfolio's update finishes
  Then Features → Dependencies shows PAY-112 waiting on PAY-98
  And PAY-98 shows no dependency

Scenario: The other end's phrase reads the other direction
  Given the Portfolio names "is depended on by"
  When the update finishes
  Then PAY-98 is shown waiting on PAY-112

Scenario: A field that is not a link type is read as before
  Given the "Card Issuing" Portfolio names the field "Blocked By Keys"
  When its update finishes
  Then its dependencies are the keys in that field, exactly as before this change

Scenario: A name that points both ways reads nothing and says why
  Given the Portfolio names "Dependency", the type's name
  When the update finishes
  Then no dependencies are recorded for the Portfolio
  And Refresh History shows one warning naming "depends on" and "is depended on by" to choose from
```

**Acceptance criteria**
- [ ] AC-1.1 A Portfolio naming a Jira link phrase shows, after its update, each Feature waiting on the issue its
  link of that type points at from the named end.
- [ ] AC-1.2 Matching is case-insensitive on name, inward and outward phrase; a real field with the same name is
  read as a field.
- [ ] AC-1.3 A type name or a shared phrase records no dependency; Refresh History shows one warning per refresh
  naming both phrases.
- [ ] AC-1.4 Naming a link replaces `is blocked by` for that Portfolio.
- [ ] AC-1.5 No Dependency Field, or one naming an ordinary field, behaves exactly as today.
- [ ] AC-1.6 Naming a link costs the admin nothing extra: a Portfolio's update with a named link puts no more load
  on the Jira instance, and takes no longer, than the same update naming nothing.
- [ ] AC-1.7 (production data) On the dev instance's slice-01 Portfolio, `OUT-6303-dependencies-recovered` meets
  its target; numbers in the slice brief.

**Technical notes** — reuse `JiraIssueLinkType.DirectionReadFrom` and the `IssueExtensions` link walker; the choice
stays in `DependencySourceSelector` (S4), which gains "a link type the Portfolio named" as a source so no tracker
decides alone. `issuelinks` already arrives with `*all` (S9), so AC-1.6 is asserted as a Jira request count equal
to the pre-change baseline. Brief: `slices/slice-01-6300-jira-dependencies-from-a-named-link-phrase.md`.

**Release point (m3).** Slice 01 alone leaves `main` releasable but is **not** #6300's release point: until slice 02
ships, a mistyped phrase reads nothing and says nothing, which is the S6/S7 failure this Story exists to end. #6300
releases after slice 02.

---

#### US-02 — A named link that matched nothing says so (Jira)

`job_id: job-config-admin-name-the-link-our-dependencies-live-in` · **ADO Story #6300** · slice 02

**Who.** The same config-admin as US-01, setting up the Dependency Field and checking that it took.

##### Elevator Pitch
Before: Priya names `depends on`, the update finishes, Dependencies is empty, and nothing says whether the phrase
was wrong or there are no such links (S7).
After: open **Settings → System Info → Refresh History**, the row for Payments Platform's latest update → sees that
no Feature in Payments Platform has a `depends on` link, and that `is blocked by` and `relates to` were seen.
Decision enabled: retype the phrase, or accept that this Portfolio records no such waits.

**Domain examples** — 1. `depends on` named, Features carry only `relates to` → one warning listing it. 2. PAY-112
`depends on` PAY-98 → no warning. 3. No Feature in "Card Issuing" carries any link → no warning (ordinary, as today).

**UAT scenarios**
```gherkin
Scenario: Nothing matched the named link, and other links exist
  Given the Portfolio names "depends on" and its Features carry only "relates to" links
  When the update finishes
  Then Refresh History shows one warning naming "depends on" and listing "relates to" as seen

Scenario: Something matched
  Given PAY-112 "depends on" PAY-98
  When the update finishes
  Then Refresh History shows no such warning

Scenario: No links at all
  Given no Feature in the Portfolio carries any link
  When the update finishes
  Then Refresh History shows no such warning
```

**Acceptance criteria**
- [ ] AC-2.1 One warning per refresh of the whole query, never one per Feature.
- [ ] AC-2.2 It appears on the Portfolio's row in Refresh History (D5a), and in the log as one line. Approved copy
  (UI-6): "The Dependency Field names "depends on", and no Feature carries that link. Links seen: is blocked by,
  relates to."; for a two-way name: "The Dependency Field names "Blocks", which points both ways. Name one end:
  "blocks" or "is blocked by"."
- [ ] AC-2.3 A Dependency Field naming an ordinary field stays silent, as today.
- [ ] AC-2.4 Docs: `docs/portfolios/edit.md` and `docs/concepts/worktrackingsystems/jira.md` describe naming a link;
  Linear and CSV are stated as not configurable.

**Technical notes** — generalise `ReportLinksThatMeantNothingHere` (`JiraWorkTrackingConnector.cs:1275-1309`) and lift
its early return at `:1282` for a link-type Dependency Field (S7); the expected name is the named phrase, else
`is blocked by`. The finding reaches Refresh History through the per-owner refresh findings Story 6301 added (S9a,
D5a). Whole-query fetch only, as today. Brief: `slices/slice-02-6300-a-named-link-that-matched-nothing-says-so.md`.

---

### Story #6312 — Read dependencies from a configurable Azure DevOps relation type

#### US-03 — An Azure DevOps Portfolio reads dependencies from the relation type it names

`job_id: job-config-admin-name-the-link-our-dependencies-live-in` · **ADO Story #6312** · slices 03, 04

**Problem.** Tomás Ortega runs Lighthouse against Azure DevOps Server 2022, where the PMO records waits with a
custom relation type `Contoso.DependsOn`. Lighthouse reads Predecessor only, so his Features show no dependencies;
an Additional Field referencing the relation type fails Validate as a missing field (S11).
**Who.** config-admin on an ADO connection (Services or Server), Portfolio write rights.

##### Elevator Pitch
Before: adding an Additional Field with Reference `Contoso.DependsOn-Forward` makes **Validate** report
"Some additional fields could not be found", so there is no way to name it.
After: in **Settings → Connections → Additional Fields** add Reference `Contoso.DependsOn-Forward`, press
**Validate** → sees the connection validate; pick it as the Portfolio's **Dependency Field**, let the update finish
→ **Features → Dependencies** shows 4711 waiting on 4702.
Decision enabled: which relation type in their process really carries "waits on", checked against the tracker
before any plan relies on it.

**Domain examples**
1. `Contoso.DependsOn-Forward` on 4711 pointing at 4702 → 4711 waits on 4702.
2. `System.LinkTypes.Related` named by a Services team that records waits as Related → no dependencies, and the
   warning says Related has no direction, so it cannot say who waits on whom.
2b. `System.LinkTypes.Dependency-Forward` (Successor) named → each Feature waits on its successors, the reverse of the
   default.
3. `Contoso.DependsOn` without an end → no dependencies; the warning names `-Forward` and `-Reverse`.
4. `Contoso.DependsOnn-Forward` (typo) → Validate names it as not found and lists the organisation's relation types.
5. The Portfolio also names a Parent Override Field → 4711 is still shown waiting on 4702.

**UAT scenarios**
```gherkin
Scenario: A relation type reference validates where a field name goes
  Given an Azure DevOps connection with an Additional Field referencing "Contoso.DependsOn-Forward"
  When the admin validates the connection
  Then the connection validates successfully

Scenario: A typo still fails and names what exists
  Given an Additional Field referencing "Contoso.DependsOnn-Forward"
  When the admin validates the connection
  Then validation names the reference as not found and lists the organisation's relation types

Scenario: A Feature waits on what its named relation end points at
  Given the "Retail Rollout" Portfolio names "Contoso.DependsOn-Forward" as its Dependency Field
  And work item 4711 has that relation to 4702
  When the update finishes
  Then Features → Dependencies shows 4711 waiting on 4702

Scenario: Naming a parent override does not hide a named relation
  Given the Portfolio also names a Parent Override Field
  When the update finishes
  Then 4711 is still shown waiting on 4702

Scenario: A reference without its end reads nothing and says why
  Given the Portfolio names "Contoso.DependsOn"
  When the update finishes
  Then no dependencies are recorded and Refresh History shows one warning naming the Forward and Reverse ends
```

**Acceptance criteria**
- [ ] AC-3.1 Validate accepts a reference matching a relation type reference name with its `-Forward` /
  `-Reverse` end, case-insensitively; a real field of the same name is read as a field.
- [ ] AC-3.2 An unmatched reference still fails Validate, listing the relation types available.
- [ ] AC-3.3 After a refresh, neither the log nor Refresh History reports a relation-type reference as "no field is
  called …".
- [ ] AC-3.4 A Portfolio naming a relation end shows, after its update, each Feature waiting on the work item at
  the other end of its relations of exactly that end; it replaces Predecessor for that Portfolio.
- [ ] AC-3.5 That holds when the Portfolio also names a Parent Override Field.
- [ ] AC-3.6 A reference without an end, or a non-directional relation type such as `System.LinkTypes.Related`,
  reads nothing and warns (for Related: that it has no direction); nothing matching anywhere warns once, listing the
  relation types seen — both on Refresh History (D2, CA-6).
- [ ] AC-3.7 Naming a relation type costs the admin nothing extra: Validate and a Portfolio's update put no more
  load on the Azure DevOps organisation than before, beyond one look-up of its relation types per connection, and
  a Portfolio with many Features takes no longer to update than the same Portfolio naming nothing.
- [ ] AC-3.8 Docs: `docs/concepts/worktrackingsystems/azuredevops.md` (:101-114, :203-207).
- [ ] AC-3.9 (slice 03 alone, M3) Until slice 04 ships, a Dependency Field that resolves to a relation type reads no
  dependencies **and** Refresh History shows one warning per refresh that the relation type is not yet read for
  dependencies. Chosen over refusing it, because refusing would need a tracker round trip in the Portfolio save
  path that does not exist, while the warning reuses slice 02's surface; slice 04 replaces the warning by reading.
- [ ] AC-3.10 A relation-type Additional Field named by any setting other than the Dependency Field (Parent
  Override, Size Estimate, Feature Owner, Estimation) is never silent: the setting reads as if nothing were
  named (for Parent Override the native parent stands), and Refresh History shows, for the owner, one warning per
  refresh naming the setting and the relation type, saying a relation type cannot be read there (approved copy, UI-6:
  "The Parent Override Field points at an Azure DevOps relation type, which only the Dependency Field reads."). ADO parent from a
  relation stays out of scope.

**Technical notes** — new relation-type lookup (S12); the SDK's relation-types call is the candidate, confirmed at
DESIGN; ADR-193 §1's unbuilt port is a natural home. `ExtractDependencyReferences` takes the `Rel` to match. The S14
early return must treat a link-type Dependency Field as "from relations". Relations already arrive (S13), so AC-3.7
is asserted as a request count against the pre-change baseline: at most one relation-type listing per connection
per Validate and per refresh, and no request per work item. AC-3.10's warning uses the same Refresh History path
as AC-3.9 (D5a).

**Release point.** #6312 releases after slice 04; slice 03 alone is safe by AC-3.9.

---

### Story #6304 — Connection defaults that Teams and Portfolios inherit and can override

#### US-04 — Set the Teams' Parent Override Field once on the connection

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6304** · slices 05a, 05b

**Problem.** Steve Pereira's customer runs 14 Teams on one Jira DC connection, each reading its parent from the
`is caused by` link. Each new Team is another chance to forget the Parent Override Field; a forgotten one shows
every Work Item unparented with nothing pointing at the cause.
**Who.** config-admin as System Admin (sets the default) and as Team admin (follows or overrides).

##### Elevator Pitch
Before: the Parent Override Field is set Team by Team; "Fraud Signals" was created without it and its Work Items
hang under no Feature.
After: in **Settings → Connections → (connection) → Defaults for Teams and Portfolios** set **Parent Override
Field for Teams** to "Caused by", save → the dialog says 3 Teams follow it; save → **Team settings → Advanced** on
Fraud Signals shows "Connection default (Caused by)", and **Settings → System Info → Logs** names Fraud Signals, Ops
Tooling and Mobile Wallet.
Decision enabled: whether a new Team can be onboarded without the parent checklist — and, from the log entry,
whether a Team that was empty on purpose must be set back to None.

**Domain examples**
1. "Fraud Signals", empty at the upgrade → follows; counted in the dialog and named in the log entry.
2. "Ledger", with its own "Caused by" → stays *Own*, unaffected.
3. "Mobile Wallet", empty at the upgrade but meant as None → follows and is named in the log entry; its admin picks
   **None** → *Own* + empty, no longer follows.
4. A Team created afterwards → starts on *Connection default*.
5. Maria tries to remove the "Caused by" Additional Field → refused, naming the default and Ledger, which uses it as
   its own (05b).

**UAT scenarios**
```gherkin
Scenario: Saving the default says how many follow it, and the log names them
  Given Fraud Signals, Ops Tooling and Mobile Wallet had no Parent Override Field at the upgrade
  And Ledger has "Caused by" as its own value
  When Maria Santos sets Parent Override Field for Teams to "Caused by" and saves the connection
  Then the save dialog says 3 Teams follow "Caused by"
  And the log entry names Fraud Signals, Ops Tooling and Mobile Wallet and not Ledger

Scenario: A follower reads the default as its own
  Given Fraud Signals follows the connection default "Caused by"
  When its update finishes
  Then its Work Items hang under the Features their "is caused by" links name
  And its settings show "Connection default (Caused by)"

Scenario: A deliberate None stops following
  Given Mobile Wallet follows the default
  When its Team admin picks None and saves
  Then Mobile Wallet reads no parent override, whatever the connection says

Scenario: A new Team starts on the default
  When a Team is created on the connection
  Then it shows "Connection default (Caused by)"

Scenario: The upgrade changes nothing on its own, except settings that pointed at nothing
  Given no connection has a default
  And Ops Tooling's Parent Override Field pointed at an Additional Field that had been deleted
  When the release is installed
  Then every other Team reads exactly what it read before, and its next update is not a full download caused by the upgrade
  And Ops Tooling follows the connection default, reads its parents from the tracker's own parent again, and its next scheduled update downloads everything

Scenario: A field a default points at cannot be removed
  Given the Teams' default is "Caused by" and Ledger uses "Caused by" as its own
  When Maria removes the "Caused by" Additional Field and saves the connection
  Then the save is refused with: "Caused by" cannot be removed. It is the Teams' Parent Override Field default, and Ledger uses it as its own. Nothing was saved.

Scenario: An older client's round trip changes nothing
  Given Mobile Wallet follows the default and Ledger owns "Caused by"
  When a script GETs each Team's settings and PUTs them back unchanged, dropping properties it does not know
  Then no Team's mode or effective Parent Override Field has changed
```

**Acceptance criteria** (05a: AC-4.1 – 4.7 and AC-F.1 – F.4; 05b: AC-4.8 – 4.10)
- [ ] AC-4.1 The connection holds an optional Parent Override default for Teams among its own Additional Fields;
  only a System Admin can set it.
- [ ] AC-4.2 A Team on *Connection default* behaves exactly as if it owned the default: after its update its Work
  Items hang under the Features the default names, and its settings show "Connection default (value)".
- [ ] AC-4.3 A Team with *Own* and nothing selected reads no parent override, whatever the default says.
- [ ] AC-4.4 After the upgrade, Teams that had no Parent Override Field follow the connection default, which is
  empty on release day, so nothing changes for them. Teams that had one own it. No Team reads anything different or
  downloads everything again because of the upgrade, except Teams whose Parent Override Field pointed at an
  Additional Field that had been deleted: those follow the default too, read the tracker's own parent again, and
  download everything once at their next scheduled update.
- [ ] AC-4.5 New Teams start on *Connection default*.
- [ ] AC-4.6 A Team admin without connection rights sees the followed field's name on the Team's settings: the
  dropdown's first entry reads "Connection default (Caused by)" with the helper text "Set on the <tracker>
  connection."; picking any other entry, None included, makes the value the Team's own (UI-2).
- [ ] AC-4.7 The connection page has a "Defaults for Teams and Portfolios" section below Additional Fields, with
  *Teams* and *Portfolios* sub-headings and one row per setting the tracker honours; it is hidden on Linear and
  ServiceNow, and shows no follower count (D21, UI-1).
- [ ] AC-4.8 Removing an Additional Field referenced by a default or by any owner is refused, naming the default and
  the Teams that use it as their own; nothing is saved (D17, UI-6).
- [ ] AC-4.9 When a Team's settings are saved by a client that does not send the mode: if it sends no Parent
  Override Field or an empty one, the Team keeps following or owning exactly as before; if it sends a different
  field, the Team owns that field from then on (G-R1 (b)).
- [ ] AC-4.10 A scripted GET-then-PUT of every Team's settings, unchanged — in the shape that echoes every property
  and in the shape that drops properties it does not know — changes no Team's mode or effective value; both shapes
  pass.

**Technical notes** — expand-only migration: a nullable default FK on the connection; a mode column on Team,
back-filled from emptiness (D10); a value naming a field the connection no longer has becomes *Connection default*
and is cleared (D10 as amended). One resolver (L3) used by both connectors, `FetchFingerprint.For` and
`WorkItemRelatedSettingsChanged`; with no default an empty follower's effective value is `null`, so stored
fingerprints are byte-identical across the upgrade, except for the repaired deleted-field owners. The followed
display name travels on the Team settings DTO. Follower counts for the save dialog come from the connection list
read; one Information log entry per save names the followers.
PUT without the mode (D18 as amended, G-R1 (b)): keep the stored mode unless the payload's value is present, non-empty
and differs from the effective value, then *Own* with that value. Briefs: `slices/slice-05a-6304-team-parent-override-default.md`,
`slices/slice-05b-6304-referenced-fields-and-older-payloads.md`.

---

#### US-05 — A changed default updates exactly the followers

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6304** · slice 06

**Who.** config-admin as System Admin, changing a default and wanting to check the result now.

##### Elevator Pitch
Before: (after US-04 alone) a changed default reaches each follower at its next scheduled update, possibly hours
later, with nothing saying so.
After: save the connection with "Parent Link" as the new default → the dialog says 9 Teams follow it and will
update; choose **Save and update** → 9 rows in **Task Manager → Activity**.
Decision enabled: check the result now, or come back later.

**Domain examples** — 1. 9 follow, 5 own → the dialog says 9 will update; Save and update → 9 updating. 2. Nobody
follows → no dialog, nothing updating, the log entry says none follow. 3. Estimation default changed (after #6313)
with 4 followers → the plain dialog "Change the Estimation default?" offers only Save; nothing updates.

**UAT scenarios**
```gherkin
Scenario: Only the followers are updated
  Given 9 Teams follow the Parent Override default and 5 carry their own
  When the System Admin changes the default and saves with Save and update
  Then the Task Manager shows an update for exactly the 9 followers
  And the save dialog said 9 Teams follow the default

Scenario: Nobody follows
  Given every Team carries its own value
  When the default changes and the System Admin saves
  Then no dialog appears, the save completes as today, no update starts, and the log entry says no Team follows the default

Scenario: Saving without updating
  Given 9 Teams follow the Parent Override default
  When the admin changes the default and chooses Save only
  Then no update starts and the log entry still names the followers
```

**Acceptance criteria**
- [ ] AC-5.1 After Save and update, the Task Manager shows an update for every follower whose fetched data the change
  affects, and for no owner that owns its value.
- [ ] AC-5.2 Save only starts no update.
- [ ] AC-5.3 Before the save, the dialog says how many followers will update — 9 in the first scenario, none in the
  second. After *Save only* none update, and they pick the change up at their next scheduled update (AC-F.5).

**Technical notes** — followers whose `FetchFingerprint.For` moved are queued through `ConnectionFetchShapeChanged`
(S18); `startUpdates=false` is the existing switch.

---

#### ~~US-06 — Owners whose own value equals the default follow it in one step~~ — DROPPED (maintainer, 2026-10-16, UI-4)

**DROPPED.** No who-follows / who-differs list, no bulk follow, no follower count on the connection page; slice 07 is
not delivered. An owner hand-set to the default's value stays *Own*; its admin switches it to *Connection default* on
its own settings page. Its job, `job-config-admin-see-which-owners-differ-from-the-connection`, is deferred in
`jobs.yaml`. The text below is kept, struck, for the record.

~~`job_id: job-config-admin-see-which-owners-differ-from-the-connection` · ADO Story #6304 · slice 07~~

~~**Who.** config-admin as System Admin on a connection configured by hand before the upgrade.~~

~~**Why it still earns its place after D10.** D10 moves only *empty* owners onto the default. An installation already
configured by hand — Steve's customer, whose 14 Teams all carry "Caused by" — keeps every Team on *Own*. Without
this, the default helps only new Teams there, and the next change of the default must still be made on 14 Teams.~~

~~Elevator Pitch. Before: 14 Teams carry "Caused by" as their own value; changing it later means 14 edits. After: on
the connection's Defaults for Teams and Portfolios → sees "Parent Override Field for Teams: Caused by — 3 follow; 11
own the same value; Ledger Ops owns Epic Link" and chooses Follow the default for the 11. Decision enabled: which
Teams genuinely differ, and whether to bring the rest under one setting.~~

~~Domain examples — 1. 11 own-and-equal → offered; moved; nothing updates. 2. Ledger Ops (`Epic Link`) listed, not
offered. 3. An owner edited by another admin meanwhile → named, not overwritten.~~

~~UAT scenarios (dropped): "The connection lists who follows and who differs"; "Matching owners follow in one step
without an update"; "A concurrent edit is not overwritten".~~

~~Acceptance criteria (dropped):~~
- ~~AC-6.1 Per default: the count of followers; own-value owners named with their value; configurable terms.~~
- ~~AC-6.2 Only owners whose own value equals the default are offered; after following, they read exactly what
  they read before and no update starts.~~
- ~~AC-6.3 If another admin changed an owner since the list was loaded, that owner is named as changed by someone
  else and keeps its value; the others follow.~~
- ~~AC-6.4 The list has a loading look and a could-not-load look (defined at DESIGN).~~
- ~~AC-6.5 Each #6313 setting appears in this list when its slice lands, with no new UI.~~
- ~~AC-6.6 The action is offered to every System Admin and to no one else (G-07).~~

---

### Story #6313 — More Team and Portfolio settings set once on the Work Tracking Connection

Each builds on finished #6304 (05a – 06): same resolver, save dialog count and log entry (AC-F.1 – F.5) and update
queue; each adds one default row in the "Defaults for Teams and Portfolios" section (UI-1), one mode column with the
D10 back-fill (as amended: a value pointing at a deleted field becomes *Connection default*), D11 for new owners, the
"Connection default (value)" dropdown entry (UI-2), and the followed name on the owner's settings. Each slice ships
alone, and each AC list includes AC-4.4 at Portfolio grain for its setting.

#### US-07 — Set the Portfolios' Parent Override Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 08

**Who.** config-admin as System Admin with several Portfolios on one Jira connection, each reading its Features'
parents from the same link; config-admin as Portfolio admin who may own a different one.

##### Elevator Pitch
Before: each of the 4 Portfolios on "Jira DC – Payments" names "Parent Link" in its own Parent Override Field, and a
new Portfolio starts without it, so its Features hang under nothing.
After: in **Defaults for Teams and Portfolios** set **Parent Override Field for Portfolios** to "Parent Link", save →
the dialog says how many follow it; a new Portfolio "Q3 Roadmap" shows "Connection default (Parent Link)" and, after
its update, each Feature hangs under the item its `Parent Link` names.
Decision enabled: whether a new Portfolio needs anything but its query.

**Domain examples**
1. "Q3 Roadmap", created after the default → follows; its Features hang under their `Parent Link` items.
2. "Card Issuing" owns "Initiative Link" → unaffected, stays *Own*.
3. "Merchant Onboarding" had no Parent Override Field at the upgrade → follows, counted in the dialog and named in the
   log entry; the Teams' default does not touch it.

**UAT scenarios**
```gherkin
Scenario: Saving the Portfolios' default counts the Portfolios that follow it
  Given "Merchant Onboarding" had no Parent Override Field at the upgrade and "Card Issuing" owns "Initiative Link"
  When Maria sets Parent Override Field for Portfolios to "Parent Link" and saves
  Then the save dialog counts "Merchant Onboarding" among the followers and the log entry names it, not "Card Issuing"

Scenario: A following Portfolio reads its parents from the default
  Given "Q3 Roadmap" follows "Parent Link"
  When its update finishes
  Then each Feature hangs under the item its "Parent Link" names

Scenario: The Teams' default never reaches a Portfolio
  Given the Teams' default is "Caused by" and no Portfolios' default is set
  When "Merchant Onboarding" updates
  Then it reads no parent override
```

**Acceptance criteria**
- [ ] AC-7.1 Saving the Portfolios' default shows, before the save completes, how many Portfolios follow it, and the
  log entry names them.
- [ ] AC-7.2 A following Portfolio's Features hang under the items the default names, after its update.
- [ ] AC-7.3 The Teams' default never applies to a Portfolio, nor the reverse (D8).
- [ ] AC-7.4 An owning Portfolio is unaffected and no update starts for it.
- [ ] AC-7.5 AC-4.4 at Portfolio grain for the Parent Override Field.

**Technical notes** — C1 at Portfolio grain: a second, separate default FK on the connection (D8) and a Portfolio mode
column with the D10 back-fill as amended (expand-only, L2); the resolver (L3), dialog count, log entry, delete refusal
and queue from #6304 reused; the Portfolio PUT gets the G-R1 (b) rule and AC-4.10's round trip. `AdvancedInputs.tsx:28-52` on
the Portfolio form gains the entry. Jira and ADO only (D21). Brief: `slices/slice-08-6313-portfolio-parent-override-default.md`.

---

#### US-08 — Set the Portfolios' Dependency Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 09

**Who.** config-admin as System Admin whose Portfolios all record waits the same way — a field or, after
#6300 / #6312, a link.

##### Elevator Pitch
Before: every Portfolio on the connection picks "Depends on" as its Dependency Field one by one, and a new
Portfolio shows an empty Dependencies view until someone remembers.
After: set **Dependency Field for Portfolios** to "Depends on", save → the dialog says how many follow it; after its
update, a follower's **Features → Dependencies** shows PAY-112 waiting on PAY-98.
Decision enabled: whether a new Portfolio forecasts with its dependencies from day one.

**Domain examples**
1. "Payments Platform" had no Dependency Field → follows "Depends on"; PAY-112 waits on PAY-98 after its update.
2. "Card Issuing" owns "Blocked By Keys" → unaffected.
3. "Merchant Onboarding" follows and has Ignore Dependencies switched on → the switch stays its own and still applies.

**UAT scenarios**
```gherkin
Scenario: A following Portfolio reads dependencies from the default
  Given "Payments Platform" follows the Dependency Field default "Depends on"
  When its update finishes
  Then Features → Dependencies shows PAY-112 waiting on PAY-98

Scenario: An owning Portfolio is unaffected
  Given "Card Issuing" owns "Blocked By Keys"
  When the default changes
  Then "Card Issuing" reads "Blocked By Keys" and no update starts for it

Scenario: Ignore Dependencies stays the Portfolio's own
  Given "Merchant Onboarding" follows the default and has Ignore Dependencies on
  When its update finishes
  Then its dependencies are still ignored
```

**Acceptance criteria**
- [ ] AC-8.1 A follower's dependencies after its update are those the default names (field or link, per #6300 /
  #6312).
- [ ] AC-8.2 An owning Portfolio is unaffected and no update starts for it.
- [ ] AC-8.3 Ignore Dependencies is never inherited.
- [ ] AC-8.4 The save dialog counts the followers and the log entry names them (AC-F.1 – F.5).
- [ ] AC-8.5 AC-4.4 at Portfolio grain for the Dependency Field.

**Technical notes** — C2: `Portfolio.cs:25`; `DependencySourceSelector` (S4) reads the effective value from the
resolver (L3), so a followed field or link reads exactly as an owned one; in the fingerprint (`FetchFingerprint.cs:52`,
`:94`). `DependenciesComponent.tsx:33-49` gains the entry; Ignore Dependencies untouched. Jira and ADO only (D21).
Brief: `slices/slice-09-6313-dependency-field-default.md`.

---

#### US-09 — Set the Portfolios' Size Estimate Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 10

**Who.** config-admin as System Admin whose Portfolios size Features from one estimate field (e.g. "T-Shirt Size").

##### Elevator Pitch
Before: a new Portfolio starts without a Size Estimate Field, so its not-yet-broken-down Features fall back to the
Portfolio default size although the estimate is in the tracker.
After: set **Size Estimate Field for Portfolios** to "T-Shirt Size", save → the dialog says how many follow it; after its update a
follower's not-yet-broken-down Feature PAY-201 shows its estimated size from "T-Shirt Size".
Decision enabled: whether forecasts for early Features rest on the teams' estimate or on one typed default.

**Domain examples**
1. "Q3 Roadmap" follows; PAY-201 estimated 8 in "T-Shirt Size" → its estimated size is 8.
2. "Card Issuing" owns "Story Points Sum" → unaffected.
3. "Merchant Onboarding" follows while "T-Shirt Size" is empty on MO-14 → MO-14 uses the Portfolio default size, as
   an empty own field does today.

**UAT scenarios**
```gherkin
Scenario: A following Portfolio sizes Features from the default field
  Given "Q3 Roadmap" follows "T-Shirt Size" and PAY-201 has 8 in it
  When its update finishes
  Then PAY-201's estimated size is 8

Scenario: An owning Portfolio is unaffected
  Given "Card Issuing" owns "Story Points Sum"
  When the default changes
  Then "Card Issuing" sizes from "Story Points Sum" and no update starts for it

Scenario: An empty value falls back as today
  Given "Merchant Onboarding" follows and MO-14 has no "T-Shirt Size"
  When its update finishes
  Then MO-14 uses the Portfolio's default size
```

**Acceptance criteria**
- [ ] AC-9.1 A follower's Features carry the estimated size the default field holds, after its update.
- [ ] AC-9.2 An owning Portfolio is unaffected and no update starts for it.
- [ ] AC-9.3 An empty value behaves as an empty own field does today.
- [ ] AC-9.4 The save dialog counts the followers and the log entry names them (AC-F.1 – F.5).
- [ ] AC-9.5 AC-4.4 at Portfolio grain for the Size Estimate Field.

**Technical notes** — C3: `Portfolio.cs:23`, read at fetch time (`JiraWorkTrackingConnector.cs:1311-1316`,
`AzureDevOpsWorkTrackingConnector.cs:1207-1218`) through the resolver (L3); in the fingerprint (`FetchFingerprint.cs:51`,
`:93`). `FeatureSizeComponent.tsx:178` gains the entry. Jira and ADO only (D21). Brief:
`slices/slice-10-6313-size-estimate-field-default.md`.

---

#### US-10 — Set the Portfolios' Feature Owner Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 11

**Who.** config-admin as System Admin whose Portfolios record each Feature's owning Team in one field
(e.g. "Owning Team").

##### Elevator Pitch
Before: each Portfolio names "Owning Team" as its Feature Owner Field separately; on a new Portfolio every Feature
shows no owning Team.
After: set **Feature Owner Field for Portfolios** to "Owning Team", save → the dialog says how many follow it; after its update a
follower's Feature PAY-112 shows "Ledger" as its owning Team.
Decision enabled: which Team to ask about a Feature, without opening the tracker.

**Domain examples**
1. "Payments Platform" follows; PAY-112 has "Ledger" in "Owning Team" → owning Team "Ledger".
2. "Card Issuing" owns "Delivery Team" → unaffected.
3. "Q3 Roadmap" follows; PAY-230 has no value → no owning Team, as an empty own field gives today.

**UAT scenarios**
```gherkin
Scenario: A following Portfolio reads owning Teams from the default field
  Given "Payments Platform" follows "Owning Team" and PAY-112 has "Ledger" in it
  When its update finishes
  Then PAY-112 shows "Ledger" as its owning Team

Scenario: An owning Portfolio is unaffected
  Given "Card Issuing" owns "Delivery Team"
  When the default changes
  Then "Card Issuing" reads "Delivery Team" and no update starts for it

Scenario: An empty value gives no owner
  Given "Q3 Roadmap" follows and PAY-230 has no "Owning Team"
  When its update finishes
  Then PAY-230 shows no owning Team
```

**Acceptance criteria**
- [ ] AC-10.1 A follower's Features show the owning Team the default field holds, after its update.
- [ ] AC-10.2 An owning Portfolio is unaffected and no update starts for it.
- [ ] AC-10.3 An empty value behaves as an empty own field does today.
- [ ] AC-10.4 The save dialog counts the followers and the log entry names them (AC-F.1 – F.5). The Portfolio's own
  Owning Team setting is not inherited.
- [ ] AC-10.5 AC-4.4 at Portfolio grain for the Feature Owner Field.

**Technical notes** — C4: `Portfolio.cs:21`, read at fetch time (`JiraWorkTrackingConnector.cs:1245-1250`,
`AzureDevOpsWorkTrackingConnector.cs:1200-1205`) through the resolver (L3); in the fingerprint (`FetchFingerprint.cs:50`,
`:92`). `OwnershipComponent.tsx:34-48` gains the entry; `OwningTeamId` stays the Portfolio's own. Shown on Jira, Azure
DevOps and CSV connections (D21; on CSV the extrapolation reads the Feature Owner Field from the Additional Field
values CSV fills, `WorkItemService.cs:756-772`). Brief: `slices/slice-11-6313-feature-owner-field-default.md`.

---

#### US-11 — Set the Estimation once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 12

**Who.** config-admin as System Admin whose Teams all estimate in Story Points; Team admins with their own scale.

##### Elevator Pitch
Before: Story Points is picked as Estimation Field on each Team and Portfolio, with "Points" typed as unit each time.
After: set **Estimation for Teams** to Story Points / "Points", save → the dialog says how many follow it and that
nothing needs updating; a follower's estimation views use Story Points with "Points" on the next page load.
Decision enabled: whether estimation views can be compared across Teams because they read the same field.

**Domain examples**
1. "Fraud Signals" follows → its estimation chart reads Story Points labelled "Points".
2. "Design Studio" owns T-shirt categories XS–XL → unaffected.
3. The default is cleared → "Fraud Signals" shows no estimation, as an unset own field does today.

**UAT scenarios**
```gherkin
Scenario: A following Team uses the default estimation at once
  Given "Fraud Signals" follows the Estimation default Story Points / "Points"
  When its estimation view is next opened
  Then it reads Story Points labelled "Points", and no update was started

Scenario: An owning Team keeps its scale
  Given "Design Studio" owns T-shirt categories XS to XL
  When the default changes
  Then "Design Studio" keeps its categories

Scenario: Clearing the default
  Given "Fraud Signals" follows
  When the System Admin clears the Estimation default
  Then "Fraud Signals" shows no estimation
```

**Acceptance criteria**
- [ ] AC-11.1 Field, unit, non-numeric flag and categories follow or are owned as one bundle (D16).
- [ ] AC-11.2 A change shows on the next page load and starts no update.
- [ ] AC-11.3 Clearing the default leaves followers with no estimation.
- [ ] AC-11.4 When owners follow it, the save dialog counts the followers and says no update is needed (plain
  Save, "Change the Estimation default?"); the log entry names them.
- [ ] AC-11.5 On the connection page the Estimation default reuses the owner's Estimation editor inline (field, unit,
  non-numeric flag, and categories when non-numeric), collapsed until a field is chosen, once for Teams and once for
  Portfolios; a following owner's page shows "Connection default (Story Points, Points)" (UI-5).
- [ ] AC-11.6 AC-4.4 at Team and Portfolio grain for the Estimation bundle: an Estimation Field pointing at a deleted
  Additional Field becomes *Connection default* at the upgrade.

**Technical notes** — C5: `WorkTrackingSystemOptionsOwner.cs:68-74`, read at read time by `EstimateNormalizer.cs:71-90`,
not in the fetch fingerprint, so slice 06's queue does nothing by construction (D13). One mode column per owner for
the bundle; defaults per grain (Teams, Portfolios), confirmed at DESIGN. `EstimationFieldComponent.tsx`
(`ModifyTeamSettings.tsx:293`, `ModifyProjectSettings.tsx:301`) is reused inline on the connection page (UI-5).
Offered on Jira, ADO and CSV (D21).
Brief: `slices/slice-12-6313-estimation-default.md`.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone**: name the link (Jira) → say when it matched nothing → the same on Azure DevOps → set a default once →
reach the followers' data → the next settings. ("Bring hand-configured owners along" was dropped with slice 07, UI-4.)

| Slice | ADO Story | User story | Goal | Est. | Brief |
|---|---|---|---|---|---|
| 01 | #6300 | US-01 | Jira Portfolio reads dependencies from a named one-way phrase | ~1d | `slices/slice-01-6300-jira-dependencies-from-a-named-link-phrase.md` |
| 02 | #6300 | US-02 | A named Jira link that matched nothing says so on Refresh History | ~0.5d | `slices/slice-02-6300-a-named-link-that-matched-nothing-says-so.md` |
| 03 | #6312 | US-03 | ADO relation type accepted where a field name goes; warns until read | ~0.5d | `slices/slice-03-6312-ado-relation-type-accepted-as-a-reference.md` |
| 04 | #6312 | US-03 | ADO Portfolio reads dependencies from the named relation end | ~1d | `slices/slice-04-6312-ado-dependencies-from-a-named-relation-type.md` |
| 05a | #6304 | US-04 | Teams' Parent Override default, end to end, with the dialog count and log entry | ~1d | `slices/slice-05a-6304-team-parent-override-default.md` |
| 05b | #6304 | US-04 | Referenced fields cannot be deleted; older payloads change nothing | ~0.5d | `slices/slice-05b-6304-referenced-fields-and-older-payloads.md` |
| 06 | #6304 | US-05 | A changed default updates exactly the followers | ~0.5d | `slices/slice-06-6304-default-change-updates-the-followers.md` |
| ~~07~~ | ~~#6304~~ | ~~US-06~~ | ~~Owners matching a default follow it in one step~~ — **dropped (UI-4)** | — | `slices/slice-07-6304-matching-owners-follow-the-default.md` (marked DROPPED) |
| 08 | #6313 | US-07 | Portfolios' Parent Override default | ~0.5d | `slices/slice-08-6313-portfolio-parent-override-default.md` |
| 09 | #6313 | US-08 | Dependency Field default | ~0.5d | `slices/slice-09-6313-dependency-field-default.md` |
| 10 | #6313 | US-09 | Size Estimate Field default | ~0.5d | `slices/slice-10-6313-size-estimate-field-default.md` |
| 11 | #6313 | US-10 | Feature Owner Field default | ~0.5d | `slices/slice-11-6313-feature-owner-field-default.md` |
| 12 | #6313 | US-11 | Estimation bundle default | ~1d | `slices/slice-12-6313-estimation-default.md` |

**Release points (L8):** #6300 after 02 (not after 01 — see US-01); #6312 after 04 (03 alone is safe by AC-3.9);
#6304 after 06 — 05a and 05b each leave `main` releasable, and D17 and the G-R1 (b) PUT rule land in 05b; #6313
after each of 08–12, every one a complete setting.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice is not thin | Resolved by the 05a / 05b split (D22). 05a still carries migration, resolver, connection section, dropdown entry and the dialog count + log entry — the minimum for one setting to be followed end to end; nothing in it is removable without leaving the mechanism unusable. |
| A new abstraction ships first | Pass. ADO's relation-type lookup arrives in 03 as user-visible validation; the resolver, dialog count, log entry and defaults section in 05a; later slices reuse them. |
| No slice disproves a pre-commitment → decoration | Pass — each brief names its hypothesis (01 one-way phrase reads direction; 03 relation types reachable with `vso.work`; 04 no extra request; 05a fingerprints survive the upgrade, except deleted-field repairs; 05b an older client round trip is harmless; ~~07 hand-configured owners want to follow~~ (dropped, UI-4); 08–12 each setting really is per connection). |
| Synthetic data only → plumbing | 01–04 dogfood on the dev instance's real Jira and ADO (P2, P8). 05a–12 on the dev instance's real connections (`:5169`) plus demo data for E2E and screenshots. |
| 2+ slices identical except for scale → merge | 08–11 look alike but each ships a different setting alone, at the maintainer's request (D7). 03 and 04 stay apart because 03 alone can end #6312 cheaply if the relation-type list is unreachable. |

### Prioritisation

1. **#6300 (01, 02)** — the reported instance is Jira; 02 at once, because until it ships 01's failure is silent.
2. **#6312 (03, 04)** — 03 first because it can disprove reachability cheaply; 04 closes the S14 trap in the slice
   that makes a relation readable.
3. **#6304 (05a, 05b, 06)** — 05a is the mechanism and where D10's consequence becomes visible (AC-F); 05b
   before anything depends on the PUT contract (G-R1 (b)); 06 at once, otherwise a changed default is invisible for
   hours. On a hand-configured installation the default reaches existing owners only when their admins switch them
   to *Connection default*, one by one.
4. **#6313 (08 → 12)** — 08 (Steve's Portfolio parent case), 09 (builds on #6300 / #6312), 10, 11, then 12 (no
   refetch, lowest risk).

---

## Wave: DISCUSS / [REF] Out of Scope

| Not built | Why |
|---|---|
| Matching ADO relation types by display name ("Predecessor", "Related") | Localised and renameable — the reason ADO's default already keys on the reference name (S3). |
| ADO Parent Override from a relation type (6028's ADO half) | Not asked; ADO's Parent Override stays field-only (S10); AC-3.10 states what a relation-type field does there. |
| Following states, work item types, state mappings, cutoff, blocked rules, wait states | Maintainer decision (D7); they legitimately vary per Team and shape the query. |
| Several dependency link types per Portfolio; union with the default | D3. |
| A Team-level dependency link type | D4. |
| Linear / CSV / ServiceNow link types | L4. |
| Defaults on Linear and ServiceNow, and the C1–C3 defaults on CSV | D21 — those trackers do not honour the settings. |
| Distinguishing, in the dialog or log entry, owners that follow because they were empty at the upgrade from owners an admin set to follow | Not asked; would need a stored marker. *Still Open* Q1. |
| A who-follows / who-differs view, a follower count on the connection page, and moving several owners onto a default at once (slice 07) | **Dropped** by the maintainer (UI-4, 2026-10-16). An owner hand-set to the default's value stays *Own*; its admin switches it on its own settings page. |
| Instance-wide defaults across connections | A default points at one connection's own Additional Fields. |
| Inheriting Ignore Dependencies, or a Portfolio's own Owning Team setting | Planning choices, not tracker facts (AC-8.3, AC-10.4). |

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Brownfield — extend existing paths; no greenfield skeleton** (maintainer: "Depends").

- #6300 / #6312: Additional Fields → Validate (Jira: Epic 6028 resolver; ADO: field lookup, then relation types
  from slice 03) → `DependencySourceSelector` → `Feature.DependsOnReferences` → Features → Dependencies; findings on
  Refresh History. Slice 01 is the skeleton; 04 extends it to ADO.
- #6304: connection edit page (S16) → save dialog with follower counts (Story 6307's, UI-3) → connection PUT →
  resolver → `FetchFingerprint.For` → connectors → stored records; the save writes one log entry naming the
  followers; → `ConnectionFetchShapeChanged` → queue → Task Manager → Activity. Slice 05a is the skeleton; 06 joins
  the queue.

E2E: one walking skeleton for #6304 on demo data (set a default, see the follower count in the save dialog, see a
Team follow). #6300 /
#6312 are covered below E2E unless demo data can carry a named link.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Port | Guard |
|---|---|---|
| Name the link | existing Additional Fields editor + Validate | `SystemAdmin` (unchanged) |
| Select it | existing Dependency Field dropdown | `PortfolioWrite` (unchanged) |
| Read the result / warning | existing Features → Dependencies; existing Refresh History (D5a) | unchanged |
| Set a default; see the follower count | new "Defaults for Teams and Portfolios" section on the connection page (UI-1), existing connection PUT, Story 6307's save dialog (UI-3); names in Settings → System Info → Logs | `SystemAdmin` |
| Follow / override | existing dropdowns on Team / Portfolio settings, one new entry "Connection default (value)" (S22, UI-2) | `TeamWrite` / `PortfolioWrite` |
| Clients | `PUT /v1/teams/{id}`, `/v1/portfolios/{id}`, `/v1/worktrackingsystemconnections/{id}` | additive fields only; no mode on a PUT keeps the stored mode (G-R1 (b)) |

---

## Wave: DISCUSS / [REF] Outcome KPIs

Self-hosted: every KPI is `per_instance` or `vendor_demo_only`. Whether a usage-data event should report any of them
is the DEVOPS wave's question (house rule) — see the checklist.

| KPI | Who / does what | Target | Measured by | Baseline |
|---|---|---|---|---|
| `OUT-6303-dependencies-recovered` | Portfolios that model waits with a non-default link show them | **100%** of Features whose named link points at a Feature Lighthouse stores show that dependency after the update (allowing only counterparts outside every Portfolio, which epic-4365 already reports) | On the dev instance (`:5169`): count Features in the slice-01 Jira Portfolio and the slice-04 ADO Portfolio carrying ≥ 1 link of the named type to a stored Feature, against Features with ≥ 1 dependency, before and after; recorded in slices 01 and 04. Steve's customer: same count, n = 1 | **0%** — no such Feature shows a dependency today (Jira silently, S6/S7) |
| `OUT-6303-set-once` | System Admin configures a setting for N owners | 1 connection save instead of N owner saves; 0 per new owner | Saves counted in the slice-05a dogfood script | N |
| ~~`OUT-6303-followers-share`~~ | **Dropped** (OQ-7, 2026-10-16, CA-D7) | — | Slice 07 was the lever and there is no usage-data event; the remaining claim (new owners start on *Connection default*) is guaranteed by D11 and pinned by AC-4.5, so a KPI adds nothing | — |
| `OUT-6303-no-change-on-upgrade` | Owners whose effective setting changed through the upgrade | **0**, except owners whose setting pointed at a deleted Additional Field (repaired by D10, named in the release notes) | Upgrade test (AC-4.4) | — |
| `OUT-6303-no-surprise` | Admin learns how many owners a default affects before the save completes, and who, after | 100% of default saves with followers show the follower count before the save completes; 100% of default saves write the log entry naming the followers (or saying none follow) | AC-F test coverage; dogfood | no count, no entry |
| `OUT-6303-no-extra-requests` | Tracker requests per refresh | Jira unchanged; ADO ≤ +1 relation-type listing per connection per Validate and per refresh | Request count vs baseline (AC-1.6, AC-3.7) | — |

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | Maintainer decisions | **Done** (2026-10-16, incl. the ADO amendment and the Story regrouping). |
| P2 | A Jira project (dev instance or the test Cloud site) with a non-Blocks link between Features | Check before slice 01's dogfood; creating a "Dependency" link on a few Features is an admin step. |
| P3 | Jira link-type endpoint on Data Center | Shipped since 6028; DC verification still owed per ADR-193. Not blocking. |
| P4 | DESIGN: loading and could-not-load looks for the connection's defaults group | Done at DESIGN (UI States). |
| P5 | DESIGN: what deleting an owner-referenced Additional Field does today (for D17 in 05b) | Answered at DESIGN (F1): today it leaves a dangling id, with no FK and nothing to clear it. On Jira every Work Item then silently loses its parent, and a dangling Dependency Field reads zero dependencies. D17 closes it going forward; DDD-21 repairs it at the upgrade. |
| P6 | `docs/ci-learnings.md` pre-applied: zod `.nullable()` for nullable DTO fields; S107 constructor width on the connection controller; CA1859/CA1861; `CreateMigration`, migration DLL build order, `HistoricalSchemaPatch` if a patched table gains columns | DELIVER. |
| P7 | Each Story finished before the next starts (D19) | Ordering rule. |
| P8 | ADO: relation-type listing reachable with the PAT scope customers grant (`vso.work`); an ADO project on the dev organisation with a non-Predecessor relation between Features | Slice 03's hypothesis; the list returned is recorded in its brief. |
| P9 | **Gates G-R1 and G-07** answered by the maintainer | **Done** (2026-10-16): G-R1 option (b); G-07 moot (slice 07 dropped). |
| P10 | ServiceNow row of the D21 table | **Done** at DESIGN: hidden. |

---

## Wave: DISCUSS / [REF] Risks

- **R1 — Older payloads and D10 — mitigated (G-R1 decided, option (b)).** Under the original D18 (absent mode =
  *Own*), a client that drops unknown properties and PUTs back an empty Parent Override would have turned a
  following owner into a deliberate None, silently. The maintainer chose instead: *a PUT without the mode keeps the
  stored mode, unless the payload sends a value for that setting — present and non-empty — that differs from the
  effective one, which makes the owner Own with that value; no mode with an empty or absent value changes nothing*.
  AC-4.10 pins it: a scripted GET-then-PUT round trip changes no owner's mode or effective value, for echoing and
  property-dropping clients alike. Residual: a client that deliberately sends a different value without a mode
  makes the owner *Own* — which is what that request means.
- **R2 — D10's accepted consequence.** An owner that was None on purpose follows the first default. The save dialog's
  follower count and the log entry naming the followers are the mitigation; slice 05a's dogfood counts how many
  owners on the dev instance are empty today.
- **R3 — #6312's value on Services.** If Services exposes only system relation types, #6312 serves teams that
  record waits with Successor / Predecessor the other way round, and on-premises Server users with custom types;
  Related is non-directional and reads nothing (CA-D5). Slice 03 records what the dev organisation offers; #6312
  ships either way.

---

## Wave: DISCUSS / [REF] DISCUSS Checklist (no silent N/A)

- **RBAC impact — yes, no new permission (D14).** Defaults through the connection PUT (`SystemAdmin`); overrides
  through Team / Portfolio PUT (`TeamWrite` / `PortfolioWrite`). The followed value's name travels on the owner's
  own settings DTO. (Slice 07's bulk follow, which raised G-07, was dropped — UI-4.) #6300 / #6312 add no route or
  guard. UI gating via `useRbac()` only.
- **Lighthouse-Clients CLI/MCP versioning — none (D18 as amended by G-R1).** Payloads pass through untyped (S23);
  new properties are additive; MCP reads Team settings only for cycle-time names. #6300 / #6312 change no payload.
  No client release: a PUT without the mode keeps the stored mode. **Under R1:** a scripted GET-then-PUT round trip
  of Team and Portfolio settings through the CLI (`lh team update` / `lh portfolio update` with the payload a GET
  returned) changes no owner's mode or effective value (AC-4.10); DISTILL runs it in both the echoing and the
  property-dropping shape, and both must pass.
- **Website marketing surface — checked in DISCUSS (2026-10-16).** `/storage/repos/website` names dependencies in
  the feature comparison (`src/pages/Lighthouse.tsx:820`, "Blockers from Jira, Azure DevOps and Linear links …
  No setup.") and in the dated v26.8.31.7 What's New entry (`src/components/LighthouseWhatsNew.tsx:92-97`,
  "Predecessor links in Azure DevOps, blocked-by links in Jira"). Both stay true: the defaults do not change, so
  "No setup" holds, and the What's New entry is history. At finalize: optionally add "or the link type you name"
  to the comparison tooltip, and decide whether "set once per connection" earns its own What's New entry. Nothing
  on the site mentions Parent Override, connection defaults or estimation.
- **Docs pages needing prose at finalization**: `docs/portfolios/edit.md` (§Parent Override Field :235, §Dependency
  Field :301-312, Feature size, ownership, estimation); `docs/teams/edit.md` (§Parent Override Field :363-373,
  estimation); `docs/settings/worktrackingsystems.md` (§Additional Fields :45 — link and relation types as
  references — plus new "Defaults for Teams and Portfolios", which connection types show which defaults (D21), the
  save dialog's follower counts and the log entry, deletion refusal); `docs/settings/systeminfo.md` (Refresh
  History: the dependency warnings; Logs: the default-save entry); `docs/concepts/worktrackingsystems/jira.md` (:162-175, :266-271);
  `docs/concepts/worktrackingsystems/azuredevops.md` (:101-114, :203-207); `docs/features/features.md:89`;
  `docs/concepts/howlighthouseforecasts.md:322`; release notes vNext (crediting Steve Pereira, D20; and, per slice
  that repairs values pointing at a deleted field, the D10 sentence from `design/upstream-changes.md`); @screenshots
  of the connection page and the Team / Portfolio sections, one per theme.
- **Usage data** — answered at DEVOPS by the maintainer: **none**, because a connection default is set once at setup
  and an event would count configuration saves, not whether the feature helps anyone. `UsageDataEventName` unchanged.
- **Terminology** — L6.
- **UI sketches** — shown and approved by the maintainer before AFK (UI-1 … UI-6 in
  `design/maintainer-decisions-2026-10-16.md`): the "Defaults for Teams and Portfolios" section, the owner dropdown
  entry, the save dialog's variants — with and without the update question — and the log entry's wording, the
  Estimation editor reused inline, and the Refresh History / refusal copy. DISTILL copies them into its
  maintainer-decision block. #6300 / #6312 add no UI beyond copy under the Dependency Field and the Refresh History
  message.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Problem in domain language | PASS | Every live user story opens from a named admin's pain or carries its own Before line (US-01 … US-05, US-07 … US-11; US-06 dropped) |
| 2 | Persona specific | PASS | A "Who" line on every live user story |
| 3 | ≥ 3 domain examples with real data | PASS | US-01 4, US-02 3, US-03 6, US-04 5, US-05 3, US-07 … US-11 3 each |
| 4 | UAT G/W/T, 3–7 | PASS | US-01 4, US-02 3, US-03 5, US-04 7, US-05 3, US-07 … US-11 3 each |
| 5 | AC from UAT | PASS | Each AC list restates its scenarios as observable outcomes; code references moved to Technical notes (M5); AC-F and AC-4.4 rewritten after DESIGN |
| 6 | Right-sized | PASS | Four ADO Stories of ~1.5–3d (D19; #6304 now ~2d); 12 live slices ≤ 1 day; 05 split decided (D22) |
| 7 | Technical notes | PASS | Feature-wide: S1–S23, C1–C5, L1–L8, D21 table. Per story: a **Technical notes** paragraph on every live user story, naming the code refs, the resolver (L3) and its slice brief |
| 8 | Dependencies tracked | PASS | P1–P10 all done or owned; G-R1 decided; G-07 moot; ServiceNow resolved at DESIGN |
| 9 | Outcome KPIs | PASS | Five KPIs, each with a numeric target and baseline (`OUT-6303-followers-share` dropped, CA-D7) |

Every live user story carries a literal `job_id`; none uses the infrastructure escape. Every live user story has its
own Elevator Pitch (Before / After / Decision enabled). G-R1 is decided; G-07 is moot.
**Verdict: READY** (re-confirmed after the DESIGN back-propagation).

---

## Wave: DISCUSS / [REF] Definition of Done

1. All slices shipped, a focused commit each, hypothesis answered in its brief.
2. Backend `dotnet build` zero warnings; `dotnet test` green with live-connector categories excluded; the
   `JiraIntegration` and `AdoIntegration` categories run deliberately for slices 01–04.
3. Frontend `pnpm test`, `pnpm build` zero warnings, Biome clean.
4. SonarQube Cloud: no new issues.
5. Stryker both stacks ≥ 80%, under `docs/feature/epic-6303-configure-once-per-connection/mutation/`.
6. Migrations expand-only via `CreateMigration`; an upgrade test proves AC-4.4.
7. Docs, screenshots and the website follow-ups at feature finalization, in configurable terms.
8. Release-notes entry crediting Steve Pereira; ADO items tagged `Release Notes`; the Epic stops at Resolved, never
   Closed.

---

## Wave: DISCUSS / [REF] Still Open

- **Q1** — Should the log entry mark which followers follow only because they were empty at the upgrade? It would
  need a stored marker. Not built unless asked.
- ~~G-R1, G-07~~ — G-R1 **decided** by the maintainer on 2026-10-16 (option (b), D18 amended); G-07 **moot**, slice 07
  dropped (UI-4). Nothing open on them.
- ~~ServiceNow in the D21 table~~ — resolved at DESIGN: hidden (P10).
- **#6313 estimate** — ~3d by the briefs against ~2.5d in ADO (Scope Assessment).

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions
D1–D22 (maintainer, 2026-10-16), gates G-R1 (decided) and G-07 (moot), and L1–L8. **Changed since pass 1**: D1 (the
Dependency Field accepts any Additional Field, resolved per tracker); D6 (ADO back in, as its own Story #6312); D7 (five
settings); D10 (empty owners follow at the upgrade); D12 (save notice); D19 (four Stories); D22 (05a / 05b). Pass 3
review fixes: per-story Elevator Pitches, examples, scenarios and Who lines for US-07 … US-11; Refresh History as
the findings surface (D5a); observable ACs; AC-3.9; AC-4.10; D21; numeric KPI target. **Changed after DESIGN**
(maintainer, 2026-10-16): D10 (deleted-field values repaired at the upgrade), D12 / AC-F (save dialog count and log
entry instead of a save notice), D21 (CSV Feature Owner; ServiceNow hidden), slice 07 and US-06 dropped, KPI
`OUT-6303-followers-share` dropped — see *Changed Assumptions (from DESIGN)* below.

### Requirements summary
#6300 / #6312: a Portfolio's Dependency Field may name a link type as well as a field; each tracker resolves it the
way Parent Override resolves on Jira — Jira by phrase, ADO by relation reference name and end; direction comes from
the named end; a two-way name, a non-directional ADO relation type and a link nobody carries all say so on Refresh
History. #6304: the connection carries a default for the Teams' Parent Override Field; owners follow or own; empty
owners (and owners pointing at a deleted field) follow from the upgrade; a default save with followers says how
many follow in the save dialog before it completes and names them in one log entry; changes reach the followers
through the existing fingerprint and queue, shown in Task Manager → Activity. #6313: the same for four more settings.

### Constraints established
- `null` keeps meaning None; following is a mode column (D9, L2).
- One resolver for the effective value (L3); fingerprints unchanged across the upgrade except for owners whose
  setting pointed at a deleted field (AC-4.4).
- Field lookup first everywhere, so no configured reference changes meaning (D1).
- No extra Jira request (AC-1.6); ADO at most one relation-type listing per connection (AC-3.7); the ADO
  relations-skip trap closed (AC-3.5); no silent interim on ADO (AC-3.9).

### Upstream changes
DISCOVER and DIVERGE were **skipped by explicit maintainer decision**. Nothing upstream to contradict. SSOT written
2026-10-16 and updated for the Story regrouping and again after DESIGN: three jobs in `docs/product/jobs.yaml`
(`job-config-admin-see-which-owners-differ-from-the-connection` now `status: deferred`); `config-admin.yaml` (jobs,
frustrations, vocabulary); journey `epic-6303-configure-once-per-connection.yaml`; REVISION notes on
`epic-4365-dependencies.yaml` D4 and `parent-from-issue-links.yaml` D7.

---

## Wave: DISCUSS / [REF] Changed Assumptions (from DESIGN, maintainer 2026-10-16)

Back-propagated from `design/upstream-changes.md` (U-1 … U-21) and `design/maintainer-decisions-2026-10-16.md`.
The DISCUSS text above has been rewritten to match; this list records what was assumed before and why it changed.

| # | Assumed in DISCUSS | Now | Why (maintainer) |
|---|---|---|---|
| CA-D1 | Every default save shows an informational **save notice** after the save, naming who follows (D12, AC-F) | No notice after the save; the page navigates to the Overview as Story 6307 does. Before the save completes, Story 6307's save dialog counts the Teams / Portfolios that follow each changed default (no dialog when nobody follows); the save writes one Information log entry naming them (Settings → System Info → Logs); their updates show in Task Manager → Activity | P-5, UI-3: reuse the existing dialog and log instead of a new surface; names belong in the log, not a dialog |
| CA-D2 | At the upgrade, a set value always stays *Own* (D10) | A value naming an Additional Field the connection no longer has is treated as empty and becomes *Connection default*; those owners read the tracker's own value again and download everything once at their next scheduled update; called out in the release notes | P-12: a value pointing at nothing was already broken; repairing it is a visible fix |
| CA-D3 | Slice 07 (who follows / who differs list, bulk follow) completes #6304 | Slice 07 and US-06 dropped; #6304 is 05a, 05b, 06 (~2d), released after 06. An owner hand-set to the default's value stays *Own*; its admin switches it on its own settings page. G-07 is moot | UI-4 |
| CA-D4 | Feature Owner default not offered on CSV; ServiceNow to verify | Feature Owner offered on CSV (the extrapolation reads it from CSV's Additional Field values); ServiceNow hidden (no Portfolio fetch, no Additional Field values read) | P-14; DESIGN verified ServiceNow |
| CA-D5 | `System.LinkTypes.Related-Forward` is a usable ADO test vehicle | Related is non-directional: it reads nothing and warns (D2 kept); test vehicle is a directional type from slice 03's list | CA-6 |
| CA-D6 | The delete refusal names the Teams that follow the default | It names the default and the owners that use the field **as their own** (approved copy, UI-6) | UI-6 |
| CA-D7 | KPI `OUT-6303-followers-share` ≥ 80% after slice 07 | **Dropped** (coordinator, OQ-7, under AFK). Slice 07 was its lever; with it gone and no usage-data event (DEVOPS: none), its only remaining claim — new owners start on *Connection default* — is guaranteed by D11 and pinned by AC-4.5, so a KPI measures nothing a test does not already prove | OQ-7 |
| CA-D8 | Job `job-config-admin-see-which-owners-differ-from-the-connection` served by slice 07 | `status: deferred` in `jobs.yaml`; no story serves it now; the job is still real | UI-4 |

---

## Wave: DESIGN / [REF] Prior Wave Consultation

```
✓ docs/feature/epic-6303-configure-once-per-connection/feature-delta.md   (DISCUSS, all 1221 lines)
✓ docs/feature/epic-6303-configure-once-per-connection/slices/*.md        (13 briefs)
✓ docs/product/journeys/epic-6303-configure-once-per-connection.yaml
✓ docs/product/architecture/brief.md       (searched by heading; story-6301 and story-6307 deltas read in full)
✓ docs/product/architecture/adr-193-…      (§1's port confirmed never built: IWorkTrackingConnector.cs has no link-type member)
✓ docs/product/architecture/adr-238-…, adr-239-…, adr-140-… (passive fan-out), adr-167-… (nullable columns behind a mutator)
✓ docs/product/architecture/c4-diagrams.md (house L1/L2 style)
✓ ARCHITECTURE.md                          (§3, §4, §5, §7, §8, §16)
✓ docs/feature/parent-from-issue-links/feature-delta.md   (DESIGN precedent; DELIVER retired ADR-193's port, :819-822)
✓ docs/feature/story-6307-refresh-on-additional-fields-change/feature-delta.md (Locked Decisions)
✓ docs/ci-learnings.md                     (S107, S6964, CA1859/CA1861, CA1869, NUnit2045, zod .nullable(), migrations)
✓ Code on main at 9d7086d77                (file:line below)
✓ /storage/repos/lighthouse-clients/packages (names none of C1–C5, DependencySource or RefreshLog)
⊘ brief.md in full                         (>512 KB; read by section)
⊘ DEVOPS                                   (follows this wave)
```

**Scope** — Application / components. No new container, no new external system, no new bounded context: the
Team, the Portfolio and the connection are the existing config aggregates, and a setting's *source* is a value on
them. **Mode** — PROPOSE. Pass 1 (2026-10-16) drafted the options; pass 2 (2026-10-16) applies the maintainer's
decisions. No decision is left open: every row of the Decisions Table is **LOCKED** (forced by code or an earlier
decision) or **ACCEPTED** (decided by the maintainer in pass 2).

---

## Wave: DESIGN / [REF] What the Code Says That DISCUSS Could Not See

| # | Finding | Where | Consequence |
|---|---|---|---|
| F1 | The five owner settings are **plain `int?` columns with no foreign key**. Only write-back mappings have one (`SetNull`). Nothing clears an owner's reference when a field is removed. | `Data/LighthouseAppContext.cs:467-473` (the only FK to `AdditionalFieldDefinition` besides the connection's own); `API/WorkTrackingSystemConnectionController.cs:302-337` removes fields; no `…DefinitionId = null` anywhere | **P5 answered:** deleting an owner-referenced field today leaves a dangling id. On Jira a dangling Parent Override still counts as set, so the tracker's own parent field stops being read *and* the override resolves to nothing (`JiraWorkTrackingConnector.cs:1680-1685`, `:1694-1697`): every record loses its parent, silently. A dangling Dependency Field reads zero dependencies (`DependencySourceSelector.cs:30-31`, `:46`). D17 closes it going forward; DDD-21 repairs what is already dangling at the upgrade. |
| F2 | `FetchFingerprint.For` splices `ForConnection` into every owner's digest, and `FetchShapingPropertyGuardTest` expects every connection-level fetch-shaping property to be rendered there. | `Services/Implementation/WorkItems/FetchFingerprint.cs:24-30`, `:80-96`, `:108-112` | Rendering the defaults in `ForConnection` would change every stored fingerprint on release day (a full download for every owner — AC-4.4 broken) and would queue owners that own their value (D13 broken). The defaults reach the fingerprint **only** through each owner's effective value, which forces a per-owner before/after comparison at save — the alternative ADR-239 rejected as unnecessary. |
| F3 | After a connection save the page navigates to `/` (6307 sketch decision S3); the browser asks *before* Save, with counts from the connection list read (`usedBy`). | `pages/Connections/Edit/EditConnection.tsx:83-98`; ADR-239 decisions 3 (struck) and 8 | The maintainer keeps S3 (pass 2, P-5): no after-save notice. Who follows a changed default is shown **in that pre-save dialog** and written to the log at save (DDD-8, DDD-9). |
| F4 | `WorkItemRelatedSettingsChanged` reads only the connection id. | `API/Helpers/WorkTrackingSystemOptionsOwnerExtensions.cs:15-24` | L3's third caller is moot: it never read C1–C5 and needs no change. A follower that moves to another connection follows the new connection's default. |
| F5 | The Feature Owner Field is **honoured on CSV**: the tracker-neutral extrapolation reads it from the Feature's Additional Field values, which the CSV connector fills. ServiceNow throws on every Portfolio fetch and reads no Additional Field value. | `WorkItems/WorkItemService.cs:756-772`; `Csv/CsvWorkTrackingConnector.cs:244-254`; `ServiceNow/ServiceNowWorkTrackingConnector.cs:758-775` | D21's table changes in one cell (accepted, P-14): the Feature Owner default is offered on CSV. **P10 answered:** ServiceNow is N/A for all five; the group is hidden there. |
| F6 | `DependencySource` is persisted on each stored reference and mirrored as a **closed** zod enum. | `Models/Dependencies/DependencySource.cs`; `Data/LighthouseAppContext.cs:303`; `Lighthouse.Frontend/src/models/FeatureDependency.ts:15`, `:38` | A link the Portfolio named *is* a tracker link, so it is stored as `TrackerLink`; no enum member. |
| F7 | Refresh History shows **per-entity aggregate figures** built from int columns; today's dependency warnings go only to the log. | `Models/RefreshLog.cs`; `pages/Settings/SystemInfo/RefreshHistorySection.tsx:176-276`; `JiraWorkTrackingConnector.cs:1303-1308` | AC-1.3 / AC-2.x name phrases and "seen" lists, which an int column cannot carry: a text-bearing value on the row and a "latest run" reading in the section (DDD-15). |
| F8 | ADO relations are a **second request batch** (`GetWorkItemsInChunks(… WorkItemExpand.Relations …)`), skipped when a Parent Override is set and a Dependency Field is set. | `AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1131-1182`, `:648-649` | "Relations already arrive" holds relative to a Portfolio naming nothing (AC-3.7 stands). A relation-type Dependency Field must count as "from relations" (the S14 trap). |
| F9 | Azure DevOps's *Related* link is **non-directional**: its `rel` is `System.LinkTypes.Related`, with no `-Forward` / `-Reverse`; directional types are listed end by end. | Azure DevOps link-type reference; slice 03's spike confirms on the dev organisation (P8) | Maintainer, pass 2: D2 stands for ADO — a non-directional type or a base name without its end reads nothing and warns. US-03 example 2 and slice 04's test vehicle must use a directional type that exists (upstream change U-13). |
| F10 | `WorkTrackingSystemConnectionController` already has **7** constructor parameters (its `ILogger` among them); S107 fires above 7. Its `IDomainEventDispatcher` is used only by the 6307 fan-out. | `API/WorkTrackingSystemConnectionController.cs:19-26`, `:169`; `docs/ci-learnings.md` | Review iteration 2: the save's rules move to one application service, `IConnectionSaveService`, which **replaces the dispatcher** on the controller, so it stays at 7 (DDD-25). |
| F14 | The connection already has an **optimistic concurrency token**: `IsConcurrencyToken()` on `WorkTrackingSystemConnection.ConcurrencyToken`, applied by the PUT when the client sends it, a stale one answered 409 `concurrency-conflict` by `ConcurrencyConflictExceptionFilter`, and the page says "This connection was changed by someone else since you opened it". The token is optional on the request. | `Data/LighthouseAppContext.cs:316-318`; `API/WorkTrackingSystemConnectionController.cs:114-117`; `API/Filters/ConcurrencyConflictExceptionFilter.cs`; `ModifyConnectionSettings.tsx:55-57` | No new row version or CAS: the save keeps using it, and "before" is rendered from the row read in the same unit of work (DDD-26). |
| F15 | Connector instances are **cached per tracker for two minutes** and shared across connections. | `Factories/WorkTrackingConnectorFactory.cs:15`, `:39` | Anything remembered on a connector outlives the refresh and crosses connections: the ADO relation-type listing must be a per-call value (DDD-14). |
| F16 | `RefreshLog` records the run's `Mode` (`SyncMode.Full` / `Delta`). | `Models/RefreshLog.cs:26`, `Models/SyncMode.cs` | Refresh History can read the findings of the latest **full** run, so a delta refresh cannot blank them (DDD-15). |
| F17 | Settings → System Info → Logs shows the Serilog file that `ILogger` writes; the shipped minimum level is Information and can be changed on that page. | `API/LogsController.cs:23`, `:82-84`; `Services/Implementation/SerilogLogConfiguration.cs`; `appsettings.json` (`Serilog:MinimumLevel:Default`) | The Information log entry (DDD-8) is visible there by default to `SystemAdmin`s; an admin who raises the level to Warning will not get it (accepted). |
| F18 | `EditConnection` sends the usage-data event `WorkTrackingSystemFieldsChanged` whenever the save dialog returned a choice. | `pages/Connections/Edit/EditConnection.tsx:88-97` | A dialog opened for a default alone must not send it; the choice says whether Additional Fields changed (DDD-9). |
| F11 | The five settings are read directly by about fifteen sites: both connectors, both selectors, `EstimateNormalizer`, `BaseMetricsService` (`:1213-1338`), `WorkItemService.cs:757`, `WorkItemEstimateDto`, `FetchFingerprint.For`, the settings DTOs. | search `AdditionalFieldDefinitionId` and the estimation members | A resolver every reader must remember to call is the bug class L3 names. Making the owner's existing property *be* the effective value leaves every reader correct without touching it (DDD-4). |
| F12 | The owner settings forms list a connection's Additional Fields from the connection list, which is `SystemAdmin`-only. | `pages/Teams/Edit/EditTeam.tsx:81-89`; `API/WorkTrackingSystemConnectionsController.cs:57-58` | A Team admin without connection rights may not see the field list at all today (OQ-4). AC-4.6 rests on the followed name travelling on the owner's own settings DTO (D14, DDD-12). |
| F13 | The log read in Settings → System Info is **`SystemAdmin`-only**. | `API/LogsController.cs:23` (class-level `RbacGuard(SystemAdmin)`) | The save's log entry (DDD-8) may name Teams and Portfolios: only readers who may already see every Team and Portfolio can read it. |

---

## Wave: DESIGN / [REF] DDD List

| # | Question | Verdict | Status |
|---|---|---|---|
| DDD-1 | Where is the mode stored? | One source column per inheritable setting on the owner's own table (Teams: Parent Override, Estimation; Portfolios: those two plus Dependency, Size Estimate, Feature Owner), each added in the slice that ships its setting | **ACCEPTED** (P-1 a; ADR-240) |
| DDD-2 | Where do the connection defaults live? | Flat nullable columns on the connection row: one field id per default; the Estimation bundle as four columns per grain | **ACCEPTED** (P-2 a; ADR-240) |
| DDD-3 | What does the mode look like in the database? | `SettingSource { ConnectionDefault = 0, Own = 1 }`, int-persisted, append-only. Migration per slice: add the column with default 0; set `Own` where the value names a field **of the owner's own connection**; clear the value where it names nothing (DDD-21). New rows default to 0, which is D11 | **LOCKED** — D9, D10 (as amended), D11, L2, the append-only enum rule |
| DDD-4 | Where does the single resolver live, and who calls it? | The owner's existing property (`ParentOverrideAdditionalFieldDefinitionId`, …) becomes a read-only **effective value**; the stored own value moves to a renamed C# property mapped to the **same column**; the rule is one pure function in `Models`. Every reader keeps reading the name it reads today | **ACCEPTED** (P-3 a; ADR-240) |
| DDD-5 | How are the own value and the mode written? | Only through two mutators per setting, *follow* and *own(value)*; private setters. Following clears the own value (for Estimation, the whole own bundle) | **LOCKED** (mutators) / **ACCEPTED** (clearing, P-4 a) |
| DDD-6 | How do the defaults reach the fetch fingerprint? | Never through `ForConnection`; only as each owner's effective value, in the owner's own slot of `For`. The guard test gains a third category for them | **LOCKED** — F2, AC-4.4, D13 |
| DDD-7 | Which owners does a connection save queue? | Those whose `FetchFingerprint.For` differs before and after the save, "before" rendered from the row read in the same unit of work, before anything is changed. Which owners moved is a pure rule in `Models` (two fingerprint maps in, owner ids out); `IConnectionSaveService` gathers the maps and publishes one `ConnectionFetchShapeChanged`, unchanged. A field-definition edit still queues everyone; an Estimation default queues nobody | **LOCKED** (ADR-241) |
| DDD-8 | How is "who follows" recorded at save? | **One Information log entry per save that changed a default**, written by `IConnectionSaveService` after `Save()`: the connection, each changed default (setting, grain, old and new display name) and its followers by name, and how many were queued. The PUT response is **unchanged** (ADR-239 §3 stands). Read in Settings → System Info → Logs, which shows the Serilog file at the shipped Information level and is `SystemAdmin`-only (F13, F17); the entry carries names only — no credentials, no tracker field values. A refused or stale save writes none | **ACCEPTED** (P-5 as changed by the maintainer; ADR-241) |
| DDD-9 | What does the browser show at save? | No after-save notice; the page navigates to `/` as today (S3). Before the save completes, the reused 6307 dialog (UI-3) shows how many Teams / Portfolios follow the changed defaults: **no dialog** when nobody follows any changed default and no Additional Field changed; *plain* (Cancel / Save) when only page-load defaults (Estimation) with followers changed, and in 05a for any changed default with followers; *update question* (Cancel / Save only / Save and update) when a fetch-time default with followers changed (slice 06 on) or an Additional Field changed. Names are never in the dialog. The dialog's choice says whether Additional Fields changed: `ISaveConnectionChoice` gains `additionalFieldsChanged: boolean`, set from `fieldsChangedSince(loadedFields, additionalFields)` (`ModifyConnectionSettings.tsx:575`), and `EditConnection.tsx:90` guards on `choice?.additionalFieldsChanged` instead of `choice` (F18). Shapes: update question → `{ startUpdates: <answer>, additionalFieldsChanged: <fieldsChangedSince> }`; plain Save → `{ startUpdates: true, additionalFieldsChanged: false }` (the request a dialog-less save sends); no dialog → no choice. The open condition at `:572-576` widens to "fields changed and used, or a changed default has followers" | **ACCEPTED** (P-5 changed; UI-3) |
| DDD-10 | Where do the dialog's counts come from? | The connection list read the page already makes: `usedBy` gains the number of followers per default (the existing `IWorkTrackingSystemConnectionUsage` port). No new fetch. Counts are as of page load and appear **only in the dialog**, never on the page (UI-1); the log entry, computed at save, is the record | **ACCEPTED** (P-6 a) |
| DDD-11 | Where does G-R1 (b) live? | In the owner's mutator that applies an incoming setting: mode present → applied; mode absent → `own(value)` only if the value is non-null and differs from the effective value **before** the request; else unchanged. Source members on the PUT DTOs are nullable. **Estimation bundle**: one value; non-null when its field is set; differs when the field, the unit (ordinal), the non-numeric flag or the ordered category list differs → `own(whole bundle)` (resolves OQ-3). A stale echo without a source can turn a follower into an owner: accepted, CA-8 | **LOCKED** — G-R1 (b), S6964 / **ACCEPTED** (bundle rule, AFK recommended option; ADR-240) |
| DDD-12 | What does a GET return per setting? | The existing id field carries the **effective** value; beside it the source and the followed default's display name | **ACCEPTED** (P-7 a) |
| DDD-13 | How does a Dependency Field that resolved to a link type get read? | `DependencySourceSelector` gains a third source, "a link type the Portfolio named", fed by the connector with the counterparts on the named end; references stored as `DependencySource.TrackerLink` | **LOCKED** — D1, D3, F6 (ADR-242) |
| DDD-14 | Where does ADO's relation-type lookup live? | Inside the ADO connector, not on `IWorkTrackingConnector`. Asked only when a reference missed the field list, at most once per connection per Validate and per refresh; matched case-insensitively on the listing's own reference names; a non-directional type or a base name without its end reads nothing and warns (D2, confirmed in pass 2). **The listing is never stored on the connector** (connectors are cached per tracker and shared, F15): it is a value created at the start of one Validate call or one owner's fetch, for that call's connection, passed down that call's chain and dropped with it | **LOCKED** (ADR-242) / **ACCEPTED** (memo scope, AFK recommended option) |
| DDD-15 | How do refresh findings reach Refresh History? | Evidence on the returned records (transient); `WorkItemService` turns it into at most one finding per kind per whole-query fetch; `SyncOutcome` carries the list; one new list-valued text column on `RefreshLog`. A delta run assesses nothing and writes none. The refresh-log read sends, per entity, the findings of its **latest `Mode == Full` run** with that run's time (whatever window the section shows), and the section shows those under the figures, so a delta refresh never blanks a standing warning (F16) | **ACCEPTED** (P-8 a / a; latest-full-run read, AFK recommended option; ADR-243) |
| DDD-16 | Where is D21 declared? | One static map in `Models` keyed by tracker; the connection DTO carries the supported set; the PUT refuses a default on an unsupported setting | **ACCEPTED** (P-9 a) |
| DDD-17 | D21 table after DESIGN | ServiceNow: N/A for all five, group hidden. CSV: Estimation **and Feature Owner** offered | **LOCKED** (ServiceNow, F5) / **ACCEPTED** (CSV Feature Owner, P-14 a) |
| DDD-18 | D17 refusal | In the connection save, after the incoming defaults are applied and before `Save()`: a removed field still named by a default or by an owner's own value refuses the whole save with **409**, `code: "additional-field-in-use"`, naming the defaults and those owners (UI-6 copy); nothing is deleted. Followers are covered through the default they follow. **The owners come from a fresh set-based query in `Commit`** (owners on this connection whose own value of any of the five settings is a removed id), not from the `Open` snapshot, so a Team saved with that field after `Open` is seen. The query's rows go to a pure, data-only refusal rule in `Models`; the controller maps its answer to the 409. Residual window (a Team save committing between that query and `Save()`) accepted and narrowed: the Team / Portfolio PUT refuses (400) an own value naming no Additional Field of its connection. **No FK** backs it (DDD-27) | **LOCKED** (placement) / **ACCEPTED** (409, P-10 a; fresh query and owner-side 400 after review, AFK recommended option) |
| DDD-19 | Slice 07 bulk follow | **Dropped with slice 07** (UI-4): no who-follows read, no follow command, no new controller; P-11 and G-07's mechanics are moot. Owners hand-set to the default's value stay *Own*; an admin switches each on its own settings page | **DROPPED** (maintainer) |
| DDD-20 | RBAC | No new permission. Defaults via the connection PUT (`SystemAdmin`); overrides via Team / Portfolio PUT (`TeamWrite` / `PortfolioWrite`); the log `SystemAdmin` only | **LOCKED** — D14 |
| DDD-21 | Owner values pointing at a field the connection no longer has, at the upgrade | Treated as **empty**: the migration sets them to `ConnectionDefault` and clears the dangling value. With no default on release day they read the tracker's own parent / dependency link / nothing again — a deliberate, visible fix. **No update is queued at the upgrade**: their fingerprint moved, so their next scheduled update downloads everything; every other owner's fingerprint is byte-identical | **ACCEPTED** (P-12 as changed by the maintainer) |
| DDD-22 | Estimation defaults grain | Two bundles, one for Teams and one for Portfolios, each edited with the owner's existing Estimation editor reused inline, collapsed until a field is chosen (UI-5) | **ACCEPTED** (P-13 a; UI-5) |
| DDD-23 | `WorkItemRelatedSettingsChanged` | Unchanged (F4) | **LOCKED** |
| DDD-24 | Usage data | No event: a connection default is set once at setup, so an event would count configuration saves, not whether the feature helps anyone. `UsageDataEventName` unchanged | **ACCEPTED** (maintainer; for DEVOPS) |
| DDD-25 | Where do the save's rules live? | One application service, `IConnectionSaveService` (`Services/Interfaces`, `Services/Implementation/Connections`): connection, Team and Portfolio repositories, the dispatcher and its own logger. It **replaces `IDomainEventDispatcher`** on the controller, which stays at 7 constructor parameters. Two calls in one request: `Open(id)` reads the tracked connection and its owners and renders each `For` from that read (it replaces `GetEntityByIdAnExecuteAction`'s read, which runs its lambda once with no retry, `API/APIHelpers.cs`); the controller maps the DTO onto the handle's connection as today and keeps the 400 / 403 checks; `Commit(handle, token, startUpdates)` runs the fresh in-use query and the refusal rule, applies the token, saves, re-reads the owners untracked and renders `For` again, asks the movers rule, publishes, logs. **The two rules are pure and take data only**: the movers rule takes two plain maps (owner id → fingerprint string) so `Models` never sees `FetchFingerprint`; the refusal rule takes removed ids, default ids and the query's owner rows. **The service does every database read.** A stored save still answers 200 if publishing or logging fails afterwards (6307). The service takes no DTO: `Services` must not depend on `API.DTO` | **ACCEPTED** (review iteration 2, AFK recommended option; ADR-241) |
| DDD-26 | Concurrency on the connection save | The existing token (F14), unchanged and still optional on the request. A stale token → 409 `concurrency-conflict`, nothing queued, no log entry; a conflict on the connection is never retried (`SaveWithRetry` retries only entities without a token). "Before" comes from the row read in the same unit of work. **Without a token**, as the code behaves today (`LighthouseAppContext.cs:759-766`, `:780-786`): the `UPDATE` is still guarded by the token value this request read, so a save committed inside this request's window still gives 409, anything older is overwritten; and the token is **not** changed (only `ApplyConcurrencyTokenForEdit` and added rows get a new one), so a browser form opened earlier can still save over a token-less save. Kept as is. **Owners changed during the save** (accepted limit): "before" is from `Open`, "after" and the log's names from an untracked re-read in `Commit`; such an owner can be queued once more than needed or missed, and a missed one is caught by its next scheduled update; the log names followers as at commit | **ACCEPTED** (review iteration 2; ADR-241) |
| DDD-27 | A database FK for D17? | **No** `OnDelete(Restrict)` FK on the owners' columns or the new default columns: on SQLite adding one rebuilds the whole `Teams` / `Portfolios` table in what should be an add-column migration; a Restrict FK would replace the named 409 with an unnamed `DbUpdateException`; a default column pointing at the connection's own fields makes a cycle for the connection DELETE's cascade. The guard is the application check in the one place a single field is removed, plus the upgrade repair and tests | **ACCEPTED** (review iteration 2, AFK recommended option; ADR-240) |
| DDD-28 | What does the upgrade's data step look like? | A data step inside the per-slice migration, still expand-only: `UPDATE`s that drop nothing, so an older build still reads the database. The same provider-neutral predicate on SQLite and Postgres goes **owner → its connection → that connection's Additional Field definitions** (`NOT EXISTS` a definition with that id and the owner's own connection id); an id naming another connection's field counts as dangling | **ACCEPTED** (review iteration 2; ADR-240) |

---

## Wave: DESIGN / [REF] Component Decomposition

### Backend

| Component | File | Change | Slice |
|---|---|---|---|
| `SettingSource` | `Models/SettingSource.cs` | **CREATE NEW** — the two-value enum of DDD-3 | 05a |
| Effective-value rule | `Models/` (one static, pure function) | **CREATE NEW** — `(source, own, default) → effective`; the one resolver of L3 | 05a |
| `WorkTrackingSystemOptionsOwner` | `Models/WorkTrackingSystemOptionsOwner.cs` | **EXTEND** — Parent Override and Estimation: stored own value (renamed C# property, same column), source, the two mutators, the effective getters under today's names | 05a (C1), 12 (C5) |
| `Portfolio` | `Models/Portfolio.cs` | **EXTEND** — the same for Dependency, Size Estimate, Feature Owner | 09, 10, 11 |
| `IWorkItemQueryOwner` | `Models/IWorkItemQueryOwner.cs` | **EXTEND** — the Parent Override member becomes get-only | 05a |
| `WorkTrackingSystemConnection` | `Models/WorkTrackingSystemConnection.cs` | **EXTEND** — default columns; one read accessor per setting and grain | 05a, 08–12 |
| D21 map | `Models/` (static) | **CREATE NEW** — supported defaults per tracker (DDD-16) | 05a |
| `LighthouseAppContext` | `Data/LighthouseAppContext.cs` | **EXTEND** — map each renamed own property to its existing column name | 05a, 09–12 |
| Migrations | via `Create-Migration.ps1`, both providers | **CREATE** — one per slice: add the source column (default `ConnectionDefault`) and the default column(s); hand-added data steps in **both** providers' files: (1) clear the owner's value where it names no Additional Field of the owner's own connection (DDD-21); (2) set `Own` where the value is still not null | 05a, 08–12 |
| `FetchFingerprint` | `Services/Implementation/WorkItems/FetchFingerprint.cs` | **NO CODE CHANGE** to `For` / `ForConnection`; doc comment only | — |
| `FetchShapingPropertyGuardTest` | tests | **EXTEND** — a third category, "reaches the fingerprint through the owner's effective value" | 05a |
| `WorkTrackingSystemConnectionController` | `API/…` | **EXTEND** — the PUT maps the DTO (defaults included, with temporary ids of fields added in the same save via `NewAdditionalFieldTargets`) onto the handle from `IConnectionSaveService.Open`, keeps the 400 / 403 checks, calls `Commit`, maps *refused* to the 409. `IDomainEventDispatcher` leaves its constructor, `IConnectionSaveService` takes its place (still 7). Response unchanged | 05a, 05b, 06 |
| `IConnectionSaveService` + implementation | `Services/Interfaces/IConnectionSaveService.cs`, `Services/Implementation/Connections/ConnectionSaveService.cs` | **CREATE NEW** — the save's unit of work: before / after `For` per owner, refusal, token, save, publish, the one log entry (DDD-25, DDD-26). The 6307 fan-out moves here from the controller | 05a (fan-out per owner, log), 05b (refusal), 06 (queue for defaults) |
| Movers rule, in-use rule | `Models/` (static, pure) | **CREATE NEW** — "which owners moved" (two fingerprint maps → owner ids); "which removed fields are still in use" (connection, owners, removed ids → referring defaults and owners) | 05a, 05b |
| `WorkTrackingSystemConnectionDto` | `API/DTO/…` | **EXTEND** — defaults and the supported set (request and response); no response-only notice | 05a |
| `IWorkTrackingSystemConnectionUsage` / `ConnectionUsage` | `Services/Interfaces/…`, `Services/Implementation/…` | **EXTEND** — follower counts per default beside the owner counts (DDD-10) | 05a |
| `SettingsOwnerDtoBase`, `PortfolioSettingDto` | `API/DTO/…` | **EXTEND** — per setting: nullable source, followed default's display name; existing id field = effective value | 05a, 08–12 |
| `TeamExtensions`, `PortfolioExtensions` | `API/Helpers/…` | **EXTEND** — apply incoming settings through the mutators (DDD-11); create applies the same rule on a new owner (D11); refuse (400) an own value naming no Additional Field of the owner's connection (DDD-18) | 05a / 05b, 08 |
| `DependencySourceSelector` | `Services/Implementation/Dependencies/…` | **EXTEND** — third source (DDD-13) | 01 |
| `IssueExtensions` (Jira) | `…/Jira/IssueExtensions.cs` | **EXTEND** — dependency references of a named link type on a named end, reusing the counterpart walker `ResolveParentFromLinks` uses | 01 |
| `JiraWorkTrackingConnector` | `…/Jira/JiraWorkTrackingConnector.cs` | **EXTEND** — route a link-type Dependency Field to the selector's third source; the "nothing matched" check (`:1275-1309`) no longer returns early for a link-type field and emits evidence | 01, 02 |
| `AzureDevOpsWorkTrackingConnector` | `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` | **EXTEND** — relation-type listing as a per-call value (never a field: F15), field-vs-relation resolution (connector-local), Validate accepting it, refresh not warning "no field", the `:1142` condition reading the resolution | 03, 04 |
| `WorkItemExtensions` (ADO) | `…/AzureDevOps/WorkItemExtensions.cs` | **EXTEND** — `ExtractDependencyReferences` takes the `Rel` to match (default unchanged) | 04 |
| Evidence on the record | `Models/WorkItemBase.cs` or `Feature` | **EXTEND** — one transient value: what the dependency source turned out to be, the link names / `Rel` values seen | 02 |
| `RefreshFinding` | `Models/` | **CREATE NEW** — closed kind + setting + what was named + what was seen | 02 |
| `WorkItemService` | `Services/Implementation/WorkItems/WorkItemService.cs` | **EXTEND** — aggregate evidence into findings once per fetch, beside the 6301 placement step | 02 |
| `SyncOutcome`, `RefreshLog`, `TeamUpdater`, `PortfolioUpdater` | `Models/…`, `…/Update/…` | **EXTEND** — carry the findings to one new nullable text column; delta runs write none | 02 |
| Refresh-log read | the existing refresh-log controller and its DTO | **EXTEND** — per entity, the findings of the latest `Mode == Full` run and that run's time (DDD-15) | 02 |

### Frontend

| Component | File | Change | Slice |
|---|---|---|---|
| Connection model + service | `models/WorkTracking/WorkTrackingSystemConnection.ts`, `services/Api/WorkTrackingSystemService.ts` | **EXTEND** — defaults, supported set, follower counts on `usedBy` (zod `.nullable()` for nullable members) | 05a |
| `ModifyConnectionSettings` | `components/Common/Connection/ModifyConnectionSettings.tsx` | **EXTEND** — "Defaults for Teams and Portfolios" group after Additional Fields (`:793-818`) from `InputGroup` + `FormSelectField`; open the dialog when a default changed | 05a |
| `UpdateQuestion` | existing 6307 dialog | **EXTEND** — follower counts for the changed defaults (no names); the *plain* variant (Cancel / Save); no dialog when nobody follows (DDD-9, UI-3) | 05a (plain), 06 (question) |
| `EditConnection` | `pages/Connections/Edit/EditConnection.tsx` | **EXTEND (one rule)** — still navigates to `/` after save; sends `WorkTrackingSystemFieldsChanged` only when the choice says Additional Fields changed (F18) | 05a |
| `FormSelectField` | `components/Common/FormSelectField/FormSelectField.tsx` | **EXTEND** — optional first entry "Connection default (…)" | 05a |
| Owner dropdowns | `AdvancedInputs.tsx` (moves to `FormSelectField`), `DependenciesComponent.tsx`, `FeatureSizeComponent.tsx`, `OwnershipComponent.tsx`, `EstimationFieldComponent.tsx` | **EXTEND** — the first entry; sends the source | 05a, 08–12 |
| Settings models, `useModifySettings` | `models/Common/BaseSettings.ts`, portfolio settings model, `hooks/useModifySettings.ts` | **EXTEND** — source as a string-literal union, followed name | 05a |
| Estimation default editor | `EstimationFieldComponent.tsx` | **REUSE inline** on the connection page, once per grain, collapsed until a field is chosen (UI-5) | 12 |
| Refresh History | `models/SystemInfo/RefreshLog.ts`, `RefreshHistorySection.tsx` | **EXTEND** — optional findings with their run's time; the latest full run's findings under the figures | 02 |

**Slice placement of the dialog.** 05a ships the plain variant (counts, Cancel / Save) for a changed default with
followers: before slice 06 no update is started by a default change, so there is nothing to ask. Slice 06 adds the
update-question variant. Slice 07 is dropped; nothing on the page lists or counts followers.

### Deliberately not touched

`AdditionalFieldDefinition` (ADR-193 §5 stands), `IWorkTrackingConnector`, `ConnectionFetchShapeChanged` and its
handler, the update queue, the PUT response shape, `EditConnection`'s navigation, `ParentSourceSelector`,
`ParentResolution`, `EstimateNormalizer`, `BaseMetricsService`, `WorkItemEstimateDto`, `WorkItemRelatedSettingsChanged`,
write-back mappings and their FK, the Linear / CSV / ServiceNow connectors, RBAC, Lighthouse-Clients.

---

## Wave: DESIGN / [REF] Driving Ports

| Surface | Port | Change | Guard |
|---|---|---|---|
| Name the link / relation type | existing Additional Fields editor → connection PUT; `POST …/worktrackingsystemconnections/validate` | ADO Validate widened (03); contract unchanged | `SystemAdmin` |
| Select the Dependency Field | existing Portfolio PUT | unchanged | `PortfolioWrite` |
| Read findings | existing refresh-log read | one optional list member per entity: the latest full run's findings and its time | unchanged |
| Set defaults | existing `PUT /v1/worktrackingsystemconnections/{id}` (`startUpdates` as today) | additive request members; response unchanged | `SystemAdmin` |
| Counts at save | existing connection list read | `usedBy` gains follower counts per default | `SystemAdmin` |
| Who followed, at save | existing Settings → System Info log | one Information entry per save that changed a default | `SystemAdmin` (`LogsController.cs:23`) |
| Follower updates | existing Task Manager → Activity | unchanged | unchanged |
| Follow / override on an owner | existing `PUT /v1/teams/{id}`, `/v1/portfolios/{id}`, their GETs and create | additive members; G-R1 (b) | `TeamWrite` / `PortfolioWrite` |

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter | Change |
|---|---|---|
| Work tracking read — `IWorkTrackingConnector` | Jira, ADO | No new member; both read effective values through the owner |
| Jira REST — field list, `issueLinkType`, `search` with `*all` | `JiraWorkTrackingConnector` | **No new request** (AC-1.6) |
| Azure DevOps — `GET _apis/wit/workitemrelationtypes` (SDK `GetRelationTypesAsync`, candidate) | `AzureDevOpsWorkTrackingConnector` | **NEW call**, only when a reference missed the field list; ≤ 1 per connection per Validate and per refresh (AC-3.7), remembered only for that call (DDD-14). Reachability with `vso.work` is slice 03's hypothesis (P8) |
| Azure DevOps — work items with relations | same | unchanged request; no longer skipped when the Dependency Field names a relation type (F8) |
| Persistence | `LighthouseAppContext` | expand-only columns per slice (plus the DDD-21 data step); one nullable text column on `RefreshLog` |
| Log | `ILogger` → Serilog file, shown in Settings → System Info → Logs (F17) | one Information entry per default-changing save, written by `IConnectionSaveService` |

**External integrations requiring contract tests** (for DEVOPS):
- Jira Cloud / Data Center REST (`issueLinkType`, `search` `issuelinks`): the link payload shape. Recommended: the
  recorded wire fixtures in `TrackerWireFormats` as the consumer contract; `JiraIntegration` run deliberately in slices 01–02.
- Azure DevOps REST (`workitemrelationtypes`, work items with `$expand=relations`): relation-type listing fixtures for
  Services and Server 2022 added in slice 03; `AdoIntegration` run deliberately in slices 03–04. PactNet is not proposed:
  the provider is a vendor API Lighthouse cannot verify against, so recorded fixtures plus the live categories are the
  practical contract.

## Wave: DESIGN / [REF] Technology Choices

Nothing new: .NET 10 / ASP.NET Core, EF Core on SQLite and Postgres, the Azure DevOps .NET SDK already referenced,
React 18 + TypeScript + MUI, TanStack Query, NUnit 4.6 + Moq, Vitest + RTL, Playwright. No new caching layer (the
relation-type list lives for one refresh), no new serialisation library (the findings column uses `System.Text.Json`
with a cached options instance, CA1869), no rules engine for inheritance.

---

## Wave: DESIGN / [REF] Reuse Analysis

Contract shape: *pure* = returns a value, writes nothing; *bounded* = writes only the named entity's named members.

| Existing component | File | Overlap | Decision | Contract shape · assertion |
|---|---|---|---|---|
| Owner settings properties (C1–C5) | `WorkTrackingSystemOptionsOwner.cs:68-76`, `Portfolio.cs:21-25` | Every reader already reads them | **EXTEND** — same name, now the effective value | pure getter · unit over source × default set/unset |
| `FetchFingerprint.For` / `ForConnection` | `FetchFingerprint.cs:78-112` | The one fetch-shape function | **EXTEND (no code)** | pure · pinned digests committed before 05a |
| `ConnectionFetchShapeChanged` + handler | `Models/Events/…`, `…/ConnectionFetchShapeChangedUpdateTriggerHandler.cs` | Queues named owners | **REUSE unchanged** | bounded (queue only) · AT: queued set equals the movers |
| 6307 `UpdateQuestion` + `startUpdates` + `usedBy` | `ModifyConnectionSettings.tsx:884-890`; ADR-239 §6, §8 | Asks before a fan-out, with counts, no extra fetch | **EXTEND** (follower counts, plain variant, none when nobody follows) | — |
| `IWorkTrackingSystemConnectionUsage` | `Services/Interfaces/…` | Per-connection counts on the list read, off the controller's constructor | **EXTEND** | pure read |
| Controller `ILogger` | `WorkTrackingSystemConnectionController.cs:26` | Already logs save-time outcomes | **Not reused for the entry**: the entry is written by `IConnectionSaveService`'s own logger (same sink) | bounded (one entry) |
| Connection concurrency token + `ConcurrencyConflictExceptionFilter` | `LighthouseAppContext.cs:316-318`, `API/Filters/ConcurrencyConflictExceptionFilter.cs` | Refuses a stale save with 409; the page already words it | **REUSE unchanged** (DDD-26) | AT: stale second save refused, nothing queued, no entry |
| `NewAdditionalFieldTargets` | `API/Helpers/NewAdditionalFieldTargets.cs` | Temporary ids of fields added in the same save | **REUSE** | — |
| `DependencySourceSelector` | `…/Dependencies/DependencySourceSelector.cs` | The one place deciding where dependencies come from | **EXTEND** (third source) | pure |
| `ParentSourceSelector` / `ResolveParentFromLinks` / `JiraIssueLinkType.DirectionReadFrom` | `…/Parents/…`, `IssueExtensions.cs:106-120`, `JiraWorkTrackingConnector.cs:1993-2019` | Field-first resolution and the named-end rule | **REUSE**, **no merge** of the two selectors | pure · wire-fixture units both ends |
| `ReportLinksThatMeantNothingHere` | `JiraWorkTrackingConnector.cs:1275-1309` | One finding per refresh, whole-query only | **EXTEND** | pure evidence |
| 6301 evidence-on-the-record → service → `SyncOutcome` → `RefreshLog` | `WorkItemService.cs:183-228`, `SyncOutcome.cs`, `RefreshLog.cs` | Carrying findings to Refresh History | **REUSE the path** | bounded (the `RefreshLog` row) |
| `FormSelectField`, `InputGroup`, `EstimationFieldComponent`, the page and section loading / error looks | existing | Dropdown, section, Estimation editor, loading and could-not-load looks | **EXTEND** / **REUSE** | — |
| `WorkTrackingSystemConnectionController` | `API/…` | The PUT | **EXTEND** as a mapper; the rules leave it for `IConnectionSaveService` (CREATE NEW: no existing service owns a connection save; the house rule puts business logic behind a port, not in a driving adapter at the S107 limit) | controller: maps only; service: bounded (connection + event + one log entry) |
| — | — | `SettingSource`, the effective-value rule, the movers and in-use rules, the D21 map, `RefreshFinding` | **CREATE NEW** — no existing type expresses where a setting comes from, which owners a save moved, which fields are in use, or a refresh finding with text | pure |

No unjustified CREATE NEW.

---

## Wave: DESIGN / [REF] C4 — System Context (extends c4-diagrams.md L1)

```mermaid
C4Context
    title System Context — Lighthouse, settings set once per Work Tracking Connection (epic-6303)

    Person(sysAdmin, "System Admin", "Owns a connection; registers Additional Fields; sets the defaults")
    Person(ownerAdmin, "Team / Portfolio Admin", "Follows the connection default or overrides it on their own Team or Portfolio")
    Person(reader, "Product owner / RTE", "Reads Features and their dependencies")

    System(lighthouse, "Lighthouse", "Forecasts delivery from tracker data; resolves each setting from the owner or its connection")

    System_Ext(jira, "Jira Cloud / Data Center", "Issues, issue links, issue link types")
    System_Ext(ado, "Azure DevOps Services / Server", "Work items, relations, relation types")

    Rel(sysAdmin, lighthouse, "Sets connection defaults in")
    Rel(ownerAdmin, lighthouse, "Follows or overrides a setting in")
    Rel(reader, lighthouse, "Reads what each Feature waits on in")
    Rel(lighthouse, jira, "Reads issues with their links, and link types, from")
    Rel(lighthouse, ado, "Reads work items with their relations, and relation types, from")
```

## Wave: DESIGN / [REF] C4 — Container (extends c4-diagrams.md L2)

```mermaid
C4Container
    title Container Diagram — Lighthouse (epic-6303 delta)

    Person(user, "Admin", "System Admin, or Team / Portfolio Admin")
    System_Ext(jira, "Jira", "REST")
    System_Ext(ado, "Azure DevOps", "REST / .NET SDK")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + MUI", "Defaults section on the connection page; follower counts in the pre-save dialog; 'Connection default (…)' entry on owner settings; findings in Refresh History")
    Container(api, "Backend API", "C# .NET 10 ASP.NET Core", "Connection PUT merges defaults, refuses deleting referenced fields, logs who follows, queues the owners whose fetch moved; owners expose effective values")
    Container(updater, "Update pipeline (in-process)", "Hosted services + update queue", "Fetches through connectors with effective values; records findings on RefreshLog")
    ContainerDb(db, "Database", "SQLite / PostgreSQL via EF Core", "Owners' source columns; connection default columns; RefreshLog findings")

    Rel(user, spa, "Configures connections, Teams and Portfolios via", "HTTPS")
    Rel(spa, api, "Saves defaults and overrides through", "HTTPS / JSON")
    Rel(api, db, "Stores settings and their sources in", "EF Core")
    Rel(api, updater, "Queues follower updates on", "domain event")
    Rel(updater, jira, "Reads issues and link types from", "HTTPS")
    Rel(updater, ado, "Reads work items, relations and relation types from", "HTTPS")
    Rel(updater, db, "Writes records and refresh findings to", "EF Core")
```

## Wave: DESIGN / [REF] C4 — Component: effective value, fetch and refresh path

```mermaid
graph TD
    subgraph API["API — driving adapters"]
        CPUT["Connection PUT<br/>maps the DTO onto the save handle · 400 / 403 / 409"]
        OPUT["Team / Portfolio PUT<br/>applies incoming setting (G-R1 b)"]
    end

    subgraph App["Application service"]
        SVC["IConnectionSaveService<br/>Open: read + For(owner) before<br/>Commit: refusal · token · save · For after · publish · one log entry"]
    end

    subgraph Kernel["Models — pure"]
        OWN["Owner entity<br/>stored own value + source<br/>effective value under today's name"]
        RULE["Effective-value rule<br/>(source, own, default) → effective"]
        CONN["Connection<br/>default columns"]
        MAP["Supported defaults per tracker"]
        MOV["Movers rule<br/>before / after maps → owner ids"]
        USE["In-use rule<br/>removed fields → referring defaults and owners"]
        FP["FetchFingerprint.For / ForConnection<br/>(unchanged code)"]
    end

    subgraph Bus["Domain events"]
        EVT["ConnectionFetchShapeChanged<br/>(movers only)"]
        H["Update trigger handler"]
    end

    subgraph Sync["Update pipeline"]
        WIS["WorkItemService<br/>findings once per fetch"]
        UPD["Team / Portfolio Updater<br/>RefreshLog findings"]
    end

    subgraph Adapters["Connectors"]
        JC["Jira: field → link type<br/>named-end walker"]
        AC["ADO: field → relation type<br/>listing per call (≤1 per connection per refresh)"]
        DSS["DependencySourceSelector<br/>tracker link · field · named link"]
    end

    CPUT -->|"opens and commits the save through"| SVC
    CPUT -->|"writes defaults to"| CONN
    SVC -->|"renders before/after with"| FP
    SVC -->|"asks which owners moved"| MOV
    SVC -->|"asks which fields are in use"| USE
    SVC -->|"publishes"| EVT
    OPUT -->|"applies setting through"| OWN
    OWN -->|"resolves through"| RULE
    RULE -->|"reads default from"| CONN
    FP -->|"renders effective value of"| OWN
    EVT -->|"is handled by"| H
    H -->|"triggers"| UPD
    UPD -->|"runs fetch in"| WIS
    WIS -->|"fetches through"| JC
    WIS -->|"fetches through"| AC
    JC -->|"hands counterparts to"| DSS
    AC -->|"hands counterparts to"| DSS
    JC -->|"reads effective values of"| OWN
    AC -->|"reads effective values of"| OWN
    MAP -->|"limits defaults accepted by"| CPUT

    style Kernel fill:#e8f0fe,stroke:#4285f4
```

No reader of a setting knows inheritance exists — the owner answers with the effective value. `IConnectionSaveService`
is the only place that compares; the controller only maps; the event and queue are unchanged.

---

## Wave: DESIGN / [REF] Decisions Table

| ID | Decision | Status | ADR |
|---|---|---|---|
| DDD-1 | Source column per setting on the owner tables, per slice | ACCEPTED | 240 |
| DDD-2 | Flat default columns on the connection; Estimation as four columns per grain | ACCEPTED | 240 |
| DDD-3 | `SettingSource`, `ConnectionDefault = 0`; `Own` where the value names a field of the owner's connection | LOCKED | 240 |
| DDD-4 | Owner's existing property = effective value; one pure rule in `Models` | ACCEPTED | 240 |
| DDD-5 | Writes only through mutators; following clears the own value (whole bundle for Estimation) | LOCKED / ACCEPTED | 240 |
| DDD-6 | Defaults never in `ForConnection` | LOCKED | 241 |
| DDD-7 | Queue the owners whose `For` moved, before vs after; movers rule pure in `Models` | LOCKED | 241 |
| DDD-8 | One Information log entry per default-changing save naming the followers; PUT response unchanged | ACCEPTED | 241 |
| DDD-9 | No after-save notice; the pre-save dialog (UI-3): none when nobody follows, plain Save when nothing refreshes, the update question otherwise; `WorkTrackingSystemFieldsChanged` only when Additional Fields changed | ACCEPTED | 241 |
| DDD-10 | Dialog counts from follower counts on the list read's `usedBy`; never shown on the page | ACCEPTED | 241 |
| DDD-11 | G-R1 (b) in the owner mutator, against the pre-request effective value; nullable sources; Estimation bundle compared as one value; stale-echo flip accepted (CA-8) | LOCKED / ACCEPTED | 240 |
| DDD-12 | GET: effective value in the existing field + source + followed name | ACCEPTED | 240 |
| DDD-13 | Third dependency source; stored as `TrackerLink` | LOCKED | 242 |
| DDD-14 | ADO relation-type lookup inside the ADO adapter, remembered only per call, never on the cached connector; D2 for non-directional types and base names | LOCKED / ACCEPTED | 242 |
| DDD-15 | Findings: evidence on records → service → `RefreshLog` text column; Refresh History shows the latest **full** run's findings with its time | ACCEPTED | 243 |
| DDD-16 | D21 as one kernel map, on the DTO, enforced on PUT | ACCEPTED | — |
| DDD-17 | ServiceNow N/A; Feature Owner default offered on CSV | LOCKED / ACCEPTED | — |
| DDD-18 | D17 refusal before save; in-use rule pure in `Models`; 409 `additional-field-in-use`, naming the defaults and the owners that use the field as their own | LOCKED / ACCEPTED | 240, 241 |
| DDD-19 | ~~Bulk follow~~ | **DROPPED** by the maintainer (slice 07 dropped, UI-4) | — |
| DDD-20 | No new permission | LOCKED | — |
| DDD-21 | Dangling values become `ConnectionDefault` at the upgrade; no update queued, the next scheduled one downloads everything | ACCEPTED | 240 |
| DDD-22 | Estimation defaults per grain; the owner's Estimation editor reused on the connection page | ACCEPTED | 240 |
| DDD-23 | `WorkItemRelatedSettingsChanged` unchanged | LOCKED | — |
| DDD-24 | Usage data: no event | ACCEPTED (for DEVOPS) | — |
| DDD-25 | `IConnectionSaveService` owns the save (Open / Commit, one unit of work) and does every DB read; pure data-only rules in `Models`; replaces the dispatcher on the controller (still 7 parameters) | ACCEPTED | 241 |
| DDD-26 | The connection's existing concurrency token; stale → 409, nothing queued, no log entry; token-less behaviour stated as the code has it; concurrent owner changes an accepted limit | ACCEPTED | 241 |
| DDD-27 | No database FK for D17 | ACCEPTED | 240 |
| DDD-28 | Upgrade repair is a data step, expand-only, owner → connection → field definitions on both providers | ACCEPTED | 240 |

No decision is left PROPOSED. Engineering choices taken while the maintainer is AFK, each the recommended option:
05a ships only the plain-Save dialog (no default change can start updates before slice 06); a log entry is written for
every save that changed a default, followers or not; the dialog's counts are per grain in the title, as in 6307. After
review iteration 2: the application service and its two pure rules (DDD-25); the per-call ADO listing (DDD-14); the
latest-full-run read for findings (DDD-15); the Estimation bundle rule (DDD-11); no FK (DDD-27).

### What review iteration 2 changed

- **Business logic out of the controller** (major 1): `IConnectionSaveService` with pure movers and in-use rules in
  `Models`; the controller maps and calls; it trades its dispatcher for the service and stays at 7 parameters (DDD-25).
- **Concurrency** (major 2): the connection's existing token is the guard (F14); "before" comes from the row read in the
  same unit of work; an AT refuses a stale second save (DDD-26).
- **ADO listing scope** (major 3): a per-call value, never on the cached connector; an AT counts requests across two
  connections (F15, DDD-14).
- **Stale echo** (major 4): recorded as an accepted consequence beside CA-8 with the "Caused by" / "Parent Link"
  example, pinned by an AT (DDD-11, CA-8).
- **Findings from the right run** (major 5): the latest full run's findings, with its time; DDD-15 and ADR-243 agree.
- **Minors**: the Estimation bundle rule (OQ-3 resolved); no FK, with the SQLite rebuild cost (DDD-27); the data step is
  expand-only and owner-scoped on both providers (DDD-28); the log sink confirmed (F17); the late-answer rule for the
  defaults section and the owner dropdowns (UI States); `DependencySourceSelector` stays in `Services` (ADR-242 §7); the
  usage-data event `WorkTrackingSystemFieldsChanged` kept to Additional Field changes (F18).

### Follow-ups after review iteration 2 (conditional approval, N1 … N7)

- **N1** the in-use check reads owners from a fresh set-based query in `Commit`, not the `Open` snapshot; residual
  window accepted and narrowed by a 400 on the owner PUT for an id not on its connection; race AT (DDD-18).
- **N2** "after" and the log's names come from an untracked re-read in `Commit`; a concurrently changed owner may be
  queued once extra or missed until its next scheduled update — accepted limit (DDD-26).
- **N3** `ISaveConnectionChoice.additionalFieldsChanged`, the `EditConnection.tsx:90` guard, and the shape of each
  dialog variant's choice (DDD-9).
- **N4** token-less saves described as the code behaves: guarded by the token this request read, token left unchanged
  (DDD-26).
- **N5** `GetEntityByIdAnExecuteAction` runs its lambda once, no retry; `Open` replaces its read; connection conflicts
  are never retried by `SaveWithRetry` (DDD-25).
- **N6** the movers rule takes plain maps; the ArchUnit rule matches the dispatcher and `FetchFingerprint` by type.
- **N7** both rules pure and data-only; the service does every database read (DDD-25).

### What pass 2 changed against pass 1

- **Save notice withdrawn (P-5 changed).** The page navigates to `/` (6307's S3 and ADR-239 §3 stand). Follower counts
  are shown in the reused pre-save dialog (UI-3) and the names go to one Information log entry. D12 and AC-F.1–F.5 are
  amended upstream. CA-3 is void.
- **Slice 07 dropped completely (UI-4).** No who-follows / who-differs view, no bulk follow, no follower count on the
  connection page. `ConnectionDefaultsController` is gone (it existed only for slice 07; the connection PUT needs no new
  constructor parameter, so S107 does not call for it). G-07's mechanics are moot. Owners hand-set to the default's
  value stay *Own* until an admin switches each on its own settings page. Story #6304 is slices 05a, 05b, 06.
- **Dangling values repaired at the upgrade (P-12 changed).** They become `ConnectionDefault` and read the tracker's own
  value again; D10 and AC-4.4 are amended upstream; release-note callout owed.
- **D2 confirmed on ADO (CA-6).** Slice 04's test vehicle must be a directional type the slice 03 spike confirms.
- **CSV Feature Owner default accepted (P-14).**
- **The D17 refusal names the defaults and the owners using the field as their own**, not the followers (approved copy,
  UI-6): followers hold no own value after DDD-5.
- **UI decided by the maintainer** (UI-1 … UI-6): the sketches below replace pass 1's.
- **Usage data: none** (DDD-24).
- All other proposals accepted as recommended.

---

## Wave: DESIGN / [REF] The Upgrade Path for Values Pointing at Nothing (DDD-21)

| Owner at the upgrade | Migration sets | Reads after the upgrade | Fingerprint | Update |
|---|---|---|---|---|
| Value names a field of its own connection | `Own`, value kept | exactly as before | byte-identical | none |
| Value empty | `ConnectionDefault` | as before (no default exists) | byte-identical | none |
| Value names no field of its own connection (deleted, or left from a connection change) | `ConnectionDefault`, value cleared | the tracker's own parent field / dependency link, or no size / owner / estimate — what an empty setting reads | moves (own id → empty) | **none queued**: its next scheduled update sees the moved fingerprint and downloads everything |

Why no update at the upgrade: queuing would need a start-up hook that finds these owners; the fingerprint already
marks them, and the next scheduled cycle repairs them at the cost of one full download each. Every other owner's stored
fingerprint stays byte-identical. Each slice's migration applies this to its own setting only (05a: the Teams' Parent
Override; 08: the Portfolios'; …). From 05b on, D17 stops new dangling values.

**Release note (finalize, per slice that lands a migration):** "Teams [Portfolios] whose Parent Override Field
[Dependency Field, …] pointed at an Additional Field that had been deleted now read the tracker's own value again,
from their next update." In configurable terms.

---

## Wave: DESIGN / [REF] UI States (maintainer-approved UI-1 … UI-6, with loading and could-not-load looks)

Decided by the maintainer on 2026-10-16 (`design/maintainer-decisions-2026-10-16.md`); DISTILL copies them into its
maintainer-decision block. Copy may be polished in DELIVER without changing meaning; configurable terms render in the
user's words. House rule: every view that fetches data has a loading look and a could-not-load look; a late answer for
an old request never lands.

**Late answers (review iteration 2).** These pages do not use TanStack Query today: `EditConnection` and the owner
settings pages load through their own promise loaders (`EditConnection.tsx:42-52`). The feature adds **no fetch** of its
own: the defaults section, the dialog's counts and the owner dropdown's "Connection default (…)" entry all render from
the one answer the page already loads, so no part of the form can show one request's data beside another's. The rule
DELIVER keeps (slice 05a): an answer is applied only if it is for the id the page is showing now (connection id on the
connection page, Team / Portfolio id on the owner pages); an answer for a previous id is dropped. If the existing
loaders do not already guard this, 05a adds that id check in the loader, or moves the read to a TanStack query keyed by
that id, whichever is the smaller change. Vitest: navigate from connection A to B while A's answer is pending; A's
answer arrives last and B's defaults are shown.

### UI-1 — Defaults for Teams and Portfolios (connection edit page, below Additional Fields)

```
Loaded
┌ Defaults for Teams and Portfolios ──────────────────┐
│ Teams and Portfolios on this connection use these   │
│ unless they set their own.                          │
│ Teams                                               │
│  Parent Override Field   [ Caused by          ▾ ]   │
│  Estimation              [ None ▾ ] (expands)       │
│ Portfolios                                          │
│  Parent Override Field   [ None               ▾ ]   │
│  Dependency Field        [ depends on         ▾ ]   │
│  Size Estimate Field     [ None               ▾ ]   │
│  Feature Owner Field     [ None               ▾ ]   │
│  Estimation              [ None ▾ ] (expands)       │
└─────────────────────────────────────────────────────┘
```
Only the settings the tracker honours (five on Jira and ADO; Feature Owner and Estimation on CSV); hidden on Linear and
ServiceNow. No follower count and no "Show who" on the page. Values arrive with the connection read the page already
makes, so the section shares the page's looks:

```
Loading: the page's existing LoadingAnimation      Could not load: the page's existing error
┌ Update Jira Connection ──────┐                     ⚠ Could not load this connection.  [ Try again ]
│              ◌               │                     (no section is shown, so nothing stale can be saved)
└──────────────────────────────┘
```

### UI-5 — Estimation default

The owner's existing Estimation editor (`EstimationFieldComponent`) is reused inline: field, unit, non-numeric flag and,
when non-numeric, categories; collapsed until a field is chosen; once for Teams and once for Portfolios. The owner page
shows `Connection default (Story Points, Points)`. Its looks are the section's.

### UI-2 — Owner settings entry

```
Parent Override Field
[ Connection default (Caused by)   ▾ ]
  ├ Connection default (Caused by)
  ├ None
  ├ Caused by
  └ Epic Link
Set on the Jira connection.
```
`FormSelectField`; helper text "Set on the <tracker> connection." Picking any other entry, None included, makes the value
the owner's own. Loading and could-not-load are the owner settings page's existing looks. If the connection's field list
cannot be read (F12), the field still shows `Connection default (Caused by)` from the owner's own settings.

### UI-3 — The save dialog (reused 6307 `UpdateQuestion`)

Opens on Save, before anything is stored; counts come from the read the page already made, so it fetches nothing new.

| Changed in this save | Dialog |
|---|---|
| Defaults nobody follows, nothing else | **none** — saves as today |
| A fetch-time default with followers (slice 06 on), or Additional Fields (as today, 6307) | update question: Cancel / Save only / Save and update |
| Only defaults read at page load (Estimation) with followers; and in 05a, any default with followers | plain: Cancel / Save |

```
┌ Update 3 Teams and 0 Portfolios? ──────────────┐
│ 3 Teams follow the Parent Override Field       │
│ default. Their parents change only once they   │
│ have been updated. Save only: they change at   │
│ their next scheduled update.                   │
│        Cancel   Save only   Save and update    │
└────────────────────────────────────────────────┘

┌ Change the Estimation default? ────────────────┐
│ 4 Teams follow it; they show the new           │
│ estimation the next time their pages load.     │
│                          Cancel   Save         │
└────────────────────────────────────────────────┘
```
Names are not listed in the dialog; they go to the log. Saving: the buttons disable under a small spinner (existing).
Save failed / refused: the page stays, with its existing error area (UI-6). After a successful save the page navigates
to `/`; the updates appear in Task Manager → Activity.

### The log entry (Settings → System Info → Logs, `SystemAdmin` only)

```
INF  Connection "Jira DC – Payments" changed the Teams' Parent Override Field default from "Caused by" to
     "Parent Link"; 3 Teams follow it: Fraud Signals, Ops Tooling, Mobile Wallet; 3 updates queued.
```
One entry per save that changed a default (stating "no Team follows it" when none do); names only. Its looks are the
log page's existing ones.

### UI-4 — slice 07 dropped

No who-follows / who-differs view, no "make these follow" action, no follower count on the page.

### UI-6 — Copy (accepted)

```
Refresh History (Portfolio row):
  ⚠ The Dependency Field names "depends on", and no Feature carries that link.
    Links seen: is blocked by, relates to.
  ⚠ The Dependency Field names "Blocks", which points both ways.
    Name one end: "blocks" or "is blocked by".
  ⚠ The Parent Override Field points at an Azure DevOps relation type,
    which only the Dependency Field reads.

Connection save refused (field still in use):
  ✕ "Caused by" cannot be removed. It is the Teams' Parent Override Field
    default, and Ledger uses it as its own. Nothing was saved.
```
Refresh History's loading and could-not-load looks are the section's existing spinner and alert. The findings shown are
the latest **full** run's, with that run's time beside them ("as of the full download on 16 Oct, 09:12"), so a delta
refresh never clears them (DDD-15).

---

## Wave: DESIGN / [REF] Architectural Enforcement

| Rule | Mechanism |
|---|---|
| A follower and an owner read alike | NUnit table over source × default set/unset × each setting, through the owner's public property |
| Upgrade moves only the dangling owners' fingerprints | Pinned digests committed **before** 05a's migration; a migration test with an own, an empty and a dangling owner: the first two digests unchanged, the third moved, `ConnectionDefault`, value cleared (AC-4.4 as amended) |
| Defaults never enter `ForConnection` | `FetchShapingPropertyGuardTest`'s new category; property-based test: a default change never moves `For` of an `Own` owner, always moves it for a follower of a fetch-time setting |
| Only mutators write | Private setters; an ArchUnitNET rule that `API` reaches the own values only through the mutators |
| Fan-out narrows exactly | WebApplicationFactory AT: queued set equals the owners whose `For` moved, for field, default and Estimation edits and `startUpdates=false`; response shape byte-identical |
| Rules out of the controller | ArchUnitNET, matched by **type**: `WorkTrackingSystemConnectionController` has no dependency on `Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventDispatcher` or `Lighthouse.Backend.Services.Implementation.WorkItems.FetchFingerprint`; the movers and in-use rules depend on no type outside `Models` and the BCL; `Services` does not depend on `API.DTO` (existing posture). Unit tables on both pure rules, fed plain data |
| A stale save cannot win or mislead | AT: two admins load the connection; the first saves a new default; the second saves its stale form with its old token → 409 `concurrency-conflict`; the first default stands; nothing queued and no log entry for the second |
| A field cannot be removed while a racing owner adopts it | AT, driven by a test seam between `Open` and `Commit`: the connection save is opened; a Team PUT sets its own Parent Override to X and commits; the connection save removing X commits → 409 `additional-field-in-use` naming that Team; X still exists. AT: a Team / Portfolio PUT whose own value names no Additional Field of its connection → 400 |
| Stale echo behaves as documented | AT: a follower of "Caused by" is loaded; the default moves to "Parent Link"; the stale owner body is PUT back without a source → *Own ("Caused by")*; with `source: ConnectionDefault` → still follows (CA-8) |
| Estimation bundle is one value | Unit table: each part alone differing owns; all equal unchanged; field null unchanged; categories reordered owns |
| The log names the followers | Logger-capturing test on `IConnectionSaveService`: one Information entry per save that changed a default, naming exactly the owners in `ConnectionDefault`; none for a save that changed no default, was refused, or was stale |
| The dialog follows UI-3 | Vitest: no dialog when every changed default has no follower; plain Save for page-load-only changes; the question otherwise; no owner name rendered; `WorkTrackingSystemFieldsChanged` sent for an Additional Field change, not for a default-only change |
| A late answer never lands | Vitest: connection A's answer arriving after B's does not replace B's defaults; the same on an owner page |
| No extra tracker request | Request-count tests (Jira 01; ADO 03, 04: ≤ 1 listing per connection per Validate / refresh) |
| The ADO listing never outlives its call | AT with one connector instance, as the factory caches it: two Portfolios on two ADO connections → one listing request each, each to its own organisation; the first again → one new request. The connector holds no field of the listing's type |
| Findings survive a delta run | AT: full run with a finding, then a delta run → the section still shows the full run's finding and time; a later full run without it → gone |
| Upgrade repair is owner-scoped | Migration test on SQLite and Postgres: an id naming another connection's field is repaired like a deleted one |
| Leaf adapters stay leaf | Existing `WorkTrackingConnectors_DoNotDependOnAnyRepository` and the leaf rule |
| Probe what the tracker says | Slice 03 spike: the relation-type listing with a `vso.work` PAT on Services and Server 2022, recorded as fixtures; an empty or refused listing fails Validate saying what happened, never resolves every reference to nothing (`JiraWorkTrackingConnector.cs:1821-1830` precedent) |

---

## Wave: DESIGN / [REF] Open Questions (deferred to DISTILL / DELIVER)

| # | Question | Deferred to | Why it can wait |
|---|---|---|---|
| OQ-1 | The relation-type listing on Server 2022 and with `vso.work`, and which directional type slice 04 dogfoods on (F9, P8) | DELIVER slice 03 spike | Decides the fixture and the test vehicle, not the design |
| OQ-2 | Final polish of the UI-6 copy and the log entry wording | DELIVER | Meaning fixed by the maintainer |
| OQ-3 | ~~How the Estimation bundle compares under G-R1 (b)~~ — **resolved** (review iteration 2, AFK recommended option): one value; non-null when its field is set; differs when any of field, unit, flag or ordered categories differs (DDD-11, ADR-240 §6) | — | — |
| OQ-4 | What a Team admin without `SystemAdmin` sees in the field dropdowns today (F12) | DISTILL | Pre-existing; affects only the scenario's Given |
| OQ-5 | ~~Owners pointing at a deleted field in a follow list~~ — **resolved**: DDD-21 repairs them and slice 07 is dropped. Remaining: slice 05a's dogfood counts how many owners the migration moves (a query before the upgrade) | DELIVER 05a | A count, not a design input |
| OQ-6 | ~~Usage-data event~~ — **resolved**: none (DDD-24) | — | — |
| OQ-7 | ~~`OUT-6303-followers-share` target~~ — **resolved upstream**: the KPI is dropped (DISCUSS CA-D7) | — | — |

---

## Wave: DESIGN / [REF] Constraint Carried Into DELIVER

- **Comments are for a stranger.** No comment may cite `D10`, `DDD-4`, `AC-F.1`, `G-R1` or an ADR number as its
  explanation; say the reason. The one exception is a narrow `#pragma warning disable` carrying its reason inline.
- **Migrations:** `Create-Migration.ps1` only; build the migration DLLs first; the two data steps (clear dangling, then
  set `Own`) hand-added to **both** providers' migrations, in that order; verify with `Database__Provider=sqlite` for
  `PendingModelChanges`; confirm the renamed own properties produce **no** column change; add every new `Teams` /
  `Portfolios` column to `HistoricalSchemaPatch`.
- **S107:** `WorkTrackingSystemConnectionController` stays at 7 constructor parameters: `IConnectionSaveService` replaces
  `IDomainEventDispatcher` (DDD-25); the service itself takes five. The list controller's follower counts ride the
  existing usage port.
- **Concurrency:** keep applying the client's connection token in `Commit` before `Save()`; run the in-use query in
  `Commit` against the database, never the `Open` snapshot; publish and log only after a successful save.
- **S6964:** every new value-type member on a `[FromBody]` DTO is nullable.
- **Enums go out as strings, in as ints:** the frontend mirrors `SettingSource` as a string-literal union.
- **zod:** every nullable member `.nullable()`, never bare `.optional()`.
- **CA1869:** the findings serializer options are a `static readonly` field.
- **The log entry:** Information level, one per default-changing save, structured template, names only.
- **Release note** for each slice whose migration repairs dangling values (wording above).
- **ARCHITECTURE.md in the same change** — §3: a paragraph on settings an owner inherits from its connection (the
  owner answers with the effective value; the defaults reach the fingerprint only through it), the Dependency Field
  naming a link type, and correct the sentence saying the connector declares the parent source (ADR-193's port was
  never built); §5: `ConnectionFetchShapeChanged` carries only the owners whose fetch moved, the browser's question also
  opens for a default change with followers, the save logs who follows, and the connection save's rules live in
  `IConnectionSaveService` with the controller as a mapper; §16: rows for ADR-240 to 243.

---

## Wave: DESIGN / [REF] Changed Assumptions

| # | DISCUSS said | Now | Effect |
|---|---|---|---|
| CA-1 | "C1–C5 are foreign keys to an `AdditionalFieldDefinition`" | Plain `int?` columns, no FK (F1) | P5 answered; D17 stands, and is also a fix |
| CA-2 | L3 / S19: `WorkItemRelatedSettingsChanged` must read the effective value | It reads only the connection id (F4) | No change there |
| CA-3 | — | **Void**: the after-save notice it was about is withdrawn | — |
| CA-4 | D21 table: C4 Feature Owner N/A on CSV | Honoured by the extrapolation on CSV (F5) | **Accepted**: offered on CSV |
| CA-5 | P10: ServiceNow decided at DESIGN | N/A for all five (F5) | Group hidden on ServiceNow |
| CA-6 | US-03 example 2 and slice 04's vehicle `System.LinkTypes.Related-Forward` | Related is non-directional (F9) | **Maintainer:** D2 stands; examples and vehicle move to a directional type (U-13) |
| CA-7 | AC-3.7: relations "already arrive" | A second request batch, skipped in one configuration today (F8) | AC-3.7 stands as written |
| CA-8 | G-R1 (b) | A mode-unaware client can no longer **clear** an owned value by sending null. **And a stale echo can silently make a follower an owner** (review iteration 2): a client loads a Team that follows the Teams' Parent Override default "Caused by"; a System Admin changes the default to "Parent Link"; the client PUTs the Team back with "Caused by" and no source → the value is non-null and differs from the effective value before the request, so the Team becomes *Own ("Caused by")* and stops following. The Team's concurrency token does not catch it (the default changed on the connection row). Mode-aware clients must send the source they read; the browser always does; future Lighthouse-Clients changes that add these fields must too | **Accepted** as known consequences of a maintainer-locked rule; pinned by an AT (Architectural Enforcement) |
| CA-9 | DependencySourceSelector "gains a source" | The stored enum is closed on the frontend (F6) | Stored references say `TrackerLink` |
| CA-10 | **D12** — "Saving a default shows a save notice naming who now follows it (maintainer requirement). Informational, never a confirmation. AC-F below; every default save, first set or changed, for every setting." **AC-F.1** "… the save notice names how many Teams (or Portfolios, by the default's grain) now follow that default and lists them by name …" **AC-F.2** "It is informational: the save is already stored; nothing asks to confirm or undo it." **AC-F.3** "When no owner follows the changed default, it says so; when no default changed, no notice appears." **AC-F.4** "Counts and names use the configurable Team / Portfolio terms. One notice per save, covering every default that changed in it." **AC-F.5** "From slice 06 on, the same notice says how many of the followers are updating (Story 6307's message folded in, not a second message)." | **Maintainer:** no on-screen notice after save; S3 stands. The follower count is in the pre-save dialog (UI-3: none when nobody follows); the names in one log entry; the updates in Task Manager → Activity | D12, AC-F.1–F.5 rewritten upstream (U-1 … U-6) |
| CA-11 | **D10** "At the upgrade, empty becomes *Connection default*; a value stays *Own*" and **AC-4.4** "… no Team reads anything different or downloads everything again because of the upgrade" | **Maintainer:** a value naming a field the connection no longer has is treated as empty (DDD-21) | D10, AC-4.4 rewritten upstream (U-7, U-8) |
| CA-12 | US-06 / slice 07, AC-6.1–6.6, G-07, job `job-config-admin-see-which-owners-differ-from-the-connection` | **Maintainer (UI-4):** slice 07 dropped completely | US-06, slice 07, the job, `OUT-6303-followers-share` and every reference to 07 change upstream (U-18 … U-20) |
| CA-13 | AC-4.8 / US-04: the refusal names "the default and the Teams that follow it" | Approved copy (UI-6) names the default and the owners using the field as their own; followers hold no own value (DDD-5) | AC-4.8 wording upstream (U-21) |

Upstream changes are listed with replacement wording in `design/upstream-changes.md`.

---

## Wave: DESIGN / [REF] brief.md

Written to `docs/product/architecture/brief.md` as "## Application Architecture — epic-6303-configure-once-per-connection
(DESIGN delta)", appended after the story-6307 section: invariants 6303-I1 … I9, the component headline, C4 (topology
unchanged), enforcement, usage data (none) and ADR-240 … 243.

---

## Wave: DESIGN / [REF] Wave Decisions Summary

### Key decisions
LOCKED: DDD-3, 5 (mutators), 6, 7, 11, 13, 14, 17 (ServiceNow), 18 (placement), 20, 23. ACCEPTED by the maintainer
(2026-10-16): DDD-1, 2, 4, 5 (clearing), 8, 9, 10, 12, 15, 16, 17 (CSV), 18 (409), 21, 22, 24. ACCEPTED after review
iteration 2 (recommended options, maintainer AFK): DDD-11 (bundle rule), 14 (per-call listing), 15 (latest full run),
25 (application service), 26 (existing concurrency token), 27 (no FK), 28 (data step). DROPPED: DDD-19 (slice 07).
ADR-240 to ADR-243 Accepted. UI-1 … UI-6 decided by the maintainer.

### Requirements summary
The owner's existing setting becomes its effective value; defaults sit on the connection; one application service owns
the connection save, compares each owner's fetch fingerprint before and after within one unit of work guarded by the
connection's existing concurrency token, queues the movers and logs who follows; the controller only maps; the browser
shows follower counts in the pre-save dialog only and still navigates to `/`; the Dependency Field reads a named link
per tracker through the existing selector; findings travel the 6301 path to Refresh History, which shows the latest
full run's; values pointing at deleted fields are repaired at the upgrade.

### Constraints established
No fingerprint moves at the upgrade except for owners whose value pointed at nothing; no new Jira request; at most one
ADO relation-type listing per connection per Validate and per refresh, never remembered on the cached connector; no new
permission; no new controller; the connection controller stays at 7 constructor parameters; no FK; no client release;
the PUT response unchanged; expand-only migrations, one per slice.

### Usage data (for DEVOPS)
**None.** N/A, because a connection default is set once at setup; an event would count configuration saves, not whether
the feature helps anyone. `UsageDataEventName` is unchanged (maintainer, 2026-10-16).

### Upstream changes
D10, D12, D14 (G-07), AC-4.4, AC-4.8, AC-F.1–F.5, AC-5.3, US-06 and AC-6.*, the AC-F references in US-07 … US-11, the
journey, KPIs `OUT-6303-no-surprise` and `OUT-6303-followers-share` (now dropped), R2's mitigation, US-03 example 2, the
job `job-config-admin-see-which-owners-differ-from-the-connection`, the story map and release points, and slice briefs
03, 04, 05a, 05b, 06, 07 (dropped), 08–12 — in `design/upstream-changes.md`; the product owner has applied them
(DISCUSS "Changed Assumptions (from DESIGN …)"). Review iteration 2's fixes add nothing upstream. Peer review
(solution-architect-reviewer) iteration 2 re-runs next.
