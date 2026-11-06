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

---

# Upstream changes from the maintainer's vision (2026-11-06)

From VR1–VR19 in `feature-delta.md` and the revised ADR-274. **These win over the two sections above where they
disagree.** Not applied to DISCUSS sections or slice files; DISTILL takes them from here. Wherever a section above says
"the level's types" or "the level's spelling", read "the types the connection maps to that level" and "the mapping's
spelling".

## OUT-1 (Outcome KPIs)

Group (ii) becomes "Teams that lost a type to the Feature level **on their own connection**".

## Slice 01: pin today's numbers

- Fixture adds: a Jira Team reading Epic and an Azure DevOps Portfolio reading Epic. The Team loses nothing.
- The "lost a type" fixture must have the Team and the Portfolio on the same connection.

## Slice 02: two levels seeded

- Title and pitch: levels are seeded from Terminology; mappings are seeded per connection from the units' types.
- ACs become: "For each connection, its Feature level lists every type of its Portfolios once, ignoring case; its Team
  level lists every type of its Teams once, ignoring case, except those on its Feature level."
- "No type is on both levels" becomes "No type is on both levels of one connection".
- New AC: "A connection with no units on a level gets today's create defaults for it."
- New AC: "At each start, a connection without a mapping gets one from its units' types; a subset the previous release
  changed is derived again, a type not mapped is dropped and recorded, and an empty result marks the unit."
- New UAT: "Given a Jira Team reads Story and Epic and an Azure DevOps Portfolio reads Epic, when the install is upgraded,
  then the Jira Team still ticks Story and Epic, and Epic is on the Feature level of the Azure DevOps connection only."

## Slice 04: a unit reads its subset

- The invariant becomes "subset within the types its connection maps to its level".
- The 400 reads: "Spike is not on the Team level of letpeoplework.atlassian.net".
- The UAT "A type the level does not list" follows that wording.

## Slice 05: levels shown in Organization

- The table has no item types column and the two rows show names only. The sketch changes (board 2).
- The leaf is marked "Where the work is done" and the other level "Groups the level below", not "generates throughput".

## Slice 05b: upgrade notice

- Lines name the connection: "{Team} no longer reads Epic: on {connection}, Epic is now a {feature} type".

## Slice 08: add and remove a connection's types (biggest change)

- Title: "Decide once what counts as a Work Item on a connection".
- Lives in Organization → Connection mapping, one tab per connection, named after the connection, one row per level.
  Not in the level dialog. Sketch on board 3; DISTILL's walk-through takes it from there.
- Every "level 1" scenario gains its connection. "12 Teams could read Task" counts the Teams on that connection.
- Refusal: a type on the other level of this connection is refused, naming that level.
- New: "Copy mapping from…" on a connection: one confirmation naming the units that lose a type, then as a removal.
- Removal confirmation adds: "To move this type to another level, Teams must tick it again there."
- A tab for a connector without types (Linear) says so. Tracker-specific panels (proposal from the tracker, parent
  sources, read-only Linear levels) are not part of this Epic.
- Loading and failure looks: a lone spinner in the tab while it loads; on failure, the could-not-load message and
  nothing that looks like an empty mapping.

## Slice 09: a unit narrows its connection's types

- The checkboxes list what the chosen connection maps to the unit's level, and change with the connection field. With
  only one connection it is picked for you and the field goes. Sketch on board 9.
- Load from board wording: "Sub-task is not mapped on this connection. A System Admin can add it in Organization."
- New: changing the connection (board 10). Three cases: same mapping, nothing changes; some types dropped, named; nothing
  left, a pick is required before saving.
- New UAT: "Given Team 'Apollo' ticks Story and Bug on Jira, and Azure DevOps maps Bug and User Story to the Team level,
  when its connection changes to Azure DevOps, then it keeps Bug and is told Story is dropped."
- New failure look: while the connection's list cannot load, the selection is kept read-only, as in the earlier sketch.

## Slice 15: Teams only

- The Connection mapping tab shows the off level's row greyed; its types can be removed. A new connection gets no
  Feature-level defaults while the level is off.
- New UAT: "Given the Feature level is off and Epic sits on it, when Priya removes Epic there and adds it to the Team
  level, then a Team can tick Epic."

## Slice 14: clients read the levels

- `levels list` returns names, not types. New `connections mapping <id>` command and MCP tool for "which types count as
  Work Items here?". The breaking-change note says how to see a connection's types.

## Release note (at /release)

"A tracker type now belongs to one level on each connection. Where a type counted for both Teams and Portfolios on the
same connection, it now counts as a {feature} type only; the Teams that lost it are listed under Organization."
