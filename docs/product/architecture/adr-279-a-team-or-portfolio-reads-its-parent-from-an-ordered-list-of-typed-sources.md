# ADR-279: A Team or Portfolio reads its parent from an ordered list of typed sources, stored as one JSON text value, ordered by one selector

**Status**: Accepted (decided 2026-11-02, amended during delivery, shipped on `main` 2026-11-07, Story 6501)
**Date**: 2026-11-07
**Feature**: `docs/feature/story-6501-ordered-parent-fields/`

Extends ADR-193 (the selector is the one tracker-neutral home of the rule; ADR-243 dropped the capability port it
proposed) and ADR-241 (own value plus source). Relies on ADR-242 for the refetch and on ADR-244 for the findings.

## Context

The Parent Override Field held one Additional Field id per owner and always fell back to the tracker's own parent. A
setup whose parents sit in two or more non-native places, or whose own parent is not a Feature, could not be expressed.
The maintainer decided: one to five ordered sources, the first that yields a parent wins, the tracker's own parent is an
optional entry (not an implicit last fallback), a link entry with no stored Feature behind it yields nothing and the next
entry is read, a duplicate is refused, and the migration writes today's list shapes explicitly. Epic 6474 will restructure
the owner tables after this ships. The settings DTOs are the public `api/v1` surface.

## Decision

1. **Value.** `ParentSources`: an immutable, validated, ordered list of `ParentSourceEntry`, each of kind `TrackerParent`
   or `AdditionalField(id)`. One to five entries, no duplicate. Violations are returned as values. Every new type is a
   pure value; nothing in them touches a record or the database.
2. **Storage.** One text column per owner table (`ParentSources`) and one per grain on the connection
   (`ParentSourcesForTeams`, `ParentSourcesForPortfolios`): SQLite `TEXT`, PostgreSQL `text`, not `jsonb`. Mapped with
   `HasConversion` and a `ValueComparer` like `StateMappings` and `CycleTimeDefinitions`. Writing calls the serialiser with
   null options, so member names are PascalCase in declaration order and enums are integers. Stored text, for example:
   `[{"Kind":1,"AdditionalFieldDefinitionId":12},{"Kind":0,"AdditionalFieldDefinitionId":null}]` (`TrackerParent` = 0,
   `AdditionalField` = 1). NULL means no own value: the converter keeps NULL as NULL (never an empty list, which is not a
   legal value), reads blank text as NULL, and refuses a text the validating factory refuses instead of repairing it.
   Reading requires every constructor parameter, so a stored entry that lost a member is refused rather than read as the
   tracker's own parent. The converter does not swallow that exception. The stored source column is reused.
3. **Migration.** `AddParentSources`, expand-only, generated with `Lighthouse.Backend/Create-Migration.ps1`. Additive
   columns plus a data step written as SQL in both providers' migrations (a text concatenation with an explicit
   `CAST(... AS TEXT)` of the integer): own override X becomes `[X, TrackerParent]`, own none becomes `[TrackerParent]`,
   followers stay NULL, connection defaults likewise. A test runs the real migration on both providers and checks that
   every migrated text survives `Serialize(Deserialize(text)) == text`. `Down` drops the new columns only.
4. **Legacy columns are written through for one release.** Whenever a list is set, the legacy integer column is written
   with the list's projection: `[X, TrackerParent]` writes X, `[TrackerParent]` writes NULL, any other shape writes its
   first field id or NULL. Nothing decides from the legacy columns; only the owner types read them, to answer the
   deprecated members and to keep the two in step. A later release drops them.
5. **One rule, one place; the record keeps what the second pass needs.** Connectors read each entry into a reading (no
   value, field value, link candidates, the tracker's own parent, cannot be read) and attach the ordered readings to the
   record as a transient, unpersisted value for the duration of one refresh. `ParentSourceSelector` alone orders them:
   the first entry that yields wins. A field or the tracker's own parent yields whenever it has a value, even one naming
   no Feature. A link entry on a Team yields only a Feature Lighthouse holds, a single candidate included; a Portfolio does
   not narrow, so a lone candidate is taken at face value and several are placed without narrowing. The tracker's own
   parent is read only when listed. The selector only **returns** a choice; `HangUnder` is the single place that assigns
   `ParentReferenceId` and sets or clears the shown values of link entries on a refresh (other writers of those shown
   values, such as write-back, are untouched), and an empty choice clears the parent. The connector runs a provisional
   pass without Feature knowledge and stops at the first link reading that needs a Feature test. The work item refresh
   then runs the full pass, with the stored Features, on exactly those records, and may retract a provisional key. The
   record's copy constructor carries the readings across the wrap between the two passes. On Jira the tracker's own parent
   is the parent key, then the instance's own parent field, looked up by name.
6. **Azure DevOps has no link entry.** A list entry there is the native parent or a field. A relation type named in the
   list reads no relations, yields nothing and raises one finding. The relations are read while the list holds the
   tracker's own parent, or while a Portfolio needs them for its dependencies, for every record an update downloads.
7. **Refetch.** The fetch fingerprint renders the ordered effective list as `F:12,P`, a form that can never equal a
   legacy rendering, and `[TrackerParent]` renders as the old `-`. The upgrade queues a one-time re-read of every Jira
   owner (a Jira reading version moves every Jira owner's fingerprint, because SQL cannot tell which owners hold a link
   entry) and of every Azure DevOps owner whose effective list holds a field. See "What the upgrade does".
8. **Findings.** Each entry that cannot be read raises its own finding, carrying whether a next source followed, so the
   sentence claims a next source only when there was one. An entry whose field is gone is named `Unknown field (id N)`,
   the words of the settings control. Findings ride on the records, as ADR-244 decided. The wire name
   `RefreshFindingSetting.ParentOverrideField` stays.
9. **API.** Additive `parentSources` and `parentSourcesDefault` on the owner settings, and `parentSourcesForTeams` and
   `parentSourcesForPortfolios` on the connection defaults, with `kind` as a string. The old integer members stay on the
   wire for one release as deprecated projections.
   - **On an owner**, a sent list wins, with one exception. Today's page and scripts echo the whole GET, so a client can
     edit the old field and send back the list unchanged. When the list sent equals what the owner already reads, and the
     old field was sent and differs from the list's projection, the old field is the edit and becomes `[X, TrackerParent]`.
     A client that sends a list must leave the old field out or keep it in step. A sent source wins over a sent list: a
     list sent with `parentOverrideSource` `ConnectionDefault`, which is what a GET of a following owner returns, is
     ignored, so a client that means the list sends `Own` too.
   - **On the connection**, the same rule holds, and the old setters stay as a bridge. An old number equal to the list's
     projection is an echo and leaves the list alone. A null that is no echo clears the default. Any other number becomes
     `[X, TrackerParent]`, dropping later entries, and sets a default where there was none.
   - **A list default names only saved fields.** A field added in the same save has no id yet and is picked on the next
     save; only the old number can still name one, through the bridge. The page never sends the old numbers.
10. **Where it shows.** The control is shown on Jira and Azure DevOps only and hidden on Linear, CSV and ServiceNow, whose
    parent behaviour does not change. Reordering is by up and down buttons, without drag. A default is cleared by leaving
    only the own parent row; the list has no None. That row reads `Parent (default)` on every tracker, and an owner picks
    `Override` to hold its own list or `Connection default` to follow. Screens and backend refusals and log labels all say
    Parent sources; the wire names of the old setting stay.

### Rollback limits

A build one release back reads only the legacy column and then the tracker's own parent. For the two legacy shapes it
behaves like the list on the data the old build could read. Any other shape resolves as the first field then the own
parent, so the order, the later fields and a list without the own parent are not honoured. It also cannot read what the
new build reads in two places: the instance's parent field after an empty parent key, and on Azure DevOps the native
parent of an owner that had an override, whose relations the old build skipped. Edits made on the older build are not
carried forward. An older build's edit of a follower leaves source Own with no list, and that owner reads the connection
default after rolling forward.

Running `Down` then `Up` rebuilds each list from the old columns: `[A,B,own]` becomes `[A,own]`, `[own,A]` becomes
`[A,own]`, and `[A,B]` gains the own parent. This is a one-release convenience with these stated limits, not a guarantee.

### What the upgrade does (decided: accept the change)

No static migration reproduces today's parent results exactly, because whether the tracker's own parent is the fallback
is decided at read time: on Azure DevOps by the grain and the dependency setup, on Jira by a field read that an override
owner does not make today. Writing `[X]` does not preserve them either: it drops the parent key fallback on Jira and
renders a new fingerprint. A single link candidate that is no Feature is held today and falls through under the new rule.

- **Accept the change (decided).** Every override owner becomes `[X, TrackerParent]`, every other owner with an own value
  `[TrackerParent]`, and the fall-through rule applies everywhere, with the one-time re-read above. Three kinds of item
  move once: an item of an override owner with an empty parent key now reads the instance's parent field; on Azure DevOps
  an item of an override owner with no override value now hangs under its native parent; and on a Team an item whose only
  link candidate is no stored Feature falls through to the next entry. On Jira an item with an empty override already
  fell back to the parent key, so it does not move. Items that move may start counting towards a Feature, so Feature sizes
  and forecasts can change once. A release note is required. No flag and no per-option branch.
- **Preserve until edited (rejected).** A per-owner legacy-mode flag would have to match a conditional Azure DevOps read
  path exactly and adds a stored flag, clear-on-save rules, selector and connector branches and a cleanup migration, to
  spare a one-time move of items whose configured sources already named nothing usable.

## Alternatives considered

- **Child table** (owner type, owner id, position, kind, field id). Rejected: polymorphic owner key over two tables,
  an `Include` on every read that, forgotten, reads as "follows nothing" (the ADR-241 warning), and a re-key in Epic 6474.
- **Five flat columns.** Rejected: the cap becomes schema and reordering rewrites five columns.
- **`jsonb` columns.** Rejected: key order and spacing are normalised, so the migrated text could not match the
  serialiser byte for byte.
- **Sentinel field id for the own parent.** Rejected: a magic number reaches every consumer of field ids.
- **A `LinkType` entry kind.** Rejected: ADR-193 §5, a stored kind lies after a link type is renamed in Jira.
- **Each connector walks the list.** Rejected: the rule written per tracker (the ADR-193 `DependencySourceSelector`
  lesson) and fall-through needs stored Features a connector may not reach.
- **Persist the readings on the work item.** Rejected: a column, a migration and staleness rules for data only the same
  refresh uses.
- **A domain event for a list change.** Rejected: the fingerprint already drives the refetch (ADR-242), an event would be
  a second path to the same refetch. A deliberate exception to preferring events.
- **Freeze the legacy columns.** Rejected: every owner edited after the upgrade would resolve differently on rollback.
- **Dropping the legacy columns now.** Rejected: expand-only.
- **The old field counts only when no list is sent.** Rejected during delivery: a client echoing the whole GET would get
  a 200 and silently lose an edit of the old field.

## Consequences

Positive: one ordering rule, tested as a table; Epic 6474 carries three plain nullable text columns; no new event, no
new edge on the context map; the class of "own parent silently wins" disappears because it is an entry.

Negative, stated honestly:

- The "field in use" guard loads one connection's owners and scans their lists in memory, because the legacy column holds
  only a list's first field and cannot serve as a filter.
- The data step builds JSON text in SQL on two providers, probed by a round-trip test on both.
- A stored row whose text the rules refuse throws when its owner is loaded, so a hand-edited or damaged row stops that
  one owner and every request that loads it, rather than being repaired. The recovery is to correct or clear the column
  of that row in the database, after which the owner follows its default or reads the tracker's own parent.
- The new build asks Azure DevOps for the relations of every owner whose list holds the own parent, override owners
  included, so those owners make more requests than before (watch for rate-limit back-off).
- Two fingerprint renderings coexist for one release.
- An install with a link entry reads the stored Features once per fetch where it read them only for ambiguous links before.
- A link entry is judged when the record is read, and a cheap refresh skips a record that did not move. So a Jira Team
  item whose link names a Feature stored later stays where the next entry put it until the item changes or a full refresh
  runs, and nothing schedules a full refresh on Jira. Azure DevOps has no link entry, so this cannot arise there. Accepted
  by the maintainer and documented on the Team settings page.
- Findings ride on records, so a refresh whose query returns nothing reports no parent findings.
- `ParentSourceMatchedNothing` says "link type" on Azure DevOps, which calls it a relation type.

What the upgrade does to already-configured owners is described above: some existing items move once.

## Enforcement

Rows 1 to 5 are rules in `DependencySingleDecisionArchUnitTest`. They read the compiled IL, because a Release build drops
async edges from what ArchUnitNET sees. Rows 6 to 9 are tests.

| # | Rule | Mechanism |
|---|---|---|
| 1 | The selector and its value types import no connector | `TheParentSourceSelector_ImportsNoConnector` |
| 2 | Every connector that reads the list (Jira and Azure DevOps) names the selector | `EveryTrackerThatReadsParents_AsksTheSelector` |
| 3 | Nothing but the one helper assigns `ParentReferenceId`, except a short allow-list with a reason each (Linear, CSV, a copy in `PortfolioMetricsService`, `WorkItemBase`'s own merge and copy); the shown link values are covered by behaviour tests | `NoOneButTheOneHelperAssignsAParent` |
| 4 | Connectors never use stored Features or the feature repository | `ConnectorsNeverUseStoredFeatures` |
| 5 | Nothing but the owner types reads the old members | `NobodyReadsTheLegacyParentColumns` |
| 6 | Fingerprint stable for legacy shapes, new forms never collide with them | `FetchFingerprintParentSourcesTest` (golden values); `FetchShapingPropertyGuardTest` records the registered property |
| 7 | The migration keeps every parent and its text round-trips | `ParentSourcesUpgradeMigrationTests` and `ParentSourcesColumnConversionTests` on SQLite and Postgres, including `Down` |
| 8 | Write-through and rollback limit | `ParentSourcesOwnerWriteThroughTest` and `ParentSourcesBridgeTest`: golden table list to legacy value, with literal expectations of what the list and the old column read for each shape (no test against the previous build) |
| 9 | List rules | Table tests in `ParentSourcesTest`; `parentSources.test.ts` mirrors them for the frontend reducer (mutation-tested) |
