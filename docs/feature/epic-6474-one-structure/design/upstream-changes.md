# Upstream changes from DESIGN (domain part, 2026-11-02)

Changes the maintainer's DESIGN answers (MD4, MD6 in `feature-delta.md`) force on DISCUSS artifacts. Not applied to
the DISCUSS sections or slice files; DISTILL takes them from here.

## OUT-1 (Outcome KPIs)

Target becomes: "100% of units identical (counts, 90-day throughput, forecast percentiles on pinned draws) **except
two named groups**, each reported separately:
(i) units whose type spelling changed to the level's (MD4): one full refetch each, identical numbers expected and
checked;
(ii) Teams that lost a type to the Feature level (MD6): listed with the type and before/after numbers; not a failure."
The "0 'my forecasts changed' reports" target excludes the Teams of group (ii), which the upgrade names.

## Slice 01: pin today's numbers

- The harness classifies each unit as *unchanged*, *respelled* (i) or *lost a type* (ii) from the pre-upgrade lists.
- *Unchanged* units must keep their stored fingerprint and pinned numbers.
- *Respelled* units must keep their pinned numbers; a changed fingerprint is expected.
- *Lost a type* units are reported (Team, type, before/after numbers) and do not fail the check.
- Fixture adds: a Team reading "Epic" while a Portfolio reads "Epic"; a Team reading "bug" while most read "Bug"; a
  Team whose only type is taken by the Feature level (empty subset afterwards, see DQ6).

## Slice 02: two levels seeded

- AC "Each level lists every type its units read, once, ignoring case" becomes: "the Feature level lists every
  Portfolio type once, ignoring case; the Team level lists every Team type once, ignoring case, except those on the
  Feature level".
- New AC: no type is on both levels.
- New AC: the migration records each (Team, type lost to the Feature level) pair.
- New UAT: "Given Team 'Payments Crew' reads Story and Epic and Portfolio 'Checkout Revamp' reads Epic, when the
  install is upgraded, then the Feature level lists Epic, the Team level lists Story, and 'Payments Crew' ticks Story
  only."
- Scenario "Nothing anyone reads has changed" becomes: "Every unit's numbers equal the pinned numbers, except the
  listed Teams that lost a type."

## Slice 04: a unit reads its subset

- IN "Each unit's subset = its pre-upgrade `WorkItemTypes`" becomes: "its pre-upgrade list in the level's spelling,
  minus any type the Feature level took".
- AC "Each unit's subset equals its old list after upgrade; fetch fingerprints unchanged" becomes: "equals its old list
  ignoring case, except types lost to the Feature level; fingerprints unchanged except for respelled units, which
  refetch once with identical numbers".
- The round-trip AC holds for a read-then-write (the read returns the level's spelling). A write of a case variant is
  stored in the level's spelling.
- The 400 also covers a type held by the other level, naming that level.
- Technical note "Unchanged fingerprints also avoid one full refetch" becomes "only respelled units refetch once".

## Slice 05: levels shown in Organisation

- New, visible: a notice on the levels screen naming the Teams that lost a type at upgrade and which type
  ("{Team} no longer reads Epic: Epic is now a {feature} type"), shown to System Admins until dismissed. It needs a
  sketch (loading and failure looks as in the levels section) at DISTILL. If slice 05 should stay read-only, the
  notice can be its own small slice after it.

## Slice 08: add and remove level types

- Adding a type the other level holds is refused, naming that level. The dialog says where the type is.

## Release note (at /release)

Name the rule: "A tracker type now belongs to one level. Where a type counted for both Teams and Portfolios, it now
counts as a {feature} type only; the Teams that lost it are listed under Organisation → levels."

---

# Upstream changes from DESIGN (application part, 2026-11-02)

From maintainer decision **MD7** (answers DQ6) and the application design (`feature-delta.md`, "Wave: DESIGN / [REF]
Application" sections, ADR-275, ADR-276). Not applied to DISCUSS sections or slice files; DISTILL takes them from here.

## Verified per connector: what an empty type list reads today (DQ6)

| Connector | Empty list | Evidence |
|---|---|---|
| Jira | **every type** (no `issuetype` clause; refused only when query, types and states are all empty) | `JiraWorkTrackingConnector.PrepareQuery` / `PrepareGenericQuery` |
| Azure DevOps | **every type** (no `[System.WorkItemType]` clause), same refusal | `AzureDevOpsWorkTrackingConnector.PrepareQuery` |
| CSV | refused at validation; a read matches no row | `CsvWorkTrackingConnector.ValidateQueryOwnerForCsv`, `CreateWorkItemBaseForRow` |
| ServiceNow | refused at validation (`FromMissingWorkItemTypes`); reads nothing by design | `ServiceNowTeamQueryVerdict` |
| Linear | types not used (`IsWorkItemTypesRequired = false`) | `LinearWorkTrackingConnector` |

So DQ6 option (a) alone would have made a Jira or Azure DevOps Team read every type. MD7 replaces it.

## MD7: a Team left with no types is flagged and not refreshed

- **Model.** A unit carries an explicit `NeedsItemTypes` state, never inferred from an empty list. "No types ticked"
  keeps today's meaning for connectors that do not use types (Linear) or read nothing without them (ServiceNow).
- **Set** by the migration for every Team whose whole list moved to the Feature level and whose connector uses types
  (Jira, Azure DevOps, CSV). **Cleared** by a save that ticks at least one type, or that moves the unit to a connector
  that does not use types.
- **Amended after review (AM1):** any save (create, change, connection move, type removal) that leaves a Jira, Azure
  DevOps or CSV unit with no effective types sets the mark too; a unit can be created paused. Slice 04 AC: "a unit
  whose connector uses types never reads with an empty type list". Whether existing empty Jira/Azure DevOps units are
  flagged at upgrade: yes (MA8).
- **Effect.** The unit keeps its items, numbers and history; it is not refreshed (scheduled, manual, catch-up), and
  the forecasts that use its throughput keep using its last data.

### Slice 01
- The harness classifies a fourth group: **needs item types** (Team, the type it lost, refresh paused). It must keep
  its pinned numbers exactly (nothing is refetched), and is reported, not failed.
- Fixture: the Team whose only type is taken by the Feature level is on **Jira** (the risky connector), plus a
  Linear Team with no types that must **not** be flagged.

### Slice 02
- New AC: "A Team whose every type moved to the Feature level is marked as needing item types; a Linear or ServiceNow
  unit with no types is not."
- New UAT: "Given Jira Team 'Roadmap Crew' reads only Epic and Portfolio 'Checkout Revamp' reads Epic, when the
  install is upgraded, then 'Roadmap Crew' ticks no types, needs item types, and keeps its items and numbers."

### Slice 04
- New AC: "A unit that needs item types is not refreshed, by schedule or by its Update button, and keeps its data."
- New AC: "Ticking at least one type clears the mark; the next refresh reads the ticked types."
- New AC: "An API write that leaves such a unit with no types keeps the mark (and is not a 400)."
- `/teams/{id}` and `/teams/{id}/settings` gain an additive, nullable `needsItemTypes` field. The manual update
  endpoint answers **409** with code `unit-needs-item-types` for such a unit.

### Slice 05 (or the notice slice after it)
- The levels notice lists two kinds of line: "{Team} no longer reads Epic: Epic is now a {feature} type" and
  "{Team} reads no types now and is not refreshed until one is ticked". The second kind stays until the Team has a
  type, even after the notice is dismissed.

### Slice 08
- Decided (MA3): removing a level type that leaves a Jira, Azure DevOps or CSV unit with no
  types marks it as needing item types too; the confirmation says so per unit ("Payments Crew will stop refreshing
  until a type is ticked").

### Slice 09
- The unit's settings and its own page show "needs item types" (the Team page: a warning above the tabs, in the
  configured words, with a link to Settings for its Admins).

## Other application changes

- **Slice 03.** The first refresh after a create moves from the controllers into a `UnitCreated` handler. Portfolio
  deletion's inline clean-up becomes `UnitDeleted` handlers. AC unchanged.
- **Slice 06.** Decision A-D11: after slice 06, `PUT /terminology` **refuses** the eight level keys with 400
  `terminology-key-on-level`, naming the keys and `PUT /levels/{id}/names`. Names are written only through the
  levels endpoint (System Admin, any licence). Between slices 02 and 06, `PUT /terminology` writes them to the levels
  (Premium, as today). Slice 14's breaking-change note lists it.
- **Slices 12 and 13 stay whole** (MA6); each is flagged as over one day (about 1.5–2). DoR "right-sized" stays
  PASS with that flag.
- **Slice 13 becomes visible (MA5).** The free-tier messages use the level's configured unit name, pluralised:
  "Free users can use up to 3 Teams." ("3 Squads." when renamed), "Free users can use up to 1 Portfolio." Today's
  text is `Free users can only use up to 3 Team.` New AC: "A refused create names the level's unit in the configured
  word and number; status 403 and the limits are unchanged." Remove "invisible" from slice 13's header and the D8
  waiver's list; slice 14's note lists the new wording (scripts may match it).
- **Slice 05b (MA7), new.** "Upgrade notice": the lost-type and needs-item-types lines move here from slice 05,
  which stays read-only. Visible, System Admin only, depends on 05; sketch with loading and failure looks at DISTILL.
- **Existing empty units (MA8).** Every Jira, Azure DevOps or CSV unit that ticks no types at upgrade is flagged too
  (it stops reading every type). Slice 02 AC: "an existing Jira or Azure DevOps unit with no types is marked as
  needing item types at upgrade"; slice 01 classifies it in the needs-item-types group; release note names it.
- **Type removal (MA3).** Slice 08 AC: "removing a type that leaves a Jira, Azure DevOps or CSV unit with no types
  marks it as needing item types; the confirmation names those units and says they stop refreshing".
- **Rollback (MA9).** `WorkItemTypes` is dual-written; the loss-notice table is the recovery record (domain rule 11
  departure accepted). Slice 04 technical note: an N-1 read test on both providers.
- **Spelling (MA10, from #6473).** Every "Organisation" in user-facing copy, sketches and routes of the slices and
  journeys becomes **"Organization"** (`/settings/organization/...`): M4's "Organization → levels", slice 05's title
  and pitch, slice 08/09's "A System Admin can add it in Organization", slice 11's "Rename in Organization", slice
  15's "Organization → Levels", the release note. The context name "Organisation set-up" in ADRs is not copy.
- **Prerequisite (MA1).** Epic 6264 S02 (Metrics pilot) is merged before slice 02 is delivered; slice 01 may start
  before it. Add it to the dependency table.
- **Slice 14.** The breaking-change note lists: unknown type → 400 (M1); the eight level keys refused by
  `PUT /terminology` (A-D11); while "Teams only" is on, `/portfolios` lists none and `/portfolios/{id}` answers 404
  (A-D17). New in both clients: `levels list` / `levels` tool reading `GET /api/latest/levels`.
- **Slice 15.** While the Feature level is off: `GET /portfolios` returns an empty list, `/portfolios/{id}…` answers
  404 with code `level-switched-off`, a Portfolio create answers 409; Team payloads leave out Portfolio and Feature
  references; Feature-level units are not refreshed or forecast; a Team refresh triggers no Portfolio forecast. On
  switch-on, each Portfolio is refreshed once. Hidden Portfolios still count towards the free tier (MD5).
