# ADR-274: A connection maps its item types onto the levels, and a unit only narrows them

**Status**: Proposed (Epic #6474 DESIGN, 2026-11-02; maintainer decisions MD3, MD4, MD6 applied). **Revised
2026-11-06** with the maintainer's vision: the types moved from the level to the connection. This replaces the
earlier "item types belong to the level". **Revised 2026-11-27**: a type may sit on several levels of one connection,
with a warning (rules 2 and 10, first consequence). **Revised 2026-11-28** (feature A DESIGN refresh): rule 8 and rule 14 follow the removal of level 2 that replaces the switch, and rule 13 says how a start tells a previous release's edit apart. Promoted to `docs/product/architecture/` with slice 04.
**Context map**: Organisation set-up (owner), Work Items (reacts), Tracker integration (receives each unit's types
with the fetch, unchanged). Related: ADR-241, ADR-270 Decisions 2 and 4, ADR-271 Decision 4, ADR-273, 6455 DISCUSS
D10, D12 and D22–D34, 6474 M1–M4, M6, M11, VR1–VR19.

## Context

Today every Team and Portfolio holds its own free-text list of tracker types. D12 lets a unit narrow a shared list,
never add to it. The 2026-11-02 version of this ADR put that shared list on the level. That made it tracker-agnostic,
and the version already named the cost: an install with two trackers had one list for both, and "N units could read
it" counted units on a tracker with no such type.

The maintainer's vision (2026-11-06) resolves this. The structure is set up once per instance and knows no tracker,
so it can exist before any connection does. A connection brings its types and says where they fit. One instance may
have both a Jira and an Azure DevOps connection, and the same word may mean different levels on them.

Two facts in today's code matter:

- Every connector matches types ignoring case (`IsItemInList` uses `OrdinalIgnoreCase`; JQL and WIQL ignore case).
- The fetch fingerprint renders a unit's types exactly as stored (sorted, case kept). Respelling a unit's "bug" as
  "Bug" reads the same records but changes the fingerprint, so that unit refetches in full once.

Some installs read the same type on both levels today, for example a Team that counts Epic as a Work Item type while
a Portfolio counts Epic as its Feature type.

## Decision

1. **Each connection has one mapping.** `ConnectionMapping` is an aggregate of Organisation set-up, one per connection,
   because Mapping describes how the organisation works and Translation stays in Tracker integration (ADR-270). It
   lists, per level, the item types of that connection: non-empty trimmed names. A list may be empty (Linear,
   ServiceNow by choice). A `Level` holds no types.
2. **A type may sit on several levels of one connection, with a warning.** Within one level a type appears once,
   ignoring case. Adding a type that another level holds on this connection is accepted, and Lighthouse warns next to
   the type on each level it sits on: this is not intended use and usually points at a problem in the tracker's
   configuration. The warning never blocks a save. Which items a unit reads is decided by its subset and its query, as
   today (for example a title filter that keeps "[SUPER]" Epics off the Feature level). Moving a type is a remove plus
   an add, so the units that tick it lose the tick and must tick it again (M2); the removal confirmation says so.
   Across connections there is no rule and no warning.
3. **A type on no level is ignored.**
4. **A unit's subset is a list of the types its connection maps to its level, stored in the mapping's spelling.** Empty
   is legal. An API write sending a case variant is accepted and stored in the mapping's spelling. A name that is not
   mapped to the unit's level on its connection is refused with 400, naming the type, the level and the connection,
   for everyone (M1). This includes a type mapped to the other level.
5. **What a unit reads is its subset intersected with its connection's types for its level**, ignoring case. A race
   between a type removal and a unit save is then harmless: a type no longer mapped never reaches a tracker.
6. **Adding a type ticks it for no unit** (M2). "N units could read it" counts the units on that connection and level.
   Only System Admins add types, only in Organization → Connection mapping (M4).
7. **Removing a type** (after a confirmation naming the units that tick it, M3) removes it from the mapping and from
   those units' subsets **in the same transaction** (MD3). This is a deliberate exception to "one transaction, one
   aggregate": both aggregates are Organisation set-up's, the command is a rare admin action, and the admin confirmed
   exactly those units. `ConnectionItemTypesChanged(ConnectionId, LevelId, Added, Removed, UnitsNarrowed)` follows
   after commit; Work Items enqueues a refresh of the narrowed units.
8. **A new connection starts from today's create defaults** (leaf: User Story, Bug; Feature level: Epic, unless level 2
   has been removed), or nothing on a connector that does not use types, or a copy of another connection's mapping
   ("Copy mapping from…"). A copy is a remove of every type the target loses plus an add of every type it gains: rule 7
   applies in full (one confirmation naming the units that lose a type, narrowing in the same transaction, the
   needs-item-types mark, one `ConnectionItemTypesChanged` per level). **No row for a level means an empty list**, which
   is what a level added later has on every connection until it is mapped.
9. **A unit may change its connection.** Its subset keeps the types the new connection maps to the same level, in the
   new mapping's spelling. The form names what is dropped. If nothing is left on a connector that uses types, the form
   asks for at least one before saving. An API write is checked against the new connection (rule 4); a write that
   leaves no types sets the needs-item-types mark (MD7, AM1) as any other save does.
10. **Migration, per connection.**
    - The Feature level first: the union of the types of all Portfolios on this connection, deduplicated ignoring case.
    - Then the leaf: the union of the types of all Teams on this connection, deduplicated the same way. A type that
      both levels read today lands on both, with the rule 2 warning; no level wins and no unit loses a type.
    - Spelling of a case-collapsed type: the one most units on that level and connection use; on a tie, the spelling
      of the unit with the lowest number. Order on the level: first appearance by unit number.
    - Each unit's subset is its old list in the mapping's spelling (MD4), minus any type its level does not list there.
    - A connection with no units on a level gets today's create defaults for it, less any default the other level
      already holds on that connection, or nothing on a connector that does not use types.
    - No unit gets a type ticked that it did not read.
    - Nothing else is restructured (D10): Change as a Feature type stays.
    - The migration records each type it put on both levels of a connection, so that the upgrade notice can name it
      with the warning.
11. **A unit's To Do, Doing and Done states stay its own** (D13). A default per connection and level is a later step
    that only adds to this.
12. **The mapping is the home for what else depends on connection and level, later.** Parent sources, dependency
    fields and the shared defaults of #6349 belong there. #6474 does not move them: the connection's existing defaults
    stay as they are, and #6455 keys them by level when a third level needs it.
13. **After a rollback, the mapping wins and nothing is lost silently.** At each start, a connection without a mapping
    (created by the previous release, or by an import) gets one by rule 10, from its units' types. Then a subset whose
    old `WorkItemTypes` column was changed by the previous release is derived again by rules 4 and 5, against the
    unit's current connection. A change is detected by comparing `WorkItemTypes` with `WorkItemTypesAsWritten`, the
    list this release last wrote there; comparing it with the subset would re-tick a removed and re-added type for a
    unit marked as needing item types, which keeps its last list in the old column (M2). A type not mapped there is dropped and recorded in `UnitTypeLossNotices`; the mapping
    does not grow, because only System Admins add types. An empty result on a connector that uses types sets the
    needs-item-types mark.
14. **Removed by the 2026-11-27 walk-through.** Level 2 is removed rather than switched off, and only while it holds
    no units, so the mapping has no hidden level to show.

## Consequences

- "No numbers change on upgrade" (OUT-1) holds with one named exception: **respelled units** refetch in full once,
  because their fingerprint changes. Identical items and numbers are expected and checked. No Team loses a type at
  upgrade (the 2026-11-06 version had a second exception for that).
- An item's level cannot be read from its type alone; it comes from the unit whose query read it. #6455 and the chain
  check (#6560) build on that, and the one-owning-unit-per-level rule (2026-11-24) must catch an item read by units on
  two levels.
- A Team can still be left with an empty subset by a type removal or a connection change. MD7 flags it and pauses its
  refresh.
- Two-tracker installs need no special handling: each connection migrates on its own.
- The old type lists stay in their columns (expand-only), so what a Team lost can be read back.
- The 400 for a type not mapped is a breaking change for Lighthouse-Clients (major version, slice 14).
- Unit forms depend on the chosen connection: the list of types changes with it, and it can only load once a
  connection is picked. With one connection it is picked for you.
- A second connection to the same tracker needs its own mapping, which is why "Copy mapping from…" ships with it.
  People who keep a connection with write rights for occasional write-backs rely on that.
- The levels screen no longer lists types. Connection mapping is a new tab in Organization.

## Alternatives considered

- **The level owns the types, tracker-agnostic** (this ADR's 2026-11-02 version). Replaced: one list for all trackers,
  wrong counts in the dialog, needless losses at migration, and no place for a Jira and an Azure DevOps meaning of the
  same word.
- **One mapping per kind of tracker.** Rejected by the maintainer: two Jira connections may differ, and everything
  else on a connection is per connection.
- **Mapping per project inside one connection,** for a type that means different things in two projects. Not now:
  unlikely, and a second connection to the same tracker covers it. Revisit on demand.
- **The mapping as part of Tracker integration.** Rejected: which type counts as what needs the user and describes how
  the organisation works (ADR-270 "Mapping is not Translation"). Only its key is a connection.
- **A unit keeps its own spelling.** Rejected by the maintainer (MD4): one refetch per respelled unit is accepted in
  return for one spelling everywhere.
- **Each type on exactly one level of a connection** (MD6, VR3; this ADR until 2026-11-27). Replaced: it cannot express
  a tracker that uses one type on two levels, told apart by the title ("[SUPER] Epic" above the Features, community
  feedback on the mockups), and it made Teams lose types at upgrade.
- **A title marker per mapping entry** ("Epic + [SUPER]" on a higher level). Not chosen: a new concept, and renaming an
  item would move it to another level. Can come with #6455 if the query is not enough.
- **The Feature level or the leaf wins a contested type.** Falls away with rule 2.
- **Subset as references to mapped-type ids.** Rejected for now: a join for every fetch, for no rule a list of names
  in the mapping's spelling cannot express.
- **Prune removed types from units later, through an event.** Rejected: for a moment the units would still show the
  type the admin was told they stop reading, and a quick re-add could tick it again for them, against M2.
