<!-- markdownlint-disable MD024 -->
# Feature Delta: epic-6577-system-wide-access

> ADO Epic #6577, Stories 6578 to 6582 with Bug 6576 as a child, replacing Story 6562, "Viewer access for all Teams and
> Portfolios through a system-level group". The `Release Notes` tag sits on the Epic. Raised at a customer workshop on 2026-11-06: they want everyone to be able to view all
> Teams without granting access Team by Team. A user with no Team-level role sees no Teams, so they made their Head of
> Engineering and CTO System Admins just to see data. They asked for "default viewer access at the very least", view
> rights from the first login. Everyone there already has Jira access, so visibility is no concern for them. Today the
> customer's Lighthouse admin maps a Jira users group per Portfolio, which does not scale with many Teams. It must stay
> opt-in: other customers want Teams hidden from each other. Builds on Story 4965 (SSO groups mapped to roles). Related
> to Epic 6473 (Settings split, Planned).
>
> Waves so far: DISCUSS and DESIGN, lean density. DISCOVER and DIVERGE were skipped by the maintainer's explicit word. ADR-270, ADR-271, ARCHITECTURE.md and `docs/settings/rbac.md` are not edited here; what they
> owe is listed under the context map and the house checklist.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ DISCOVER: none, skipped by the maintainer. Evidence: one customer workshop, paraphrased in the ADO text.
- ⊘ DIVERGE: none, skipped by the maintainer. The mechanism was decided directly (D1). Risk: one customer's need; the
  opposite need (Teams hidden from each other) is the reason everything here is opt-in.
- ⊘ `docs/product/vision.md`, `docs/project-brief.md`: not found.
- ✓ `docs/product/jobs.yaml`: the four RBAC jobs and the file's tail. New jobs appended; none contradicted.
- ✓ `docs/product/journeys/rbac-enhancements.yaml`: personas Alex Chen (System Admin), Jordan Reyes (Team Admin), Morgan
  Davies (Viewer); the "no-access" alert decision (Q7).
- ✓ `docs/product/personas/config-admin.yaml`. No viewer persona existed although `jobs.yaml` names one; added.
- ✓ ADR-270 (contexts, Access row, glossary, Decision 4), ADR-271 (Access surface), `ARCHITECTURE.md` §4 and its
  authorization section, `docs/settings/rbac.md`, the Epic 6455 delta (Access mentions).
- ✓ Code: `RbacAdministrationService.cs`, `AuthorizationController.cs`, `ApiKeyController.cs`, `TeamController.cs`,
  `PortfolioController.cs`, `RefinementVotesController.cs`, `CurrentUserProfileService.cs`, `RbacSettings.tsx`,
  `ScopedGroupMappingManager.tsx`, `TeamDetail.tsx`, `PortfolioDetail.tsx`, `OverviewDashboard.tsx`, `useRbac.ts`,
  `RoleBasedAccessControl.spec.ts`, `examples/keycloak/realm-export.json`.

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

Paths under `Lighthouse.Backend/Lighthouse.Backend/` and `Lighthouse.Frontend/src/`.

| Surface | Today | Why it matters here |
|---|---|---|
| `Models/Authorization/RbacGroupMapping.cs` | GroupValue, Role, ScopeType (System, Team, Portfolio), nullable ScopeId | The mapping can already say "System"; only System Admin may use it |
| `IsValidGroupMappingScope` (`RbacAdministrationService.cs:1289`) | System scope for System Admin only; Viewer needs a Team or Portfolio id | Refuses the rule this Story needs |
| `HasTeamReadPermission` and its three twins (L1257-1287) | Look up the exact unit key | No "all units" concept |
| `GetOwnerEffectivePermissionsAsync` (L1053) | Merges group and explicit roles; an explicit role replaces a group role on the same key. Pinned by `CanWriteTeamAsync_ExplicitViewerOverridesVirtualTeamAdmin_ReturnsFalse` | Precedence question P5 |
| `GetVirtualPermissionsAsync` (L1146) | Nothing without group values; API key and embed principals fall back to `LastKnownGroupClaimValues` | "Everyone signed in" cannot ride on it; a key keeps a group until its owner next signs in |
| `IntersectWithApiKeyScope`, `OwnerCoversScope` (L1097-1144) | Exact key only | A limited key under a rule-only owner would read nothing |
| `ApiKeyController.cs:50-71` | A limited key is allowed if the caller can read or write that unit | After slice 01 the key is created, then reads nothing, until slice 02 |
| `GetAuthorizationSummaryAsync` (L418), `CanSatisfyAnyScopedAdminAsync` (L385) | Exact keys feed `adminTeamIds`, `adminPortfolioIds`, and the guard for "any scoped admin" | Slice 05 |
| `RbacGuardRequirement.TeamContribute` (L367) | Any reader may vote and comment on Refinement | A Viewer is not strictly read-only: P4 |
| `TeamController.cs:93`, `PortfolioController.cs:69` | `DELETE` is guarded by `CanDeleteUnit`: the enforcement gate, then System Admin (Bug 6575, shipped). The UI hides it too (`OverviewDashboard.tsx:572,582`) | Admin on all stays out, because it never makes `CanManageRbacAsync` true |
| `AuthorizationController.cs:366,411,452` | Creating, changing, removing a mapping needs System Admin. Reading a unit's mappings needs membership rights | A Team Admin's Access tab shows an Add Group Mapping form whose save is always refused: P10 |
| `AuthorizationController.cs:318` | `GET group-mappings` is System Admin only | The Access tab line needs its own read for Team and Portfolio Admins |
| `GetUsersAsync`, `GetUnassignedUserCountAsync` | "Unassigned" means no explicit row | A person reached by a rule still reads as Unassigned: P8 |
| `IsEnforcementGateSatisfiedAsync` (L1340) | Needs Premium and an explicit System Admin row, else every read is refused | System-wide rules inherit it |
| `pages/Settings/Rbac/RbacSettings.tsx:60-65,133-138,349` | Lists and creates only System plus System Admin mappings; heading "System Admin SSO Groups" | The surface to widen |
| `components/Common/Authorization/ScopedGroupMappingManager.tsx` | Group value, Role select, table, Remove | The form to reuse |
| `TeamDetail.tsx:148-149`, `PortfolioDetail.tsx:119-120` | The Access tab shows only to admins of that unit | A Viewer never sees the D4 line |
| `OverviewDashboard.tsx:428-432` | "You currently do not have access to any teams or portfolios." | What the customer's leaders see today |

**Documentation drift, confirmed in code.** `docs/settings/rbac.md` L201-205 describes Settings → Access → Group
Mappings with a Role and scope choice. The page has no such tab or choice: `RbacSettings.tsx` hard-codes `SystemAdmin`
and `System` and offers only a group value and Add Mapping, matching `docs/assets/settings/rbac_groupmapping.png`.
Scoped mappings are made on the unit's Access tab. L184-190, L244 and L254-255 describe an Edit icon and a role picker that
Settings → Access does not have. L33 says the higher role wins, while the code lets an explicit role replace a group
role. All of it is owed at finalize.

## Wave: DISCUSS / [REF] Persona IDs

- `viewer` (new file): Noor Haddad, Head of Engineering, 42 Teams and 9 Portfolios in view, no role on any; Morgan Davies,
  Product Manager, from `rbac-enhancements.yaml`.
- `config-admin` as System Admin: Alex Chen, owns the instance's access rules.
- `config-admin` as Team Admin: Jordan Reyes, Team Admin of Team Zenith.
- `config-admin` as admin on all (slice 05): Ines Moreau, flow lead who keeps every Team's settings in order, and should
  not run the instance.

## Wave: DISCUSS / [REF] JTBD One-Liner

When I lead people across many Teams, I want to see every Team and Portfolio from my first login without anyone
granting them one by one, so I can read the numbers without being handed the keys to the system.

| Job (new in `jobs.yaml`) | Persona | Importance / satisfaction / gap | Stories |
|---|---|---|---|
| `job-viewer-see-every-unit-from-first-login` | viewer | 5 / 1 / 4 | US-01, US-02, US-04 |
| `job-rbac-grant-access-once-for-all-units` | config-admin | 4 / 1 / 3 | carried by US-01, US-04, US-05 |
| `job-rbac-see-every-route-to-a-unit` | config-admin | 3 / 2 / 1 | US-03 |
| `job-rbac-look-after-all-units-without-running-the-system` | config-admin | 3 / 1 / 2 | US-05 |

Scores are desk estimates from one workshop. Forces per job are in `jobs.yaml`.

## Wave: DISCUSS / [REF] Locked Decisions

Maintainer decisions, not reopened here.

- **D1 Mechanism: both, group first.** A system-level group mapping, then an opt-in "everyone signed in" switch. Each
  ships alone.
- **D2 Scope: one generic scope.** All Teams and Portfolios, including ones created later. No separate choice.
- **D3 Role: Viewer and Admin are both in this Story.** Admin on all is a later slice of it.
- **D4 Visibility.** A Team's or Portfolio's Access tab shows a read-only line that the unit is also reachable through a
  system-wide rule, naming the group or "everyone signed in". Only System Admins change those rules, in Settings → Access.
- **C1 Design constraint (maintainer, for DESIGN).** A rule has a distinct "applies to" part, and "all Teams and
  Portfolios" is its first value, so another value could be added later without changing the read check or the Access
  tab line. The UI column is named "Applies to" for that reason. The pattern itself is not designed.

Wave settings: cross-cutting feature; brownfield, walking skeleton evaluated (below); lightweight UX research; JTBD on.

## Wave: DISCUSS / [REF] Proposals and Decisions

Decided by the maintainer, except P8 and P12, which wait for the sketch review, and P15, which DESIGN answered. **Blocks**
names the slice that could not start without an answer.

| # | Item | Proposal | Blocks |
|---|---|---|---|
| P1 | What "Admin on all" grants | DECIDED: Team Admin plus Portfolio Admin on every unit. See the capability table | Slice 05 |
| P2 | Can the everyone switch grant Admin? | DECIDED: Viewer only, never Admin | Slice 04 |
| P3 | Default state | DECIDED: off, so existing instances change nothing | Slice 01 |
| P4 | A Viewer may vote and comment on Refinement today (`TeamContribute`). Through a rule that is every Team, and with the switch everyone signed in | DECIDED: keep one meaning of Viewer; say it in the confirm dialog and docs. | Slice 01 |
| P5 | Does a role on a unit narrow Admin on all there? The code lets an explicit role replace a group role on the same unit | DECIDED: a system-wide rule only adds, the higher role wins, and the Access line stays true. | Slice 05 |
| P6 | Does the switch reach API keys and embed sessions? | DECIDED: yes, a key can do what its owner can. A key of a removed person has no profile and reads nothing | Slice 04 |
| P7 | A removed user who signs in again gets a new profile, and with the switch on sees everything | DECIDED: accept; docs say to block people in the identity provider | Slice 04 |
| P8 | People reached only by a rule show as "Unassigned: Yes", which already holds for group-mapped people | PROPOSED: leave the column; decide the wording at the sketch review, since with the switch on it misleads | No |
| P9 | A system-wide role for one named person, without a group | DECIDED: out of scope; a group of one, or the switch, covers it | No |
| P10 | Existing defect: the Access tab offers Team Admins a mapping form whose save is always refused; `job-rbac-scoped-admin` claims they can | DECIDED: Bug 6576, a child of Epic 6577, done alongside slice 03 on the same tab. Add and remove are hidden from everyone but System Admins, and the list stays readable. Admin on all would otherwise meet the refused form on every unit. `job-rbac-scoped-admin` and rbac.md are corrected to match | Slice 03 |
| P11 | Someone leaves the identity provider group: their browser session loses the rule when the token refreshes or at next sign-in, their API key only at next sign-in. Removing the rule works at once | DECIDED: accept and document; existing behaviour, wider reach | Slice 02 |
| P12 | The word on screen: D4 says "system-wide rule", ADR-270 says "organisation-wide roles", and ADR-271 has separate areas named Organisation and System | OPEN: settle at the sketch review; this document uses "system-wide rule" | No |
| P13 | A key limited to "Viewer on all" | DECIDED: out of scope; keys stay per unit or unlimited | No |
| P14 | **`DELETE` on a Team or Portfolio was open to its admin through the API, against rbac.md and against P1** | DECIDED and shipped: Bug 6575 guards both as System Admin, ahead of this work, which also closed it for today's Team and Portfolio Admins | Slice 05 |
| P15 | Log a line naming who changed a system-wide rule? Mapping changes log nothing today | DECIDED at DESIGN: yes, see DDD-14 | No |

## Wave: DISCUSS / [REF] Capability table for P1

DECIDED. Rows are the matrix of `docs/settings/rbac.md` L55-80; the last rows are not in that matrix but exist in code.

| Capability | Viewer on all | Admin on all | Stays with System Admin? |
|---|---|---|---|
| Create a new Team | No | No | Yes |
| Create a new Portfolio | No | No | Yes |
| Clone a Team or Portfolio | No | No | Yes |
| Delete a Team or Portfolio | No | No (Bug 6575) | Yes |
| See who the licence belongs to | No | No | Yes |
| Edit settings on a Team or Portfolio | No | Yes, every unit | |
| Read a Team or Portfolio | Yes, every unit | Yes, every unit | |
| Create, edit, delete reports | No | Yes, every unit | |
| Read reports | Yes | Yes | |
| Export a report as a PDF | Yes | Yes | |
| See a report on the Reports page | Yes | Yes | |
| Create reports for several Teams and Portfolios | No | Yes | |
| Remove several reports | No | Yes | |
| Export several reports as one PDF | Yes | Yes | |
| Manage users and group mappings | No | No: instance users and all mappings stay out. Members of a unit: yes, on its Access tab, as a Team Admin can today | Yes |
| Manage work tracking system connections | No | No | Yes |
| Read connection names and the fields settings need | No | Yes | |
| Install a Lighthouse update | No | No | Yes |
| Update all Teams (`POST teams/update-all`) | No | No | Yes |
| Update one Team's or Portfolio's data | No | Yes, every unit | |
| Vote and comment on Refinement | Yes (P4) | Yes | |
| Change system-wide rules or the switch | No | No | Yes |

## Wave: DISCUSS / [REF] Work items

Story 6562 outgrew one Story, so on 2026-11-07 the maintainer replaced it with Epic 6577, one Story per slice. The Release
Notes tag sits on the Epic only.

| Slice | Story |
|---|---|
| 01 One group reads every Team and Portfolio | 6578 |
| 02 Limited API keys follow their owner, ships with 01 | 6579 |
| 03 The Access tab says who else reaches a unit | 6580 |
| 04 Everyone signed in can view, if we say so | 6581 |
| 05 One group administers every unit | 6582 |

Related: Bug 6575 closed the open `DELETE` and has shipped. Bug 6576, a child of this Epic, hides the refused mapping form from scoped admins, alongside slice 03.

## Wave: DISCUSS / [REF] Context-Map Placement (ADR-270)

- **Contexts touched.** Access is the only one that changes. It "follows the units that exist": the rule names no unit,
  so Organisation set-up is read, not changed. Licensing gates it as today, unchanged. Reports and Refinement see more
  readers and no change of their own.
- **New terms, after the glossary.** *System-wide rule*: a role on every unit that a rule's "applies to" covers, held
  through a group or through everyone signed in. *Everyone signed in*: every person with a stable subject and a profile.
  *Viewer on all*, *Admin on all*: the two roles a rule can give. *Applies to*: the part of a rule that says which units
  it reaches. The glossary's **Member** stays "a person with a role on one unit": someone reached by a rule is not a
  Member, which is why the Access tab line sits apart from the member list.
- **New concepts.** The system-wide rule and the everyone switch. ADR-270's "broad readers" is this concept made real.
- **Domain events.** None published, none reacted to, because permissions are computed on each request and nothing
  downstream acts on them. A Team created later needs no reaction either: the rule names no unit, so there is nothing to
  backfill. `TeamDeleted` needs no handler for the same reason.
- **Owed in this feature, not edited now.** ADR-270: Access row (name Admin on all beside broad readers; drop "target"
  from what ships), Access glossary rows for the terms above. ARCHITECTURE.md §4 and the authorization bullet at L289: one
  sentence that a role can reach every unit through a system-wide rule. ADR-271 L57: add system-wide rules to the Access
  surface. No new context or edge.

## Wave: DISCUSS / [REF] UI Surface and Sketches (ADR-271)

Target surface: **Access**. Today that is Settings → Access, and the unit's Access tab for the line. Epic 6473 moves the
page unchanged, so it is no dependency. One step closer to the target: the section stops being about System Admin only.
These sketches are reviewed with the maintainer at the start of DISTILL. Team and Portfolio render through Terminology.

**Settings → Access** (slices 01, 04, 05). Reuses the layout of `ScopedGroupMappingManager` (text field, Role select, table,
Remove) in place of the bare form, and the existing `Dialog` of `RbacSettings`. `Scope` and `Scope ID` become one column.

```text
Settings > Access
  > RBAC Status                                           (unchanged)
  System Admins                                           (unchanged)

  SSO Groups                                was: "System Admin SSO Groups"
  +----------------------------------------------------------------------+
  | [Group value        ] [Role v] [Add Mapping]                         |
  |                        System Admin                                  |
  |                        Viewer on all          (slice 01)             |
  |                        Admin on all           (slice 05)             |
  |                                                                      |
  | Group                Role           Applies to                       |
  | lh-system-admins     System Admin   Everything              [Remove] |
  | lighthouse-readers   Viewer         All Teams and Portfolios [Remove]|
  | delivery-leads       Admin          All Teams and Portfolios [Remove]|
  +----------------------------------------------------------------------+
  Group values are case-sensitive; copy them from your identity provider.

  Everyone signed in                                      (slice 04)
  +----------------------------------------------------------------------+
  | ( ) Everyone signed in can view all Teams and Portfolios             |
  |     Off: people see only what they are given.                        |
  +----------------------------------------------------------------------+

  Confirm dialog when turning it on:
  +----------------------------------------------------------------------+
  | Let everyone signed in view all Teams and Portfolios?                |
  | Everyone who can sign in, now and later, can read every Team and     |
  | Portfolio, and can vote and comment on Refinement.                   |
  |                                     [Cancel] [Turn on]               |
  +----------------------------------------------------------------------+
```

**A Team's or Portfolio's Access tab** (slices 03, 04, 05). A read-only MUI `Alert` (severity info) above the unchanged
`ScopedMembershipManager` and `ScopedGroupMappingManager`. No rules, no Alert: the tab looks as it does today.

```text
Team Zenith > Access
  +----------------------------------------------------------------------+
  | (i) Also reachable through system-wide rules                         |
  |     Viewer: group lighthouse-readers                                 |
  |     Admin: group delivery-leads                                      |
  |     Viewer: everyone signed in                                       |
  |     Removing someone below does not take away what these rules give. |
  |     Only a System Admin changes them, in Settings > Access.          |
  +----------------------------------------------------------------------+
  Team Access          (unchanged)
  Team Group Access    (unchanged)
```

**Overview for Noor Haddad** is unchanged as a page: her list fills, and the no-access alert goes.

**Views that fetch data (loading and failure looks are a DESIGN item):** the mappings table and the switch on Settings →
Access; the Access tab line. The line must not look like "no rules" while loading or after a failed load, since that would
tell a Team Admin the unit is private when it is not. The Overview's lists are unchanged.

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-6577-system-wide-access.yaml`. Two journeys, in brief.

```text
Alex (System Admin)                        Noor (Head of Engineering)
Anxious: "am I opening too much?"          Stuck: "no access to any Teams"
   |  Settings > Access: add group, Viewer on all
   v
Focused: table says who and what            First login: every Team and Portfolio
   |                                          |
   v                                          v
Relieved: 9 per-Portfolio mappings go       Informed: reads forecasts, no admin rights
   |
   v
Confident: Noor's System Admin is revoked, and Jordan's Access tab explains the rule
```

Shared artifacts, each with one source: the rules (`RbacGroupMappings`, read on Settings → Access and the Access tab
line); the switch state (one stored setting, read by both views and every permission check); the group value (the
identity provider's token, or the owner's stored snapshot for a key); the reach of an admin (`my-summary` through
`useRbac()`, never fetched directly); Team and Portfolio nouns (Terminology).

## Wave: DISCUSS / [REF] Scope Assessment

**PASS: 5 stories, 1 changed bounded context (Access), estimated 4.5 days.** Oversized signals: more than 10 stories, no;
more than 3 contexts, no; more than 5 integration points, no; more than 2 weeks, no; several outcomes that could ship
separately, yes (group, switch, admin). One signal, and the Story's shape already orders them. D1 and D3 keep them in one
Story, so no split is proposed.

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone: Decide the rule → Grant it → Person arrives → Person works → Others understand it → Take it back.

| Activity | Slice 01 | Slice 02 | Slice 03 | Slice 04 | Slice 05 |
|---|---|---|---|---|---|
| Decide and grant | Group as Viewer on all | | | Switch, with confirm | Group as Admin on all |
| Person arrives | Sees all at first login | | | Without any group | Sees Settings and Access tabs |
| Person works | Reads, cannot change | Limited key works | | Reads | Edits, manages members |
| Others understand | Table says it | | Access tab line | Line names everyone | Line says Admin |
| Take it back | Remove the mapping | Key follows | Line disappears | Switch off | Remove the mapping |

| Order | Slice | Size | Disproves if it fails | Depends on |
|---|---|---|---|---|
| 1 | 01 One group reads every unit | 1 day | That one rule can reach all read paths, built on exact unit keys, without touching each caller | P3, P4 |
| 2 | 02 Limited keys follow their owner | 0.5 day | That a key can follow a rule and stay no wider than its limit | 01 |
| 3 | 03 The Access tab says so | 1 day | That a read-only line is enough for a Team Admin to trust who sees their Team | 01 |
| 4 | 04 Everyone signed in can view | 1 day | That the rule's reach does not depend on groups, and opt-in holds | 01, 02 for keys, 03 for the line; P2, P6, P7 |
| 5 | 05 One group administers every unit | 1 day, top of band | That wide write can stop short of System Admin rights | 01, 02, 03 |

**Priority rationale.** Slice 01 first: it holds the riskiest assumption and is what the customer asked for, because
they already have the group. Slice 02 straight after because slice 01 leaves a trap: a limited key is created without
complaint and reads nothing. The two go out in one release; if 02 were dropped, key creation would have to refuse instead.
Slice 03 comes before the switch because a wider rule needs its explanation first. Slice 05 last: the widest power. Slices 04 and 05 are independent of each other, and either can be dropped.

**Taste tests.** No slice ships 4 or more new components. The shared seam, a rule that reaches every unit, ships inside
slice 01 rather than as an abstraction of its own. Every slice can disprove something. Every slice runs on the real
Keycloak realm and demo data, not synthetic data. No two differ only in scale. No slice is `@infrastructure`.

## Wave: DISCUSS / [REF] User Stories

System constraints (all stories): every rule is opt-in, and without one nothing changes for anyone. Rules work only while
RBAC is enforced: with RBAC off everyone has full access as today; before the first System Admin exists, or with Premium
lapsed, reads are refused as today. A rule names no unit, and its "applies to" part is a distinct concept (C1). A person with
no stable subject gets nothing. All logic flows through `IRbacAdministrationService`, and all UI gating through `useRbac()`.
Migrations are expand-only.

### US-01: Everyone in one group can read every Team and Portfolio (slice 01)

As Alex Chen, System Admin of an instance with 42 Teams and 9 Portfolios, I want to give one SSO group Viewer on all, so
Noor Haddad sees every Team from her first login and I stop mapping a group per Portfolio.

`job_id: job-viewer-see-every-unit-from-first-login`

#### Elevator Pitch

Before: Noor signs in and the Overview says "You currently do not have access to any teams or portfolios", so she was
made a System Admin just to read forecasts.
After: Settings → Access → SSO Groups → enter `lighthouse-readers`, pick Viewer on all, Add Mapping → the table shows
`lighthouse-readers | Viewer | All Teams and Portfolios`, and Noor's Overview lists all 42 Teams and 9 Portfolios.
Decision enabled: Alex revokes Noor's System Admin and removes the 9 per-Portfolio mappings.

#### Domain examples

1. **Happy path.** Noor is in `lighthouse-readers` and holds no role. She opens Team Zenith's forecast and Project
   Apollo's Features.
2. **A Team created later.** Alex creates Team Meridian on Monday. Noor sees it on her next page load; nobody grants it.
3. **Outside the group.** Sam Weber is in `finance-staff` only. His Overview still shows the no-access alert.
4. **Typing error.** Alex enters `Lighthouse-Readers`. Group values are case-sensitive, so nobody matches, and Noor sees
   nothing; the hint under the form says so.

#### UAT scenarios

```gherkin
Scenario: A group member sees every Team and Portfolio at first login
  Given Alex maps the group "lighthouse-readers" as Viewer on all
  And Noor is in that group and holds no role
  When Noor signs in for the first time
  Then her Overview lists all 42 Teams and 9 Portfolios
  And she can open Team Zenith's forecast

Scenario: A Team created later is readable without a further grant
  Given the rule exists and Noor is in the group
  When Alex creates Team Meridian
  Then Noor can open Team Meridian on her next page load

Scenario: Someone outside the group sees nothing new
  Given the rule exists and Sam Weber is not in the group
  When Sam signs in
  Then he sees the no-access alert

Scenario: A reader through the rule cannot change anything
  Given Noor reads through the rule
  When she opens Team Zenith
  Then she sees no Settings tab, no Access tab and no write controls
  And a direct request to change Team Zenith's settings is refused

Scenario: Removing the rule takes the access away
  Given Noor is reading Team Zenith through the rule
  When Alex removes the mapping
  Then on her next page load she sees the no-access alert

Scenario: An instance without a rule behaves as before
  Given no system-wide rule exists
  When anyone signs in
  Then they see exactly the Teams and Portfolios they saw before the upgrade
```

#### Acceptance criteria

- AC-1.1 A System Admin can add and remove a group as Viewer on all on Settings → Access; the table shows the group, the
  role and "All Teams and Portfolios".
- AC-1.2 A member of that group reads every Team and Portfolio, with no role on any, from first sign-in.
- AC-1.3 A unit created after the rule is covered with no further action.
- AC-1.4 A person outside the group gains nothing; the Overview no-access alert stays.
- AC-1.5 Through the rule alone, the write rows of the capability table are refused by the server and hidden in the UI.
  Refinement votes and comments follow P4.
- AC-1.6 Removing the rule ends the reach on the member's next request.
- AC-1.7 A Team Admin or Viewer cannot create the rule: the server refuses them.
- AC-1.8 With no rule, every read answers as before the upgrade.

Technical notes: existing System Admin mappings keep working, and `RbacSettings.tsx` must stop filtering out the new rows. An
unlimited API key follows its owner through the stored snapshot with no change. Reading all units must not cost a
lookup per unit (NFR-1). E2E: one new step in `RoleBasedAccessControl.spec.ts` with `team-readers` and `teamreader@user.com`;
`portfolioreader@user.com` is the outsider. Do not reuse the realm's `system-admins` group.

KPI: K1, K2.

### US-02: A limited API key keeps working when its owner reads through a rule (slice 02)

As Noor, who reads through `lighthouse-readers` and runs the CLI for one Team's numbers, I want a key limited to Team
Zenith to do just that, so my script works and cannot reach further than I said.

`job_id: job-viewer-see-every-unit-from-first-login`

#### Elevator Pitch

Before: Lighthouse accepts Noor's key limited to Viewer on Team Zenith, and then every call with it answers as if she had
no access.
After: Settings → API Keys → create a key with scope Viewer on Team Zenith → `GET /api/latest/teams` with that key
returns Team Zenith only, and `GET /api/latest/teams/{id}` for any other Team is refused.
Decision enabled: Noor gives the key to her reporting script instead of asking for a System Admin's key.

#### Domain examples

1. **Happy path.** Key "zenith-report", Viewer on Team Zenith: reads Team Zenith, no other Team.
2. **Unlimited key.** Noor's key without scopes reads all 42 Teams and 9 Portfolios, as she can.
3. **Asking for more.** Noor asks for a key as Team Admin on Team Zenith: refused with "Cannot issue API key with broader
   scope than your own permissions."
4. **Left the group.** Noor leaves `lighthouse-readers` in the identity provider. Her key keeps reading until she next
   signs in (P11). When Alex removes the rule, it stops at once.

#### UAT scenarios

```gherkin
Scenario: A limited key reads its Team and nothing else
  Given Noor reads through the rule and holds the key "zenith-report", Viewer on Team Zenith
  When the key asks for all Teams
  Then only Team Zenith comes back
  And a request for Team Meridian is refused

Scenario: An unlimited key reads what its owner reads
  Given Noor holds a key without scopes
  When the key asks for all Teams
  Then all 42 Teams come back

Scenario: A key cannot claim more than its owner has
  Given Noor reads through the rule
  When she asks for a key as Team Admin on Team Zenith
  Then the request is refused

Scenario: Removing the rule stops the key
  Given the key "zenith-report" reads Team Zenith
  When Alex removes the rule
  Then the key's next request reads nothing
```

#### Acceptance criteria

- AC-2.1 A key limited to a unit reads that unit when its owner reaches it through a rule, and no other unit.
- AC-2.2 A key never exceeds the lower of its limit and its owner's role.
- AC-2.3 A key without scopes reads what its owner reads.
- AC-2.4 Asking for a role above the owner's reach is refused with the existing message.
- AC-2.5 Removing the rule ends the key's reach on its next request.
- AC-2.6 A limited key keeps its exact scope: it does not widen to all units.

Technical notes: `OwnerCoversScope` is the exact-key consumer. P11's staleness is documented, not fixed. Slice 05 extends the
coverage to Admin on all.

KPI: guardrail G1.

### US-03: A unit's Access tab says who else reaches it (slice 03)

As Jordan Reyes, Team Admin of Team Zenith, I want the Access tab to tell me when a system-wide rule also reaches my Team,
so I do not remove someone, watch them keep reading, and think Lighthouse is broken.

`job_id: job-rbac-see-every-route-to-a-unit`

#### Elevator Pitch

Before: Jordan removes Noor from Team Access, and Noor still reads Team Zenith; nothing on the page says why.
After: Team Zenith → Access → an information box above Team Access reads "Also reachable through system-wide rules.
Viewer: group lighthouse-readers", and that only a System Admin changes it, in Settings → Access.
Decision enabled: Jordan stops chasing the member list and asks Alex whether the rule should stay.

#### Domain examples

1. **Happy path.** One group rule: the box names `lighthouse-readers` and Viewer.
2. **No rule.** On an instance with no rule, the tab looks as it does today.
3. **Portfolio.** Maria Santos, Portfolio Admin of Project Apollo, sees the same box there.
4. **Viewer.** Noor has no Access tab, so she sees no box.

#### UAT scenarios

```gherkin
Scenario: A Team Admin sees that a rule also reaches the Team
  Given the group "lighthouse-readers" is Viewer on all
  When Jordan opens Team Zenith's Access tab
  Then the box names the group and the role Viewer
  And it says only a System Admin changes it, in Settings > Access

Scenario: A Portfolio Admin sees the same on a Portfolio
  Given the rule exists
  When Maria opens Project Apollo's Access tab
  Then the box names the group and the role Viewer

Scenario: With no rule the tab is unchanged
  Given no system-wide rule exists
  When Jordan opens the Access tab
  Then no box is shown

Scenario: The box changes nothing
  Given the box is shown
  Then it offers no control to change or remove the rule
  And removing Noor from Team Access leaves her reading through the rule

Scenario: A reader gets no view of the rules
  Given Noor reads through the rule
  When she opens Team Zenith
  Then she has no Access tab
  And a direct request for the rules behind the box is refused
```

#### Acceptance criteria

- AC-3.1 A unit's Access tab lists every system-wide rule that reaches it, by role and group name. System Admin groups are
  not rules on units, so they are not listed.
- AC-3.2 With no rule, no box shows.
- AC-3.3 The box has no control; only Settings → Access changes rules.
- AC-3.4 A Team or Portfolio Admin may read the rules for a unit they manage, and nobody else except a System Admin.
- AC-3.5 The list comes from the rule's "applies to" part, not from a hard-coded "all" (C1).

Technical notes: `GET group-mappings` is System Admin only, so this needs a read guarded like the unit's own mappings.
Group names become visible to Team and Portfolio Admins, as D4 asks.

KPI: guardrail G3.

### US-04: Everyone signed in can view every Team and Portfolio, if we say so (slice 04)

As Alex Chen, on an instance where everyone already has Jira access, I want one switch that gives every signed-in person
Viewer on all, so a new colleague can read on day one with no group to maintain.

`job_id: job-viewer-see-every-unit-from-first-login`

#### Elevator Pitch

Before: Morgan Davies joins on Monday, is in no mapped group, and sees the no-access alert until someone acts.
After: Settings → Access → Everyone signed in → turn on, confirm "Turn on" → Morgan's first sign-in lists all Teams and
Portfolios, and each unit's Access tab reads "Viewer: everyone signed in".
Decision enabled: Alex drops the group from the identity provider set-up, and stops handling requests to see a Team.

#### Domain examples

1. **Happy path.** Switch on: Morgan, no group, no role, reads Team Zenith at first sign-in.
2. **Upgrade.** A customer who keeps Teams apart upgrades: the switch is off, and nothing changes.
3. **Changing their mind.** Alex turns it off: Morgan sees the no-access alert on his next page load, and Jordan's Team
   Admin role is untouched.
4. **Removed person.** Alex removes Sam from Users. Sam's API key reads nothing. If Sam signs in again, he gets a new profile
   and reads as anyone else does (P7).

#### UAT scenarios

```gherkin
Scenario: After an upgrade the switch is off and nothing changes
  Given an instance upgraded with scoped roles only
  When Sam Weber, who holds no role, signs in
  Then he sees the no-access alert
  And Settings > Access shows the switch off

Scenario: With the switch on a newcomer reads everything at first sign-in
  Given Alex turned the switch on and confirmed
  When Morgan signs in for the first time with no group and no role
  Then his Overview lists every Team and Portfolio

Scenario: Cancelling the confirmation changes nothing
  Given the switch is off
  When Alex turns it on and chooses Cancel
  Then the switch stays off

Scenario: Turning it off ends the reach and keeps real roles
  Given the switch is on and Jordan is Team Admin of Team Zenith
  When Alex turns it off
  Then Morgan sees the no-access alert on his next page load
  And Jordan still administers Team Zenith

Scenario: The switch gives reading and never admin
  Given the switch is on
  When Morgan opens Team Zenith
  Then he sees no Settings tab, no Access tab and no write controls
  And no Admin option exists for the switch

Scenario: Only a System Admin can use the switch
  Given Jordan is a Team Admin
  When he tries to turn the switch on with a direct request
  Then the request is refused
  And his Access tab reads "Viewer: everyone signed in" when Alex turns it on

Scenario: A removed person's embedded session reads nothing
  Given the switch is on and Sam reads Team Zenith in a session embedded in another tool
  When Alex removes Sam from Users
  Then Sam's next request in that session reads nothing
  And no new profile is created for him
```

#### Acceptance criteria

- AC-4.1 The switch is off after install and upgrade; off changes nothing.
- AC-4.2 On, every signed-in person with a stable subject reads every Team and Portfolio, with no group claim configured.
- AC-4.3 Turning on asks for confirmation that names the reach; Cancel changes nothing.
- AC-4.4 Off ends the reach on the next request and leaves roles and mappings alone.
- AC-4.5 The switch grants Viewer only (P2).
- AC-4.6 Only a System Admin may change it; the control does not show before the first System Admin exists.
- AC-4.7 Each Access tab names "everyone signed in" while it is on (D4).
- AC-4.8 An API key or embed session reads as its owner, and a key whose owner has no profile reads nothing (P6).
- AC-4.9 Two System Admins changing it at once: a stale change is refused, not silently applied.
- AC-4.10 A session embedded in another tool speaks only for a person who still has a profile: after removal it reads nothing
  and creates no profile. A new ordinary sign-in stays under P7.

Technical notes: `GetVirtualPermissionsAsync` stops at no group values, so the switch cannot ride on it. Where the setting
lives is DESIGN's. Premium needs no separate check (see the house checklist). E2E: `test@user.com` has no groups, so it is the
newcomer; turn the switch off again at the end of the step, since the spec shares one instance.

KPI: K3, G1, G2.

### US-05: One group looks after every Team and Portfolio without running the system (slice 05)

As Ines Moreau, flow lead, I want to edit settings and members on every Team and Portfolio, including new ones, so I can keep
them in order without a System Admin role and without being given each unit.

`job_id: job-rbac-look-after-all-units-without-running-the-system`

#### Elevator Pitch

Before: Ines needs Team Admin on 42 Teams one at a time, or System Admin, which also hands her connections and licence
details.
After: Alex picks Admin on all for `delivery-leads` on Settings → Access → Ines opens Team Zenith and sees the Settings and
Access tabs, saves a change, and has no Add Team button, no Delete and no Connections.
Decision enabled: Alex revokes System Admin from people who only ever needed to edit Teams.

#### Domain examples

1. **Happy path.** Ines changes Team Zenith's SLE and Project Apollo's settings, and adds Morgan as Viewer on Team Zenith.
2. **A Team created later.** Alex creates Team Meridian. Ines edits it on her next page load.
3. **System Admin things.** Ines sends `DELETE` for Team Zenith directly: refused. Settings → Access is not offered.
4. **Narrowing.** Ines also holds an explicit Viewer on Team Zenith. Under P5 she stays Admin there.

#### UAT scenarios

```gherkin
Scenario: An admin through the rule edits any Team and Portfolio
  Given Alex maps the group "delivery-leads" as Admin on all
  And Ines is in that group with no other role
  When she changes Team Zenith's SLE and saves
  Then the change is stored
  And she can do the same on Project Apollo

Scenario: A Team created later can be edited without a further grant
  Given the rule exists
  When Alex creates Team Meridian
  Then Ines sees its Settings and Access tabs on her next page load

Scenario: An admin through the rule manages a unit's members
  Given the rule exists
  When Ines adds Morgan as Viewer on Team Zenith
  Then Morgan reads Team Zenith

Scenario: An admin through the rule cannot do System Admin things
  Given Ines holds Admin on all and nothing else
  Then she has no Add Team, Add Portfolio, Clone or Delete
  And she has no Settings > Access, no connection editing and no licence details
  And direct requests to create, delete, change a rule or update all Teams are refused

Scenario: Removing the rule takes the admin rights away
  Given Ines edits Team Zenith through the rule
  When Alex removes the mapping
  Then on her next page load she has no Settings tab and sees the no-access alert

Scenario: The everyone switch cannot make people admins
  Given the switch is on
  Then no way exists to give everyone signed in Admin on all
```

#### Acceptance criteria

- AC-5.1 A System Admin can add a group as Admin on all; it covers every unit, including later ones.
- AC-5.2 Every row of the capability table holds for Admin on all, server-side and in the UI.
- AC-5.3 `DELETE` on a Team and on a Portfolio is refused for Admin on all, as Bug 6575 refuses it for scoped admins.
- AC-5.4 A rule-holder sees the Settings and Access tabs of every unit, through `useRbac()`, and on a unit created after the page
  loaded at the latest on the next page load.
- AC-5.5 The Access tab line reads "Admin: group delivery-leads".
- AC-5.6 A key limited to Team Admin on a unit works under an owner with Admin on all; the coverage rule of US-02 extends to it.
- AC-5.7 A role held on a unit does not narrow the rule there (P5).
- AC-5.8 Removing the rule ends the reach on the next request.

Technical notes: no role value says "admin of everything" today; `TeamAdmin` and `PortfolioAdmin` are per type, so how Admin on all is
expressed is DESIGN's, within C1. Exact-key consumers: `HasTeamWritePermission`, `HasPortfolioWritePermission`,
`GetAuthorizationSummaryAsync`, `CanSatisfyAnyScopedAdminAsync`. Check `FeatureMoveAuthorization`, the report write checks and
`GetWritablePortfolioIdsAsync` still agree. Bug 6576 has already hidden the mapping form from scoped admins (P10). E2E: `team-admins` and
`teamadmin@user.com`, as a thin step.

KPI: K4.

## Wave: DISCUSS / [REF] Requirements Completeness

Score 1.0: 24 of 24 checks answered, after the maintainer's decisions of 2026-11-07.

| Group | Checks | State |
|---|---|---|
| Functional (10) | grant, list, remove a group rule; reach later units; read path; keys; Access line; switch; admin rule; admin UI gating | All in US-01 to US-05 |
| Non-functional (7) | below | All stated |
| Business rules (7) | opt-in; Viewer meaning (P4); switch never Admin (P2); precedence (P5); System Admin only; Premium; bootstrap | 7 of 7 |

- NFR-1 Cost: the number of permission lookups for a list does not grow with the number of units; checked with 200 Teams.
- NFR-2 Effect: adding or removing a rule or the switch takes effect on the next request, with no restart.
- NFR-3 Fail closed: when a rule or the switch cannot be read, nobody gains access.
- NFR-4 Upgrade: expand-only; with no rule and the switch off, every permission answer equals the one before the upgrade.
- NFR-5 Concurrency: edits to the switch follow the stale-edit rule of other configuration.
- NFR-6 Accessibility: the switch and the dialog work by keyboard, with focus returned on close.
- NFR-7 Mutation kill rate at least 80 % on both stacks, per feature.

## Wave: DISCUSS / [REF] Outcome KPIs

Lighthouse sees none of this directly: RBAC is Premium, usage data is opt-in, and the population is small. K1 and K2 come from
asking one customer, so they are honest proxies with a sample of one.

| # | Who | Does what | By how much | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | The reporting customer's leaders | Stop holding System Admin just to read | 2 to 0 within 30 days of their upgrade | 2 (Head of Engineering, CTO) | The maintainer asks the customer's Lighthouse admin; their Settings → Access table | Leading |
| K2 | The customer's Lighthouse admin | Replace per-Portfolio group mappings with one rule | 100 % of those mappings removed within 30 days | One per Portfolio; exact count to ask | Same conversation | Leading |
| K3 | Instances sharing usage data with RBAC on | Turn on a system-wide rule | At least 3 instances within 90 days of release | 0 | Usage-data event, proposed below; DEVOPS decides | Leading, proxy |
| K4 | Instances that adopt Admin on all | Hold fewer System Admins | At least 1 customer confirms a System Admin was revoked within 90 days | 0 | Maintainer conversation; no event | Leading, proxy |

Guardrails: **G1** zero reports of someone reaching more than the rules say, for 90 days (issues, support). **G2** zero instances
change behaviour without opting in; pinned by NFR-4 and tests rather than measured. **G3** zero "removed but still reads" support
questions in 90 days.

North star: K1. **Hypothesis:** we believe that one system-wide rule will let organisations stop handing out System Admin for
reading. We will know when the reporting customer's two leaders hold no System Admin 30 days after upgrading.

Handoff to DEVOPS: K3's event only; no dashboard or alert. No baseline work beyond asking the customer K2's count.

## Wave: DISCUSS / [REF] Definition of Done

1. All UAT scenarios pass in DISTILL's tests, on both stacks.
2. The security negatives pass: outsider sees nothing, switch off means no change, revoking takes effect, Viewer on all cannot
   write, Admin on all cannot do System Admin things.
3. `pnpm test`, `pnpm build`, `dotnet build`, filtered `dotnet test` and Sonar are clean.
4. One thin step per flow in `RoleBasedAccessControl.spec.ts` passes against the real Keycloak realm.
5. Mutation kill rate at least 80 % on both stacks.
6. Sketches were approved before their UI was built; loading and failure looks are checked on a throttled connection.
7. `docs/settings/rbac.md` is true again, including the drift above, with per-feature screenshots.
8. ADR-270, ADR-271 and ARCHITECTURE.md match what shipped.
9. The usage-data event, if DEVOPS keeps it, is emitted and listed in `docs/settings/usagedata.md`.

## Wave: DISCUSS / [REF] Out-of-scope

- A system-wide role for one named person (P9), and a key limited to "Viewer on all" (P13).
- Hiding one unit from a system-wide rule. D2 gives one generic scope; a customer who wants Teams hidden does not opt in.
- Creating, cloning or deleting units, connections, system settings, RBAC admin or licence details through Admin on all (P1).
- Letting Team Admins save mappings for their own unit: Bug 6576 hides the form instead (P10). Refreshing a key's stale group snapshot (P11).
- Moving the page to its target surface (Epic 6473). Levels and per-level membership (Epic 6455).
- **PROPOSED follow-up candidate, for maintainer confirmation, not locked: rights on all Teams matching a pattern.** No slice or
  story here. Why not now: there is no evidence yet that anyone needs a subset defined by a pattern. Name matching also means
  a rename or a new Team silently changes who can see what, which conflicts with customers keeping Teams hidden from each other,
  so it would need a preview of what matches. C1 keeps the door open at no cost. **Weigh first:** rights inherited down the
  hierarchy ("all Teams under this unit"), which fits Epic 6455's membership per unit on any level and does not depend on names.

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

Strategy A (real, thinnest end-to-end), and no separate skeleton slice. This is brownfield: the OIDC sign-in, mapping store and
`RoleBasedAccessControl.spec.ts` already connect the whole path. Slice 01 is the thin line through it: Settings → Access, real
Keycloak group, read path, Overview.

## Wave: DISCUSS / [REF] Driving Ports

- `POST`, `PUT`, `DELETE /api/latest/authorization/group-mappings` and `GET .../group-mappings`, widened (slices 01, 05).
- `GET /api/latest/teams`, `/portfolios` and the unit read routes, through the `RbacGuard` read requirements (slice 01).
- `POST /api/latest/apikeys` and any key-authenticated route (slice 02).
- A new read of the rules for a unit, guarded like `GET .../teams/{id}/group-mappings` (slice 03; DESIGN names it).
- A new read and write of the switch, System Admin only for the write (slice 04; DESIGN names it).
- `GET /api/latest/authorization/my-summary` through `useRbac()` (slice 05); `DELETE /api/latest/teams/{id}` and the Portfolio twin.
- UI: Settings → Access, a unit's Access tab, Overview.

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| R1 | Story 4965, SSO groups mapped to roles | Shipped |
| R2 | The customer's identity provider sends the group in its token | They map a Jira users group today, so yes; confirm its value |
| R3 | P3 and P4 answered | Done 2026-11-07 |
| R4 | P2, P6, P7 answered | Done 2026-11-07 |
| R5 | P1, P5, P14 answered; Bug 6575 fixed | Answered 2026-11-07; the Bug has shipped |
| R6 | Sketch walkthrough with the maintainer, with P8 and P12 | Start of DISTILL |
| R7 | Premium licence fixture copied into the worktree | Needed for E2E and 2 Licensing tests |

## Wave: DISCUSS / [REF] Definition of Ready (9 items)

| # | Item | Evidence | State |
|---|---|---|---|
| 1 | Problem in domain language | Leaders made System Admin to read; a mapping per Portfolio does not scale | PASS |
| 2 | Persona with characteristics | Noor (42 Teams, no role), Alex, Jordan, Ines, Morgan | PASS |
| 3 | 3+ domain examples with real data | US-01: 4, US-02: 4, US-03: 4, US-04: 4, US-05: 4 | PASS |
| 4 | UAT in Given/When/Then, 3-7 | US-01: 6, US-02: 4, US-03: 5, US-04: 7, US-05: 6 | PASS |
| 5 | AC derived from UAT | AC-x.y map to the scenarios; AC-1.7, AC-2.6, AC-3.5, AC-4.9, AC-5.6 add negatives and constraints for DISTILL | PASS |
| 6 | Right-sized | 0.5 to 1 day per slice, 4.5 days in all; slice 05 at the top of the band, a watch item | PASS |
| 7 | Technical notes | Each story | PASS |
| 8 | Dependencies tracked | Pre-requisites table; the PROPOSED table names what each slice waits for | PASS |
| 9 | Outcome KPIs | K1 to K4 with numeric targets and methods, G1 to G3; proxies marked | PASS |

Every story has a `job_id` and an Elevator Pitch. No `@infrastructure` story. Per-wave peer review not run: optional in this wave,
and the consolidated review fires at the end of DISTILL.

## Wave: DISCUSS / [REF] House Checklist (every item answered)

- **RBAC impact: this Story is RBAC.** New: a Viewer and an Admin that reach every unit, the switch, a read of rules for unit
  admins. `DELETE` guards changed under Bug 6575, which has shipped. All through `IRbacAdministrationService`; nothing fetches `my-summary` directly.
- **Lighthouse-Clients CLI/MCP versioning: no change expected.** `/storage/repos/lighthouse-clients` turned out to be readable: a
  search for `group-mapping`, `groupMapping`, `authorization/`, `apikeys` and `api-keys` over 293 files finds nothing, so no client
  lists or creates mappings or keys. Clients read Teams and Portfolios with a key, and read more only where the owner may.
  Re-check at finalize.
- **Website marketing surface: wording to check at finalize.** `/storage/repos/website/src/pages/Lighthouse.tsx:862`, the
  "Authentication & RBAC" tooltip, says "four roles and SSO group mappings". Whether Viewer on all counts as a new role decides
  whether it needs a word. `docs/assets/settings/rbac.png` and `rbac_groupmapping.png` will change; check for hot-links first.
- **Licensing: answered, not open.** The switch and the rules are Premium by construction: the enforcement gate needs Premium, and
  every read is refused without it, so they need no gate of their own. Without RBAC, signed-in people already have full access, so
  a free "everyone can view" would mean nothing. Unit limits are unaffected: a rule creates no unit.
- **Epic 6455, deeper hierarchies.** The target is membership per unit on any level. The rule stays correct because it names no
  unit and no type: "all Teams and Portfolios" means every unit, on every level, including ones that arrive later. Only its
  on-screen words need the new level names. Admin on all then means the admin role on each level, which Epic 6455 defines. Which
  level a person sees first is navigation, not Access, as ADR-270 says.
- **Auth off, RBAC off, bootstrap.** Auth off or RBAC off: everyone has full access, and rules are stored but do nothing. Bootstrap: a
  group-mapped System Admin does not end it, and reads are refused, so the rules are not offered before the first explicit System Admin.
- **Terminology.** Team, Portfolio, Feature and Work Item; Team and Portfolio render through Terminology, including "All Teams and
  Portfolios" and the confirm dialog.
- **Visualization skill: N/A, because** nothing here shows data as a chart, tile or report.
- **Sketch before building:** above, walked through at the start of DISTILL.
- **Slow connection:** views are named above; the looks are DESIGN's.
- **Usage data (DEVOPS) proposal:** one name-only event when a system-wide rule is added or the switch is turned on, for K3; never
  the group value. Settled at DEVOPS.
- **Continuous-improvement check: run for Epic 6577.** No item has to come first. FE5 #6393, a shared RBAC handler hook for
  `TeamDetail` and `PortfolioDetail`, is an optional item during this work, as its own improvement item. S03e #6363, packaging
  Access, should wait until this Epic has shipped.
- **Docs at finalize, not now.** `docs/settings/rbac.md`: L43-48 (roles table: Viewer on "a specific team"), L53 (System Admin
  the only unscoped role: false), the matrix, L201-205 and the Common Tasks drift, the sentence that removing the last role leaves
  nothing, API Keys interplay, a new section with P7 and P11. Release notes are written at `/release`.
- **No nWave scenario tags in code.** DISTILL reminder; `@rbac` stays.
- **ADO:** nothing touched.

## Wave: DISCUSS / [REF] Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| A read or write path keeps checking exact keys and misses the rule, or grants too much | Medium | High | Consumers listed per story; DISTILL pins each capability row |
| Admin on all reaches `DELETE` | Closed by Bug 6575, shipped | High | AC-5.3 pins it for Admin on all |
| The switch is turned on by mistake on a confidential instance | Low | High | Off by default, confirm dialog, every Access tab says so |
| One customer's need shapes the model | Medium | Medium | Opt-in; slices 04 and 05 droppable |
| A stale key snapshot keeps wide read for a departed person | Medium | Medium | P11, remove the rule or the user |

## Wave: DISCUSS / [REF] Changed Assumptions

No DISCOVER documents exist, so nothing is back-propagated. The briefing's "a Viewer-for-all cannot write" holds except for
Refinement votes and comments (P4). The stated proposal that Admin on all excludes delete needed a code change, shipped as Bug 6575.

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Decisions: D1 to D4 and C1 locked; P1 to P7, P9 to P11, P13 and P14 decided by the maintainer on 2026-11-07; P8 and P12 wait for the sketch review; DESIGN answered P15.
- Primary need: read every Team and Portfolio without unit-by-unit grants or System Admin, opt-in.
- Walking skeleton: Strategy A, slice 01. Feature type: cross-cutting.
- Constraints: opt-in, Access context only, rule names no unit, "applies to" distinct, expand-only.
- Upstream changes: none.
- Density: lean, ask-intelligent. Triggers are in the hand-back; no expansion rendered.

## Wave: DESIGN / [REF] Prior-Wave Reading Confirmation

- ✓ This delta, slices 01 to 05, `docs/product/journeys/epic-6577-system-wide-access.yaml`.
- ✓ ADR-001, ADR-002, ADR-004, ADR-270 (Access row, glossary), ADR-271 (Access row), `docs/ci-learnings.md`.
- ✓ `brief.md`: outline and the embed section on the group snapshot. ADR-136, ADR-137, ADR-217: through the code and `brief.md`.
- ✓ The code of the Current-State Surface Inventory, plus `CurrentUserProfileService`, `OidcGroupSnapshotWriter`,
  `EmbedEntryController`, `AuthModeResolver`, `AuthConfigurationValidator`, `AppSettingService`, `OptionalFeatureSeeder`.
- ⊘ No SPIKE, none needed. Paradigm: OOP, as the project states. Style: unchanged, ports and adapters in the modular monolith.
- Continuous improvement: run, see the house checklist.

Maintainer decisions of 2026-11-07, taken from options: M1 the `AllUnits` scope on the mapping row, M2 the generic `UnitAdmin`
role, M3 an own one-row record for the switch, M4 System Admin groups stay off the Access tab line, and removed people's
embedded sessions are closed in this Epic. ADR-280 and ADR-281 hold the alternatives.

## Wave: DESIGN / [REF] DDD List

Source M is the maintainer, A the architect.

- **DDD-1 (M) A rule is a mapping row on a new scope.** `PermissionScopeType.AllUnits` (3) with a null `ScopeId` on
  `RbacGroupMapping`. The scope is the "applies to" part (C1), read from the row. No schema change.
- **DDD-2 (M) Admin on all is `UserRole.UnitAdmin` (4), shown as "Admin".** One row, as D2 asks. It reads as `TeamAdmin` on a
  Team and `PortfolioAdmin` on a Portfolio, so write checks compare what they compare today.
- **DDD-3 (A) Permissions resolve by reach, in one pure value.** `EffectiveAccess` replaces the private dictionary and keeps
  unit roles, the System role and reach apart. Rules are not expanded into unit keys: permission checks stay off the unit tables.
- **DDD-4 (A) A rule only adds (P5).** The role on a unit is the higher of the unit role and any reach. The merge that lets an
  explicit role replace a group role runs before that, on unit keys only, unchanged.
- **DDD-5 (A) "Applies to" answers one question.** `AppliesTo.Reaches(unit)` alone decides coverage, so a later value changes
  it and its stored form, not the read check or the Access tab line. A value that needs a unit's name extends the unit reference.
- **DDD-6 (A) Limited keys carry no reach.** The intersection yields the key's exact rows only, so a key cannot widen (AC-2.6).
  Coverage asks `RoleOn` for that unit on each request, so removing a rule stops the key at once. Slices 01 and 02 ship together.
- **DDD-7 (A) `my-summary` keeps its contract.** Viewer on all needs nothing in the browser: lists arrive filtered. For an admin
  reach the server fills `adminTeamIds` and `adminPortfolioIds` from the units that exist at that call, through `Reaches`.
  `useRbac()` does not change, nothing is stored, and a limited key gets the right answer.
- **DDD-8 (M) The switch is its own record.** `EveryoneSignedInSetting`: one row, one boolean `EveryoneCanView`, a
  `ConcurrencyToken`. It can only say view, so P2 needs no check. Written by an idempotent `ISeeder`; a missing row is off.
- **DDD-9 (A) The switch is read outside the group path.** `GetOwnerEffectivePermissionsAsync` reads it once a profile is resolved
  and adds a Viewer reach. So it works with no group claim, and a person with no stable subject or no profile gets nothing.
- **DDD-10 (M, A) A unit's rules have their own read.** See Driving Ports. It merges rules and the switch through `Reaches`,
  and answers role plus group name or "everyone signed in", with no mapping id. System Admin groups are left out.
- **DDD-11 (A) No domain event, published or consumed.** Evaluated on each request. A rule names no unit, so nothing reacts.
- **DDD-12 (A) No cache; fail closed.** The service has no cache dependency, so a change applies on the next request (NFR-2). A
  failed read ends in an error, never a grant (NFR-3).
- **DDD-13 (A) Removal removes all twins.** The unique index does not hold for a null `ScopeId`, so a race can store two rows.
  Removing a rule deletes every row with the same group, role and scope.
- **DDD-14 (A) P15: log who changed it.** One Information line per rule add, rule remove and switch change: actor's profile id
  and group value, in the existing log. Nothing goes to usage data.
- **DDD-15 (M) Embedded sessions speak only for an existing person.** The guard in `CurrentUserProfileService` widens from API
  keys to embed principals, so a person removed during an embedded session reads nothing and gets no new profile (AC-4.10).
- **DDD-16 (A) Bug 6576 through `useRbac()`, with one guard.** `isSystemAdmin` is true while loading and after a failed load, so
  the parent passes `isSystemAdmin && !isLoading && !loadFailed` as a prop to `ScopedGroupMappingManager`. The add form and the
  Actions column go; the list stays. No hook in the child.
- **DDD-17 (A) Modes need no new code.** RBAC off answers before a rule is read. Before the first explicit System Admin, or with
  Premium lapsed, the gate refuses first. Authorization on with authentication off is a configuration error, so the shared
  auth-off subject is not reached, except on standalone, which is single-user. The Settings section stays behind
  `status.hasSystemAdmin`.
- **DDD-18 (A) Bulk reports are outside NFR-1.** `ReportBulkCommands` and `ReportBulkRemover` check per owner, as they do for a
  System Admin. That is a bulk write, not a list, and its cost grows with owners selected. Left alone.

## Wave: DESIGN / [REF] Decisions Table

| # | Decision | Source | ADR | First slice |
|---|---|---|---|---|
| DDD-1 | `AllUnits` scope on the mapping row | Maintainer | 280 | 01 |
| DDD-2 | `UnitAdmin` role, shown as "Admin" | Maintainer | 280 | 05 |
| DDD-3 | `EffectiveAccess`, rule-aware | Architect | 280 | 01 |
| DDD-4 | Higher role wins against a rule | Architect, from P5 | 280 | 01 |
| DDD-5 | `AppliesTo.Reaches` | Architect, from C1 | 280 | 01 |
| DDD-6 | Limited keys carry no reach | Architect | 280 | 02 |
| DDD-7 | `my-summary` contract unchanged | Architect | 280 | 05 |
| DDD-8 | Switch as an own record | Maintainer | 281 | 04 |
| DDD-9 | Switch read outside the group path | Architect | 281 | 04 |
| DDD-10 | A unit's own rules read, no System Admin groups | Maintainer, Architect | 280 | 03 |
| DDD-11 | No domain event | Architect | 280 | 01 |
| DDD-12 | No cache, fail closed | Architect | 280, 281 | 01 |
| DDD-13 | Removal removes all twins | Architect | 280 | 01 |
| DDD-14 | Log who changed a rule or the switch | Architect, answers P15 | 280, 281 | 01, 04 |
| DDD-15 | Embedded sessions speak only for an existing person | Maintainer | 281 | 04 |
| DDD-16 | Bug 6576 by a prop from `useRbac()` | Architect | none | 03 |
| DDD-17 | Modes need no new code | Architect | none | 01 |
| DDD-18 | Bulk reports outside NFR-1 | Architect | 280 | none |

## Wave: DESIGN / [REF] Component Decomposition

Paths under `Lighthouse.Backend/Lighthouse.Backend/` and `Lighthouse.Frontend/src/`.

| Component | Path | Change | Slice |
|---|---|---|---|
| `PermissionScopeType`, `UserRole` | `Models/Authorization/` | Modified: `AllUnits`, `UnitAdmin` appended | 01, 05 |
| `AppliesTo` | `Models/Authorization/` | New: pure value | 01 |
| `EffectiveAccess` | `Models/Authorization/` | New: pure value, takes over the role priorities | 01 |
| `SystemWideRule` | `Models/Authorization/` | New: read model of holder, role, applies to | 03 |
| `EveryoneSignedInSetting` | `Models/Authorization/` | New: entity, `IConcurrencyTokenEntity` | 04 |
| `IRbacAdministrationService`, `RbacAdministrationService` | `Services/` | Modified: resolves `EffectiveAccess`, valid pairs, removal, unit rules, switch; test doubles widen too | 01 to 05 |
| `CurrentUserProfileService` | `Services/Implementation/Auth/` | Modified: guard covers embed principals | 04 |
| `EveryoneSignedInSeeder` | `Services/Implementation/Seeding/` | New: `ISeeder` | 04 |
| `LighthouseAppContext` | `Data/` | Modified: one `DbSet`, token configured; migration through `Create-Migration.ps1` | 04 |
| `AuthorizationController` | `API/` | Modified: four new actions, actor logged on rule changes | 01, 03, 04 |
| `RbacModels.ts`, the RBAC service | `models/`, `services/Api/` | Modified: new unions and calls | 01, 03, 04 |
| `RbacSettings.tsx` | `pages/Settings/Rbac/` | Modified: Role select, "Applies to", switch, dialog, loading and failure looks | 01, 04, 05 |
| `ScopedGroupMappingManager.tsx` | `components/Common/Authorization/` | Modified: read-only mode | 03 |
| Access tab notice | `components/Common/Authorization/` | New: read-only, one keyed TanStack query, three looks | 03 |
| `TeamDetail.tsx`, `PortfolioDetail.tsx` | `pages/` | Modified: pass the prop, mount the notice | 03 |

Unchanged: `RbacGuardAttribute`, `RbacGuardRequirement`, `useRbac`, `useRbacGate`, `ApiKeyController`, `GetVirtualPermissionsAsync`,
`RbacGroupMapping`'s columns, every controller that calls a read or write check, `FeatureMoveAuthorization`, the report write
checks, the Overview, the identity provider and sign-in.

Slice 01 builds `EffectiveAccess` with the reach it needs, so slice 02 is mostly its coverage check and tests. Only slice 04 has a
migration.

## Wave: DESIGN / [REF] Driving Ports

All under `api/latest` and `api/v1`, through the single inbound port `IRbacAdministrationService`.

| Port | Change | Access | Slice |
|---|---|---|---|
| `GET`, `POST`, `PUT`, `DELETE authorization/group-mappings` | Accept and return the new scope and role | System Admin, as today | 01, 05 |
| `GET authorization/teams/{teamId}/system-wide-rules` | New | Those who manage that Team's members | 03 |
| `GET authorization/portfolios/{portfolioId}/system-wide-rules` | New | Those who manage that Portfolio's members | 03 |
| `GET authorization/everyone-signed-in` | New: `everyoneCanView`, `concurrencyToken` | System Admin | 04 |
| `PUT authorization/everyone-signed-in` | New: refuses a missing token; a stale one answers 409 | System Admin | 04 |
| `GET authorization/my-summary` | Same contract, fuller lists for an admin reach | Signed in; only through `useRbac()` | 05 |
| Unit read and write routes, `POST apikeys`, any key-authenticated route | No change of their own | `RbacGuard`, as today | 01, 02 |

UI: Settings → Access, a unit's Access tab, Overview.

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port or adapter | Use | Change |
|---|---|---|
| `LighthouseAppContext` on SQLite and PostgreSQL | Mappings, permissions, keys, the switch row, unit ids for `my-summary` | One additive table |
| Identity provider token, stored group snapshot | Group values | Unchanged, no new call |
| `ILicenseService` | Enforcement gate | Unchanged |
| `ILogger` | Who changed a rule or the switch | One new line each |

**External integrations:** none new, so no contract tests are recommended.

**Earned Trust.** No new driven adapter, so no new probe. The substrate facts this design must not assume, and what covers each:

- Null `ScopeId` is distinct in a unique index on both databases: DDD-13, and the switch is not a mapping row.
- EF InMemory is not proof of a stale save: one test on SQLite and PostgreSQL through the shared test container.
- A group claim can be missing, malformed or stale: rules then give nothing and the switch still works; a key's snapshot
  stays stale as P11 accepts.
- A restore can bring back a database with no switch row: a missing row is off, and the seeder runs again.

## Wave: DESIGN / [REF] Technology Choices

Nothing new. .NET 10, ASP.NET Core, EF Core, SQLite and PostgreSQL, React 19, MUI 9, TanStack Query, NUnit, Vitest, Playwright: all as
pinned today. No new dependency, so no new licence.

## Wave: DESIGN / [REF] Reuse Analysis

Contract shape: pure means return-only; bounded means a declared set of rows changes.

| Existing component | File | Overlap | Decision | Shape and assertion | Justification |
|---|---|---|---|---|---|
| `RbacGroupMapping` | `Models/Authorization/RbacGroupMapping.cs` | Group, role, scope | EXTEND | Bounded: only mapping rows change | Two enum values, no column |
| `IsValidGroupMappingScope` | `RbacAdministrationService.cs:1289` | Role fits scope | EXTEND | Pure: table test over every pair | Serves create and update |
| `GetVirtualPermissionsAsync` | `:1146` | Groups to mappings | Unchanged | Read-only | Already returns rule rows |
| `GetOwnerEffectivePermissionsAsync` | `:1053` | Merges roles | EXTEND | Read-only: pinned explicit-replaces-group test | Reads the switch, builds `EffectiveAccess` |
| Private dictionary and `Has*Permission` | `:1257-1287` | Exact-key lookup | CREATE NEW `EffectiveAccess` | Pure: table tests | Seven readers need one higher-role rule; as a dictionary P5 would be written seven times. Nothing outside the class sees it |
| `OwnerCoversScope`, `IntersectWithApiKeyScope` | `:1097-1144` | Key limit | EXTEND | Pure: a key never exceeds the lower of limit and owner role | Result stays exact rows |
| `GetAuthorizationSummaryAsync` | `:418` | `my-summary` | EXTEND | Read-only | `HasAnyTeamAsync` already reads `context.Teams` |
| `useRbac`, `useRbacGate` | `hooks/` | UI gating | Unchanged | | Fed by the same lists |
| `AuthorizationController` | `API/AuthorizationController.cs` | Access endpoints | EXTEND | | `EveryActionDeclaresItsAccessArchitectureTest` covers new actions |
| `GetTeamGroupMappingsAsync` and twin | `:882`, `:899` | Unit mappings | NOT reused | | Rule rows would land beside a Remove button |
| `ApplyConcurrencyTokenForEdit`, `ConcurrencyConflictExceptionFilter` | `Data/`, `API/Filters/` | Stale edit | Reuse | Bounded: one row | NFR-5 as a house rule |
| `AppSetting`, `OptionalFeature` | `Models/` | A stored toggle | Rejected | | No token, and neither is Access (ADR-281) |
| `EveryoneSignedInSetting` | new | The switch | CREATE NEW | Bounded: one row, one boolean | Nothing existing has a token and belongs to Access |
| `ISeeder` | `Services/Implementation/Seeding/` | Startup rows | Reuse | Idempotent | Also runs after a restore |
| `ScopedGroupMappingManager` | `components/Common/Authorization/` | Form and table | EXTEND | | Read-only mode; layout reused on Settings |
| Access tab notice | new | D4 line | CREATE NEW | Read-only | No existing read-only list of rules |
| Domain-event bus | | | NOT used | | DDD-11 |

## Wave: DESIGN / [REF] Loading and Failure Looks

No view may show the previous answer as current, or an empty area that reads as "no rules".

| View | While loading | When it cannot load | Reuses |
|---|---|---|---|
| Access tab notice | Same slot, small spinner, "Checking system-wide rules" | Warning `Alert`: the check failed, so more people may reach this unit than listed here, with Retry. Never hidden | `CircularProgress`, `Alert`, a TanStack query keyed by unit |
| Settings → Access rules table | Spinner where the table goes | Error in place of the table, with Retry. Today the error sits at the top over an empty table | `CircularProgress` and `Alert`, as in `ScopedGroupMappingManager` |
| Everyone switch | Disabled with a spinner; never shown off until known | No toggle: an error with Retry. A 409 reloads the state and shows the existing conflict message | Same |

- The notice's query is keyed by unit, so a late answer for another unit never lands. With no rule, the tab looks as it does today,
  and only after the answer has arrived.
- `WidgetShell` does not apply: these are not dashboard widgets.
- The Overview is unchanged: its no-access alert sits inside `LoadingAnimation`, which covers its failure.
- These looks go into the sketches at the start of DISTILL, and are checked by hand on a throttled connection.

## Wave: DESIGN / [REF] Context-Map Placement (ADR-270)

- **Contexts touched:** Access only. Organisation set-up is read for unit ids, as `HasAnyTeamAsync` already does. Licensing gates it
  unchanged. Reports and Refinement see more readers and do not change. No new context or edge.
- **New terms:** as DISCUSS listed; the design adds no user-facing term. `UnitAdmin` is the code's name for "Admin" in a rule.
- **Domain events:** none published, none reacted to (DDD-11).
- **UI surface (ADR-271):** Access. Settings → Access today, and the unit's Access tab for the line. The section stops being about
  System Admin only, which is the one step towards the target.

**Owed at DELIVER, not edited now, because nothing has shipped:**

- **ADR-270, Access row:** name Admin on all beside broad readers.
- **ADR-270, glossary:** Access rows for system-wide rule, everyone signed in, Viewer on all, Admin on all, applies to.
- **ADR-270 "target only" marks: nothing to change.** The Access row's only target mark is membership per unit on any level, which
  this Epic does not deliver, and broad readers carries no mark. The DISCUSS note "drop 'target' from what ships" has nothing to
  act on.
- **`ARCHITECTURE.md` §4 and the authorization bullet at L289:** one sentence that a role can reach every unit through a system-wide
  rule. The ADR table at L440 gains ADR-280 and ADR-281.
- **ADR-271 L57:** add system-wide rules to the Access surface.
- **`job-rbac-scoped-admin` and `docs/settings/rbac.md`:** as P10 and the house checklist list them.

## Wave: DESIGN / [REF] Architecture Enforcement

Style: ports and adapters in the modular monolith. Language: C#, TypeScript. Tools: the repository's ArchUnitNET tests, NUnit.

- Only the Access service reads `RbacGroupMappings` and `UserPermissions` (new, reading compiled IL).
- Every `UserRole` and `PermissionScopeType` value has an answer in `EffectiveAccess` and the valid-pairs table (new).
- New actions declare their access (existing `EveryActionDeclaresItsAccessArchitectureTest`).
- NFR-1: a test counts queries for a list with 200 Teams.

## Wave: DESIGN / [REF] Open Questions

For DISTILL:

- P8 and P12, the "Unassigned" wording and the on-screen word, at the sketch review, together with the loading and failure looks.
- Exact copy of the notice's failure sentence, at the same review.

For DELIVER:

- Who reads `LastSeenAt` for embed viewers: the widened guard also skips the profile update, so keep whatever depends on it
  (slice 04).
- Optional FE5 #6393, as its own improvement item, if the new prop and notice make `TeamDetail` and `PortfolioDetail` repeat more.

For DEVOPS: the usage-data event for K3.

## Wave: DESIGN / [REF] Changed Assumptions

- AC-3.1 said "every system-wide rule"; it now says System Admin groups are not listed. Maintainer, M4.
- AC-4.10 and one scenario are new, and slice 04 carries them: DISCUSS assumed "a key of a removed person has no profile", which held
  for keys and not for a live embedded session. Maintainer.
- Quoted from the DISCUSS context map: `drop "target" from what ships`. Nothing is marked target for this feature, so that part
  of what ADR-270 owes falls away.
- Slice 01's hypothesis, "without touching each caller", holds for every caller outside the service; inside it, seven readers move to
  `EffectiveAccess`.

## Wave: DESIGN / [REF] Wave Decisions Summary

- Pattern: unchanged, ports and adapters in the modular monolith. Paradigm: OOP, with `EffectiveAccess` and `AppliesTo` as pure values.
- Key components: `EffectiveAccess`, `AppliesTo`, `EveryoneSignedInSetting`, the unit rules read, the Access tab notice.
- Migration: one additive table, in slice 04. Rule rows need none.
- External integrations: none new.
- Records: ADR-280, ADR-281, a correcting note on ADR-002, the `epic-6577-system-wide-access` section of `brief.md`.
