# ADR-274: Each item type belongs to one level, and a unit only narrows its level's types

**Status**: Proposed (Epic #6474 DESIGN, 2026-11-02; maintainer decisions MD3, MD4, MD6 applied). Promoted to
`docs/product/architecture/` with slice 04.
**Context map**: Organisation set-up (owner), Work Items (reacts), Tracker integration (receives each unit's types
with the fetch, unchanged). Related: ADR-273, ADR-270 Decision 4, 6455 DISCUSS D10 and D12, 6474 M1–M4, M6, M11.

## Context

Today every Team and Portfolio holds its own free-text list of tracker types. D12 makes the types the level's: a
unit may untick types from its level's list, never add one. M1 refuses an API write naming a type the level lacks,
for everyone. M6 deduplicates the union ignoring case. The maintainer added (MD6) that a type lives on exactly one
level: a tracker type is either a Work Item or a Feature, never both.

Two facts in today's code matter:

- Every connector matches types ignoring case (`IsItemInList` uses `OrdinalIgnoreCase`; JQL and WIQL ignore case).
- The fetch fingerprint renders a unit's types exactly as stored (sorted, case kept). Respelling a unit's "bug" as
  "Bug" reads the same records but changes the fingerprint, so that unit refetches in full once.

Some installs read the same type on both levels today, for example a Team that counts Epic as a Work Item type while
a Portfolio counts Epic as its Feature type.

## Decision

1. **A level lists its item types**: non-empty trimmed names. The list may be empty (Linear, ServiceNow by choice).
2. **Each type belongs to exactly one level across the whole structure, ignoring case.** The organisation structure
   aggregate guards this. Adding a type that another level holds is refused, naming that level. Moving a type is a
   remove plus an add.
3. **A unit's subset is a list of its level's types, stored in the level's spelling.** Empty is legal. An API write
   sending a case variant of a level type is accepted and stored in the level's spelling. A name that is not on the
   unit's level is refused with 400, naming the type and the level, for everyone (M1). This includes a type held by
   the other level.
4. **What a unit reads is its subset intersected with its level's types**, ignoring case. A race between a type
   removal and a unit save is then harmless: a type the level no longer lists never reaches a tracker.
5. **Adding a type to a level ticks it for no unit** (M2). Only System Admins add types, only in Organisation (M4).
6. **Removing a type** (after a confirmation naming the units that tick it, M3) removes it from the level and from
   those units' subsets **in the same transaction** (MD3). This is a deliberate exception to "one transaction, one
   aggregate": both aggregates are Organisation set-up's, the command is a rare admin action, and the admin confirmed
   exactly those units. `LevelItemTypesChanged(LevelId, Added, Removed, UnitsNarrowed)` follows after commit; Work
   Items enqueues a refresh of the narrowed units.
7. **Migration rules.**
   - The Feature level first: the union of all Portfolios' types, deduplicated ignoring case.
   - Then the leaf: the union of all Teams' types, deduplicated the same way, minus every type already on the Feature
     level, ignoring case. **The Feature level wins** (MD6).
   - Spelling of a case-collapsed type: the one most units on that level use; on a tie, the spelling of the unit with
     the lowest number. Order on the level: first appearance by unit number.
   - Each unit's subset is its old list in the level's spelling (MD4), minus any type its level does not list.
   - An install with no units on a level gets today's create defaults (leaf: User Story, Bug; Feature level: Epic),
     less any default the other level already holds.
   - Two-tracker installs get one list per level for both trackers. No unit gets a type ticked that it did not read.
   - Nothing else is restructured (D10): Change as a Feature type stays.
   - The migration records each (Team, type lost to the Feature level) pair, so that the upgrade can name them.
8. **State mapping stays the unit's own** (D13); no level or organisation default is introduced with the types.

## Consequences

- "No numbers change on upgrade" (OUT-1) holds with two named exceptions:
  1. **Respelled units** refetch in full once, because their fingerprint changes. Identical items and numbers are
     expected and checked.
  2. **Teams that lose a type to the Feature level** stop reading it. Their item counts, throughput and forecasts may
     change. The levels screen shows a notice naming them, the release note states the rule, and slice 01's harness
     reports them separately instead of failing.
- A Team can be left with an empty subset by the second exception. What an empty list reads on Jira, Azure DevOps
  and CSV must be confirmed per connector (open question DQ6).
- The old type lists stay in their columns (expand-only), so what a Team lost can be read back.
- Level lists are tracker-agnostic. "N units could read it" counts every unit on the level, including those on a
  tracker that has no such type. A per-tracker layer stays open (ADR-270 "Org default or per-tracker default").
- The 400 for a type not on the level is a breaking change for Lighthouse-Clients (major version, slice 14).

## Alternatives considered

- **A unit keeps its own spelling.** Rejected by the maintainer (MD4): one refetch per respelled unit is accepted in
  return for one spelling everywhere.
- **The same type allowed on several levels.** Rejected by the maintainer (MD6): a type means one level's item.
- **The leaf level wins a contested type.** Not chosen: the maintainer picked the Feature level. The leaf winning
  would take Features away from Portfolios, and with them Deliveries and Feature forecasts.
- **Subset as references to level-type ids.** Rejected for now: a join for every fetch, for no rule a list of names
  in the level's spelling cannot express.
- **Prune removed types from units later, through an event.** Rejected: for a moment the units would still show the
  type the admin was told they stop reading, and a quick re-add could tick it again for them, against M2.
