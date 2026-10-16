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
| #6304 | Work Tracking Connection defaults that Teams and Portfolios inherit and can override (the mechanism + the Teams' Parent Override Field) | 05a, 05b, 06, 07 | ~3d |
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
| `config-admin` (as System Admin) | **Primary.** Owns the connection; registers the link as an Additional Field; sets defaults once; reads who follows them. |
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
- **`job-config-admin-see-which-owners-differ-from-the-connection`** (NEW; #6304) — When a forecast on one Team
  looks off, I want to see who follows the connection's setting and who carries its own, so I find the odd one
  out without opening each of them.

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
| CSV | N/A — never consulted (6028 S4; no read in the CSV connector) | N/A — dependencies are a column; opts out (S4) | N/A — no read in the CSV connector | N/A — no read in the CSV connector | ✓ — CSV reads Additional Field values (`CsvWorkTrackingConnector.cs:244-254`), and estimation reads them at page load |
| Linear | N/A — no read in the Linear connector | N/A — one relation type; opts out (S4) | N/A | N/A | N/A — the Linear connector reads no Additional Field values, so there is nothing to point at |
| ServiceNow | **Not offered until DESIGN verifies** — no read of C1–C4 or of Additional Field values was found in `WorkTrackingConnectors/ServiceNow/`; DESIGN confirms and either adds it to this table or records the N/A | same | same | same | same |

So the group shows five settings on Jira and Azure DevOps, Estimation only on CSV, and nothing on Linear (the
group is hidden). ServiceNow is decided at DESIGN.

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
  - **Azure DevOps** (#6312): a relation type **reference name with its end** (`System.LinkTypes.Related-Forward`,
    `Contoso.DependsOn-Reverse`), checked against the organisation's relation types at Validate and matched
    against the relations that already arrive with the fetch (S13). Display names are not matched (S3).
- **D2 — One-way only (OD-2 A).** The named end sets the direction. A Jira type name or a phrase both ends share,
  or an ADO reference name without its end, reads no dependencies and warns once per refresh naming both phrases
  / ends.
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
- **D10 — At the upgrade, empty becomes *Connection default*; a value stays *Own* (OD-12, changed).** No default
  exists on release day, so nothing changes then. Accepted consequence: an owner that was deliberately None follows
  a default once one is set. D12 makes that visible.
- **D11 — New Teams and Portfolios start on *Connection default* (OD-13 A).**
- **D12 — Saving a default shows a save notice naming who now follows it (maintainer requirement).**
  Informational, never a confirmation. AC-F below; every default save, first set or changed, for every setting.
- **D13 — A changed default updates exactly the followers whose fetched data it changes (OD-14 A).** Reuses Story
  6307's event and queue. Estimation queues nothing (read-time).
- **D14 — Permissions unchanged (OD-15).** Defaults `SystemAdmin`; overrides `TeamWrite` / `PortfolioWrite`; the
  followed value's name is carried on the owner's own settings, so an owner admin sees it without connection
  rights. No new permission. Slice 07's bulk follow needs `SystemAdmin` only, with no per-owner check (G-07,
  decided).
- **D15 — Shown as the first dropdown entry "Connection default (value)" (OD-10 A).** Exact copy at the DISTILL
  sketch review.
- **D16 — Estimation follows as one bundle (OD-16 A).** Field, unit, non-numeric flag and categories together.
- **D17 — A referenced Additional Field cannot be deleted (OD-17 A).** Refused while a default or an owner points
  at it, naming the referrers. **Lands in slice 05b** (M4).
- **D18 — A PUT without the mode keeps the stored mode (OD-18, amended by G-R1 (b), maintainer 2026-10-16).**
  Unless the payload sends a present, non-empty value that differs from the effective one — then *Own* with that
  value; an empty or absent value changes nothing. Replaces pass 2's "absent mode = *Own*". No client release.
- **D19 — Four Stories in Next, in order #6300 → #6312 → #6304 → #6313 (maintainer, 2026-10-16).** Each ships on
  its own; later ones build only on finished earlier ones. Resolves review finding B5.
- **D20 — Steve Pereira stays named (maintainer, 2026-10-16).** A public, credited community contributor in the
  release notes. Review finding m6 declined.
- **D21 — The defaults group offers only the settings the connection's tracker honours (m7).** Per the table
  above; on Linear the group is hidden; ServiceNow decided at DESIGN.
- **D22 — Slice 05 splits into 05a and 05b now (M4).** 05a: the mechanism end to end for the Teams' Parent
  Override Field, with the save notice. 05b: D17's delete refusal and the older-payload round trip (G-R1 (b)).

### Gates — both decided by the maintainer (2026-10-16)

- **G-R1 — What an absent mode means on a PUT. DECIDED: option (b).** A Team / Portfolio PUT without the mode keeps
  the stored mode, unless the payload sends a value for that setting that is **present and non-empty** and differs
  from the effective one; then the owner becomes *Own* with that value. No mode with an empty or absent value
  changes nothing. Option (a) (absent = *Own*) was rejected: it would let a client that drops unknown properties
  silently turn followers into a deliberate None. Amends D18; AC-4.9, AC-4.10; slices 05b and 08.
- **G-07 — Whether slice 07's bulk follow needs a per-owner check. DECIDED: `SystemAdmin` is enough.** No per-owner
  `TeamWrite` / `PortfolioWrite` check, no new permission. D14, AC-6.6, slice 07.

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

### The save notice — acceptance criteria (D12; slices 05a and 06–12)

- **AC-F.1** When a System Admin saves a connection on which a default was set for the first time or changed,
  the save notice names how many Teams (or Portfolios, by the default's grain) now follow that default and lists
  them by name — every owner in *Connection default* mode, including those that follow because their setting was
  empty at the upgrade — so the admin can see what the save changes.
- **AC-F.2** It is informational: the save is already stored; nothing asks to confirm or undo it.
- **AC-F.3** When no owner follows the changed default, it says so; when no default changed, no notice appears.
- **AC-F.4** Counts and names use the configurable Team / Portfolio terms. One notice per save, covering every
  default that changed in it.
- **AC-F.5** From slice 06 on, the same notice says how many of the followers are updating (Story 6307's message
  folded in, not a second message). Exact layout at the DISTILL sketch review.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS.** 13 slices, ~9 days, 3 contexts (connectors; configuration aggregates; sync fingerprint + queue), in four
ADO Stories that each ship alone (D19) — which resolves the review's story-size finding (B5): no Story exceeds
~3 days or 7 scenarios per user story.

| Story | User stories | Slices | Est. |
|---|---|---|---|
| #6300 | US-01, US-02 | 01, 02 | ~1.5d |
| #6312 | US-03 | 03, 04 | ~1.5d |
| #6304 | US-04, US-05, US-06 | 05a, 05b, 06, 07 | ~3d |
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
- [ ] AC-2.2 It appears on the Portfolio's row in Refresh History (D5a), and in the log as one line.
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
2. `System.LinkTypes.Related-Forward` named by a Services team that records waits as Related → read from those.
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
- [ ] AC-3.6 A reference without an end reads nothing and warns; nothing matching anywhere warns once, listing the
  relation types seen — both on Refresh History.
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
  refresh naming the setting and the relation type, saying a relation type cannot be read there. ADO parent from a
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
Field for Teams** to "Caused by", save → sees the save notice "3 Teams now follow Caused by: Fraud Signals, Ops
Tooling, Mobile Wallet"; then **Team settings → Advanced** on Fraud Signals shows "Connection default (Caused by)".
Decision enabled: whether a new Team can be onboarded without the parent checklist — and, from the notice,
whether a Team that was empty on purpose must be set back to None.

**Domain examples**
1. "Fraud Signals", empty at the upgrade → follows; named in the notice.
2. "Ledger", with its own "Caused by" → stays *Own*, unaffected.
3. "Mobile Wallet", empty at the upgrade but meant as None → follows and is named; its admin picks **None** → *Own*
   + empty, no longer follows.
4. A Team created afterwards → starts on *Connection default*.
5. Maria tries to delete the "Caused by" Additional Field → refused, naming the default and the 3 followers (05b).

**UAT scenarios**
```gherkin
Scenario: Saving the default says who now follows it
  Given Fraud Signals, Ops Tooling and Mobile Wallet had no Parent Override Field at the upgrade
  And Ledger has "Caused by" as its own value
  When Maria Santos sets Parent Override Field for Teams to "Caused by" and saves the connection
  Then the save notice says 3 Teams now follow "Caused by" and names them
  And Ledger is not among them

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

Scenario: The upgrade changes nothing on its own
  Given no connection has a default
  When the release is installed
  Then every Team reads exactly what it read before, and no Team's next update is a full download caused by the upgrade

Scenario: A field a default points at cannot be deleted
  Given the Teams' default is "Caused by"
  When Maria deletes the "Caused by" Additional Field
  Then the deletion is refused, naming the default and the Teams that follow it

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
  empty on release day, so nothing changes; Teams that had one own it; no Team reads anything different or
  downloads everything again because of the upgrade.
- [ ] AC-4.5 New Teams start on *Connection default*.
- [ ] AC-4.6 A Team admin without connection rights sees the followed field's name on the Team's settings.
- [ ] AC-4.7 The defaults group appears only on connections whose tracker honours the setting (D21).
- [ ] AC-4.8 Deleting an Additional Field referenced by a default or by any owner is refused, naming them (D17).
- [ ] AC-4.9 When a Team's settings are saved by a client that does not send the mode: if it sends no Parent
  Override Field or an empty one, the Team keeps following or owning exactly as before; if it sends a different
  field, the Team owns that field from then on (G-R1 (b)).
- [ ] AC-4.10 A scripted GET-then-PUT of every Team's settings, unchanged — in the shape that echoes every property
  and in the shape that drops properties it does not know — changes no Team's mode or effective value; both shapes
  pass.

**Technical notes** — expand-only migration: a nullable default FK on the connection; a mode column on Team,
back-filled from emptiness (D10). One resolver (L3) used by both connectors, `FetchFingerprint.For` and
`WorkItemRelatedSettingsChanged`; with no default an empty follower's effective value is `null`, so stored
fingerprints are byte-identical across the upgrade. The followed display name travels on the Team settings DTO.
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
After: save the connection with "Parent Link" as the new default → sees the save notice "9 Teams now follow Parent
Link … 9 are updating", and 9 rows in the Task Manager.
Decision enabled: check the result now, or come back later.

**Domain examples** — 1. 9 follow, 5 own → 9 updating. 2. Nobody follows → nothing updating, notice says so.
3. Estimation default changed (after #6313) → nothing updating; notice says it applies on the next page load.

**UAT scenarios**
```gherkin
Scenario: Only the followers are updated
  Given 9 Teams follow the Parent Override default and 5 carry their own
  When the System Admin changes the default and saves
  Then the Task Manager shows an update for exactly the 9 followers
  And the save notice says 9 are updating

Scenario: Nobody follows
  Given every Team carries its own value
  When the default changes
  Then no update starts and the save notice says no Team follows it

Scenario: Saving without updating
  When the admin saves the connection without starting updates
  Then no update starts and the save notice still names the followers
```

**Acceptance criteria**
- [ ] AC-5.1 After the save, the Task Manager shows an update for every follower whose fetched data the change
  affects, and for no owner that owns its value.
- [ ] AC-5.2 Saving without starting updates starts none.
- [ ] AC-5.3 The one save notice also says how many of the followers are updating — 9 in the first scenario, none
  in the second, and none when the admin saved without starting updates — never as a second message (AC-F.5).

**Technical notes** — followers whose `FetchFingerprint.For` moved are queued through `ConnectionFetchShapeChanged`
(S18); `startUpdates=false` is the existing switch.

---

#### US-06 — Owners whose own value equals the default follow it in one step

`job_id: job-config-admin-see-which-owners-differ-from-the-connection` · **ADO Story #6304** · slice 07

**Who.** config-admin as System Admin on a connection configured by hand before the upgrade.

**Why it still earns its place after D10.** D10 moves only *empty* owners onto the default. An installation already
configured by hand — Steve's customer, whose 14 Teams all carry "Caused by" — keeps every Team on *Own*. Without
this, the default helps only new Teams there, and the next change of the default must still be made on 14 Teams.
The persistent list also answers "hard to see at a glance" between saves, which the save notice cannot.

##### Elevator Pitch
Before: 14 Teams carry "Caused by" as their own value; changing it later means 14 edits.
After: on the connection's **Defaults for Teams and Portfolios** → sees "Parent Override Field for Teams: Caused
by — 3 follow; 11 own the same value; Ledger Ops owns Epic Link" and chooses **Follow the default** for the 11.
Decision enabled: which Teams genuinely differ, and whether to bring the rest under one setting.

**Domain examples** — 1. 11 own-and-equal → offered; moved; nothing updates. 2. Ledger Ops (`Epic Link`) listed, not
offered. 3. An owner edited by another admin meanwhile → named, not overwritten.

**UAT scenarios**
```gherkin
Scenario: The connection lists who follows and who differs
  Given 3 Teams follow, 11 own "Caused by" and Ledger Ops owns "Epic Link"
  When Maria opens the connection's defaults
  Then she sees 3 following, 11 owning the same value, and "Ledger Ops: Epic Link"

Scenario: Matching owners follow in one step without an update
  When she has the 11 matching Teams follow the default
  Then they show "Connection default (Caused by)" and no update starts

Scenario: A concurrent edit is not overwritten
  Given another admin saved one of the 11 since the list was loaded
  When she has the 11 follow the default
  Then that Team is named as changed by someone else and keeps its value, and the other 10 follow
```

**Acceptance criteria**
- [ ] AC-6.1 Per default: the count of followers; own-value owners named with their value; configurable terms.
- [ ] AC-6.2 Only owners whose own value equals the default are offered; after following, they read exactly what
  they read before and no update starts.
- [ ] AC-6.3 If another admin changed an owner since the list was loaded, that owner is named as changed by someone
  else and keeps its value; the others follow.
- [ ] AC-6.4 The list has a loading look and a could-not-load look (defined at DESIGN).
- [ ] AC-6.5 Each #6313 setting appears in this list when its slice lands, with no new UI.
- [ ] AC-6.6 The action is offered to every System Admin and to no one else; a System Admin can move any owner on
  the connection, with no separate Team or Portfolio right asked for (G-07, decided).

**Technical notes** — each owner written with its concurrency token; following changes the mode only, so the
fingerprint does not move. G-07 (maintainer, 2026-10-16): the route is guarded by `SystemAdmin` alone through
`IRbacAdministrationService`; no per-owner `TeamWrite` / `PortfolioWrite` check, no new permission.

---

### Story #6313 — More Team and Portfolio settings set once on the Work Tracking Connection

Each builds on finished #6304: same resolver, save notice (AC-F.1 – F.5), update queue and US-06 list; each adds
one default column, one mode column with the D10 back-fill, D11 for new owners, the dropdown entry, and the followed
name on the owner's settings. Each slice ships alone.

#### US-07 — Set the Portfolios' Parent Override Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 08

**Who.** config-admin as System Admin with several Portfolios on one Jira connection, each reading its Features'
parents from the same link; config-admin as Portfolio admin who may own a different one.

##### Elevator Pitch
Before: each of the 4 Portfolios on "Jira DC – Payments" names "Parent Link" in its own Parent Override Field, and a
new Portfolio starts without it, so its Features hang under nothing.
After: in **Defaults for Teams and Portfolios** set **Parent Override Field for Portfolios** to "Parent Link", save →
sees the save notice naming the Portfolios that now follow it; a new Portfolio "Q3 Roadmap" shows "Connection
default (Parent Link)" and, after its update, each Feature hangs under the item its `Parent Link` names.
Decision enabled: whether a new Portfolio needs anything but its query.

**Domain examples**
1. "Q3 Roadmap", created after the default → follows; its Features hang under their `Parent Link` items.
2. "Card Issuing" owns "Initiative Link" → unaffected; listed as differing.
3. "Merchant Onboarding" had no Parent Override Field at the upgrade → follows, named in the notice; the Teams'
   default does not touch it.

**UAT scenarios**
```gherkin
Scenario: Saving the Portfolios' default names the Portfolios that follow it
  Given "Merchant Onboarding" had no Parent Override Field at the upgrade and "Card Issuing" owns "Initiative Link"
  When Maria sets Parent Override Field for Portfolios to "Parent Link" and saves
  Then the save notice names "Merchant Onboarding" as following and does not name "Card Issuing"

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
- [ ] AC-7.1 Saving the Portfolios' default shows the save notice naming the Portfolios that follow it.
- [ ] AC-7.2 A following Portfolio's Features hang under the items the default names, after its update.
- [ ] AC-7.3 The Teams' default never applies to a Portfolio, nor the reverse (D8).
- [ ] AC-7.4 An owning Portfolio is unaffected and appears as differing in the US-06 list.

**Technical notes** — C1 at Portfolio grain: a second, separate default FK on the connection (D8) and a Portfolio mode
column with the D10 back-fill (expand-only, L2); the resolver (L3), save notice, delete refusal, queue and US-06 list
from #6304 reused; the Portfolio PUT gets the G-R1 (b) rule and AC-4.10's round trip. `AdvancedInputs.tsx:28-52` on
the Portfolio form gains the entry. Jira and ADO only (D21). Brief: `slices/slice-08-6313-portfolio-parent-override-default.md`.

---

#### US-08 — Set the Portfolios' Dependency Field once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 09

**Who.** config-admin as System Admin whose Portfolios all record waits the same way — a field or, after
#6300 / #6312, a link.

##### Elevator Pitch
Before: every Portfolio on the connection picks "Depends on" as its Dependency Field one by one, and a new
Portfolio shows an empty Dependencies view until someone remembers.
After: set **Dependency Field for Portfolios** to "Depends on", save → sees the save notice naming the Portfolios
that follow; after its update, a follower's **Features → Dependencies** shows PAY-112 waiting on PAY-98.
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
- [ ] AC-8.4 The save notice names the followers (AC-F.1 – F.5).

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
After: set **Size Estimate Field for Portfolios** to "T-Shirt Size", save → sees the save notice; after its update a
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
- [ ] AC-9.4 The save notice names the followers.

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
After: set **Feature Owner Field for Portfolios** to "Owning Team", save → sees the save notice; after its update a
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
- [ ] AC-10.4 The save notice names the followers. The Portfolio's own Owning Team setting is not inherited.

**Technical notes** — C4: `Portfolio.cs:21`, read at fetch time (`JiraWorkTrackingConnector.cs:1245-1250`,
`AzureDevOpsWorkTrackingConnector.cs:1200-1205`) through the resolver (L3); in the fingerprint (`FetchFingerprint.cs:50`,
`:92`). `OwnershipComponent.tsx:34-48` gains the entry; `OwningTeamId` stays the Portfolio's own. Jira and ADO only
(D21). Brief: `slices/slice-11-6313-feature-owner-field-default.md`.

---

#### US-11 — Set the Estimation once

`job_id: job-config-admin-set-a-connection-setting-once` · **ADO Story #6313** · slice 12

**Who.** config-admin as System Admin whose Teams all estimate in Story Points; Team admins with their own scale.

##### Elevator Pitch
Before: Story Points is picked as Estimation Field on each Team and Portfolio, with "Points" typed as unit each time.
After: set **Estimation for Teams** to Story Points / "Points", save → sees the save notice naming the followers and
that no update is needed; a follower's estimation views use Story Points with "Points" on the next page load.
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
- [ ] AC-11.4 The save notice names the followers and says no update is needed.

**Technical notes** — C5: `WorkTrackingSystemOptionsOwner.cs:68-74`, read at read time by `EstimateNormalizer.cs:71-90`,
not in the fetch fingerprint, so slice 06's queue does nothing by construction (D13). One mode column per owner for
the bundle; defaults per grain (Teams, Portfolios), confirmed at DESIGN. `EstimationFieldComponent.tsx`
(`ModifyTeamSettings.tsx:293`, `ModifyProjectSettings.tsx:301`) gains the entry. Offered on Jira, ADO and CSV (D21).
Brief: `slices/slice-12-6313-estimation-default.md`.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone**: name the link (Jira) → say when it matched nothing → the same on Azure DevOps → set a default once →
reach the followers' data → bring hand-configured owners along → the next settings.

| Slice | ADO Story | User story | Goal | Est. | Brief |
|---|---|---|---|---|---|
| 01 | #6300 | US-01 | Jira Portfolio reads dependencies from a named one-way phrase | ~1d | `slices/slice-01-6300-jira-dependencies-from-a-named-link-phrase.md` |
| 02 | #6300 | US-02 | A named Jira link that matched nothing says so on Refresh History | ~0.5d | `slices/slice-02-6300-a-named-link-that-matched-nothing-says-so.md` |
| 03 | #6312 | US-03 | ADO relation type accepted where a field name goes; warns until read | ~0.5d | `slices/slice-03-6312-ado-relation-type-accepted-as-a-reference.md` |
| 04 | #6312 | US-03 | ADO Portfolio reads dependencies from the named relation end | ~1d | `slices/slice-04-6312-ado-dependencies-from-a-named-relation-type.md` |
| 05a | #6304 | US-04 | Teams' Parent Override default, end to end, with the save notice | ~1d | `slices/slice-05a-6304-team-parent-override-default.md` |
| 05b | #6304 | US-04 | Referenced fields cannot be deleted; older payloads change nothing | ~0.5d | `slices/slice-05b-6304-referenced-fields-and-older-payloads.md` |
| 06 | #6304 | US-05 | A changed default updates exactly the followers | ~0.5d | `slices/slice-06-6304-default-change-updates-the-followers.md` |
| 07 | #6304 | US-06 | Owners matching a default follow it in one step | ~1d | `slices/slice-07-6304-matching-owners-follow-the-default.md` |
| 08 | #6313 | US-07 | Portfolios' Parent Override default | ~0.5d | `slices/slice-08-6313-portfolio-parent-override-default.md` |
| 09 | #6313 | US-08 | Dependency Field default | ~0.5d | `slices/slice-09-6313-dependency-field-default.md` |
| 10 | #6313 | US-09 | Size Estimate Field default | ~0.5d | `slices/slice-10-6313-size-estimate-field-default.md` |
| 11 | #6313 | US-10 | Feature Owner Field default | ~0.5d | `slices/slice-11-6313-feature-owner-field-default.md` |
| 12 | #6313 | US-11 | Estimation bundle default | ~1d | `slices/slice-12-6313-estimation-default.md` |

**Release points (L8):** #6300 after 02 (not after 01 — see US-01); #6312 after 04 (03 alone is safe by AC-3.9);
#6304 after 07 — 05a, 05b and 06 each leave `main` releasable but D17 and the G-R1 (b) PUT rule land in 05b and the bulk follow in
07; #6313 after each of 08–12, every one a complete setting.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice is not thin | Resolved by the 05a / 05b split (D22). 05a still carries migration, resolver, connection group, dropdown entry and notice — the minimum for one setting to be followed end to end; nothing in it is removable without leaving the mechanism unusable. |
| A new abstraction ships first | Pass. ADO's relation-type lookup arrives in 03 as user-visible validation; the resolver, notice and defaults group in 05a; later slices reuse them. |
| No slice disproves a pre-commitment → decoration | Pass — each brief names its hypothesis (01 one-way phrase reads direction; 03 relation types reachable with `vso.work`; 04 no extra request; 05a fingerprints survive the upgrade; 05b an older client round trip is harmless; 07 hand-configured owners want to follow; 08–12 each setting really is per connection). |
| Synthetic data only → plumbing | 01–04 dogfood on the dev instance's real Jira and ADO (P2, P8). 05a–12 on the dev instance's real connections (`:5169`) plus demo data for E2E and screenshots. |
| 2+ slices identical except for scale → merge | 08–11 look alike but each ships a different setting alone, at the maintainer's request (D7). 03 and 04 stay apart because 03 alone can end #6312 cheaply if the relation-type list is unreachable. |

### Prioritisation

1. **#6300 (01, 02)** — the reported instance is Jira; 02 at once, because until it ships 01's failure is silent.
2. **#6312 (03, 04)** — 03 first because it can disprove reachability cheaply; 04 closes the S14 trap in the slice
   that makes a relation readable.
3. **#6304 (05a, 05b, 06, 07)** — 05a is the mechanism and where D10's consequence becomes visible (AC-F); 05b
   before anything depends on the PUT contract (G-R1 (b)); 06 at once, otherwise a changed default is invisible for hours;
   07 because on hand-configured installations (the reporter's) the default does nothing for existing Teams
   without it.
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
| Defaults on Linear, and the C1–C4 defaults on CSV | D21 — those trackers do not honour the settings. |
| Distinguishing, in the save notice, owners that follow because they were empty at the upgrade from owners an admin set to follow | Not asked; would need a stored marker. *Still Open* Q1. |
| Instance-wide defaults across connections | A default points at one connection's own Additional Fields. |
| Inheriting Ignore Dependencies, or a Portfolio's own Owning Team setting | Planning choices, not tracker facts (AC-8.3, AC-10.4). |

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Brownfield — extend existing paths; no greenfield skeleton** (maintainer: "Depends").

- #6300 / #6312: Additional Fields → Validate (Jira: Epic 6028 resolver; ADO: field lookup, then relation types
  from slice 03) → `DependencySourceSelector` → `Feature.DependsOnReferences` → Features → Dependencies; findings on
  Refresh History. Slice 01 is the skeleton; 04 extends it to ADO.
- #6304: connection edit page (S16) → connection PUT → resolver → `FetchFingerprint.For` → connectors → stored records;
  connection save → save notice; → `ConnectionFetchShapeChanged` → queue → Task Manager. Slice 05a is the skeleton;
  06 joins the queue.

E2E: one walking skeleton for #6304 on demo data (set a default, read the notice, see a Team follow). #6300 /
#6312 are covered below E2E unless demo data can carry a named link.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Port | Guard |
|---|---|---|
| Name the link | existing Additional Fields editor + Validate | `SystemAdmin` (unchanged) |
| Select it | existing Dependency Field dropdown | `PortfolioWrite` (unchanged) |
| Read the result / warning | existing Features → Dependencies; existing Refresh History (D5a) | unchanged |
| Set a default; read the save notice | new group on the connection page, existing connection PUT | `SystemAdmin` |
| Follow / override | existing dropdowns on Team / Portfolio settings, one new entry (S22) | `TeamWrite` / `PortfolioWrite` |
| Move matching owners to follow | new action on the connection's defaults group; route per DESIGN | `SystemAdmin` only, no per-owner check (G-07) |
| Clients | `PUT /v1/teams/{id}`, `/v1/portfolios/{id}`, `/v1/worktrackingsystemconnections/{id}` | additive fields only; no mode on a PUT keeps the stored mode (G-R1 (b)) |

---

## Wave: DISCUSS / [REF] Outcome KPIs

Self-hosted: every KPI is `per_instance` or `vendor_demo_only`. Whether a usage-data event should report any of them
is the DEVOPS wave's question (house rule) — see the checklist.

| KPI | Who / does what | Target | Measured by | Baseline |
|---|---|---|---|---|
| `OUT-6303-dependencies-recovered` | Portfolios that model waits with a non-default link show them | **100%** of Features whose named link points at a Feature Lighthouse stores show that dependency after the update (allowing only counterparts outside every Portfolio, which epic-4365 already reports) | On the dev instance (`:5169`): count Features in the slice-01 Jira Portfolio and the slice-04 ADO Portfolio carrying ≥ 1 link of the named type to a stored Feature, against Features with ≥ 1 dependency, before and after; recorded in slices 01 and 04. Steve's customer: same count, n = 1 | **0%** — no such Feature shows a dependency today (Jira silently, S6/S7) |
| `OUT-6303-set-once` | System Admin configures a setting for N owners | 1 connection save instead of N owner saves; 0 per new owner | Saves counted in the slice-05a dogfood script | N |
| `OUT-6303-followers-share` | Owners on a connection with a default that follow it | ≥ 80% after slice 07 on a hand-configured connection | **Per instance**: modes counted per connection by a query on that instance; Lighthouse cannot see it across installations. Whether a usage-data event should carry it is the DEVOPS question (checklist) | 0% (no concept) |
| `OUT-6303-no-change-on-upgrade` | Owners whose effective setting changed through the upgrade | **0** | Upgrade test (AC-4.4) | — |
| `OUT-6303-no-surprise` | Admin learns who a default affects at the moment of saving | 100% of default saves show the save notice | AC-F test coverage; dogfood | no notice exists |
| `OUT-6303-no-extra-requests` | Tracker requests per refresh | Jira unchanged; ADO ≤ +1 relation-type listing per connection per Validate and per refresh | Request count vs baseline (AC-1.6, AC-3.7) | — |

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | Maintainer decisions | **Done** (2026-10-16, incl. the ADO amendment and the Story regrouping). |
| P2 | A Jira project (dev instance or the test Cloud site) with a non-Blocks link between Features | Check before slice 01's dogfood; creating a "Dependency" link on a few Features is an admin step. |
| P3 | Jira link-type endpoint on Data Center | Shipped since 6028; DC verification still owed per ADR-193. Not blocking. |
| P4 | DESIGN: loading and could-not-load looks for the connection's defaults group and the slice 07 list | Owed at DESIGN (house rule). |
| P5 | DESIGN: what deleting an owner-referenced Additional Field does today (for D17 in 05b) | Owed at DESIGN. |
| P6 | `docs/ci-learnings.md` pre-applied: zod `.nullable()` for nullable DTO fields; S107 constructor width on the connection controller; CA1859/CA1861; `CreateMigration`, migration DLL build order, `HistoricalSchemaPatch` if a patched table gains columns | DELIVER. |
| P7 | Each Story finished before the next starts (D19) | Ordering rule. |
| P8 | ADO: relation-type listing reachable with the PAT scope customers grant (`vso.work`); an ADO project on the dev organisation with a non-Predecessor relation between Features | Slice 03's hypothesis; the list returned is recorded in its brief. |
| P9 | **Gates G-R1 and G-07** answered by the maintainer | **Done** (2026-10-16): G-R1 option (b); G-07 `SystemAdmin` is enough. |
| P10 | ServiceNow row of the D21 table | DESIGN verifies. |

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
- **R2 — D10's accepted consequence.** An owner that was None on purpose follows the first default. The save notice
  is the mitigation; slice 05a's dogfood counts how many owners on the dev instance are empty today.
- **R3 — #6312's value on Services.** If Services exposes only system relation types, #6312 serves teams that
  record waits as Related / Affects, and on-premises Server users with custom types. Slice 03 records what the dev
  organisation offers; #6312 ships either way.

---

## Wave: DISCUSS / [REF] DISCUSS Checklist (no silent N/A)

- **RBAC impact — yes, no new permission (D14).** Defaults through the connection PUT (`SystemAdmin`); overrides
  through Team / Portfolio PUT (`TeamWrite` / `PortfolioWrite`). The followed value's name travels on the owner's
  own settings DTO. Slice 07's bulk follow writes several owners from the connection page under `SystemAdmin`, and
  `SystemAdmin` is enough — no per-owner check, no new permission (G-07, decided). #6300 / #6312 add no route or
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
  save notice, the follow action, deletion refusal); `docs/settings/systeminfo.md` (Refresh History: the dependency
  warnings); `docs/concepts/worktrackingsystems/jira.md` (:162-175, :266-271);
  `docs/concepts/worktrackingsystems/azuredevops.md` (:101-114, :203-207); `docs/features/features.md:89`;
  `docs/concepts/howlighthouseforecasts.md:322`; release notes vNext (crediting Steve Pereira, D20); @screenshots of
  the connection page and the Team / Portfolio sections, one per theme.
- **Usage data** — DEVOPS's question; a candidate is a name-only event when a connection default is saved, which
  would also be the only way `OUT-6303-followers-share` is ever seen beyond one instance.
- **Terminology** — L6.
- **UI sketches** — at the start of DISTILL: the connection's defaults group (likely `InputGroup` +
  `FormSelectField`), the save notice (with the 6307 updating count folded in), the owner dropdown entry, the
  slice 07 list (`DataGridBase` or a plain list), and the Refresh History wording for the dependency warnings.
  #6300 / #6312 add no UI beyond copy under the Dependency Field and the Refresh History message.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Problem in domain language | PASS | Every user story opens from a named admin's pain or carries its own Before line (US-01 … US-11) |
| 2 | Persona specific | PASS | A "Who" line on every user story, US-01 … US-11 |
| 3 | ≥ 3 domain examples with real data | PASS | US-01 4, US-02 3, US-03 5, US-04 5, US-05 3, US-06 3, US-07 … US-11 3 each |
| 4 | UAT G/W/T, 3–7 | PASS | US-01 4, US-02 3, US-03 5, US-04 7, US-05 3, US-06 3, US-07 … US-11 3 each |
| 5 | AC from UAT | PASS | Each AC list restates its scenarios as observable outcomes; code references moved to Technical notes (M5) |
| 6 | Right-sized | PASS | Four ADO Stories of ~1.5–3d (D19); 13 slices ≤ 1 day; 05 split decided (D22) |
| 7 | Technical notes | PASS | Feature-wide: S1–S23, C1–C5, L1–L8, D21 table. Per story: a **Technical notes** paragraph on every user story, US-01 … US-11, naming the code refs, the resolver (L3) and its slice brief |
| 8 | Dependencies tracked | PASS | P1–P10; gates G-R1 and G-07 decided by the maintainer (2026-10-16); only P10 (ServiceNow row) and P4/P5 remain as DESIGN work, each owned |
| 9 | Outcome KPIs | PASS | Six KPIs, each with a numeric target and baseline |

Every user story carries a literal `job_id`; none uses the infrastructure escape. Every user story has its own
Elevator Pitch (Before / After / Decision enabled). Both DESIGN gates are closed: G-R1 (b) and G-07 (`SystemAdmin`
is enough).
**Verdict: READY for DESIGN.**

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

- **Q1** — Should the save notice mark which followers follow only because they were empty at the upgrade? It would
  need a stored marker. Not built unless asked.
- ~~G-R1, G-07~~ — **decided** by the maintainer on 2026-10-16 (G-R1 option (b), D18 amended; G-07 `SystemAdmin`
  is enough, D14). Nothing open on them.
- **ServiceNow** in the D21 table — DESIGN verifies (P10).
- **#6313 estimate** — ~3d by the briefs against ~2.5d in ADO (Scope Assessment).

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions
D1–D22 (maintainer, 2026-10-16), gates G-R1 and G-07, and L1–L8. **Changed since pass 1**: D1 (the Dependency
Field accepts any Additional Field, resolved per tracker); D6 (ADO back in, as its own Story #6312); D7 (five
settings); D10 (empty owners follow at the upgrade); D12 (save notice); D19 (four Stories); D22 (05a / 05b). Pass 3
review fixes: per-story Elevator Pitches, examples, scenarios and Who lines for US-07 … US-11; Refresh History as
the findings surface (D5a); observable ACs; AC-3.9; AC-4.10; D21; numeric KPI target.

### Requirements summary
#6300 / #6312: a Portfolio's Dependency Field may name a link type as well as a field; each tracker resolves it the
way Parent Override resolves on Jira — Jira by phrase, ADO by relation reference name and end; direction comes from
the named end; a two-way name and a link nobody carries both say so on Refresh History. #6304: the connection
carries a default for the Teams' Parent Override Field; owners follow or own; empty owners follow from the upgrade;
every default save shows a save notice naming who follows; changes reach the followers through the existing
fingerprint and queue; hand-configured owners can follow in one step. #6313: the same for four more settings.

### Constraints established
- `null` keeps meaning None; following is a mode column (D9, L2).
- One resolver for the effective value (L3); fingerprints unchanged across the upgrade (AC-4.4).
- Field lookup first everywhere, so no configured reference changes meaning (D1).
- No extra Jira request (AC-1.6); ADO at most one relation-type listing per connection (AC-3.7); the ADO
  relations-skip trap closed (AC-3.5); no silent interim on ADO (AC-3.9).

### Upstream changes
DISCOVER and DIVERGE were **skipped by explicit maintainer decision**. Nothing upstream to contradict. SSOT written
2026-10-16 and updated for the Story regrouping: three jobs in `docs/product/jobs.yaml`; `config-admin.yaml` (jobs,
frustrations, vocabulary); journey `epic-6303-configure-once-per-connection.yaml`; REVISION notes on
`epic-4365-dependencies.yaml` D4 and `parent-from-issue-links.yaml` D7.
